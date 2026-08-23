using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.DesignFiles;

/// <summary>Summary of a design slot assignment for the UI.</summary>
public sealed record DesignSlotSummary(
    Guid DesignAreaId,
    string AreaName,
    Guid? AssetId,
    string? ThumbnailPath,
    bool IsMissing,
    bool CanPreview,
    bool CanExport)
{
    public Guid PlaceholderId => DesignAreaId;
    public string? Position { get; init; }
    public string? DecorationMethod { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

/// <summary>Summary of a design variant row for the UI.</summary>
public sealed record DesignRowSummary(
    Guid RowId,
    bool IsDefault,
    int SortOrder,
    IReadOnlyList<string> ColorValues,
    IReadOnlyList<DesignSlotSummary> Slots);

/// <summary>Full state of the Design Stage for an item.</summary>
public sealed record DesignStageState(
    Guid ItemId,
    bool IsReadOnly,
    string ReadOnlyReason,
    Guid? SelectedOfferingId,
    string? SelectedOfferingName,
    FulfillmentKind? SelectedOfferingKind,
    string? SelectedOfferingProviderName,
    IReadOnlyList<FulfillmentOffering> AvailableOfferings, // filtered to item's store
    IReadOnlyList<string> AvailableColors, // deduplicated from offering
    IReadOnlyList<string> SelectedColors,
    IReadOnlyList<DesignRowSummary> Rows,
    IReadOnlyList<DesignSlotSummary> SupportingImages)
{
    public IReadOnlyList<BlueprintOffering> AvailableBlueprintOfferings { get; init; } = [];
    public IReadOnlyList<OfferingPlaceholder> AvailablePlaceholders { get; init; } = [];
    public string? SelectedProviderNetworkCode { get; init; }
    public string? SelectedBlueprintName { get; init; }
    public string? ProviderNetworkWarning { get; init; }
}
