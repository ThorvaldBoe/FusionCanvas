namespace FusionCanvas.Application.Listings;

public sealed record ListingProductLinkVerification(
    bool Succeeded,
    ListingProductLinkCandidate? Candidate = null,
    string? Message = null);
