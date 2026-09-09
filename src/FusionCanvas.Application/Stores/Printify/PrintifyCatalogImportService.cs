using System.Text.Json;
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
        new(PrintifyCatalogResultKind.InvalidRequest, "Save an active Shopify + Printify Store with a selected Printify shop first.");

    public Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
        ExecuteAsync(scope, (key, token) => client.LoadBlueprintsAsync(key, token), cancellationToken);

    public Task<PrintifyCatalogResult> LoadSelectedAsync(StoreCredentialScope scope, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) =>
        ExecuteAsync(scope, (key, token) => client.LoadSelectedAsync(key, blueprintIds, token), cancellationToken);

    private async Task<PrintifyCatalogResult> ExecuteAsync(
        StoreCredentialScope scope,
        Func<string, CancellationToken, Task<PrintifyCatalogResult>> operation,
        CancellationToken cancellationToken)
    {
        if (scope.WorkspaceId == Guid.Empty || scope.StoreId == Guid.Empty) return InvalidContext;
        var state = await stores.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (state.ActiveWorkspaceId != scope.WorkspaceId || stores.ActiveWorkspaceId != scope.WorkspaceId)
            return InvalidContext;
        var store = state.ActiveStores.SingleOrDefault(candidate => candidate.Id == scope.StoreId && candidate.WorkspaceId == scope.WorkspaceId);
        if (store is null || store.IsArchived || store.FulfillmentStrategy != FulfillmentStrategy.ShopifyPrintify || store.Context.PrintifyShopId is null)
            return InvalidContext;
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(read.Secret))
            return new(PrintifyCatalogResultKind.InvalidKey, "Add and verify a Printify key before loading the catalog.");
        var result = await operation(read.Secret, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || repository is null || result.SelectedCatalog is null)
            return result;

        try
        {
            var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var updated = ImportSelected(snapshot, scope.StoreId, result.SelectedCatalog);
            await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return result with { Message = "Selected Printify catalog imported." };
        }
        catch (InvalidOperationException)
        {
            return new(PrintifyCatalogResultKind.UnexpectedResponse, "Printify catalog data could not be imported safely.");
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

        foreach (var importedBlueprint in catalog)
        {
            var blueprint = blueprints.FirstOrDefault(value => value.StoreId == storeId && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id));
            if (blueprint is null)
            {
                blueprint = new Blueprint(Guid.NewGuid(), storeId, importedBlueprint.Summary.Title, importedBlueprint.Summary.Description, false, now, now, Metadata("blueprint", importedBlueprint.Summary.Id));
                blueprints.Add(blueprint);
            }
            else
            {
                blueprint = blueprint with { Name = importedBlueprint.Summary.Title, Description = importedBlueprint.Summary.Description, IsArchived = false, UpdatedAt = now };
                Replace(blueprints, value => value.Id == blueprint.Id, blueprint);
            }

            foreach (var importedProvider in importedBlueprint.Providers)
            {
                var provider = providers.SingleOrDefault(value => value.StoreId == storeId && value.ExternalProviderId == importedProvider.Id.ToString());
                if (provider is null)
                {
                    provider = new PrintProvider(Guid.NewGuid(), storeId, importedProvider.Title, importedProvider.Id.ToString(), false, now, now, Metadata("provider", importedProvider.Id));
                    providers.Add(provider);
                }
                else
                {
                    provider = provider with { Name = importedProvider.Title, IsArchived = false, UpdatedAt = now };
                    Replace(providers, value => value.Id == provider.Id, provider);
                }

                var externalOfferingId = $"{importedBlueprint.Summary.Id}:{importedProvider.Id}";
                var offering = offerings.SingleOrDefault(value => value.StoreId == storeId && value.ExternalOfferingId == externalOfferingId);
                if (offering is null)
                {
                    offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, $"{importedBlueprint.Summary.Title} · {importedProvider.Title}", importedBlueprint.Summary.Description, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, externalOfferingId, false, now, now, Metadata("offering", importedBlueprint.Summary.Id, importedProvider.Id));
                    offerings.Add(offering);
                }
                else
                {
                    offering = offering with { BlueprintId = blueprint.Id, Name = $"{importedBlueprint.Summary.Title} · {importedProvider.Title}", Description = importedBlueprint.Summary.Description, PrintProviderId = provider.Id, IsArchived = false, UpdatedAt = now };
                    Replace(offerings, value => value.Id == offering.Id, offering);
                }

                var optionIds = importedProvider.Variants.SelectMany(value => value.OptionValueIds).Distinct().ToArray();
                var optionValuesBySourceId = EnsureOptions(options, values, offering.Id, optionIds, now);
                foreach (var importedVariant in importedProvider.Variants)
                {
                    var optionValueIds = importedVariant.OptionValueIds.Where(optionValuesBySourceId.ContainsKey).Select(id => optionValuesBySourceId[id]).ToArray();
                    if (optionValueIds.Length == 0) continue;
                    var variant = variants.SingleOrDefault(value => value.OfferingId == offering.Id && MetadataHasId(value.MetadataJson, importedVariant.Id));
                    var variantId = variant?.Id ?? Guid.NewGuid();
                    var replacement = new OfferingVariant(variantId, offering.Id, importedVariant.Title, optionValueIds, !importedVariant.IsEnabled || !importedVariant.IsAvailable, variant?.CreatedAt ?? now, now, Metadata("variant", importedVariant.Id));
                    if (variant is null) variants.Add(replacement); else Replace(variants, value => value.Id == variant.Id, replacement);
                    foreach (var importedPlaceholder in importedVariant.Placeholders)
                    {
                        var placeholder = placeholders.SingleOrDefault(value => value.OfferingId == offering.Id
                            && value.ProviderReference == importedPlaceholder.Position
                            && MetadataHasId(value.MetadataJson, importedBlueprint.Summary.Id)
                            && MetadataHasId(value.MetadataJson, importedProvider.Id));
                        var variantIds = placeholder is null ? [variantId] : placeholder.VariantIds.Concat([variantId]).Distinct().ToArray();
                        var placeholderReplacement = new OfferingPlaceholder(placeholder?.Id ?? Guid.NewGuid(), offering.Id, importedPlaceholder.Position, null, importedPlaceholder.Position, importedPlaceholder.DecorationMethod, importedPlaceholder.Width, importedPlaceholder.Height, variantIds, false, placeholder?.CreatedAt ?? now, now, Metadata("placeholder", importedBlueprint.Summary.Id, importedProvider.Id), importedPlaceholder.Position);
                        if (placeholder is null) placeholders.Add(placeholderReplacement); else Replace(placeholders, value => value.Id == placeholder.Id, placeholderReplacement);
                    }
                }
            }
        }

        return snapshot with { Blueprints = blueprints, PrintProviders = providers, BlueprintOfferings = offerings, OfferingOptions = options, OfferingOptionValues = values, OfferingVariants = variants, OfferingPlaceholders = placeholders };
    }

    private static void ValidateCatalog(IReadOnlyList<PrintifyCatalogBlueprint> catalog)
    {
        if (catalog.GroupBy(value => value.Summary.Id).Any(group => group.Count() > 1))
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
                foreach (var variant in provider.Variants)
                {
                    if (variant.Id <= 0 || string.IsNullOrWhiteSpace(variant.Title) || variant.OptionValueIds.Any(value => value <= 0))
                        throw new InvalidOperationException("The Printify response contains an invalid variant relationship.");
                    if (variant.Placeholders.Any(value => string.IsNullOrWhiteSpace(value.Position) || value.Width <= 0 || value.Height <= 0))
                        throw new InvalidOperationException("The Printify response contains an invalid print area.");
                }
            }
        }
    }

    private static Dictionary<int, Guid> EnsureOptions(List<OfferingOption> options, List<OfferingOptionValue> values, Guid offeringId, IReadOnlyList<int> sourceIds, DateTimeOffset now)
    {
        var result = new Dictionary<int, Guid>();
        var kinds = new[] { OptionKind.Color, OptionKind.Size, OptionKind.Other };
        for (var index = 0; index < sourceIds.Count && index < kinds.Length; index++)
        {
            var sourceId = sourceIds[index];
            var option = options.SingleOrDefault(value => value.OfferingId == offeringId
                && value.OptionKind == kinds[index]
                && MetadataHasId(value.MetadataJson, sourceId));
            if (option is null)
            {
                option = new OfferingOption(Guid.NewGuid(), offeringId, kinds[index], $"Printify option {sourceId}", index, metadataJson: Metadata("option", sourceId));
                options.Add(option);
            }
            var optionValue = values.SingleOrDefault(value => value.OptionId == option.Id
                && value.OfferingId == offeringId
                && MetadataHasId(value.MetadataJson, sourceId));
            if (optionValue is null)
            {
                optionValue = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, $"Printify value {sourceId}", 0, metadataJson: Metadata("option-value", sourceId));
                values.Add(optionValue);
            }
            result[sourceId] = optionValue.Id;
        }
        return result;
    }

    private static string Metadata(string kind, params int[] ids) => JsonSerializer.Serialize(new { source = "printify", kind, ids });
    private static bool MetadataHasId(string json, int id) => json.Contains($"\"ids\":[{id}", StringComparison.Ordinal)
        || json.Contains($",{id}]", StringComparison.Ordinal)
        || json.Contains($",{id},", StringComparison.Ordinal);
    private static void Replace<T>(List<T> values, Func<T, bool> match, T replacement)
    {
        var index = values.FindIndex(value => match(value));
        if (index >= 0) values[index] = replacement;
    }
}
