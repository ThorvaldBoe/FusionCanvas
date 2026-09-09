namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogBlueprintSummary(
    int Id,
    string Title,
    string? Description,
    string? Brand,
    string? Model);
