namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingVariant(int Id, string Title, int PriceMinorUnits, bool IsEnabled, bool IsAvailable, IReadOnlyList<int> OptionValueIds);
