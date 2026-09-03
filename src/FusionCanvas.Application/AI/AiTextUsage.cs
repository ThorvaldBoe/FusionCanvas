namespace FusionCanvas.Application.AI;

public sealed record AiTextUsage(
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
    decimal? Cost);
