namespace FusionCanvas.Application.AI;

public sealed record AiProfileSettings(
    string? ModelId,
    int? MaxCompletionTokens,
    double? Temperature,
    double? TopP,
    int? TopK,
    double? MinP,
    double? TopA,
    double? FrequencyPenalty,
    double? PresencePenalty,
    double? RepetitionPenalty,
    int? Seed,
    string[] StopSequences,
    AiReasoningSettings Reasoning)
{
    public static AiProfileSettings Empty { get; } = new(
        ModelId: null,
        MaxCompletionTokens: null,
        Temperature: null,
        TopP: null,
        TopK: null,
        MinP: null,
        TopA: null,
        FrequencyPenalty: null,
        PresencePenalty: null,
        RepetitionPenalty: null,
        Seed: null,
        StopSequences: [],
        Reasoning: AiReasoningSettings.ProviderDefault);
}
