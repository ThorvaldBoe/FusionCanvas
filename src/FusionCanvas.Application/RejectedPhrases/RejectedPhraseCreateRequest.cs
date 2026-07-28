namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseCreateRequest(
    string Text,
    string? Reason,
    RejectedPhraseScope Scope,
    string? SearchText = null);
