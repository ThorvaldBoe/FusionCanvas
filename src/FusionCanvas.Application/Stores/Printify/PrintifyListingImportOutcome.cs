namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingImportOutcome(string ProductId, bool Succeeded, Guid? ItemId, string Message);
