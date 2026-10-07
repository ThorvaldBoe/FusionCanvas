namespace FusionCanvas.Application.Listings;

public sealed record ListingProjectionResult(
    ListingProductProjection? Projection,
    IReadOnlyList<ListingReadinessIssue> Issues)
{
    public bool IsValid => Projection is not null && Issues.Count == 0;
}
