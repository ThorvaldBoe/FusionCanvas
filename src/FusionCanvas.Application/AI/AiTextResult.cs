namespace FusionCanvas.Application.AI;

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
    public AiGenerationReceipt RoutingReceipt { get; init; } = AiGenerationReceipt.Unavailable;

    public static AiTextResult Success(
        string text,
        string requestedModel,
        string? actualModel = null,
        string? provider = null,
        string? finishReason = null,
        AiTextUsage? usage = null,
        string? generationId = null,
        AiRoutingPolicy? routing = null,
        string? actualEndpoint = null) =>
        new(true, text, requestedModel, actualModel, provider, finishReason, usage, generationId, null, null, null, null)
        {
            RoutingReceipt = new(
                requestedModel,
                routing?.Mode ?? AiRoutingMode.Automatic,
                routing?.ProviderId,
                routing?.EndpointId,
                actualModel,
                provider,
                actualEndpoint,
                finishReason,
                usage,
                usage?.Cost,
                generationId)
        };

    public static AiTextResult Failure(
        AiTextFailureKind kind,
        string message,
        string? requestedModel = null,
        TimeSpan? retryAfter = null,
        string? partialText = null,
        AiRoutingPolicy? routing = null) =>
        new(false, null, requestedModel, null, null, null, null, null, kind, message, retryAfter, partialText)
        {
            RoutingReceipt = AiGenerationReceipt.ForRequest(requestedModel, routing)
        };
}
