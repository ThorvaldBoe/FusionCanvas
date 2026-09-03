namespace FusionCanvas.Application.AI;

public sealed record AiReasoningSettings(
    AiReasoningMode Mode,
    string? Effort = null,
    int? TokenBudget = null)
{
    public static AiReasoningSettings ProviderDefault { get; } = new(AiReasoningMode.ProviderDefault);
}
