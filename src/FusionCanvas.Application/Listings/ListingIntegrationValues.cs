namespace FusionCanvas.Application.Listings;

public sealed record ListingIntegrationValues(
    string? Title,
    string? Description,
    string? ShippingProfile,
    IReadOnlyDictionary<int, decimal> VariantRetailPrices)
{
    public static ListingIntegrationValues Empty { get; } = new(null, null, null, new Dictionary<int, decimal>());
}
