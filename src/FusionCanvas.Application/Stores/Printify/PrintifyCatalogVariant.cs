namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyCatalogVariant(
    int Id,
    string Title,
    bool IsEnabled,
    bool IsAvailable,
    IReadOnlyList<int> OptionValueIds,
    IReadOnlyList<PrintifyCatalogPlaceholder> Placeholders);
