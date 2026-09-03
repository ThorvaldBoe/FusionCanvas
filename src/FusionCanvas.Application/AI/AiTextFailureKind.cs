namespace FusionCanvas.Application.AI;

public enum AiTextFailureKind
{
    InvalidRequest,
    NotConfigured,
    CredentialUnavailable,
    InvalidConfiguration,
    Authentication,
    InsufficientCredit,
    RateLimited,
    Blocked,
    NoEligibleProvider,
    ModelUnavailable,
    NetworkFailure,
    Timeout,
    IncompleteGeneration,
    InvalidProviderResponse,
    ProviderFailure
}
