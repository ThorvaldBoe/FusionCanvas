namespace FusionCanvas.Application.Products;

public sealed record ReplaceDesignTargetsRequest(
    Guid ItemId,
    IReadOnlyList<Guid> DesignAreaIds);
