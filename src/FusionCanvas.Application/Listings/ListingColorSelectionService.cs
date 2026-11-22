using System.Text.Json;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Listings;

public sealed record ListingColorChoice(
    Guid OptionValueId,
    string Name,
    string? ApproximateHex,
    bool IsFavorite,
    bool IsSelected,
    int ActiveVariantCount,
    int OutOfStockVariantCount,
    int AvailabilityUnknownVariantCount)
{
    public Guid? ArtworkVariantId { get; init; }
}

public sealed record ListingArtworkVariantChoice(Guid Id, string Name, int SortOrder);
public sealed record ListingUnresolvedColorChoice(string Label, int SortOrder, Guid ArtworkVariantId);
public sealed record ListingColorSelectionState(
    Guid ItemId,
    Guid OfferingId,
    IReadOnlyList<ListingColorChoice> Favorites,
    IReadOnlyList<ListingColorChoice> OtherColors,
    IReadOnlyList<ListingArtworkVariantChoice> ArtworkVariants,
    IReadOnlyList<ListingUnresolvedColorChoice> UnresolvedColors);

/// <summary>Loads and updates Item-scoped Listing Colors and Offering-scoped favorite state.</summary>
public sealed class ListingColorSelectionService(IWorkspaceRepository repository, IWorkspaceFileStore? fileStore = null) : IListingColorSelectionService
{
    public async Task<ListingColorSelectionState> LoadAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId)
            ?? throw new InvalidOperationException("Item was not found.");
        var offeringId = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId
            ?? throw new InvalidOperationException("Select an active Offering in Design before selecting Listing Colors.");
        var colorOptionIds = snapshot.OfferingOptions
            .Where(value => value.OfferingId == offeringId && value.OptionKind == OptionKind.Color && !value.IsArchived)
            .Select(value => value.Id).ToHashSet();
        var selected = snapshot.ListingColorSelections.Where(value => value.ItemId == itemId && value.ColorOptionValueId is not null)
            .ToDictionary(value => value.ColorOptionValueId!.Value);
        var defaultArtworkVariantId = snapshot.DesignVariantRows.SingleOrDefault(value => value.ItemId == itemId && value.IsDefault)?.Id;
        var choices = snapshot.OfferingOptionValues
            .Where(value => value.OfferingId == offeringId && colorOptionIds.Contains(value.OptionId) && !value.IsArchived)
            .OrderByDescending(value => value.IsFavorite)
            .ThenBy(value => value.SortOrder)
            .ThenBy(value => value.Value, StringComparer.OrdinalIgnoreCase)
            .Select(value =>
            {
                var variants = snapshot.OfferingVariants.Where(variant => !variant.IsArchived && variant.OptionValueIds.Contains(value.Id)).ToArray();
                var outOfStock = variants.Count(variant => variant.ProviderIsAvailable is false);
                var unknownAvailability = variants.Count(variant => variant.ProviderIsAvailable is null);
                var choice = new ListingColorChoice(value.Id, value.Value, ReadHex(value.MetadataJson), value.IsFavorite,
                    selected.ContainsKey(value.Id), variants.Length, outOfStock, unknownAvailability);
                return choice with
                {
                    ArtworkVariantId = selected.TryGetValue(value.Id, out var selection)
                        ? selection.ArtworkVariantId
                        : defaultArtworkVariantId
                };
            }).ToArray();
        var variantRows = snapshot.DesignVariantRows.Where(value => value.ItemId == itemId)
            .OrderBy(value => value.SortOrder)
            .Select(value => new ListingArtworkVariantChoice(value.Id, value.IsDefault ? "Default artwork" : $"Artwork variant {value.SortOrder + 1}", value.SortOrder))
            .ToArray();

        var unresolvedColors = snapshot.ListingColorSelections.Where(value => value.ItemId == itemId
                && value.ColorOptionValueId is null && !string.IsNullOrWhiteSpace(value.LegacyColorLabel))
            .OrderBy(value => value.SortOrder)
            .Select(value => new ListingUnresolvedColorChoice(value.LegacyColorLabel!, value.SortOrder, value.ArtworkVariantId))
            .ToArray();

        return new ListingColorSelectionState(item.Id, offeringId,
            choices.Where(value => value.IsFavorite).ToArray(),
            choices.Where(value => !value.IsFavorite).ToArray(),
            variantRows,
            unresolvedColors);
    }

    public async Task SetSelectedColorsAsync(Guid itemId, IReadOnlyCollection<Guid> colorOptionValueIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(colorOptionValueIds);
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = ResolveEditableItem(snapshot, itemId);
        var offeringId = ResolveOffering(snapshot, itemId);
        var requested = colorOptionValueIds.Distinct().ToHashSet();
        var colorOptionIds = snapshot.OfferingOptions.Where(value => value.OfferingId == offeringId
                && value.OptionKind == OptionKind.Color && !value.IsArchived)
            .Select(value => value.Id).ToHashSet();
        var valid = snapshot.OfferingOptionValues.Where(value => requested.Contains(value.Id)
                && value.OfferingId == offeringId && colorOptionIds.Contains(value.OptionId) && !value.IsArchived)
            .ToDictionary(value => value.Id);
        if (valid.Count != requested.Count)
            throw new InvalidOperationException("One or more Colors are no longer available in the selected Offering.");
        var availableColorIds = snapshot.OfferingVariants.Where(value => !value.IsArchived && value.OfferingId == offeringId)
            .SelectMany(value => value.OptionValueIds).ToHashSet();
        var unavailable = requested.Where(value => !availableColorIds.Contains(value)).ToArray();
        if (unavailable.Length > 0)
            throw new InvalidOperationException("One or more selected Colors have no configured provider Variants. Refresh the catalog or choose a Color with configured Sizes.");
        var defaultVariant = snapshot.DesignVariantRows.SingleOrDefault(value => value.ItemId == itemId && value.IsDefault)
            ?? throw new InvalidOperationException("Add a default artwork variant in Design before selecting Colors.");
        var existing = snapshot.ListingColorSelections.Where(value => value.ItemId == itemId).ToArray();
        var retained = existing.Where(value => value.ColorOptionValueId is Guid id && requested.Contains(id)).ToDictionary(value => value.ColorOptionValueId!.Value);
        var selections = new List<ListingColorSelection>();
        var order = 0;
        foreach (var id in requested.OrderBy(id => valid[id].SortOrder))
        {
            selections.Add(retained.TryGetValue(id, out var current)
                ? current with { SortOrder = order++ }
                : new ListingColorSelection(itemId, id, defaultVariant.Id, order++));
        }
        selections.AddRange(existing.Where(value => value.ColorOptionValueId is null).Select(value => value with { SortOrder = order++ }));
        var updated = snapshot with { ListingColorSelections = [.. snapshot.ListingColorSelections.Where(value => value.ItemId != itemId), .. selections] };
        DesignStagePolicy.ValidateListingColorSelections(itemId, updated.ListingColorSelections, updated.DesignVariantRows);
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetArtworkVariantAsync(Guid itemId, Guid colorOptionValueId, Guid artworkVariantId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        ResolveEditableItem(snapshot, itemId);
        var selection = snapshot.ListingColorSelections.SingleOrDefault(value =>
            value.ItemId == itemId && value.ColorOptionValueId == colorOptionValueId)
            ?? throw new InvalidOperationException("Select this Color for the Item before assigning artwork.");
        if (!snapshot.DesignVariantRows.Any(value => value.Id == artworkVariantId && value.ItemId == itemId))
            throw new InvalidOperationException("The artwork variant does not belong to this Item.");
        var updated = snapshot with
        {
            ListingColorSelections = [.. snapshot.ListingColorSelections.Where(value => !ReferenceEquals(value, selection)),
                selection with { ArtworkVariantId = artworkVariantId }]
        };
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFavoriteAsync(Guid offeringId, Guid colorOptionValueId, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var optionValue = snapshot.OfferingOptionValues.SingleOrDefault(value =>
            value.Id == colorOptionValueId && value.OfferingId == offeringId && !value.IsArchived)
            ?? throw new InvalidOperationException("The Color is no longer available in this Offering.");
        var option = snapshot.OfferingOptions.SingleOrDefault(value => value.Id == optionValue.OptionId
            && value.OfferingId == offeringId && value.OptionKind == OptionKind.Color && !value.IsArchived)
            ?? throw new InvalidOperationException("Only active Color values can be favorites.");
        _ = option;
        await repository.SaveAsync(snapshot with
        {
            OfferingOptionValues = [.. snapshot.OfferingOptionValues.Select(value =>
                value.Id == colorOptionValueId ? value with { IsFavorite = isFavorite } : value)]
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResolveUnresolvedColorAsync(Guid itemId, int sortOrder, Guid colorOptionValueId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        ResolveEditableItem(snapshot, itemId);
        var offeringId = ResolveOffering(snapshot, itemId);
        var unresolved = snapshot.ListingColorSelections.SingleOrDefault(value => value.ItemId == itemId
            && value.SortOrder == sortOrder && value.ColorOptionValueId is null)
            ?? throw new InvalidOperationException("The unresolved Listing Color no longer exists.");
        var optionValue = snapshot.OfferingOptionValues.SingleOrDefault(value => value.Id == colorOptionValueId
            && value.OfferingId == offeringId && !value.IsArchived)
            ?? throw new InvalidOperationException("Choose an active Color value from this Offering.");
        if (!snapshot.OfferingOptions.Any(value => value.Id == optionValue.OptionId && value.OfferingId == offeringId
            && value.OptionKind == OptionKind.Color && !value.IsArchived))
            throw new InvalidOperationException("Choose an active Color value from this Offering.");
        if (snapshot.ListingColorSelections.Any(value => value.ItemId == itemId && value.ColorOptionValueId == colorOptionValueId))
            throw new InvalidOperationException("This Color is already selected for the Item.");

        var updated = snapshot with
        {
            ListingColorSelections = [.. snapshot.ListingColorSelections.Select(value =>
                value.ItemId == itemId && value.SortOrder == sortOrder && value.ColorOptionValueId is null
                    ? value with { ColorOptionValueId = colorOptionValueId, LegacyColorLabel = null }
                    : value)]
        };
        DesignStagePolicy.ValidateListingColorSelections(itemId, updated.ListingColorSelections, updated.DesignVariantRows);
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> OpenArtworkPreviewAsync(Guid itemId, Guid colorOptionValueId, CancellationToken cancellationToken = default)
    {
        if (fileStore is null) return null;
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var selection = snapshot.ListingColorSelections.SingleOrDefault(value =>
            value.ItemId == itemId && value.ColorOptionValueId == colorOptionValueId);
        var artworkVariantId = selection?.ArtworkVariantId
            ?? snapshot.DesignVariantRows.SingleOrDefault(value => value.ItemId == itemId && value.IsDefault)?.Id;
        if (artworkVariantId is null) return null;
        var assignment = snapshot.DesignSlotAssignments.FirstOrDefault(value => value.RowId == artworkVariantId && value.AssetId is not null);
        var asset = assignment?.AssetId is Guid assetId
            ? snapshot.Assets.SingleOrDefault(value => value.Id == assetId && value.Kind == AssetKind.ExportedImage && !value.IsMissing && !value.IsArchived)
            : null;
        return asset is null ? null : await fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
    }

    private static Item ResolveEditableItem(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId)
            ?? throw new InvalidOperationException("Item was not found.");
        var decision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StageContent);
        if (!decision.IsAllowed) throw new InvalidOperationException(decision.Reason);
        return item;
    }

    private static Guid ResolveOffering(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var offeringId = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId
            ?? throw new InvalidOperationException("Select an active Offering in Design before selecting Listing Colors.");
        if (!snapshot.BlueprintOfferings.Any(value => value.Id == offeringId && !value.IsArchived))
            throw new InvalidOperationException("The Item's Offering is unavailable.");
        return offeringId;
    }

    private static string? ReadHex(string metadataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("hex", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                    && property.Value.GetString() is { } hex
                    && (hex.Length == 7 || hex.Length == 4)
                    && hex[0] == '#'
                    && hex[1..].All(Uri.IsHexDigit))
                    return hex;
            }
        }
        catch (JsonException) { }
        return null;
    }

}
