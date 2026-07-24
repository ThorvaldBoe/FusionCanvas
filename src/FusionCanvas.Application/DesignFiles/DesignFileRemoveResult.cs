namespace FusionCanvas.Application.DesignFiles;

public sealed record DesignFileRemoveResult(bool Succeeded, string? Error)
{
    public static DesignFileRemoveResult Success() => new(true, null);
    public static DesignFileRemoveResult Failure(string error) => new(false, error);
}
