namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseManagementState(
    IReadOnlyList<RejectedPhraseSummary> AllRejections,
    IReadOnlyList<RejectedPhraseSummary> VisibleRejections,
    RejectedPhraseScope Scope,
    string SearchText)
{
    public static RejectedPhraseManagementState Empty(RejectedPhraseScope scope) =>
        new([], [], scope, string.Empty);
}
