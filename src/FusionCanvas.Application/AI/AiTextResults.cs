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

public sealed record AiTextUsage(
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
    decimal? Cost);

public sealed record AiTextResult(
    bool Succeeded,
    string? Text,
    string? RequestedModel,
    string? ActualModel,
    string? Provider,
    string? FinishReason,
    AiTextUsage? Usage,
    string? GenerationId,
    AiTextFailureKind? FailureKind,
    string? Message,
    TimeSpan? RetryAfter,
    string? PartialText)
{
    public static AiTextResult Success(
        string text,
        string requestedModel,
        string? actualModel = null,
        string? provider = null,
        string? finishReason = null,
        AiTextUsage? usage = null,
        string? generationId = null) =>
        new(true, text, requestedModel, actualModel, provider, finishReason, usage, generationId, null, null, null, null);

    public static AiTextResult Failure(
        AiTextFailureKind kind,
        string message,
        string? requestedModel = null,
        TimeSpan? retryAfter = null,
        string? partialText = null) =>
        new(false, null, requestedModel, null, null, null, null, null, kind, message, retryAfter, partialText);
}

public sealed record AiProviderTextRequest(
    string ApiKey,
    string ModelId,
    IReadOnlyList<AiTextMessage> Messages,
    AiProfileSettings Profile,
    bool RequireZeroDataRetention);
