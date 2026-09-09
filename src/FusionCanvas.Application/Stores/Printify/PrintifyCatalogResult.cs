namespace FusionCanvas.Application.Stores.Printify;

public enum PrintifyCatalogResultKind
{
    Succeeded,
    Empty,
    InvalidKey,
    PermissionDenied,
    RateLimited,
    NetworkFailure,
    UnexpectedResponse,
    InvalidRequest
}
