namespace FusionCanvas.Application.Listings;

public sealed record ListingOperation(
    ListingOperationKind Kind,
    ListingOperationState State,
    DateTimeOffset StartedAt,
    string? RequestFingerprint = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);
