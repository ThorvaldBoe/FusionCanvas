namespace FusionCanvas.Application.Listings;

public enum ListingConnectionState
{
    Unknown = 0,
    Checking = 1,
    Ready = 2,
    Unavailable = 3,
    NeedsReconnection = 4
}
