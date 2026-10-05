namespace FusionCanvas.Application.AI;

public sealed record AiModelEndpointDescriptor(
    string ModelId,
    string ProviderId,
    string ProviderName,
    string EndpointId,
    string? EndpointName,
    int? ContextLength,
    int? MaxCompletionTokens,
    IReadOnlyList<string> SupportedParameters,
    bool ZeroDataRetentionCompatible,
    decimal? PromptPrice,
    decimal? CompletionPrice,
    double? LatencyMilliseconds,
    double? ThroughputTokensPerSecond,
    bool IsStale = false);
