using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Versioning;
using FusionCanvas.Application.Updates;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.App.Tests.TestSupport;

namespace FusionCanvas.App.Tests.Settings;

public class SettingsWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public void SettingsWindow_ConstructsAndShowsGeneralPaneByDefault()
    {
        using var fixture = new MainWindowFixture();
        var settings = fixture.ViewModel.Settings;
        settings.OpenCommand.Execute(null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.Equal(SettingsSection.General, settings.SelectedSection);
            Assert.True(settings.IsGeneralSection);
            var toggle = FindToggleSwitch(window);
            Assert.NotNull(toggle);
            Assert.True(toggle!.IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SettingsWindow_SelectingWorkspaceDiagnosticsShowsItsActiveScope()
    {
        var management = NewWorkspaceManagement(new WorkspaceSnapshot([NewWorkspace("Personal")], [], [], [], [], [], [], [], [], []));
        var settings = NewSettingsViewModel(management);
        settings.OpenCommand.Execute(null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            settings.SelectedSection = SettingsSection.WorkspaceDiagnostics;
            PumpLayout(window);

            Assert.True(settings.IsWorkspaceDiagnosticsSection);
            Assert.True(settings.CanManageArtworkStyles);
            var stylesButton = FindControl<Button>(window, b => (b.Content as string) == "Manage artwork styles" && b.IsVisible);
            Assert.NotNull(stylesButton);
            var managementRequested = false;
            settings.ArtworkStylesManagementRequested += (_, _) => managementRequested = true;
            stylesButton!.Command!.Execute(null);
            Assert.True(managementRequested);
            var name = FindControl<TextBlock>(window, t => t.IsVisible && t.Text == "Active workspace: Personal");
            Assert.NotNull(name);
            Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(),
                b => (b.Content as string) == "Manage workspaces" && b.IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SettingsWindow_ReopenResetsToGeneralSection()
    {
        var settings = NewSettingsViewModel();
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.WorkspaceDiagnostics;
        settings.CloseCommand.Execute(null);

        settings.OpenCommand.Execute(null);

        Assert.Equal(SettingsSection.General, settings.SelectedSection);
    }

    [AvaloniaFact]
    public async Task ToggleSwitch_OnPropagatesToDarkThemeAndPersists()
    {
        var store = new InMemoryApplicationSettingsStore();
        var settings = new SettingsViewModel(store, new AvaloniaApplicationThemeController(), ApplicationSettings.Default, loadWarning: null);
        settings.OpenCommand.Execute(null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            var toggle = FindToggleSwitch(window);
            toggle!.IsChecked = true;
            PumpLayout(window);

            Assert.True(settings.IsDarkMode);
            Assert.Equal(ThemeVariant.Dark, Avalonia.Application.Current!.RequestedThemeVariant);
            await settings.FlushAsync();
            Assert.True(store.Current.DarkMode);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ToggleSwitch_OffRestoresLightTheme()
    {
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            new ApplicationSettings(DarkMode: true),
            loadWarning: null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.Equal(ThemeVariant.Dark, Avalonia.Application.Current!.RequestedThemeVariant);

            var toggle = FindToggleSwitch(window);
            toggle!.IsChecked = false;
            PumpLayout(window);

            Assert.False(settings.IsDarkMode);
            Assert.Equal(ThemeVariant.Light, Avalonia.Application.Current!.RequestedThemeVariant);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WorkspaceDiagnostics_ShowsNoWorkspaceWhenNoneActive()
    {
        var management = NewWorkspaceManagement(WorkspaceSnapshot.Empty);
        var settings = NewSettingsViewModel(management);
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.WorkspaceDiagnostics;

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            var noWorkspace = FindControl<TextBlock>(window, t =>
                t.IsVisible && t.Text == "No workspace selected. Select an active workspace to manage diagnostics.");
            Assert.NotNull(noWorkspace);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MainShell_ShowsGlobalSettingsRailAndWorkspaceContextControl()
    {
        using var fixture = new MainWindowFixture(width: 900, height: 600);

        var settingsButton = FindControl<Button>(fixture.Window, b =>
            AutomationProperties.GetName(b) == "Settings" && b.IsVisible);
        Assert.NotNull(settingsButton);
        Assert.True(settingsButton!.Command?.CanExecute(null) ?? false);
        Assert.Equal("Settings", ToolTip.GetTip(settingsButton));

        var rail = FindControl<Border>(fixture.Window, b => Grid.GetColumn(b) == 0 && b.IsVisible);
        Assert.NotNull(rail);
        Assert.Equal("Workspace: Personal", AutomationProperties.GetName(
            FindControl<Button>(fixture.Window, b => AutomationProperties.GetName(b)?.StartsWith("Workspace:", StringComparison.Ordinal) == true)!));
        Assert.NotNull(FindControl<Button>(fixture.Window, b => ToolTip.GetTip(b) is string tip && tip == "Switch workspace or manage workspaces"));

        var oldLabel = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == "Workspaces");
        Assert.Null(oldLabel);
    }

    [AvaloniaFact]
    public void MainShell_RailAndWorkspaceControlFitAtMinimumWidth()
    {
        using var fixture = new MainWindowFixture(width: 900, height: 600);

        var header = fixture.Window.GetVisualDescendants()
            .OfType<Grid>()
            .First(g => g.Name == "NavigationHeader");
        var settingsButton = fixture.Window.GetVisualDescendants()
            .OfType<Button>()
            .First(b => AutomationProperties.GetName(b) == "Settings" && b.IsVisible);
        var workspaceButton = fixture.Window.GetVisualDescendants()
            .OfType<Button>()
            .First(b => ToolTip.GetTip(b) is string tip && tip == "Switch workspace or manage workspaces");
        var rail = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .First(b => Grid.GetColumn(b) == 0 && b.IsVisible);

        Assert.True(settingsButton.Bounds.Right <= rail.Bounds.Width + 1,
            $"Settings button extends beyond rail: {settingsButton.Bounds.Right} > {rail.Bounds.Width}");
        Assert.True(workspaceButton.Bounds.Right <= header.Bounds.Right + 1,
            $"Workspace control extends beyond header: {workspaceButton.Bounds.Right} > {header.Bounds.Right}");
        var documentArea = fixture.Window.GetVisualDescendants()
            .OfType<Grid>()
            .First(g => Grid.GetColumn(g) == 3 && g.IsVisible);
        Assert.True(documentArea.Bounds.Width >= 480,
            $"Document area is too narrow at minimum window size: {documentArea.Bounds.Width} < 480");
    }

    [AvaloniaFact]
    public async Task UtilityRail_UpdateActionIsHiddenUntilAnUpdateIsAvailable()
    {
        using var fixture = new MainWindowFixture();
        var settings = fixture.ViewModel.Settings;
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, new UpdateManifest(
                UpdateManifestValidator.CurrentSchemaVersion,
                "0.3.0",
                UpdatePlatform.WindowsX64,
                UpdateTrack.Stable,
                "v0.3.0",
                new string('C', 40),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe"),
                new string('A', 64),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe.sigstore.json")))
        };
        settings.ReplaceUpdateService(updateService);
        var updateButton = FindControl<Button>(fixture.Window, b => AutomationProperties.GetName(b) == "Update available");
        Assert.NotNull(updateButton);
        var railUpdateButton = updateButton!;

        Assert.False(railUpdateButton.IsVisible);
        Assert.NotNull(FindControl<Button>(fixture.Window, b => AutomationProperties.GetName(b) == "Settings" && b.IsVisible));

        settings.Updates.CheckCommand.Execute(null);
        await ((FusionCanvas.App.Commands.AsyncRelayCommand)settings.Updates.CheckCommand).ExecutionTask!
            .WaitAsync(TimeSpan.FromSeconds(3));
        fixture.PumpLayout();

        Assert.True(railUpdateButton.IsVisible);
        Assert.Equal("Updates.Available", AutomationProperties.GetAutomationId(railUpdateButton));
        Assert.Same(settings.Updates.UpdateCommand, railUpdateButton.Command);
        Assert.Equal(1, Grid.GetRow(railUpdateButton));
    }

    [AvaloniaFact]
    public void WorkspaceSelector_OffersWorkspaceChoicesAndSeparateManagementAction()
    {
        using var fixture = new MainWindowFixture();
        var selector = FindControl<Button>(fixture.Window, b =>
            ToolTip.GetTip(b) is string tip && tip == "Switch workspace or manage workspaces");
        Assert.NotNull(selector);
        var workspaceSelector = selector!;

        Assert.Equal("Workspace: Personal", AutomationProperties.GetName(workspaceSelector));
        Assert.NotNull(workspaceSelector.Flyout);

        workspaceSelector.Flyout!.ShowAt(workspaceSelector);
        fixture.PumpLayout();

        var workspaceChoice = FindControl<Button>(fixture.Window, b => (b.Content as string) == "Personal" && b.IsVisible);
        var manage = FindControl<Button>(fixture.Window, b => (b.Content as string) == "Manage workspaces" && b.IsVisible);
        Assert.NotNull(workspaceChoice);
        Assert.NotNull(manage);
        Assert.DoesNotContain(
            fixture.ViewModel.WorkspaceManagement.SelectorWorkspaces,
            entry => fixture.ViewModel.WorkspaceManagement.ArchivedWorkspaces.Any(archived => archived.Id == entry.Id));

        manage!.Command!.Execute(null);
        Assert.True(fixture.ViewModel.WorkspaceManagement.IsWorkspaceManagementOpen);
    }

    [AvaloniaFact]
    public void WorkspaceSelector_NoWorkspaceStillOffersManagement()
    {
        var snapshot = WorkspaceSnapshot.Empty;
        using var fixture = new MainWindowFixture(snapshot: snapshot, repository: new InMemoryWorkspaceRepository(snapshot));
        var selector = FindControl<Button>(fixture.Window, b =>
            ToolTip.GetTip(b) is string tip && tip == "Switch workspace or manage workspaces");
        Assert.NotNull(selector);
        var workspaceSelector = selector!;

        Assert.Equal("Workspace: No workspace", AutomationProperties.GetName(workspaceSelector));
        workspaceSelector.Flyout!.ShowAt(workspaceSelector);
        fixture.PumpLayout();

        var manage = FindControl<Button>(fixture.Window, b => (b.Content as string) == "Manage workspaces" && b.IsVisible);
        Assert.NotNull(manage);
        Assert.Empty(fixture.ViewModel.WorkspaceManagement.SelectorWorkspaces);
        manage!.Command!.Execute(null);
        Assert.True(fixture.ViewModel.WorkspaceManagement.IsWorkspaceManagementOpen);
    }

    [AvaloniaFact]
    public void ShellHeader_ActivatingSettingsOpensOneOwnedSettingsInstance()
    {
        using var fixture = new MainWindowFixture();
        var settingsButton = FindControl<Button>(fixture.Window, b =>
            AutomationProperties.GetName(b) == "Settings" && b.IsVisible);

        settingsButton!.Command!.Execute(null);

        Assert.True(fixture.ViewModel.Settings.IsOpen);
        Assert.Equal(SettingsSection.General, fixture.ViewModel.Settings.SelectedSection);

        fixture.ViewModel.Settings.CloseCommand.Execute(null);
        Assert.False(fixture.ViewModel.Settings.IsOpen);
    }

    [AvaloniaFact]
    public async Task DarkModeToggle_UpdatesThemeAcrossOpenWindows()
    {
        using var fixture = new MainWindowFixture();
        var settings = fixture.ViewModel.Settings;
        settings.OpenCommand.Execute(null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(fixture.Window);

            settings.IsDarkMode = true;
            PumpLayout(fixture.Window);
            Assert.Equal(ThemeVariant.Dark, Avalonia.Application.Current!.RequestedThemeVariant);

            settings.IsDarkMode = false;
            PumpLayout(fixture.Window);
            Assert.Equal(ThemeVariant.Light, Avalonia.Application.Current!.RequestedThemeVariant);
            await settings.FlushAsync();
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AboutSection_ShowsProductVersionAndCopyDiagnosticsButton()
    {
        var info = new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a");
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            versionProvider: new ConstantVersionProvider(info),
            clipboard: new NullClipboardService());
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.About;

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.True(settings.IsAboutSection);
            var version = FindControl<TextBlock>(window, t => t.IsVisible && t.Text == "Version 0.1.42");
            Assert.NotNull(version);
            var copyButton = FindControl<Button>(window, b => (b.Content as string) == "Copy diagnostics" && b.IsVisible);
            Assert.NotNull(copyButton);
            Assert.Same(settings.CopyDiagnosticsCommand, copyButton!.Command);
            var product = FindControl<TextBlock>(window, t => t.IsVisible && t.Text == "FusionCanvas");
            Assert.NotNull(product);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task AboutSection_ShowsUpdateCheckAndInstallControls()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, new UpdateManifest(
                UpdateManifestValidator.CurrentSchemaVersion,
                "0.3.0",
                UpdatePlatform.WindowsX64,
                UpdateTrack.Stable,
                "v0.3.0",
                new string('C', 40),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe"),
                new string('A', 64),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe.sigstore.json")))
        };
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            updateService: updateService);
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.About;
        var window = new SettingsWindow { DataContext = settings };

        try
        {
            window.Show();
            PumpLayout(window);
            var checkButton = FindControl<Button>(window, b => (b.Content as string) == "Check for updates" && b.IsVisible);
            Assert.NotNull(checkButton);
            var trackSelector = FindControl<ComboBox>(window, combo => combo.IsVisible && AutomationProperties.GetAutomationId(combo) == "Updates.Track");
            Assert.NotNull(trackSelector);
            Assert.True(trackSelector!.IsEnabled);

            checkButton!.Command!.Execute(null);
            await ((FusionCanvas.App.Commands.AsyncRelayCommand)settings.Updates.CheckCommand).ExecutionTask!
                .WaitAsync(TimeSpan.FromSeconds(3));
            PumpLayout(window);

            var downloadButton = FindControl<Button>(window, b => (b.Content as string) == "Download update" && b.IsVisible);
            Assert.NotNull(downloadButton);
            Assert.Contains("0.3.0", settings.Updates.StatusMessage);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task AboutSection_ShowsEnabledCancelDuringDownloadAndReturnsToAvailable()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, new UpdateManifest(
                UpdateManifestValidator.CurrentSchemaVersion,
                "0.3.0",
                UpdatePlatform.WindowsX64,
                UpdateTrack.Stable,
                "v0.3.0",
                new string('C', 40),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe"),
                new string('A', 64),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.3.0"),
                new Uri("https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v0.3.0/FusionCanvas-0.3.0-win-x64-Setup.exe.sigstore.json"))),
            BlockDownload = true
        };
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            updateService: updateService);
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.About;
        var window = new SettingsWindow { DataContext = settings };

        try
        {
            window.Show();
            PumpLayout(window);

            var checkButton = FindControl<Button>(window, b => (b.Content as string) == "Check for updates" && b.IsVisible);
            Assert.NotNull(checkButton);
            checkButton!.Command!.Execute(null);
            await ((FusionCanvas.App.Commands.AsyncRelayCommand)settings.Updates.CheckCommand).ExecutionTask!
                .WaitAsync(TimeSpan.FromSeconds(3));

            var downloadButton = FindControl<Button>(window, b => (b.Content as string) == "Download update" && b.IsVisible);
            Assert.NotNull(downloadButton);
            downloadButton!.Command!.Execute(null);
            await updateService.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
            PumpLayout(window);

            var cancelButton = FindControl<Button>(window, b =>
                (b.Content as string) == "Cancel" && b.IsVisible);
            Assert.NotNull(cancelButton);
            var trackSelector = FindControl<ComboBox>(window, combo => combo.IsVisible && AutomationProperties.GetAutomationId(combo) == "Updates.Track");
            Assert.NotNull(trackSelector);
            Assert.False(trackSelector!.IsEnabled);
            Assert.True(cancelButton!.Command?.CanExecute(null));

            cancelButton.Command!.Execute(null);
            await ((FusionCanvas.App.Commands.AsyncRelayCommand)settings.Updates.UpdateCommand).ExecutionTask!
                .WaitAsync(TimeSpan.FromSeconds(3));
            PumpLayout(window);

            Assert.Equal(UpdatePresentationStatus.UpdateAvailable, settings.Updates.Status);
            Assert.Null(FindControl<Button>(window, b => (b.Content as string) == "Cancel" && b.IsVisible));
            Assert.NotNull(FindControl<Button>(window, b => (b.Content as string) == "Download update" && b.IsVisible));
        }
        finally { window.Close(); }
    }

    private sealed class ConstantVersionProvider(ApplicationVersionInfo info) : IApplicationVersionProvider
    {
        public ApplicationVersionInfo GetVersion() => info;
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public UpdateCheckResult CheckResult { get; set; } = new(UpdateCheckStatus.UpToDate, null);
        public bool BlockDownload { get; set; }
        public TaskCompletionSource<bool> DownloadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<UpdateCheckResult> CheckForUpdateAsync(UpdateTrack track = UpdateTrack.Stable, CancellationToken cancellationToken = default) =>
            Task.FromResult(CheckResult);

        public async Task<VerifiedUpdatePackage> DownloadAsync(UpdateManifest manifest, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (BlockDownload)
            {
                DownloadStarted.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new VerifiedUpdatePackage(manifest, "C:\\Temp\\setup.exe");
        }

        public Task ApplyAsync(VerifiedUpdatePackage package, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static SettingsViewModel NewSettingsViewModel(WorkspaceManagementViewModel? management = null)
    {
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null);
        if (management is not null)
        {
            settings.AttachWorkspaceManagement(management);
        }
        return settings;
    }

    private static WorkspaceManagementViewModel NewWorkspaceManagement(WorkspaceSnapshot snapshot)
    {
        var management = new WorkspaceManagementViewModel(new WorkspaceManagementService(new InMemoryWorkspaceRepository(snapshot), new FusionCanvas.Integration.Workspaces.WorkspaceContextMapper(), () => Now));
        management.LoadAsync().GetAwaiter().GetResult();
        return management;
    }

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }

    private static ToggleSwitch? FindToggleSwitch(Window window) =>
        FindControl<ToggleSwitch>(window, _ => true);

    private static T? FindControl<T>(Window window, Func<T, bool> predicate) where T : Control =>
        window.GetVisualDescendants().OfType<T>().FirstOrDefault(predicate);

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
