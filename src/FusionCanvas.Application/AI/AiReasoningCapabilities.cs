namespace FusionCanvas.Application.AI;

public sealed record AiReasoningCapabilities(
    bool Mandatory,
    bool DefaultEnabled,
    IReadOnlyList<string> SupportedEfforts,
    string? DefaultEffort,
    bool SupportsTokenBudget);
