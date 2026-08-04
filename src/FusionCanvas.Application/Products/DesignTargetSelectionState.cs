namespace FusionCanvas.Application.Products;

public sealed record DesignTargetSelectionState
(
    Guid ItemId,
    bool IsReadOnly,
    IReadOnlyList<DesignAreaTargetOption> Options);
