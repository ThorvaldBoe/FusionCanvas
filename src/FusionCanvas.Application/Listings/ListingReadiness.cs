namespace FusionCanvas.Application.Listings;

public sealed record ListingReadiness(
    ListingConnectionState ConnectionState,
    bool ProductOperationsAvailable,
    bool PublicationOperationsAvailable,
    IReadOnlyList<ListingReadinessIssue> Issues)
{
    public bool IsReady => ProductOperationsAvailable;

    public static ListingReadiness Unavailable(string code, string message) =>
        new(ListingConnectionState.Unavailable, false, false, [new ListingReadinessIssue(code, message)]);
}
