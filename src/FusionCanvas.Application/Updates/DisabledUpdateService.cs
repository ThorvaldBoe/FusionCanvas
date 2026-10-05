namespace FusionCanvas.Application.Updates;

public sealed class DisabledUpdateService : IUpdateService
{
    public static DisabledUpdateService Instance { get; } = new();

    public Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new UpdateCheckResult(UpdateCheckStatus.Unsupported, null));

    public Task<VerifiedUpdatePackage> DownloadAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.FromException<VerifiedUpdatePackage>(new InvalidOperationException("Updates are unavailable."));

    public Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException("Updates are unavailable."));
}
