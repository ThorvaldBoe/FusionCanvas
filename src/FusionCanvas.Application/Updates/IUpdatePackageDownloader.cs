namespace FusionCanvas.Application.Updates;

public interface IUpdatePackageDownloader
{
    Task<VerifiedUpdatePackage> DownloadAndVerifyAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
