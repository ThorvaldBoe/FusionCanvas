using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Views;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

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
    public void SettingsWindow_SelectingWorkspaceShowsWorkspacePaneAndManageButton()
    {
        var management = NewWorkspaceManagement(new WorkspaceSnapshot([NewWorkspace("Personal")], [], [], [], [], [], [], [], [], []));
        var settings = NewSettingsViewModel(management);
        settings.OpenCommand.Execute(null);

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            settings.SelectedSection = SettingsSection.Workspace;
            PumpLayout(window);

            Assert.True(settings.IsWorkspaceSection);
            var manageButton = FindControl<Button>(window, b => (b.Content as string) == "Manage workspaces" && b.IsVisible);
            Assert.NotNull(manageButton);
            var name = FindControl<TextBlock>(window, t => t.IsVisible && t.Text == "Personal");
            Assert.NotNull(name);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SettingsWindow_ReopenResetsToGeneralSection()
    {
        var settings = NewSettingsViewModel();
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.Workspace;
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
    public void WorkspaceSection_ShowsNoWorkspaceWhenNoneActive()
    {
        var management = NewWorkspaceManagement(WorkspaceSnapshot.Empty);
        var settings = NewSettingsViewModel(management);
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.Workspace;

        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            PumpLayout(window);

            var noWorkspace = FindControl<TextBlock>(window, t => t.IsVisible && t.Text == "No workspace");
            Assert.NotNull(noWorkspace);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShellHeader_ShowsSettingsButtonAndIndicatorTooltipWithoutOldWorkspacesRow()
    {
        using var fixture = new MainWindowFixture(width: 900, height: 600);

        var settingsButton = FindControl<Button>(fixture.Window, b =>
            AutomationProperties.GetName(b) == "Settings" && b.IsVisible);
        Assert.NotNull(settingsButton);
        Assert.True(settingsButton!.Command?.CanExecute(null) ?? false);

        var indicatorBorder = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => ToolTip.GetTip(b) is string tip && tip == "Manage workspaces in settings");
        Assert.NotNull(indicatorBorder);

        var oldLabel = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == "Workspaces");
        Assert.Null(oldLabel);
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
        var management = new WorkspaceManagementViewModel(new WorkspaceManagementService(new InMemoryWorkspaceRepository(snapshot), () => Now));
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
