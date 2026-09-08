namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogBlueprint(
    PrintifyCatalogBlueprintSummary Summary,
    IReadOnlyList<PrintifyCatalogProvider> Providers);
