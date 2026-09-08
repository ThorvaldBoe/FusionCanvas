namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogOption(
    string Name,
    string Type,
    IReadOnlyList<PrintifyCatalogOptionValue> Values);
