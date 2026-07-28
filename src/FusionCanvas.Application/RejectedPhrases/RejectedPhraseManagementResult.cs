namespace FusionCanvas.Application.RejectedPhrases;

public sealed record RejectedPhraseManagementResult(
    bool Succeeded,
    string? Error,
    RejectedPhraseManagementState State,
    RejectedPhraseSummary? AffectedSummary = null)
{
    public static RejectedPhraseManagementResult Success(
        RejectedPhraseManagementState state,
        RejectedPhraseSummary? affectedSummary = null) =>
        new(true, null, state, affectedSummary);

    public static RejectedPhraseManagementResult Failure(string error, RejectedPhraseManagementState state) =>
        new(false, error, state, null);
}
