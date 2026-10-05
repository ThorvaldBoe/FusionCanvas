using FusionCanvas.Application.Versioning;

namespace FusionCanvas.Application.Updates;

public sealed class UpdateService : IUpdateService
{
    private readonly IApplicationVersionProvider _versionProvider;
    private readonly IUpdateSource _source;
    private readonly IUpdatePackageDownloader _downloader;
    private readonly IUpdateInstallerLauncher _launcher;
    private readonly IUpdateApplicationLifecycle _lifecycle;
    private readonly string _platform;

    public UpdateService(
        IApplicationVersionProvider versionProvider,
        IUpdateSource source,
        IUpdatePackageDownloader downloader,
        IUpdateInstallerLauncher launcher,
        IUpdateApplicationLifecycle lifecycle,
        string platform)
    {
        _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _platform = string.IsNullOrWhiteSpace(platform) ? throw new ArgumentException("Platform is required.", nameof(platform)) : platform;
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_platform, UpdatePlatform.WindowsX64, StringComparison.Ordinal))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);
        }

        var currentVersion = _versionProvider.GetVersion();
        if (!StableProductVersion.TryParse(currentVersion.ProductVersion, out var current))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);
        }

        var manifest = await _source.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        if (!UpdateManifestValidator.IsSupported(manifest, _platform)
            || !StableProductVersion.TryParse(manifest!.ProductVersion, out var available))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);
        }

        return available.CompareTo(current) > 0
            ? new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, manifest)
            : new UpdateCheckResult(UpdateCheckStatus.UpToDate, null);
    }

    public Task<VerifiedUpdatePackage> DownloadAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!UpdateManifestValidator.IsSupported(manifest, _platform))
        {
            throw new InvalidOperationException("The update manifest is not supported by this application.");
        }

        return _downloader.DownloadAndVerifyAsync(manifest, progress, cancellationToken);
    }

    public async Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (!UpdateManifestValidator.IsSupported(package.Manifest, _platform)
            || string.IsNullOrWhiteSpace(package.InstallerPath))
        {
            throw new InvalidOperationException("The verified update package is not valid.");
        }

        await _lifecycle.FlushAsync(cancellationToken).ConfigureAwait(false);
        await _launcher.ScheduleAfterExitAsync(package.InstallerPath, Environment.ProcessId, cancellationToken).ConfigureAwait(false);
        await _lifecycle.RequestShutdownAsync(cancellationToken).ConfigureAwait(false);
    }
}
