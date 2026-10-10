using System.Globalization;
using System.Text;
using System.Text.Json;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.Stores.Printify;

/// <summary>Application orchestration for Printify's read-only listing bootstrap flow.</summary>
public sealed class PrintifyListingImportService(
    IStoreContextReader stores,
    IStorePrintifyCredentialStore credentials,
    IPrintifyListingImportClient client,
    IWorkspaceRepository repository,
    IWorkspaceFileOutputStore files,
    IPrintifyCatalogClient? catalogClient = null,
    Func<DateTimeOffset>? clock = null,
    Func<Guid>? newId = null)
{
    private const string ProviderKey = "printify";
    private const int MaximumDuplicateTextLength = 2048;
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly Func<Guid> _newId = newId ?? Guid.NewGuid;

    public sealed record VariantSetupResult(bool Succeeded, bool IsPartial, string Message);

    public async Task<VariantSetupResult> DownloadVariantSetupAsync(StoreCredentialScope scope, Guid itemId, CancellationToken cancellationToken = default)
    {
        if (catalogClient is null) return new(false, false, "Printify catalog setup is unavailable.");
        try
        {
            var store = await stores.ResolveActiveStoreAsync(scope.WorkspaceId, scope.StoreId, cancellationToken).ConfigureAwait(false);
            if (store is null || store.IsArchived || store.Context.PrintifyShopId is not int shopId) return new(false, false, "Select an active Store with a Printify shop.");
            var credential = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
            if (credential.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(credential.Secret)) return new(false, false, "Add and verify a Printify key in Store setup.");
            var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId && value.StoreId == scope.StoreId && !value.IsArchived);
            var mapping = snapshot.ExternalListingMappings.SingleOrDefault(value => value.ItemId == itemId && value.StoreId == scope.StoreId && value.ProviderKey == ProviderKey);
            if (item is null || mapping?.ProductId is null) return new(false, false, "This Item has no linked Printify product.");
            if (item.Stage != WorkflowStage.Listing) return new(false, false, "Variant setup can only be downloaded from the Listing stage.");
            var existingConfiguration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId);

            var catalog = await catalogClient.LoadSelectedProductsAsync(credential.Secret, shopId, [mapping.ProductId], cancellationToken).ConfigureAwait(false);
            if (!catalog.Succeeded || catalog.SelectedProducts is null || catalog.SelectedProducts.Count != 1) return new(false, false, catalog.Message);
            var detail = await client.GetProductAsync(credential.Secret, shopId, mapping.ProductId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The linked Printify product is no longer available.");
            var incoming = catalog.SelectedProducts[0];
            var incomingProvider = incoming.Providers.Single();
            var externalOfferingId = $"{mapping.ProductId}:{incomingProvider.Id}";
            var conflictingOffering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.StoreId == scope.StoreId && value.ExternalOfferingId == externalOfferingId);
            if (conflictingOffering is not null && (!IsPrintifyOwned(conflictingOffering.MetadataJson)
                || !string.Equals(conflictingOffering.Name, $"{incoming.Summary.Title} · {incomingProvider.Title}", StringComparison.Ordinal)
                || !string.Equals(conflictingOffering.Description, incoming.Summary.Description, StringComparison.Ordinal)))
                return new(false, false, "The matching catalog Offering has local changes that conflict with Printify setup. Resolve the Offering in Store catalog before downloading again.");
            var updated = PrintifyCatalogImportService.ApplySelectedCatalog(snapshot, scope.StoreId, catalog.SelectedProducts, _clock, _newId);
            var provider = incomingProvider;
            var offering = updated.BlueprintOfferings.SingleOrDefault(value => value.StoreId == scope.StoreId && value.ExternalOfferingId == externalOfferingId)
                ?? throw new InvalidOperationException("The Printify catalog import did not produce a matching Offering.");
            if (existingConfiguration is not null && existingConfiguration.OfferingId != offering.Id)
                return new(false, false, "The linked product resolves to a different Offering than the Item's existing configuration.");
            if (existingConfiguration is not null && snapshot.DesignVariantRows.Any(value => value.ItemId == itemId))
                return new(true, false, "Variant setup is already present; existing local design choices were preserved.");
            if (existingConfiguration is not null && (snapshot.DesignSelectedColors.Any(value => value.ItemId == itemId)
                || snapshot.DesignSlotAssignments.Any(value => snapshot.DesignVariantRows.Any(row => row.ItemId == itemId && row.Id == value.RowId))))
                return new(false, false, "This Item has partial local Design setup. Resolve it before downloading Printify setup to avoid overwriting local choices.");

            var now = _clock();
            var configurations = updated.ItemListingConfigurations.Where(value => value.ItemId != itemId).Append(new FusionCanvas.Domain.Products.ItemListingConfiguration(itemId, offering.Id)).ToArray();
            var colorOption = provider.Options.FirstOrDefault(option => option.Name.Equals("Color", StringComparison.OrdinalIgnoreCase) || option.Type.Equals("color", StringComparison.OrdinalIgnoreCase));
            var colorValues = colorOption?.Values.ToDictionary(value => value.Id, value => value.Title) ?? [];
            var activeVariants = provider.Variants.Where(value => value.IsEnabled && value.IsAvailable).ToArray();
            var colorVariantIds = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var variant in activeVariants)
            {
                var color = variant.OptionValueIds.Select(id => colorValues.GetValueOrDefault(id)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "Default";
                if (!colorVariantIds.TryGetValue(color, out var ids)) colorVariantIds[color] = ids = [];
                ids.Add(variant.Id);
            }
            if (colorVariantIds.Count == 0) colorVariantIds["Default"] = activeVariants.Select(value => value.Id).ToHashSet();
            var colors = colorVariantIds.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var selectedColors = updated.DesignSelectedColors.Where(value => value.ItemId != itemId).Concat(colors.Select(color => new FusionCanvas.Domain.Products.DesignSelectedColor(itemId, color))).ToArray();

            var imageByVariantArea = detail.PrintAreas.SelectMany(area => area.Images.Select(image => (area, image)))
                .SelectMany(pair => pair.image.VariantIds.Select(variantId => (variantId, pair.image.Position, pair.image.ImageId, pair.image, pair.area.HasUnsupportedLayers)))
                .GroupBy(value => (value.variantId, value.Position)).ToDictionary(group => group.Key, group => group.ToArray());
            var signatures = colors.Select(color =>
            {
                var variantIds = colorVariantIds[color];
                var signature = string.Join("|", imageByVariantArea.Where(pair => variantIds.Contains(pair.Key.variantId))
                    .SelectMany(pair => pair.Value.Select(value => $"{pair.Key.Position}:{value.ImageId}"))
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
                return (Color: color, VariantIds: variantIds, Signature: signature);
            }).GroupBy(value => value.Signature, StringComparer.Ordinal).ToArray();
            var rows = updated.DesignVariantRows.Where(value => value.ItemId != itemId).ToList();
            var rowColors = updated.DesignVariantRowColors.Where(value => updated.DesignVariantRows.Any(row => row.ItemId != itemId && row.Id == value.RowId)).ToList();
            var assignments = updated.DesignSlotAssignments.Where(value => updated.DesignVariantRows.Any(row => row.ItemId != itemId && row.Id == value.RowId)).ToList();
            var partial = false;
            var unsupportedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var localAreas = updated.DesignAreas.Where(area => area.FulfillmentOfferingId == offering.Id).ToArray();
            foreach (var (group, index) in signatures.Select((value, index) => (value, index)))
            {
                var rowId = _newId();
                rows.Add(new(rowId, itemId, index == 0, index));
                rowColors.AddRange(group.Select(value => new FusionCanvas.Domain.Products.DesignVariantRowColor(rowId, value.Color)));
                foreach (var designArea in localAreas)
                {
                    var candidateImages = group.SelectMany(value => value.VariantIds.SelectMany(variantId => imageByVariantArea.GetValueOrDefault((variantId, designArea.Position)) ?? []))
                        .Select(value => value.image).DistinctBy(value => value.ImageId, StringComparer.Ordinal).ToArray();
                    var unsupported = candidateImages.Length > 1 || detail.PrintAreas.Any(area => area.HasUnsupportedLayers && area.Images.Any(image => image.Position.Equals(designArea.Position, StringComparison.OrdinalIgnoreCase)));
                    if (unsupported) { partial = true; unsupportedSlots.Add(designArea.Position); assignments.Add(new(rowId, designArea.Id, null)); continue; }
                    var image = candidateImages.SingleOrDefault();
                    var asset = image is null ? null : updated.Assets.FirstOrDefault(value => value.StoreId == scope.StoreId && MetadataHasImageId(value.MetadataJson, image.ImageId));
                    assignments.Add(new(rowId, designArea.Id, asset?.Id));
                    if (image is not null && asset is null) { partial = true; unsupportedSlots.Add(designArea.Position); }
                }
            }
            await repository.SaveAsync(updated with
            {
                ItemListingConfigurations = configurations,
                DesignSelectedColors = selectedColors,
                DesignVariantRows = rows,
                DesignVariantRowColors = rowColors,
                DesignSlotAssignments = assignments
            }, cancellationToken).ConfigureAwait(false);
            return new(true, partial, partial
                ? $"Variant setup downloaded. Unsupported or ambiguous artwork slots left empty: {string.Join(", ", unsupportedSlots.Order(StringComparer.OrdinalIgnoreCase))}."
                : "Printify variant setup downloaded.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return new(false, false, exception.Message); }
    }

    public async Task<IReadOnlyList<PrintifyListingImportPreview>> LoadPreviewAsync(StoreCredentialScope scope, Guid nicheId, CancellationToken cancellationToken = default)
    {
        var store = await RequireStoreAsync(scope, nicheId, cancellationToken).ConfigureAwait(false);
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(read.Secret))
            throw new InvalidOperationException("Add and verify a Printify key in Store setup before importing listings.");
        var shopId = store.Context.PrintifyShopId ?? throw new InvalidOperationException("Select a Printify shop in Store setup before importing listings.");
        var products = await client.GetShopProductsAsync(read.Secret, shopId, cancellationToken).ConfigureAwait(false);
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var linkedIds = snapshot.ExternalListingMappings.Where(mapping => mapping.StoreId == scope.StoreId && mapping.ProviderKey == ProviderKey && mapping.ShopId == shopId.ToString(CultureInfo.InvariantCulture))
            .Select(mapping => mapping.ProductId).Where(id => id is not null).ToHashSet(StringComparer.Ordinal);
        return products.Select(product => new PrintifyListingImportPreview(product, linkedIds.Contains(product.ProductId), [])).ToArray();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<PrintifyListingImportCandidate>>> CheckDuplicatesAsync(StoreCredentialScope scope, IReadOnlyCollection<string> selectedProductIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedProductIds);
        if (selectedProductIds.Count == 0) throw new InvalidOperationException("Select at least one product before checking for duplicates.");
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = await stores.ResolveActiveStoreAsync(scope.WorkspaceId, scope.StoreId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsArchived) throw new InvalidOperationException("The selected Store is no longer available.");
        var items = snapshot.Items.Where(item => item.StoreId == scope.StoreId && !item.IsArchived).ToArray();
        var mapped = snapshot.ExternalListingMappings.Where(mapping => mapping.StoreId == scope.StoreId && mapping.ProviderKey == ProviderKey).Select(mapping => mapping.ItemId).ToHashSet();
        var result = new Dictionary<string, IReadOnlyList<PrintifyListingImportCandidate>>(StringComparer.Ordinal);
        foreach (var id in selectedProductIds.Distinct(StringComparer.Ordinal))
        {
            var product = await GetProductAsync(scope, id, cancellationToken).ConfigureAwait(false);
            var matches = items.Select(item =>
            {
                var title = Similarity(product.Title, item.Name);
                var description = Similarity(product.Description, item.Description);
                return new PrintifyListingImportCandidate(item.Id, item.Name, item.Description, Location(snapshot, item), !mapped.Contains(item.Id), title, description);
            }).Where(candidate => candidate.TitleSimilarity >= 0.90 || candidate.DescriptionSimilarity >= 0.90)
                .OrderByDescending(candidate => Math.Max(candidate.TitleSimilarity, candidate.DescriptionSimilarity)).ToArray();
            result.Add(id, matches);
        }
        return result;
    }

    public async Task<IReadOnlyList<PrintifyListingImportOutcome>> ImportAsync(StoreCredentialScope scope, Guid nicheId, IReadOnlyCollection<PrintifyListingImportDecision> decisions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        if (decisions.Count == 0) return [];
        var store = await RequireStoreAsync(scope, nicheId, cancellationToken).ConfigureAwait(false);
        var shopId = store.Context.PrintifyShopId!.Value;
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(read.Secret)) throw new InvalidOperationException("Add and verify a Printify key in Store setup before importing listings.");
        var outcomes = new List<PrintifyListingImportOutcome>();
        Guid? groupId = null;
        foreach (var decision in decisions.DistinctBy(value => value.ProductId))
        {
            var staged = new List<ManagedWorkspaceFile>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var detail = await client.GetProductAsync(read.Secret, shopId, decision.ProductId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Printify product is no longer available.");
                var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
                if (snapshot.ExternalListingMappings.Any(mapping => mapping.StoreId == scope.StoreId && mapping.ProviderKey == ProviderKey && mapping.ShopId == shopId.ToString(CultureInfo.InvariantCulture) && mapping.ProductId == detail.ProductId))
                    throw new InvalidOperationException("This Printify product is already linked to an Item.");
                var item = ResolveItem(snapshot, scope.StoreId, decision.ConnectToItemId);
                if (item is null && decision.ConnectToItemId is not null) throw new InvalidOperationException("The selected duplicate Item is no longer available.");
                if (item is not null && snapshot.ExternalListingMappings.Any(mapping => mapping.StoreId == scope.StoreId && mapping.ItemId == item.Id && mapping.ProviderKey == ProviderKey))
                    throw new InvalidOperationException("The selected Item is already linked to a Printify product.");
                var now = _clock();
                item ??= new Item(_newId(), scope.StoreId, nicheId, null, detail.Title, detail.Description,
                    detail.IsVisible ? ItemStatus.Published : ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
                var artwork = detail.PrintAreas.SelectMany(area => area.Images).GroupBy(value => value.ImageId, StringComparer.Ordinal)
                    .Select(group => (Image: group.First(), Relations: group.Select(image => new { image.Position, image.VariantIds }).ToArray())).ToArray();
                foreach (var source in artwork)
                {
                    var download = await client.DownloadArtworkAsync(source.Image.SourceUrl, cancellationToken).ConfigureAwait(false);
                    await using var content = new MemoryStream(download.Content, writable: false);
                    staged.Add(await files.SaveAsync($"printify-{source.Image.ImageId}{download.Extension}", AssetKind.ExportedImage, content, cancellationToken).ConfigureAwait(false));
                }

                var updated = snapshot;
                if (decision.ConnectToItemId is null)
                {
                    var groups = snapshot.Groups.ToList();
                    if (groupId is null || !groups.Any(group => group.Id == groupId))
                    {
                        var name = UniqueGroupName(groups, nicheId, now);
                        var group = new TopicGroup(_newId(), scope.StoreId, nicheId, null, name, null, false, now, now, "{}");
                        groups.Add(group);
                        groupId = group.Id;
                    }
                    item = item with { GroupId = groupId };
                    updated = updated with { Groups = groups };
                }
                var assets = updated.Assets.ToList();
                var links = updated.AssetLinks.ToList();
                for (var index = 0; index < staged.Count; index++)
                {
                    var file = staged[index];
                    var source = artwork[index];
                    var assetId = _newId();
                    assets.Add(new Asset(assetId, scope.StoreId, source.Image.Name ?? Path.GetFileNameWithoutExtension(file.Name), null, AssetKind.ExportedImage, file.WorkspaceRelativePath, SafeProvenance(source.Image.SourceUrl), false, false, now, now,
                        JsonSerializer.Serialize(new { source = "printify", imageId = source.Image.ImageId, sourceUrl = SafeProvenance(source.Image.SourceUrl), relations = source.Relations })));
                    links.Add(new AssetLink(assetId, WorkspaceEntityKind.Item, item.Id));
                }
                var publicationState = detail.IsVisible ? ExternalListingPublicationState.Published : ExternalListingPublicationState.Unpublished;
                var mapping = new ExternalListingMapping(scope.StoreId, item.Id, ProviderKey, shopId.ToString(CultureInfo.InvariantCulture), detail.ProductId, null, null,
                    ExternalListingSyncState.Synchronized, publicationState, ExternalListingOperationState.Succeeded, null, null, now, now, now,
                    integrationValuesJson: JsonSerializer.Serialize(new { version = 1, product = SafeProduct(detail) }));
                var itemsUpdated = updated.Items.ToList();
                if (!itemsUpdated.Any(value => value.Id == item.Id)) itemsUpdated.Add(item);
                await repository.SaveAsync(updated with
                {
                    Items = itemsUpdated,
                    Assets = assets,
                    AssetLinks = links,
                    ExternalListingMappings = updated.ExternalListingMappings.Append(mapping).ToArray()
                }, cancellationToken).ConfigureAwait(false);
                outcomes.Add(new(detail.ProductId, true, item.Id, "Imported."));
            }
            catch (OperationCanceledException)
            {
                await CleanupAsync(staged);
                outcomes.Add(new(decision.ProductId, false, null, "Cancelled before this product was saved. Retry it later."));
                break;
            }
            catch (Exception exception)
            {
                await CleanupAsync(staged);
                outcomes.Add(new(decision.ProductId, false, null, exception.Message));
            }
        }
        return outcomes;
    }

    private async Task<PrintifyListingProductDetail> GetProductAsync(StoreCredentialScope scope, string id, CancellationToken cancellationToken)
    {
        var store = await RequireStoreAsync(scope, null, cancellationToken).ConfigureAwait(false);
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available || string.IsNullOrWhiteSpace(read.Secret)) throw new InvalidOperationException("Printify connection is unavailable.");
        return await client.GetProductAsync(read.Secret, store.Context.PrintifyShopId!.Value, id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Printify product is no longer available.");
    }

    private async Task<StoreSummary> RequireStoreAsync(StoreCredentialScope scope, Guid? nicheId, CancellationToken cancellationToken)
    {
        var store = await stores.ResolveActiveStoreAsync(scope.WorkspaceId, scope.StoreId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.IsArchived || !FulfillmentStrategyPolicy.RequiresPrintifyKey(store.FulfillmentStrategy) || store.Context.PrintifyShopId is null) throw new InvalidOperationException("Save an active Printify Store with a selected Printify shop first.");
        if (nicheId is Guid id)
        {
            var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!snapshot.Niches.Any(niche => niche.Id == id && niche.StoreId == scope.StoreId && !niche.IsArchived)) throw new InvalidOperationException("The selected Niche is no longer available.");
        }
        return store;
    }

    private static Item? ResolveItem(WorkspaceSnapshot snapshot, Guid storeId, Guid? id) => id is null ? null : snapshot.Items.SingleOrDefault(item => item.Id == id && item.StoreId == storeId && !item.IsArchived);
    private static string UniqueGroupName(IReadOnlyList<TopicGroup> groups, Guid nicheId, DateTimeOffset now)
    {
        var root = $"Printify {now.ToLocalTime():yyyy-MM-dd}";
        var existing = groups.Where(group => group.NicheId == nicheId && !group.IsArchived).Select(group => group.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(root)) return root;
        for (var suffix = 2; ; suffix++) if (!existing.Contains($"{root} ({suffix})")) return $"{root} ({suffix})";
    }
    private static string Location(WorkspaceSnapshot snapshot, Item item) => string.Join(" / ", new[] { snapshot.Niches.FirstOrDefault(value => value.Id == item.NicheId)?.Name, snapshot.Groups.FirstOrDefault(value => value.Id == item.GroupId)?.Name }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string SafeProvenance(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty }.Uri.AbsoluteUri : string.Empty;
    private static bool MetadataHasImageId(string json, string imageId)
    {
        try { using var document = JsonDocument.Parse(json); return document.RootElement.TryGetProperty("imageId", out var value) && value.GetString() == imageId; }
        catch (JsonException) { return false; }
    }
    private static bool IsPrintifyOwned(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("source", out var source) && source.GetString() == "printify";
        }
        catch (JsonException) { return false; }
    }
    private static PrintifyListingProductDetail SafeProduct(PrintifyListingProductDetail detail) => detail with
    {
        PrintAreas = detail.PrintAreas.Select(area => area with
        {
            Images = area.Images.Select(image => image with { SourceUrl = SafeProvenance(image.SourceUrl) }).ToArray()
        }).ToArray()
    };
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormKD).ToLowerInvariant();
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed) if (char.IsLetterOrDigit(character)) builder.Append(character);
        return builder.ToString();
    }
    private static double Similarity(string? left, string? right)
    {
        var a = Normalize(left); var b = Normalize(right);
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a.Length > MaximumDuplicateTextLength || b.Length > MaximumDuplicateTextLength) return 0;
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1]; current[0] = i;
            for (var j = 1; j <= b.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return 1d - (double)previous[b.Length] / Math.Max(a.Length, b.Length);
    }
    private async Task CleanupAsync(IEnumerable<ManagedWorkspaceFile> staged)
    {
        foreach (var file in staged) files.TryDelete(file.WorkspaceRelativePath);
    }
}
