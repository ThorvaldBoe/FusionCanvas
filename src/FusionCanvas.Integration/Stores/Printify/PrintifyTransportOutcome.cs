namespace FusionCanvas.Integration.Stores.Printify;

public enum PrintifyTransportOutcome
{
    Succeeded = 0, InvalidCredential = 1, PermissionDenied = 2, NotFound = 3,
    RateLimited = 4, Locked = 5, NetworkFailure = 6, UnexpectedResponse = 7
}
