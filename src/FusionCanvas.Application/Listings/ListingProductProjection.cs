using System.Collections.ObjectModel;

namespace FusionCanvas.Application.Listings;

public sealed record ListingProductProjection(
    Guid ItemId,
    Guid StoreId,
    Guid BlueprintId,
    Guid OfferingId,
    Guid ProviderId,
    string Title,
    string? Description,
    string? ShippingProfile,
    string? OutOfStockPolicy,
    ListingPricingInput Pricing,
    IReadOnlyList<ListingVariantProjection> Variants,
    IReadOnlyList<ListingArtworkProjection> Artwork)
{
    public int? ExternalBlueprintId { get; init; }
    public int? ExternalProviderId { get; init; }

    public IReadOnlyDictionary<Guid, ListingPrice> Prices =>
        new ReadOnlyDictionary<Guid, ListingPrice>(Variants.ToDictionary(value => value.SourceVariantId, value => value.Price));
}
