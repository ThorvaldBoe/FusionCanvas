using FusionCanvas.App.Commands;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Updates;

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
        var service = new FakeUpdateService { Exception = new HttpRequestException("offline") };
        var vm = new UpdateViewModel(service);

        vm.CheckCommand.Execute(null);
        await ((AsyncRelayCommand)vm.CheckCommand).ExecutionTask!.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(UpdatePresentationStatus.Error, vm.Status);
        Assert.True(vm.HasError);
        Assert.True(vm.CheckCommand.CanExecute(null));
    }

    private static UpdateManifest Manifest(string version) => new(
        1, version, UpdatePlatform.WindowsX64,
        new Uri("https://example.test/setup.exe"), new string('A', 64), new Uri("https://example.test/release"));

    private sealed class FakeUpdateService : IUpdateService
    {
        public UpdateCheckResult CheckResult { get; set; } = new(UpdateCheckStatus.UpToDate, null);
        public VerifiedUpdatePackage? Package { get; set; }
        public Exception? Exception { get; set; }

        public Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(CheckResult) : Task.FromException<UpdateCheckResult>(Exception);

        public Task<VerifiedUpdatePackage> DownloadAsync(UpdateManifest manifest, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Package is not null ? Task.FromResult(Package) : Task.FromException<VerifiedUpdatePackage>(new InvalidOperationException());

        public Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
