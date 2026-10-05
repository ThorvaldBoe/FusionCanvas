namespace FusionCanvas.Application.Updates;

public enum UpdateCheckStatus
{
    Unsupported,
    UpToDate,
    UpdateAvailable
}

public sealed record UpdateCheckResult(UpdateCheckStatus Status, UpdateManifest? Manifest)
{
    public bool IsUpdateAvailable => Status == UpdateCheckStatus.UpdateAvailable && Manifest is not null;
}
