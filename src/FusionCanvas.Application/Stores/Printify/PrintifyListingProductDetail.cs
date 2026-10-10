namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingProductDetail(string ProductId, string Title, string? Description, bool IsVisible, int BlueprintId, int ProviderId, string? ShippingProfile, IReadOnlyList<PrintifyListingOption> Options, IReadOnlyList<PrintifyListingVariant> Variants, IReadOnlyList<PrintifyListingPrintArea> PrintAreas);
