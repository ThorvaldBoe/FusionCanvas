namespace FusionCanvas.Application.DesignFiles;

public sealed record DesignStageResult
(
    bool Succeeded,
    string? Error,
    DesignStageState? State)
{
    public static DesignStageResult Success(DesignStageState state) =>
        new(true, null, state);

    public static DesignStageResult Failure(string error, DesignStageState? state = null) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Design stage operation failed." : error, state);
}