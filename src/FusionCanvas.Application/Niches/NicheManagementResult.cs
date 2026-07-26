namespace FusionCanvas.Application.Niches;

public sealed record NicheManagementResult(
    bool Succeeded,
    string? Error,
    NicheSummary? Niche,
    NicheManagementState State)
{
    public static NicheManagementResult Success(NicheSummary? niche, NicheManagementState state) =>
        new(true, null, niche, state);

    public static NicheManagementResult Failure(string error, NicheManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Niche operation failed." : error, null, state);
}
