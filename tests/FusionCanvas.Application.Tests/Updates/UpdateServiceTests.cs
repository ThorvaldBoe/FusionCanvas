using FusionCanvas.Application.Updates;
using FusionCanvas.Application.Versioning;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class UpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdate_ReturnsAvailableOnlyWhenManifestIsNewer()
    {
        var manifest = Manifest("0.3.0");
        var service = CreateService("0.2.0", manifest);

        var result = await service.CheckForUpdateAsync();

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Same(manifest, result.Manifest);
    }

    [Theory]
    [InlineData("0.3.0", "0.3.0", UpdateCheckStatus.UpToDate)]
    [InlineData("0.1.9", "0.2.0", UpdateCheckStatus.UpToDate)]
    public async Task CheckForUpdate_HidesEqualOrOlderReleases(string available, string current, UpdateCheckStatus expected)
    {
        var result = await CreateService(current, Manifest(available)).CheckForUpdateAsync();

        Assert.Equal(expected, result.Status);
        Assert.Null(result.Manifest);
    }

    [Fact]
    public async Task CheckForUpdate_RejectsUnsupportedManifest()
    {
        var manifest = Manifest("0.3.0") with { InstallerUri = new Uri("http://example.test/setup.exe") };

        var result = await CreateService("0.2.0", manifest).CheckForUpdateAsync();

        Assert.Equal(UpdateCheckStatus.Unsupported, result.Status);
    }

    [Theory]
    [InlineData("0.2.0")]
    [InlineData("0.1.9")]
    public async Task Download_RejectsStaleManifest(string version)
    {
        var service = CreateService("0.2.0", Manifest(version));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(Manifest(version)));
    }

    [Fact]
    public async Task Apply_RejectsStalePackageWithoutLaunchingInstaller()
    {
        var launcher = new RecordingLauncher();
        var service = new UpdateService(
            new ConstantVersionProvider("0.3.0"),
            new ConstantSource(Manifest("0.4.0")),
            new RecordingDownloader(),
            launcher,
            new RecordingLifecycle(),
            UpdatePlatform.WindowsX64);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(new VerifiedUpdatePackage(Manifest("0.3.0"), "C:\\Temp\\FusionCanvas-Setup.exe")));

        Assert.Null(launcher.InstallerPath);
    }

    [Fact]
    public async Task Apply_FlushesSchedulesInstallerAndRequestsShutdown()
    {
        var lifecycle = new RecordingLifecycle();
        var launcher = new RecordingLauncher();
        var service = new UpdateService(
            new ConstantVersionProvider("0.2.0"),
            new ConstantSource(Manifest("0.3.0")),
            new RecordingDownloader(),
            launcher,
            lifecycle,
            UpdatePlatform.WindowsX64);
        var package = new VerifiedUpdatePackage(Manifest("0.3.0"), "C:\\Temp\\FusionCanvas-Setup.exe");

        await service.ApplyAsync(package);

        Assert.Equal(new[] { "flush", "shutdown" }, lifecycle.Events);
        Assert.Equal(Environment.ProcessId, launcher.ProcessId);
        Assert.Equal(package.InstallerPath, launcher.InstallerPath);
    }

    [Fact]
    public async Task Apply_WhenFlushFails_DoesNotScheduleInstallerOrRequestShutdown()
    {
        var lifecycle = new RecordingLifecycle { FlushException = new InvalidOperationException("settings could not be saved") };
        var launcher = new RecordingLauncher();
        var service = new UpdateService(
            new ConstantVersionProvider("0.2.0"),
            new ConstantSource(Manifest("0.3.0")),
            new RecordingDownloader(),
            launcher,
            lifecycle,
            UpdatePlatform.WindowsX64);
        var package = new VerifiedUpdatePackage(Manifest("0.3.0"), "C:\\Temp\\FusionCanvas-Setup.exe");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(package));

        Assert.Equal(new[] { "flush" }, lifecycle.Events);
        Assert.Null(launcher.InstallerPath);
        Assert.Equal(0, launcher.ProcessId);
    }

    private static UpdateService CreateService(string current, UpdateManifest manifest) => new(
        new ConstantVersionProvider(current),
        new ConstantSource(manifest),
        new RecordingDownloader(),
        new RecordingLauncher(),
        new RecordingLifecycle(),
        UpdatePlatform.WindowsX64);

    private static UpdateManifest Manifest(string version) => new(
        UpdateManifestValidator.CurrentSchemaVersion,
        version,
        UpdatePlatform.WindowsX64,
        new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v" + version + "/FusionCanvas-" + version + "-win-x64-Setup.exe"),
        new string('A', 64),
        new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v" + version),
        new string('B', 64));

    private sealed class ConstantVersionProvider(string version) : IApplicationVersionProvider
    {
        public ApplicationVersionInfo GetVersion() => new(version, version, "test");
    }

    private sealed class ConstantSource(UpdateManifest manifest) : IUpdateSource
    {
        public Task<UpdateManifest?> GetLatestAsync(CancellationToken cancellationToken = default) => Task.FromResult<UpdateManifest?>(manifest);
    }

    private sealed class RecordingDownloader : IUpdatePackageDownloader
    {
        public Task<VerifiedUpdatePackage> DownloadAndVerifyAsync(UpdateManifest manifest, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new VerifiedUpdatePackage(manifest, "C:\\Temp\\FusionCanvas-Setup.exe"));
    }

    private sealed class RecordingLauncher : IUpdateInstallerLauncher
    {
        public int ProcessId { get; private set; }
        public string? InstallerPath { get; private set; }

        public Task ScheduleAfterExitAsync(string installerPath, int processId, CancellationToken cancellationToken = default)
        {
            InstallerPath = installerPath;
            ProcessId = processId;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLifecycle : IUpdateApplicationLifecycle
    {
        public List<string> Events { get; } = new();
        public Exception? FlushException { get; init; }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("flush");
            return FlushException is null ? Task.CompletedTask : Task.FromException(FlushException);
        }

        public Task RequestShutdownAsync(CancellationToken cancellationToken = default) { Events.Add("shutdown"); return Task.CompletedTask; }
    }
}
