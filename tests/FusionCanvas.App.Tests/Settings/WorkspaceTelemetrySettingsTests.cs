using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.App.Tests.Settings;

public sealed class WorkspaceTelemetrySettingsTests
{
    [AvaloniaFact]
    public async Task DebugWindow_EnablesCaptureAndCopyClearOnlyAffectDisplayedText()
    {
        using var temp = new TemporaryDirectory();
        var workspaceId = Guid.NewGuid();
        var workspaceContext = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), workspaceContext);
        var clipboard = new RecordingClipboard();
        var viewModel = new WorkspaceTelemetrySettingsViewModel(service, workspaceContext, clipboard);
        var settingsLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settingsNotifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkspaceTelemetrySettingsViewModel.DebugModeEnabled)
                && Interlocked.Increment(ref settingsNotifications) > 1)
                settingsLoaded.TrySetResult();
        };
        viewModel.SetWorkspace(workspaceId, "Test workspace");
        var view = new WorkspaceTelemetrySettingsView { DataContext = viewModel };
        var window = new Window { Content = view };

        try
        {
            window.Show();
            Assert.NotEmpty(view.GetVisualDescendants().OfType<ToggleSwitch>());

            await settingsLoaded.Task.WaitAsync(TimeSpan.FromSeconds(3));
            viewModel.ShowDebugWindow = true;
            await WaitUntilAsync(() => service.IsCaptureEnabled && viewModel.IsDebugWindowOpen);
            await viewModel.RecordAsync(new TelemetryEventRequest(
                "Workspace", "KnownEvent", "Information", "Succeeded", "visible message"));
            await WaitUntilAsync(() => viewModel.WindowText.Contains("KnownEvent", StringComparison.Ordinal));

            viewModel.CopyWindowCommand.Execute(null);
            await WaitUntilAsync(() => clipboard.Text is not null);
            Assert.Contains("visible message", clipboard.Text);
            viewModel.ClearWindowCommand.Execute(null);

            Assert.Empty(viewModel.WindowText);
            var persisted = await service.ReadAllAsync(workspaceId);
            Assert.Contains(persisted, entry => entry.Name == "KnownEvent");

            viewModel.DebugModeEnabled = false;
            await WaitUntilAsync(() => !service.IsCaptureEnabled && !viewModel.IsDebugWindowOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task WorkspaceChange_ClearsOldWindowTextAndShowsOnlyNewWorkspaceEvents()
    {
        using var temp = new TemporaryDirectory();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var context = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), context);
        var settings = WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true, ShowDebugWindow = true };
        await service.SaveSettingsAsync(firstId, settings);
        await service.SaveSettingsAsync(secondId, settings);
        var viewModel = new WorkspaceTelemetrySettingsViewModel(service, context, NullClipboardService.Instance);
        viewModel.SetWorkspace(firstId, "First");

        await WaitUntilAsync(() => viewModel.IsDebugWindowOpen);
        await service.RecordAsync(new TelemetryEventRequest("Workspace", "FirstWorkspaceEvent", "Information", "Succeeded", "first"));
        await WaitUntilAsync(() => viewModel.WindowText.Contains("FirstWorkspaceEvent", StringComparison.Ordinal));
        viewModel.SetWorkspace(secondId, "Second");
        Assert.Empty(viewModel.WindowText);
        await WaitUntilAsync(() => viewModel.IsDebugWindowOpen);
        await service.RecordAsync(new TelemetryEventRequest("Workspace", "SecondWorkspaceEvent", "Information", "Succeeded", "second"));
        await WaitUntilAsync(() => viewModel.WindowText.Contains("SecondWorkspaceEvent", StringComparison.Ordinal));

        Assert.DoesNotContain("FirstWorkspaceEvent", viewModel.WindowText, StringComparison.Ordinal);
        Assert.Equal(secondId, context.ActiveWorkspaceId);
    }

    [AvaloniaFact]
    public async Task DeleteRequiresConfirmationAndCancelPreservesRecordsAndSettings()
    {
        using var temp = new TemporaryDirectory();
        var workspaceId = Guid.NewGuid();
        var context = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), context);
        var settings = WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true };
        await service.SaveSettingsAsync(workspaceId, settings);
        var viewModel = new WorkspaceTelemetrySettingsViewModel(service, context, NullClipboardService.Instance);
        viewModel.SetWorkspace(workspaceId, "Delete test");
        await WaitUntilAsync(() => service.IsCaptureEnabled);
        await service.RecordAsync(new TelemetryEventRequest("Workspace", "KeepMe", "Information", "Succeeded", "stored"));

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.ConfirmDelete);
        viewModel.CancelDeleteCommand.Execute(null);
        Assert.Single(await service.ReadAllAsync(workspaceId));

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);
        await WaitUntilAsync(async () => (await service.ReadAllAsync(workspaceId)).Count == 0);

        Assert.Equal(settings, await service.GetSettingsAsync(workspaceId));
    }

    [AvaloniaFact]
    public async Task Search_FiltersByAreaAndShowsNormalEmptyResults()
    {
        using var temp = new TemporaryDirectory();
        var workspaceId = Guid.NewGuid();
        var context = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), context);
        await service.SaveSettingsAsync(workspaceId, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        var viewModel = new WorkspaceTelemetrySettingsViewModel(service, context, NullClipboardService.Instance);
        viewModel.SetWorkspace(workspaceId, "Search test");
        await WaitUntilAsync(() => service.IsCaptureEnabled);
        await service.RecordAsync(new TelemetryEventRequest("Design", "DesignEvent", "Information", "Succeeded", "design data"));
        await service.RecordAsync(new TelemetryEventRequest("Workspace", "WorkspaceEvent", "Information", "Succeeded", "workspace data"));

        viewModel.SelectedArea = "Design";
        viewModel.SearchCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage?.StartsWith("Showing", StringComparison.Ordinal) == true);
        Assert.Contains("DesignEvent", viewModel.ResultText);
        Assert.DoesNotContain("WorkspaceEvent", viewModel.ResultText);

        viewModel.SelectedArea = "Concept";
        viewModel.SearchCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "No telemetry records match this search.");
        Assert.Empty(viewModel.ResultText);
    }

    [AvaloniaFact]
    public async Task Export_ChangesBusyStateOnTheUiContext()
    {
        using var temp = new TemporaryDirectory();
        Directory.CreateDirectory(temp.GetPath("exports"));
        var uiContext = SynchronizationContext.Current;
        Assert.NotNull(uiContext);
        var service = new ExportTelemetryService();
        var picker = new DelayedExportFilePicker();
        var viewModel = new WorkspaceTelemetrySettingsViewModel(service, null, NullClipboardService.Instance);
        viewModel.SetWorkspace(Guid.NewGuid(), "Export test");
        viewModel.SetExportFilePicker(picker);
        var busyOnUiContext = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkspaceTelemetrySettingsViewModel.IsBusy)
                && viewModel.IsBusy)
            {
                busyOnUiContext.TrySetResult(ReferenceEquals(SynchronizationContext.Current, uiContext));
            }
        };

        viewModel.ExportCommand.Execute(null);
        var exportPath = temp.GetPath(Path.Combine("exports", "telemetry.json"));
        picker.Complete(exportPath);

        Assert.True(await busyOnUiContext.Task.WaitAsync(TimeSpan.FromSeconds(3)));
        service.CompleteExport("{}\n");
        await WaitUntilAsync(() => !viewModel.IsBusy);
        Assert.Equal("{}\n", await File.ReadAllTextAsync(exportPath));
    }

    [AvaloniaFact]
    public void DebugWindow_IsNativeResizableWindow()
    {
        var window = new TelemetryDebugWindow();

        Assert.True(window.CanResize);
        Assert.True(window.MinWidth > 0);
        Assert.True(window.MinHeight > 0);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout) throw new TimeoutException("The telemetry UI state did not update in time.");
            await Task.Delay(10);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (!await condition())
        {
            if (DateTime.UtcNow >= timeout) throw new TimeoutException("The telemetry UI state did not update in time.");
            await Task.Delay(10);
        }
    }

    private sealed class RecordingClipboard : IClipboardService
    {
        public string? Text { get; private set; }
        public Task SetTextAsync(string text)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }

    private sealed class DelayedExportFilePicker : ITelemetryExportFilePicker
    {
        private readonly TaskCompletionSource<string?> _path = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string?> PickPathAsync(CancellationToken cancellationToken = default) => _path.Task;

        public void Complete(string path) => _path.TrySetResult(path);
    }

    private sealed class ExportTelemetryService : ITelemetryService
    {
        private readonly TaskCompletionSource<string> _export = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler<TelemetryEntry>? EntryRecorded;
        public bool IsCaptureEnabled => false;
        public Task<WorkspaceTelemetrySettings> GetSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceTelemetrySettings.Default);
        public Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>([]);
        public Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>([]);
        public Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<string> ExportJsonAsync(Guid workspaceId, CancellationToken cancellationToken = default) => _export.Task;
        public Task<int> CleanupExpiredAsync(Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void CompleteExport(string json) => _export.TrySetResult(json);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvasTelemetryUiTests", Guid.NewGuid().ToString("N"));
        public string GetPath(string name) => Path.Combine(_path, name);
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_path)) Directory.Delete(_path, recursive: true);
        }
    }
}
