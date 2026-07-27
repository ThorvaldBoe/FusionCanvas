namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneLibraryResult(
    bool Succeeded,
    string? Error,
    SnowcloneLibraryState State,
    SnowcloneSummary? AffectedSnowclone,
    int AddedCount,
    int SkippedCount)
{
    public static SnowcloneLibraryResult Success(
        SnowcloneLibraryState state,
        SnowcloneSummary? affectedSnowclone = null,
        int addedCount = 0,
        int skippedCount = 0) =>
        new(true, null, state, affectedSnowclone, addedCount, skippedCount);

    public static SnowcloneLibraryResult Failure(string error, SnowcloneLibraryState state) =>
        new(false, error, state, null, 0, 0);
}
