namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogResult(
    PrintifyCatalogResultKind Kind,
    string Message,
    IReadOnlyList<PrintifyCatalogBlueprintSummary>? Blueprints = null,
    IReadOnlyList<PrintifyCatalogBlueprint>? SelectedCatalog = null,
    IReadOnlyList<PrintifyShopProductSummary>? Products = null,
    IReadOnlyList<PrintifyCatalogBlueprint>? SelectedProducts = null)
{
    public bool Succeeded => Kind == PrintifyCatalogResultKind.Succeeded;
}
