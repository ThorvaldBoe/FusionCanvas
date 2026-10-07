namespace FusionCanvas.Application.Listings;

public sealed record ListingMapping(
    Guid StoreId,
    Guid ItemId,
    ListingExternalIdentity? Identity,
    ListingSynchronizationState SynchronizationState,
    ListingPublicationState PublicationState,
    ListingOperation? Operation,
    string? LastSynchronizedSnapshotJson = null,
    DateTimeOffset? LastSynchronizedAt = null);
