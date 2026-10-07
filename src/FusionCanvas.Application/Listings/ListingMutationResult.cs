namespace FusionCanvas.Application.Listings;

public sealed record ListingMutationResult(
    string? ProductId,
    bool IsDefinitive,
    ListingRemoteProduct? Product = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);
