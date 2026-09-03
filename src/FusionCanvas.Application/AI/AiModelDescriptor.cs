namespace FusionCanvas.Application.AI;

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
