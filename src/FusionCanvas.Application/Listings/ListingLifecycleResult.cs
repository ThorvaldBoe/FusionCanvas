namespace FusionCanvas.Application.Listings;

public sealed record ListingLifecycleResult(
    ListingLifecycleResultKind Kind,
    ListingMapping? Mapping = null,
    ListingLifecycleConflict? Conflict = null,
    string? Message = null)
{
    public bool Succeeded => Kind == ListingLifecycleResultKind.Succeeded;
}
