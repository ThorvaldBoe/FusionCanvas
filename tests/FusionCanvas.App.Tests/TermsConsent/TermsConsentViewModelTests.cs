using FusionCanvas.App.TermsConsent;
using FusionCanvas.App.Commands;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentViewModelTests
{
    [Fact]
    public void AgreeCommand_RemainsDisabledUntilAllFourAcknowledgementsAreSelected()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.CanAgree);
        viewModel.FusionCanvasTermsSelected = true;
        viewModel.PrintifyTermsSelected = true;
        viewModel.ShopifyTermsSelected = true;

        Assert.False(viewModel.CanAgree);

        viewModel.IntellectualPropertySelected = true;

        Assert.True(viewModel.CanAgree);
        Assert.True(viewModel.AgreeCommand.CanExecute(null));
    }

    [Fact]
    public async Task AgreeCommand_SavesVersionedRecordAndRaisesAccepted()
    {
        var store = new RecordingStore();
        var viewModel = CreateViewModel(store);
        ApplicationSettings? accepted = null;
        viewModel.Accepted += settings => accepted = settings;
        SelectAll(viewModel);

        viewModel.AgreeCommand.Execute(null);
        await ((FusionCanvas.App.Commands.AsyncRelayCommand)viewModel.AgreeCommand).ExecutionTask!;

        Assert.NotNull(store.LastSaved.TermsConsent);
        Assert.True(FusionCanvas.Application.TermsConsent.TermsConsentPolicy.IsCurrent(store.LastSaved.TermsConsent));
        Assert.Same(store.LastSaved, accepted);
        Assert.True(viewModel.IsCompleted);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task AgreeCommand_SaveFailureKeepsSelectionsAndReportsActionableError()
    {
        var viewModel = CreateViewModel(new FailingStore());
        SelectAll(viewModel);

        viewModel.AgreeCommand.Execute(null);
        await ((FusionCanvas.App.Commands.AsyncRelayCommand)viewModel.AgreeCommand).ExecutionTask!;

        Assert.True(viewModel.FusionCanvasTermsSelected);
        Assert.True(viewModel.IntellectualPropertySelected);
        Assert.False(viewModel.IsCompleted);
        Assert.Contains("disk is full", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestQuit_DuringSave_DoesNotRaceThePendingAcceptance()
    {
        var saveStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCompletion = new TaskCompletionSource<ApplicationSettingsSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = CreateViewModel(new BlockingStore(saveStarted, saveCompletion));
        var quitRequested = false;
        viewModel.QuitRequested += () => quitRequested = true;
        SelectAll(viewModel);

        var command = Assert.IsType<AsyncRelayCommand>(viewModel.AgreeCommand);
        command.Execute(null);
        var execution = command.ExecutionTask;
        Assert.NotNull(execution);
        await saveStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            Assert.True(viewModel.IsSaving);
            Assert.False(viewModel.QuitCommand.CanExecute(null));

            viewModel.RequestQuit();

            Assert.False(quitRequested);
        }
        finally
        {
            saveCompletion.TrySetResult(ApplicationSettingsSaveResult.Success);
            await execution!;
        }
    }

    [Fact]
    public async Task StartupCancellation_CancelsSaveAndPendingSaveWaitCompletes()
    {
        using var startupCancellation = new CancellationTokenSource();
        var saveStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCompletion = new TaskCompletionSource<ApplicationSettingsSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = CreateViewModel(
            new BlockingStore(saveStarted, saveCompletion),
            startupCancellationToken: startupCancellation.Token);
        SelectAll(viewModel);

        var command = Assert.IsType<AsyncRelayCommand>(viewModel.AgreeCommand);
        command.Execute(null);
        await saveStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(startupCancellation.Token, await saveStarted.Task);
        startupCancellation.Cancel();

        await viewModel.WaitForPendingSaveAsync().WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsSaving);
        Assert.False(viewModel.IsCompleted);
        Assert.Contains("cancelled", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PolicyLinksUseOfficialProviderAddresses()
    {
        var launcher = new RecordingLauncher();
        var viewModel = CreateViewModel(linkLauncher: launcher);

        viewModel.OpenPrintifyTermsCommand.Execute(null);
        viewModel.OpenPrintifyIpPolicyCommand.Execute(null);
        viewModel.OpenShopifyTermsCommand.Execute(null);
        viewModel.OpenShopifyAupCommand.Execute(null);
        viewModel.OpenShopifyApiTermsCommand.Execute(null);

        Assert.Equal(5, launcher.Opened.Count);
        Assert.Contains("printify.com/terms-of-service", launcher.Opened[0].ToString());
        Assert.Contains("shopify.com/legal/api-terms", launcher.Opened[^1].ToString());
    }

    private static TermsConsentViewModel CreateViewModel(
        IApplicationSettingsStore? store = null,
        IExternalLinkLauncher? linkLauncher = null,
        CancellationToken startupCancellationToken = default) =>
        new(
            ApplicationSettings.Default,
            store ?? new RecordingStore(),
            "Offline policy text",
            linkLauncher,
            startupCancellationToken);

    private static void SelectAll(TermsConsentViewModel viewModel)
    {
        viewModel.FusionCanvasTermsSelected = true;
        viewModel.PrintifyTermsSelected = true;
        viewModel.ShopifyTermsSelected = true;
        viewModel.IntellectualPropertySelected = true;
    }

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public ApplicationSettings LastSaved { get; private set; } = ApplicationSettings.Default;

        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(LastSaved));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            LastSaved = settings;
            return Task.FromResult(ApplicationSettingsSaveResult.Success);
        }
    }

    private sealed class FailingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Failed("The disk is full."));
    }

    private sealed class BlockingStore(
        TaskCompletionSource<CancellationToken> saveStarted,
        TaskCompletionSource<ApplicationSettingsSaveResult> saveCompletion) : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public async Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default)
        {
            saveStarted.TrySetResult(cancellationToken);
            return await saveCompletion.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class RecordingLauncher : IExternalLinkLauncher
    {
        public List<Uri> Opened { get; } = [];

        public void Open(Uri uri) => Opened.Add(uri);
    }
}
