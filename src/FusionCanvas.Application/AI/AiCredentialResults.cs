namespace FusionCanvas.Application.AI;

public enum AiCredentialStateKind
{
    NotFound,
    Available,
    Unavailable,
    Locked,
    AccessDenied,
    InvalidStoredValue
}

public sealed record AiCredentialReadResult(
    AiCredentialStateKind State,
    string? Secret = null,
    string? Message = null)
{
    public static AiCredentialReadResult NotFound { get; } = new(AiCredentialStateKind.NotFound);
    public static AiCredentialReadResult Available(string secret) => new(AiCredentialStateKind.Available, secret);
    public static AiCredentialReadResult Failure(AiCredentialStateKind state, string message) => new(state, Message: message);
}

public sealed record AiCredentialOperationResult(bool Succeeded, string? Message = null)
{
    public static AiCredentialOperationResult Success { get; } = new(true);
    public static AiCredentialOperationResult Failed(string message) => new(false, message);
}

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

public sealed record AiCredentialValidationResult(
    AiCredentialValidationKind Kind,
    string? Message = null,
    decimal? LimitRemaining = null,
    TimeSpan? RetryAfter = null);
