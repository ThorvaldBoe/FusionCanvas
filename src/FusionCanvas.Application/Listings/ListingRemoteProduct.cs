namespace FusionCanvas.Application.Listings;

public sealed record ListingRemoteProduct(
    string ProductId,
    bool IsLocked,
    ListingPublicationState PublicationState,
    ListingSnapshot Snapshot,
    string? ExternalSalesChannelId = null,
    string? ExternalHandle = null);
