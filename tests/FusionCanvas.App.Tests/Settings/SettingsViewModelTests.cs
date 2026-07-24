using FusionCanvas.App.Settings;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Settings;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Tests;

public class SettingsViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_AppliesInitialThemeAndDefaultsToGeneralSection()
    {
        var theme = new FakeThemeController();
        var vm = new SettingsViewModel(new RecordingStore(), theme, new ApplicationSettings(DarkMode: true), loadWarning: null);

        Assert.Equal(SettingsSection.General, vm.SelectedSection);
        Assert.True(vm.IsDarkMode);
        Assert.False(vm.IsOpen);
        Assert.Equal("No workspace", vm.WorkspaceName);
        Assert.Equal(new[] { true }, theme.Calls);
    }

    [Fact]
    public void Open_ResetsToGeneralSectionAndShowsWindow()
    {
        var vm = NewViewModel();
        vm.SelectedSection = SettingsSection.Workspace;

        vm.OpenCommand.Execute(null);

        Assert.True(vm.IsOpen);
        Assert.Equal(SettingsSection.General, vm.SelectedSection);
    }

    [Fact]
    public void Close_HidesWindow()
    {
        var vm = NewViewModel();
        vm.OpenCommand.Execute(null);

        vm.CloseCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void Reopen_ResetsToGeneralAfterWorkspaceSection()
    {
        var vm = NewViewModel();
        vm.OpenCommand.Execute(null);
        vm.SelectedSection = SettingsSection.Workspace;
        vm.CloseCommand.Execute(null);

        vm.OpenCommand.Execute(null);

        Assert.Equal(SettingsSection.General, vm.SelectedSection);
    }

    [Fact]
    public async Task IsDarkMode_AppliesThemeImmediatelyAndQueuesSave()
    {
        var theme = new FakeThemeController();
        var store = new RecordingStore();
        var vm = new SettingsViewModel(store, theme, ApplicationSettings.Default, loadWarning: null);

        vm.IsDarkMode = true;
        await vm.FlushAsync();

        Assert.True(theme.Calls[^1]);
        Assert.True(store.LastSaved.DarkMode);
    }

    [Fact]
    public void LoadWarning_IsExposedAsInlineMessage()
    {
        var vm = NewViewModel(loadWarning: "The saved appearance preference could not be read.");

        Assert.Equal("The saved appearance preference could not be read.", vm.ErrorMessage);
        Assert.True(vm.HasMessage);
    }

    [Fact]
    public async Task SuccessfulSave_ClearsLoadWarningMessage()
    {
        var vm = NewViewModel(loadWarning: "unread");
        vm.IsDarkMode = true;
        await vm.FlushAsync();

        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasMessage);
    }

    [Fact]
    public async Task SaveFailure_ReportsMessageButKeepsSelectedTheme()
    {
        var theme = new FakeThemeController();
        var vm = new SettingsViewModel(new FailingStore(), theme, ApplicationSettings.Default, loadWarning: null);

        vm.IsDarkMode = true;
        await vm.FlushAsync();

        Assert.True(theme.Calls[^1]);
        Assert.NotNull(vm.ErrorMessage);
        Assert.True(vm.HasMessage);
    }

    [Fact]
    public async Task RapidToggle_RetainsMostRecentValueForNextStart()
    {
        var store = new RecordingStore();
        var vm = new SettingsViewModel(store, new FakeThemeController(), ApplicationSettings.Default, loadWarning: null);

        vm.IsDarkMode = true;
        vm.IsDarkMode = false;
        vm.IsDarkMode = true;
        vm.IsDarkMode = false;
        await vm.FlushAsync();

        Assert.False(store.LastSaved.DarkMode);
    }

    [Fact]
    public async Task RapidToggle_StaleCompletionDoesNotOverwriteLatestValue()
    {
        var store = new GatedStore();
        var vm = new SettingsViewModel(store, new FakeThemeController(), ApplicationSettings.Default, loadWarning: null);

        vm.IsDarkMode = true;
        await store.FirstSaveReached;
        vm.IsDarkMode = false;
        store.ReleaseGate();
        await vm.FlushAsync();

        Assert.NotEmpty(store.Saved);
        Assert.False(store.Saved[^1].DarkMode);
    }

    [Fact]
    public async Task Flush_CompletesPendingSaveBeforeReturning()
    {
        var store = new RecordingStore();
        var vm = new SettingsViewModel(store, new FakeThemeController(), ApplicationSettings.Default, loadWarning: null);

        vm.IsDarkMode = true;
        Assert.False(store.LastSaved.DarkMode);

        await vm.FlushAsync();

        Assert.True(store.LastSaved.DarkMode);
    }

    [Fact]
    public async Task WorkspaceProjection_ShowsNoWorkspaceWhenNoneAttached()
    {
        var vm = NewViewModel();
        vm.AttachWorkspaceManagement(NewWorkspaceManagement(WorkspaceSnapshot.Empty));

        Assert.Equal("No workspace", vm.WorkspaceName);
    }

    [Fact]
    public async Task WorkspaceProjection_ShowsActiveWorkspaceNameWhenAttached()
    {
        var personal = NewWorkspace("Personal");
        var management = NewWorkspaceManagement(new WorkspaceSnapshot([personal], [], [], [], [], [], [], [], [], []));
        await management.LoadAsync(TestContext.Current.CancellationToken);

        var vm = NewViewModel();
        vm.AttachWorkspaceManagement(management);

        Assert.Equal("Personal", vm.WorkspaceName);
    }

    [Fact]
    public async Task WorkspaceProjection_UpdatesWhenActiveWorkspaceChanges()
    {
        var personal = NewWorkspace("Personal");
        var client = NewWorkspace("Client");
        var management = NewWorkspaceManagement(new WorkspaceSnapshot([personal, client], [], [], [], [], [], [], [], [], []));
        await management.LoadAsync(TestContext.Current.CancellationToken);

        var vm = NewViewModel();
        vm.AttachWorkspaceManagement(management);

        await management.SelectWorkspaceAsync(
            management.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal("Client", vm.WorkspaceName);
    }

    [Fact]
    public async Task ManageWorkspaces_DelegatesToExistingWorkspaceManagement()
    {
        var management = NewWorkspaceManagement(WorkspaceSnapshot.Empty);

        var vm = NewViewModel();
        vm.AttachWorkspaceManagement(management);

        vm.ManageWorkspacesCommand.Execute(null);

        Assert.True(management.IsWorkspaceManagementOpen);
    }

    private static SettingsViewModel NewViewModel(string? loadWarning = null) =>
        new(new RecordingStore(), new FakeThemeController(), ApplicationSettings.Default, loadWarning);

    private static WorkspaceManagementViewModel NewWorkspaceManagement(WorkspaceSnapshot snapshot) =>
        new(new WorkspaceManagementService(new InMemoryWorkspaceRepository(snapshot), () => Now));

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public ApplicationSettings LastSaved { get; private set; } = ApplicationSettings.Default;

        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsLoadResult.Success(LastSaved));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            LastSaved = settings;
            return Task.FromResult(ApplicationSettingsSaveResult.Success);
        }
    }

    private sealed class FailingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsSaveResult.Failed("The disk is full."));
    }

    private sealed class GatedStore : IApplicationSettingsStore
    {
        private readonly TaskCompletionSource<object?> _gate = new();
        private readonly TaskCompletionSource<object?> _firstSaveReached = new();
        private readonly List<ApplicationSettings> _saved = new();

        public IReadOnlyList<ApplicationSettings> Saved => _saved;
        public Task FirstSaveReached => _firstSaveReached.Task;

        public void ReleaseGate() => _gate.TrySetResult(null);

        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public async Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            _firstSaveReached.TrySetResult(null);
            await _gate.Task.ConfigureAwait(false);
            _saved.Add(settings);
            return ApplicationSettingsSaveResult.Success;
        }
    }

    private sealed class FakeThemeController : IApplicationThemeController
    {
        private readonly List<bool> _calls = new();
        public IReadOnlyList<bool> Calls => _calls;

        public void ApplyDarkMode(bool darkMode) => _calls.Add(darkMode);
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
