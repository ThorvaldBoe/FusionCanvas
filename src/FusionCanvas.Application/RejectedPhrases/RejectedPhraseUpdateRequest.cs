namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseUpdateRequest(
    Guid Id,
    string Text,
    string? Reason,
    RejectedPhraseScope Scope,
    string? SearchText = null);
