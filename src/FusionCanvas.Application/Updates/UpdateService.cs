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

    public async Task<UpdateCheckResult> CheckForUpdateAsync(UpdateTrack track = UpdateTrack.Stable, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_platform, UpdatePlatform.WindowsX64, StringComparison.Ordinal))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);
        }

        var currentVersion = _versionProvider.GetVersion();
        if (!Enum.IsDefined(track) || !UpdateProductVersion.TryParse(currentVersion.ProductVersion, out var current))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);
        }

        var releases = await _source.GetReleasesAsync(cancellationToken).ConfigureAwait(false);
        var candidate = releases
            .Where(release => UpdateProductVersion.TryParse(release.ProductVersion, out var version)
                && string.Equals(release.ProductVersion, version.ToString(), StringComparison.Ordinal)
                && (track == UpdateTrack.Preview || release.Track == UpdateTrack.Stable)
                && (!version.IsPreview || release.Track == UpdateTrack.Preview))
            .Where(release => UpdateProductVersion.TryParse(release.ProductVersion, out var version) && version.CompareTo(current) > 0)
            .OrderByDescending(release => UpdateProductVersion.TryParse(release.ProductVersion, out var version) ? version : default)
            .ThenBy(release => release.TagName, StringComparer.Ordinal)
            .FirstOrDefault();
        if (candidate is null)
        {
            return new UpdateCheckResult(UpdateCheckStatus.UpToDate, null);
        }

        var manifest = await _source.GetManifestAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (!UpdateManifestValidator.IsSupported(manifest, _platform) || !UpdateManifestValidator.MatchesRelease(manifest!, candidate))
            return new UpdateCheckResult(UpdateCheckStatus.Unsupported, null);

        return new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, manifest);
    }

    public async Task<VerifiedUpdatePackage> DownloadAsync(
        UpdateManifest manifest,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!UpdateManifestValidator.IsSupported(manifest, _platform)
            || !IsNewerThanCurrent(manifest))
        {
            throw new InvalidOperationException("The update manifest is not supported by this application.");
        }

        return await _downloader.DownloadAndVerifyAsync(manifest, progress, cancellationToken).ConfigureAwait(false);
    }

    public async Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (!UpdateManifestValidator.IsSupported(package.Manifest, _platform)
            || !IsNewerThanCurrent(package.Manifest)
            || string.IsNullOrWhiteSpace(package.InstallerPath))
        {
            throw new InvalidOperationException("The verified update package is not valid.");
        }

        await _lifecycle.FlushAsync(cancellationToken).ConfigureAwait(false);
        await _launcher.ScheduleAfterExitAsync(package.InstallerPath, Environment.ProcessId, cancellationToken).ConfigureAwait(false);
        await _lifecycle.RequestShutdownAsync(cancellationToken).ConfigureAwait(false);
    }

    private bool IsNewerThanCurrent(UpdateManifest manifest) =>
        UpdateProductVersion.TryParse(_versionProvider.GetVersion().ProductVersion, out var current)
        && UpdateProductVersion.TryParse(manifest.ProductVersion, out var available)
        && available.CompareTo(current) > 0;
}
