namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingImportDecision(string ProductId, Guid? ConnectToItemId = null);
