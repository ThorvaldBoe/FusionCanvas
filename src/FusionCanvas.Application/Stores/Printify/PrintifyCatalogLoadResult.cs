namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogResult(
    PrintifyCatalogResultKind Kind,
    string Message,
    IReadOnlyList<PrintifyCatalogBlueprintSummary>? Blueprints = null,
    IReadOnlyList<PrintifyCatalogBlueprint>? SelectedCatalog = null,
    IReadOnlyList<PrintifyShopProductSummary>? Products = null,
    IReadOnlyList<PrintifyCatalogBlueprint>? SelectedProducts = null,
    IReadOnlyList<PrintifyShopBlueprintSummary>? ShopBlueprints = null,
    int CreatedRecordCount = 0,
    int UpdatedRecordCount = 0)
{
    public bool Succeeded => Kind == PrintifyCatalogResultKind.Succeeded;
}
