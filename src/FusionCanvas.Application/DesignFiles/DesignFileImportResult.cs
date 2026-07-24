namespace FusionCanvas.Application.DesignFiles;

public sealed record DesignFileImportResult(bool Succeeded, string? Error, DesignFileSummary? File)
{
    public static DesignFileImportResult Success(DesignFileSummary file) => new(true, null, file);
    public static DesignFileImportResult Failure(string error) => new(false, error, null);
}
