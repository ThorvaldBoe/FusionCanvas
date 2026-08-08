namespace FusionCanvas.Application.Listings;

public sealed record ListingPreparationResult(
    bool Succeeded,
    ListingPreparationState? State,
    string? Error)
{
    public static ListingPreparationResult Success(ListingPreparationState state) => new(true, state, null);

    public static ListingPreparationResult Failure(string error, ListingPreparationState? state = null) => new(false, state, error);
}
