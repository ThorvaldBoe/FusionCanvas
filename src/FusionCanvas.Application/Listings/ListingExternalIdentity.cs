namespace FusionCanvas.Application.Listings;

public sealed record ListingExternalIdentity(
    string ShopId,
    string ProductId,
    string? ExternalSalesChannelId = null,
    string? ExternalHandle = null);
