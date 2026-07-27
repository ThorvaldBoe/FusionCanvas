namespace FusionCanvas.Application.AI;

public sealed record AiReasoningCapabilities(
    bool Mandatory,
    bool DefaultEnabled,
    IReadOnlyList<string> SupportedEfforts,
    string? DefaultEffort,
    bool SupportsTokenBudget);

public sealed record AiModelDescriptor(
    string Id,
    string Name,
    string? Author,
    string? Description,
    IReadOnlyList<string> InputModalities,
    IReadOnlyList<string> OutputModalities,
    IReadOnlyList<string> SupportedParameters,
    int? ContextLength,
    int? MaxCompletionTokens,
    decimal? PromptPrice,
    decimal? CompletionPrice,
    bool ZeroDataRetentionCompatible,
    AiReasoningCapabilities? Reasoning);

public sealed record AiModelCatalog(
    bool RequireZeroDataRetention,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<AiModelDescriptor> Models,
    bool IsStale = false);
