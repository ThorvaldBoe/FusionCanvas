using System.Text.Json;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Stores.Printify;

public sealed class PrintifyCatalogImportService(
    IStoreManagementService stores,
    IStorePrintifyCredentialStore credentials,
    IPrintifyCatalogClient client,
    IWorkspaceRepository? repository = null) : IPrintifyCatalogImportService
{
    private static readonly PrintifyCatalogResult InvalidContext =
        new(PrintifyCatalogResultKind.InvalidRequest, "Save an active Printify Store with a selected Printify shop first.");

    public Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
        ExecuteAsync(scope, (key, shopId, token) => client.LoadShopProductsAsync(key, shopId, token), cancellationToken);

    public Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default) =>
        ExecuteAsync(scope, (key, shopId, token) => client.LoadSelectedProductsAsync(key, shopId, productIds, token), cancellationToken);

    public Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) =>
        LoadSelectedAsync(scope, blueprintIds.Select(value => value.ToString()).ToArray(), cancellationToken);

    private async Task<PrintifyCatalogResult> ExecuteAsync(
        StoreCredentialScope scope,
        Func<string, int, CancellationToken, Task<PrintifyCatalogResult>> operation,
        CancellationToken cancellationToken)
    {
        if (scope.WorkspaceId == Guid.Empty || scope.StoreId == Guid.Empty) return InvalidContext;
        var state = await stores.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (state.ActiveWorkspaceId != scope.WorkspaceId || stores.ActiveWorkspaceId != scope.WorkspaceId)
            return InvalidContext;
        var store = state.ActiveStores.SingleOrDefault(candidate => candidate.Id == scope.StoreId && candidate.WorkspaceId == scope.WorkspaceId);
        if (store is null || store.IsArchived || !FulfillmentStrategyPolicy.RequiresPrintifyKey(store.FulfillmentStrategy) || store.Context.PrintifyShopId is null)
            return InvalidContext;
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(read.Secret))
            return new(PrintifyCatalogResultKind.InvalidKey, "Add and verify a Printify key before loading the catalog.");
        var result = await operation(read.Secret, store.Context.PrintifyShopId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || repository is null || result.SelectedProducts is null)
            return result;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // The request may outlive a Store/workspace switch. Never publish
            // a response into the context that was active only when loading began.
            var current = await stores.LoadAsync(cancellationToken).ConfigureAwait(false);
            var currentStore = current.ActiveStores.SingleOrDefault(candidate => candidate.Id == scope.StoreId && candidate.WorkspaceId == scope.WorkspaceId);
            if (current.ActiveWorkspaceId != scope.WorkspaceId || stores.ActiveWorkspaceId != scope.WorkspaceId
                || currentStore is null || currentStore.IsArchived
                || !FulfillmentStrategyPolicy.RequiresPrintifyKey(currentStore.FulfillmentStrategy)
                || currentStore.Context.PrintifyShopId != store.Context.PrintifyShopId)
                return InvalidContext;
            var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var updated = ImportSelected(snapshot, scope.StoreId, result.SelectedProducts);
            // The Products editor still reads the legacy projection while the
            // catalog editor reads the normalized records written above. Keep
            // both views aligned so imported variants and design areas are
            // visible from either route.
            var synchronized = CatalogCompatibilitySynchronizer
                .SynchronizeStore(updated, scope.StoreId, () => DateTimeOffset.UtcNow, Guid.NewGuid)
                .Snapshot;
            await repository.SaveAsync(synchronized, cancellationToken).ConfigureAwait(false);
            return result with { Message = "Selected Printify catalog imported." };
        }
        catch (InvalidOperationException exception)
        {
            return new(PrintifyCatalogResultKind.UnexpectedResponse, $"Printify catalog data could not be imported safely: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new(PrintifyCatalogResultKind.UnexpectedResponse, "Printify catalog data could not be saved safely. No imported records were committed.");
        }
    }

    private static WorkspaceSnapshot ImportSelected(
        WorkspaceSnapshot snapshot,
        Guid storeId,
        IReadOnlyList<PrintifyCatalogBlueprint> catalog)
    {
        ValidateCatalog(catalog);
        var now = DateTimeOffset.UtcNow;
        var blueprints = snapshot.Blueprints.ToList();
        var providers = snapshot.PrintProviders.ToList();
        var offerings = snapshot.BlueprintOfferings.ToList();
        var options = snapshot.OfferingOptions.ToList();
        var values = snapshot.OfferingOptionValues.ToList();
        var variants = snapshot.OfferingVariants.ToList();
        var placeholders = snapshot.OfferingPlaceholders.ToList();
        var placeholderReplacements = new Dictionary<Guid, Guid>();

        foreach (var importedBlueprint in catalog)
        {
            var productId = importedBlueprint.ProductId;
            var blueprint = productId is not null
                ? blueprints.FirstOrDefault(value => value.StoreId == storeId && MetadataHasProductId(value.MetadataJson, productId))
                    ?? blueprints.FirstOrDefault(value => value.StoreId == storeId
                        && MetadataKindIs(value.MetadataJson, "blueprint")
                        && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id))
                : blueprints.FirstOrDefault(value => value.StoreId == storeId
                    && MetadataKindIs(value.MetadataJson, "blueprint")
                    && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id));
            if (blueprint is null)
            {
                blueprint = new Blueprint(Guid.NewGuid(), storeId, BlueprintName(importedBlueprint.Summary), importedBlueprint.Summary.Description, false, now, now,
                    productId is not null ? Metadata("product", importedBlueprint.Summary.Id, productId) : Metadata("blueprint", importedBlueprint.Summary.Id));
                blueprints.Add(blueprint);
            }
            else
            {
                blueprint = blueprint with { Name = BlueprintName(importedBlueprint.Summary), Description = importedBlueprint.Summary.Description, IsArchived = false, UpdatedAt = now,
                    MetadataJson = productId is not null ? Metadata("product", importedBlueprint.Summary.Id, productId) : blueprint.MetadataJson };
                Replace(blueprints, value => value.Id == blueprint.Id, blueprint);
            }

            foreach (var importedProvider in importedBlueprint.Providers)
            {
                var provider = providers.FirstOrDefault(value => value.StoreId == storeId
                    && (string.Equals(value.ExternalProviderId, importedProvider.Id.ToString(), StringComparison.Ordinal)
                        || value.ExternalProviderId is null && string.Equals(value.Name, importedProvider.Title, StringComparison.OrdinalIgnoreCase)));
                if (provider is null)
                {
                    provider = new PrintProvider(Guid.NewGuid(), storeId, importedProvider.Title, importedProvider.Id.ToString(), false, now, now, Metadata("provider", importedProvider.Id));
                    providers.Add(provider);
                }
                else
                {
                    provider = provider with
                    {
                        Name = importedProvider.Title,
                        ExternalProviderId = provider.ExternalProviderId ?? importedProvider.Id.ToString(),
                        IsArchived = false,
                        UpdatedAt = now
                    };
                    Replace(providers, value => value.Id == provider.Id, provider);
                }

                var externalProductId = importedBlueprint.ProductId ?? importedBlueprint.Summary.Id.ToString();
                var externalOfferingId = $"{externalProductId}:{importedProvider.Id}";
                var offering = offerings.SingleOrDefault(value => value.StoreId == storeId && value.ExternalOfferingId == externalOfferingId)
                    // Before shop-product imports, the same stable relationship was stored
                    // as <blueprint id>:<provider id>. Preserve that offering identity so
                    // existing mockup templates and local configuration remain attached.
                    ?? offerings.SingleOrDefault(value => value.StoreId == storeId
                        && value.BlueprintId == blueprint.Id
                        && value.PrintProviderId == provider.Id
                        && value.ExternalOfferingId == $"{importedBlueprint.Summary.Id}:{importedProvider.Id}");
                if (offering is null)
                {
                    offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, $"{importedBlueprint.Summary.Title} · {importedProvider.Title}", importedBlueprint.Summary.Description, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, externalOfferingId, false, now, now, Metadata("offering", importedBlueprint.Summary.Id, importedProvider.Id));
                    offerings.Add(offering);
                }
                else
                {
                    offering = offering with { BlueprintId = blueprint.Id, Name = $"{importedBlueprint.Summary.Title} · {importedProvider.Title}", Description = importedBlueprint.Summary.Description, PrintProviderId = provider.Id, ExternalOfferingId = externalOfferingId, IsArchived = false, UpdatedAt = now };
                    Replace(offerings, value => value.Id == offering.Id, offering);
                }

                var optionIds = importedProvider.Variants.SelectMany(value => value.OptionValueIds).Distinct().ToArray();
                var optionValuesBySourceId = EnsureOptions(options, values, offering.Id, importedProvider.Options, optionIds, now);
                var localVariantIds = new Dictionary<int, Guid>();
                foreach (var importedVariant in importedProvider.Variants)
                {
                    var optionValueIds = importedVariant.OptionValueIds.Where(optionValuesBySourceId.ContainsKey).Select(id => optionValuesBySourceId[id]).ToArray();
                    var variant = variants.SingleOrDefault(value => value.OfferingId == offering.Id && MetadataHasId(value.MetadataJson, importedVariant.Id));
                    var variantId = variant?.Id ?? Guid.NewGuid();
                    var replacement = new OfferingVariant(variantId, offering.Id, importedVariant.Title, optionValueIds, !importedVariant.IsEnabled || !importedVariant.IsAvailable, variant?.CreatedAt ?? now, now, Metadata("variant", importedVariant.Id));
                    if (variant is null) variants.Add(replacement); else Replace(variants, value => value.Id == variant.Id, replacement);
                    localVariantIds[importedVariant.Id] = variantId;
                }

                var incomingGroups = importedProvider.Variants
                    .SelectMany(variant => variant.Placeholders.Select(placeholder => (variant.Id, Placeholder: placeholder)))
                    .GroupBy(value => (value.Placeholder.Position, value.Placeholder.DecorationMethod))
                    .ToArray();
                var importedLocalIds = localVariantIds.Values.ToHashSet();
                foreach (var existing in placeholders.Where(value => value.OfferingId == offering.Id
                    && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id)
                    && MetadataHasId(value.MetadataJson, importedProvider.Id)).ToArray())
                {
                    var retained = existing.VariantIds.Where(value => !importedLocalIds.Contains(value)).ToArray();
                    if (!retained.SequenceEqual(existing.VariantIds))
                        Replace(placeholders, value => value.Id == existing.Id, new OfferingPlaceholder(existing.Id, existing.OfferingId, existing.Name, existing.Description, existing.Position, existing.DecorationMethod, existing.Width, existing.Height, retained, existing.IsArchived, existing.CreatedAt, existing.UpdatedAt, existing.MetadataJson, existing.ProviderReference, existing.ArtworkGuidance));
                }
                foreach (var group in incomingGroups)
                {
                    var sample = group.First().Placeholder;
                    var owned = placeholders.Where(value => value.OfferingId == offering.Id
                        && value.ProviderReference == sample.Position
                        && value.DecorationMethod == sample.DecorationMethod
                        && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id)
                        && MetadataHasId(value.MetadataJson, importedProvider.Id)).ToArray();
                    var placeholder = owned.Where(value => !value.IsArchived)
                        .OrderByDescending(value => value.Width)
                        .ThenByDescending(value => value.Height)
                        .ThenBy(value => value.CreatedAt)
                        .ThenBy(value => value.Id)
                        .FirstOrDefault()
                        ?? owned.OrderByDescending(value => value.Width)
                            .ThenByDescending(value => value.Height)
                            .ThenBy(value => value.CreatedAt)
                            .ThenBy(value => value.Id)
                            .FirstOrDefault();
                    var presentVariantIds = group.Select(value => localVariantIds[value.Id]).Distinct().ToArray();
                    var variantIds = (placeholder?.VariantIds ?? []).Concat(presentVariantIds).Distinct().ToArray();
                    var replacement = new OfferingPlaceholder(placeholder?.Id ?? Guid.NewGuid(), offering.Id, sample.Position, null, sample.Position, sample.DecorationMethod,
                        group.Max(value => value.Placeholder.Width), group.Max(value => value.Placeholder.Height), variantIds, false,
                        placeholder?.CreatedAt ?? now, now, Metadata("placeholder", importedBlueprint.Summary.Id, importedProvider.Id), sample.Position);
                    if (placeholder is null) placeholders.Add(replacement); else Replace(placeholders, value => value.Id == placeholder.Id, replacement);

                    foreach (var duplicate in owned.Where(value => value.Id != replacement.Id))
                    {
                        placeholderReplacements[duplicate.Id] = replacement.Id;
                        if (!duplicate.IsArchived)
                            Replace(placeholders, value => value.Id == duplicate.Id, duplicate with { IsArchived = true, UpdatedAt = now });
                    }
                }
            }
        }

        var imported = MigratePlaceholderReferences(snapshot with
        {
            Blueprints = blueprints,
            PrintProviders = providers,
            BlueprintOfferings = offerings,
            OfferingOptions = options,
            OfferingOptionValues = values,
            OfferingVariants = variants,
            OfferingPlaceholders = placeholders
        }, placeholderReplacements);
        return CatalogCompatibilitySynchronizer
            .SynchronizeStore(imported, storeId, () => now, Guid.NewGuid)
            .Snapshot;
    }

    private static WorkspaceSnapshot MigratePlaceholderReferences(
        WorkspaceSnapshot snapshot,
        IReadOnlyDictionary<Guid, Guid> replacements)
    {
        if (replacements.Count == 0)
            return snapshot;

        Guid Map(Guid id) => replacements.TryGetValue(id, out var replacement) ? replacement : id;
        Guid? MapNullable(Guid? id) => id is Guid value ? Map(value) : null;

        var offerings = snapshot.BlueprintOfferings
            .Select(value => value with
            {
                DefaultPlaceholderId = MapNullable(value.DefaultPlaceholderId),
                PrimaryArtworkDesignAreaId = MapNullable(value.PrimaryArtworkDesignAreaId)
            })
            .ToArray();
        var templates = snapshot.MockupTemplates
            .Select(value => value with { TargetPlaceholderId = MapNullable(value.TargetPlaceholderId) })
            .ToArray();
        var revisions = snapshot.MockupTemplateRevisions
            .Select(value => value with { TargetPlaceholderId = MapNullable(value.TargetPlaceholderId) })
            .ToArray();
        var assignments = snapshot.DesignSlotAssignments
            .GroupBy(value => (value.RowId, DesignAreaId: Map(value.DesignAreaId)))
            .Select(group => group
                .OrderBy(value => replacements.ContainsKey(value.DesignAreaId))
                .ThenByDescending(value => value.AssetId is not null)
                .First() with { DesignAreaId = group.Key.DesignAreaId })
            .ToArray();

        return snapshot with
        {
            BlueprintOfferings = offerings,
            MockupTemplates = templates,
            MockupTemplateRevisions = revisions,
            DesignSlotAssignments = assignments,
            DesignAreas = snapshot.DesignAreas.Where(value => !replacements.ContainsKey(value.Id)).ToArray()
        };
    }

    private static string BlueprintName(PrintifyCatalogBlueprintSummary summary)
    {
        var brand = summary.Brand?.Trim();
        var model = summary.Model?.Trim();
        return !string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(model)
            ? $"{brand} {model}"
            : summary.Title.Trim();
    }

    private static void ValidateCatalog(IReadOnlyList<PrintifyCatalogBlueprint> catalog)
    {
        if (catalog.Where(value => value.ProductId is not null).GroupBy(value => value.ProductId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidOperationException("The Printify response contains duplicate product identities.");
        if (catalog.Where(value => value.ProductId is null).GroupBy(value => value.Summary.Id).Any(group => group.Count() > 1))
            throw new InvalidOperationException("The Printify response contains duplicate Blueprint identities.");

        foreach (var blueprint in catalog)
        {
            if (blueprint.Summary.Id <= 0 || string.IsNullOrWhiteSpace(blueprint.Summary.Title))
                throw new InvalidOperationException("The Printify response contains an invalid Blueprint identity.");
            if (blueprint.Providers.GroupBy(value => value.Id).Any(group => group.Count() > 1))
                throw new InvalidOperationException("The Printify response contains duplicate provider identities.");

            foreach (var provider in blueprint.Providers)
            {
                if (provider.Id <= 0 || string.IsNullOrWhiteSpace(provider.Title))
                    throw new InvalidOperationException("The Printify response contains an invalid provider identity.");
                if (provider.Variants.GroupBy(value => value.Id).Any(group => group.Count() > 1))
                    throw new InvalidOperationException("The Printify response contains duplicate variant identities.");
                var optionValueIds = new HashSet<int>();
                foreach (var option in provider.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Name) || string.IsNullOrWhiteSpace(option.Type))
                        throw new InvalidOperationException("The Printify response contains an invalid option identity.");
                    if (option.Values.Any(value => value.Id <= 0 || string.IsNullOrWhiteSpace(value.Title))
                        || option.Values.GroupBy(value => value.Id).Any(group => group.Count() > 1))
                        throw new InvalidOperationException("The Printify response contains duplicate or invalid option values.");
                    foreach (var value in option.Values)
                        if (!optionValueIds.Add(value.Id))
                            throw new InvalidOperationException("The Printify response contains duplicate option value identities.");
                }
                var knownOptionValueIds = optionValueIds;
                foreach (var variant in provider.Variants)
                {
                    if (variant.Id <= 0 || string.IsNullOrWhiteSpace(variant.Title) || variant.OptionValueIds.Any(value => value <= 0 || !knownOptionValueIds.Contains(value)))
                        throw new InvalidOperationException("The Printify response contains an invalid variant relationship.");
                    if (variant.Placeholders.Any(value => string.IsNullOrWhiteSpace(value.Position) || value.Width <= 0 || value.Height <= 0))
                        throw new InvalidOperationException("The Printify response contains an invalid print area.");
                    if (variant.Placeholders.GroupBy(value => value.Position, StringComparer.Ordinal).Any(group => group.Count() > 1))
                        throw new InvalidOperationException("The Printify response contains duplicate print area identities.");
                }
            }
        }
    }

    private static Dictionary<int, Guid> EnsureOptions(
        List<OfferingOption> options,
        List<OfferingOptionValue> values,
        Guid offeringId,
        IReadOnlyList<PrintifyCatalogOption> importedOptions,
        IReadOnlyList<int> variantValueIds,
        DateTimeOffset now)
    {
        var result = new Dictionary<int, Guid>();
        foreach (var importedOption in importedOptions)
        {
            var kind = ParseOptionKind(importedOption);
            var declaredValues = importedOption.Values.Where(value => value.Id > 0).ToArray();
            if (declaredValues.Length == 0) continue;
            var option = options.FirstOrDefault(value => value.OfferingId == offeringId && value.OptionKind == kind && !value.IsArchived)
                ?? options.FirstOrDefault(value => value.OfferingId == offeringId && value.OptionKind == kind);
            if (option is null)
            {
                option = new OfferingOption(Guid.NewGuid(), offeringId, kind, importedOption.Name.Trim(), options.Count(value => value.OfferingId == offeringId), metadataJson: Metadata("option", declaredValues.Select(value => value.Id).ToArray()));
                options.Add(option);
            }
            else
            {
                var updatedOption = option with { Name = importedOption.Name.Trim(), IsArchived = false };
                Replace(options, value => value.Id == option.Id, updatedOption);
                option = updatedOption;
            }

            foreach (var declaredValue in declaredValues)
            {
                var optionValue = values.FirstOrDefault(value => value.OptionId == option.Id && value.OfferingId == offeringId
                    && !value.IsArchived && MetadataHasId(value.MetadataJson, declaredValue.Id))
                    ?? values.FirstOrDefault(value => value.OptionId == option.Id && value.OfferingId == offeringId
                        && MetadataHasId(value.MetadataJson, declaredValue.Id));
                if (optionValue is null)
                {
                    optionValue = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, declaredValue.Title.Trim(), values.Count(value => value.OptionId == option.Id), metadataJson: Metadata("option-value", declaredValue.Id));
                    values.Add(optionValue);
                }
                else
                {
                    var updatedValue = optionValue with { Value = declaredValue.Title.Trim(), IsArchived = false };
                    Replace(values, value => value.Id == optionValue.Id, updatedValue);
                    optionValue = updatedValue;
                }
                result[declaredValue.Id] = optionValue.Id;
            }
        }

        return result;
    }

    private static OptionKind ParseOptionKind(PrintifyCatalogOption option) =>
        (option.Type.Trim().ToLowerInvariant(), option.Name.Trim().ToLowerInvariant()) switch
        {
            ("color", _) or (_, "color") => OptionKind.Color,
            ("size", _) or (_, "size") => OptionKind.Size,
            _ => OptionKind.Other
        };

    private static string Metadata(string kind, params int[] ids) => JsonSerializer.Serialize(new { source = "printify", kind, ids });
    private static string Metadata(string kind, int blueprintId, string productId) => JsonSerializer.Serialize(new { source = "printify", kind, blueprintId, productId });
    private static bool MetadataKindIs(string json, string kind)
    {
        using var document = ParseMetadata(json);
        return document is not null && document.RootElement.TryGetProperty("source", out var source)
            && source.ValueKind == JsonValueKind.String && source.GetString() == "printify"
            && document.RootElement.TryGetProperty("kind", out var value)
            && value.ValueKind == JsonValueKind.String && value.GetString() == kind;
    }
    private static bool MetadataHasProductId(string json, string productId)
    {
        using var document = ParseMetadata(json);
        return document is not null && document.RootElement.TryGetProperty("source", out var source)
            && source.ValueKind == JsonValueKind.String && source.GetString() == "printify"
            && document.RootElement.TryGetProperty("productId", out var value)
            && value.ValueKind == JsonValueKind.String && value.GetString() == productId;
    }
    private static bool MetadataHasId(string json, int id)
    {
        using var document = ParseMetadata(json);
        if (document is null || !document.RootElement.TryGetProperty("source", out var source)
            || source.ValueKind != JsonValueKind.String || source.GetString() != "printify"
            || !document.RootElement.TryGetProperty("ids", out var values) || values.ValueKind != JsonValueKind.Array) return false;
        return values.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var actual) && actual == id);
    }
    private static JsonDocument? ParseMetadata(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) { document.Dispose(); return null; }
            return document;
        }
        catch (JsonException) { return null; }
    }
    private static void Replace<T>(List<T> values, Func<T, bool> match, T replacement)
    {
        var index = values.FindIndex(value => match(value));
        if (index >= 0) values[index] = replacement;
    }
}
