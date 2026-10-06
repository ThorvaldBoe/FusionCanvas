using FusionCanvas.App.Commands;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Updates;
using System.Windows.Input;

namespace FusionCanvas.App.Tests.Settings;

public sealed class UpdateViewModelTests
{
    [Fact]
    public async Task CheckAndDownload_ExposeActionableStates()
    {
        var service = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, Manifest("0.3.0")),
            Package = new VerifiedUpdatePackage(Manifest("0.3.0"), "C:\\Temp\\setup.exe")
        };
        var vm = new UpdateViewModel(service);

        vm.CheckCommand.Execute(null);
        await ((AsyncRelayCommand)vm.CheckCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(UpdatePresentationStatus.UpdateAvailable, vm.Status);
        Assert.True(vm.IsUpdateActionVisible);

        vm.UpdateCommand.Execute(null);
        await ((AsyncRelayCommand)vm.UpdateCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(UpdatePresentationStatus.ReadyToInstall, vm.Status);
        Assert.True(vm.IsReadyToInstall);
    }

    [Fact]
    public async Task CheckFailure_IsVisibleAndRetryable()
    {
        const string rawMessage = "https://updates.example.test/?token=secret";
        var service = new FakeUpdateService { CheckException = new HttpRequestException(rawMessage) };
        var vm = new UpdateViewModel(service);

        vm.CheckCommand.Execute(null);
        await ((AsyncRelayCommand)vm.CheckCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(UpdatePresentationStatus.Error, vm.Status);
        Assert.True(vm.HasError);
        Assert.True(vm.CheckCommand.CanExecute(null));
        Assert.Equal("We couldn't check for updates. Check your internet connection and try again.", vm.ErrorMessage);
        Assert.DoesNotContain(rawMessage, vm.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MalformedManifest_UsesSafeGuidance()
    {
        const string rawMessage = "secret response payload";
        var vm = new UpdateViewModel(new FakeUpdateService
        {
            CheckException = new System.Text.Json.JsonException(rawMessage)
        });

        vm.CheckCommand.Execute(null);
        await ((AsyncRelayCommand)vm.CheckCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal("The update information couldn't be read. Try again later.", vm.ErrorMessage);
        Assert.DoesNotContain(rawMessage, vm.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownloadFailure_UsesSafeFilesystemGuidanceAndOffersRetry()
    {
        const string rawMessage = "C:\\Users\\owner\\AppData\\Local\\FusionCanvas\\updates\\setup.exe";
        var service = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, Manifest("0.3.0")),
            DownloadException = new IOException(rawMessage)
        };
        var vm = new UpdateViewModel(service);

        await ExecuteAsync(vm.CheckCommand);
        await ExecuteAsync(vm.UpdateCommand);

        Assert.Equal(UpdatePresentationStatus.Error, vm.Status);
        Assert.True(vm.IsDownloadRetryAvailable);
        Assert.True(vm.IsDownloadCommandVisible);
        Assert.True(vm.UpdateCommand.CanExecute(null));
        Assert.Equal("We couldn't save the update. Check available disk space and permissions, then try again.", vm.ErrorMessage);
        Assert.DoesNotContain(rawMessage, vm.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChecksumFailure_UsesSafeVerificationGuidance()
    {
        const string rawMessage = "checksum failed for https://updates.example.test/setup.exe?token=secret";
        var service = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, Manifest("0.3.0")),
            DownloadException = new InvalidOperationException(rawMessage)
        };
        var vm = new UpdateViewModel(service);

        await ExecuteAsync(vm.CheckCommand);
        await ExecuteAsync(vm.UpdateCommand);

        Assert.Equal("The downloaded update could not be verified. Try downloading it again later.", vm.ErrorMessage);
        Assert.DoesNotContain(rawMessage, vm.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownloadCancellation_ReturnsToAvailableWithoutError()
    {
        var service = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, Manifest("0.3.0")),
            BlockDownload = true
        };
        var vm = new UpdateViewModel(service);

        await ExecuteAsync(vm.CheckCommand);
        vm.UpdateCommand.Execute(null);
        await service.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(UpdatePresentationStatus.Downloading, vm.Status);
        Assert.True(vm.IsCancelVisible);
        Assert.True(vm.CancelCommand.CanExecute(null));

        vm.CancelCommand.Execute(null);
        await ((AsyncRelayCommand)vm.UpdateCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(UpdatePresentationStatus.UpdateAvailable, vm.Status);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsDownloadRetryAvailable);
        Assert.False(vm.IsCancelVisible);
        Assert.True(vm.UpdateCommand.CanExecute(null));
    }

    [Fact]
    public async Task InstallFailure_UsesSafeInstallerGuidanceAndKeepsInstallAvailable()
    {
        const string rawMessage = "The installer process could not be started for C:\\Users\\owner\\secret.exe";
        var service = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, Manifest("0.3.0")),
            Package = new VerifiedUpdatePackage(Manifest("0.3.0"), "C:\\Temp\\setup.exe"),
            ApplyException = new System.ComponentModel.Win32Exception(5, rawMessage)
        };
        var vm = new UpdateViewModel(service);

        await ExecuteAsync(vm.CheckCommand);
        await ExecuteAsync(vm.UpdateCommand);
        await ExecuteAsync(vm.InstallCommand);

        Assert.Equal(UpdatePresentationStatus.ReadyToInstall, vm.Status);
        Assert.True(vm.HasError);
        Assert.True(vm.InstallCommand.CanExecute(null));
        Assert.Equal("We couldn't start the installer. Check that FusionCanvas can access its installation folder, then try again.", vm.ErrorMessage);
        Assert.DoesNotContain(rawMessage, vm.ErrorMessage, StringComparison.Ordinal);
    }

    private static async Task ExecuteAsync(ICommand command)
    {
        command.Execute(null);
        await ((AsyncRelayCommand)command).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static UpdateManifest Manifest(string version) => new(
        UpdateManifestValidator.CurrentSchemaVersion,
        version,
        UpdatePlatform.WindowsX64,
        new Uri($"https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v{version}/FusionCanvas-{version}-win-x64-Setup.exe"),
        new string('A', 64),
        new Uri($"https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v{version}"),
        new string('B', 64));

    private sealed class FakeUpdateService : IUpdateService
    {
        public UpdateCheckResult CheckResult { get; set; } = new(UpdateCheckStatus.UpToDate, null);
        public VerifiedUpdatePackage? Package { get; set; }
        public Exception? CheckException { get; set; }
        public Exception? DownloadException { get; set; }
        public Exception? ApplyException { get; set; }
        public bool BlockDownload { get; set; }
        public TaskCompletionSource<bool> DownloadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
            CheckException is null ? Task.FromResult(CheckResult) : Task.FromException<UpdateCheckResult>(CheckException);

        public async Task<VerifiedUpdatePackage> DownloadAsync(UpdateManifest manifest, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (BlockDownload)
            {
                DownloadStarted.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            if (DownloadException is not null)
            {
                throw DownloadException;
            }
            return Package ?? throw new InvalidOperationException();
        }

        public Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default) =>
            ApplyException is null ? Task.CompletedTask : Task.FromException(ApplyException);
    }
}
