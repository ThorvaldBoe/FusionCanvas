namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingProductSummary(string ProductId, string Title, string? Description, bool IsVisible, int BlueprintId, int ProviderId);
