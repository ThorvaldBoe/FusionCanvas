using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.DesignFiles;

/// <summary>Summary of a design slot assignment for the UI.</summary>
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
