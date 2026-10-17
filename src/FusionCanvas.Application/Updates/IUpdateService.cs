namespace FusionCanvas.Application.Updates;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdateAsync(UpdateTrack track = UpdateTrack.Stable, CancellationToken cancellationToken = default);

    Task<VerifiedUpdatePackage> DownloadAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default);
}
