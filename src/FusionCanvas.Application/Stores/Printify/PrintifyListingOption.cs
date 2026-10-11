namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingOption(string Name, string Type, IReadOnlyList<PrintifyListingOptionValue> Values);
