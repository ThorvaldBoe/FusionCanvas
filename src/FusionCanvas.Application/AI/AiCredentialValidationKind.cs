namespace FusionCanvas.Application.AI;

public enum AiCredentialValidationKind
{
    Valid,
    Invalid,
    ManagementKey,
    PermissionDenied,
    RateLimited,
    NetworkFailure,
    ServiceUnavailable
}
