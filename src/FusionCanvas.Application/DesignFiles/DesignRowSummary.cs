using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.DesignFiles;

/// <summary>Summary of a design slot assignment for the UI.</summary>
public sealed record DesignRowSummary(
    Guid RowId,
    bool IsDefault,
    int SortOrder,
    IReadOnlyList<string> ColorValues,
    IReadOnlyList<DesignSlotSummary> Slots);

/// <summary>Full state of the Design Stage for an item.</summary>
