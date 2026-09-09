namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogProvider(
    int Id,
    string Title,
    IReadOnlyList<PrintifyCatalogOption> Options,
    IReadOnlyList<PrintifyCatalogVariant> Variants);
