namespace FusionCanvas.Domain.Listings;

public enum ListingSyncStatus
{
    NotConnected = 0,
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Conflict = 4
}
