namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyShopProductSummary(
    string ProductId,
    string Title,
    string? Description,
    int BlueprintId,
    int ProviderId,
    string? BlueprintName = null)
{
    public IReadOnlyList<PrintifyCatalogOption> Options { get; init; } = [];
}
