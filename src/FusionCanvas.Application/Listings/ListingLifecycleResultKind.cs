namespace FusionCanvas.Application.Listings;

public enum ListingLifecycleResultKind
{
    Succeeded = 0,
    Unavailable = 1,
    Conflict = 2,
    Failed = 3,
    Uncertain = 4,
    Missing = 5,
    NotAllowed = 6
}
