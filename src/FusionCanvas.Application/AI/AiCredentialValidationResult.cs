namespace FusionCanvas.Application.AI;

public sealed record AiCredentialValidationResult(
    AiCredentialValidationKind Kind,
    string? Message = null,
    decimal? LimitRemaining = null,
    TimeSpan? RetryAfter = null);
