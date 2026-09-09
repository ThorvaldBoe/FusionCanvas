namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogResult(
    PrintifyCatalogResultKind Kind,
    string Message,
    IReadOnlyList<PrintifyCatalogBlueprintSummary>? Blueprints = null,
    IReadOnlyList<PrintifyCatalogBlueprint>? SelectedCatalog = null)
{
    public bool Succeeded => Kind == PrintifyCatalogResultKind.Succeeded;
}
