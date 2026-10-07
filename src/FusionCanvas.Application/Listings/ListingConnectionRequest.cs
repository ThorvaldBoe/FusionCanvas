namespace FusionCanvas.Application.Listings;

public sealed record ListingConnectionRequest(Guid StoreId, string ShopId, bool RequirePublication);
