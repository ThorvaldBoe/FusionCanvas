namespace FusionCanvas.Application.Listings;

public enum ListingSynchronizationState
{
    Unmapped = 0,
    Draft = 1,
    Synchronized = 2,
    RemoteChanged = 3,
    Missing = 4,
    Pending = 5,
    Uncertain = 6,
    Failed = 7,
    Archived = 8,
    Deleted = 9
}
