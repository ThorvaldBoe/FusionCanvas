using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.IssueReporting;
using FusionCanvas.App.Settings;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.App.Tests.IssueReporting;

public sealed class IssueReportingViewModelTests
{
    [Fact]
    public async Task ReportCapture_CopiesRecentAllowlistedDiagnosticsAndClearsOnlyCapture()
    {
        using var temp = new TemporaryDirectory();
        var workspaceId = Guid.NewGuid();
        var workspaceContext = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), workspaceContext);
        await service.SaveSettingsAsync(workspaceId, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        var settings = new WorkspaceTelemetrySettingsViewModel(service, workspaceContext, NullClipboardService.Instance);
        settings.SetWorkspace(workspaceId, "Report workspace");
        await settings.RefreshSettingsAsync();
        await settings.RecordAsync(new TelemetryEventRequest(
            "Workspace", "BeforeCapture", "Information", "Succeeded", "older record", RequestBody: "private-body-sentinel"));

        var clipboard = new RecordingClipboard { Text = "existing clipboard" };
        using var viewModel = new IssueReportingViewModel(settings, clipboard, new FakeScreenshotStager(), new RecordingLinkLauncher());
        await viewModel.InitializeAsync();
        Assert.False(viewModel.CanCopyDiagnostics);
        Assert.False(viewModel.CopyDiagnosticsCommand.CanExecute(null));
        Assert.Equal("existing clipboard", clipboard.Text);
        await settings.RecordAsync(new TelemetryEventRequest(
            "Updates", "UpdateCheckFailed", "Warning", "Failed", "safe diagnostic api_key=secret-sentinel",
            RequestBody: "private-body-sentinel", ResponseBody: "response-body-sentinel"));
        Assert.True(viewModel.CanCopyDiagnostics);

        viewModel.CopyDiagnosticsCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.CopyButtonText == "Copied to clipboard");

        Assert.Contains("UpdateCheckFailed", clipboard.Text);
        Assert.DoesNotContain("BeforeCapture", clipboard.Text);
        Assert.DoesNotContain("private-body-sentinel", clipboard.Text);
        Assert.DoesNotContain("response-body-sentinel", clipboard.Text);
        Assert.DoesNotContain("secret-sentinel", clipboard.Text);

        viewModel.DebugModeEnabled = false;
        await WaitUntilAsync(() => !service.IsCaptureEnabled);
        viewModel.RequestClearCaptureCommand.Execute(null);
        viewModel.ConfirmClearCaptureCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.HasCapture);

        var remaining = await service.ReadAllAsync(workspaceId);
        Assert.Equal("BeforeCapture", Assert.Single(remaining).Name);
    }

    [Fact]
    public async Task NoWorkspace_LeavesReportAndBrowserHandoffAvailableWithoutTelemetryControls()
    {
        var settings = new WorkspaceTelemetrySettingsViewModel(null, null, NullClipboardService.Instance);
        var links = new RecordingLinkLauncher();
        using var viewModel = new IssueReportingViewModel(settings, NullClipboardService.Instance, new FakeScreenshotStager(), links);

        await viewModel.InitializeAsync();
        viewModel.OpenIssueFormCommand.Execute(null);

        Assert.True(viewModel.HasNoWorkspace);
        Assert.False(viewModel.CanCopyDiagnostics);
        Assert.Equal(IssueReportingViewModel.IssueFormUri, links.LastOpened);
    }

    [AvaloniaFact]
    public async Task ScreenshotPaste_ReplacesAndRemovesStagedAttachment_AndCloseCleansIt()
    {
        var stager = new FakeScreenshotStager();
        var settings = new WorkspaceTelemetrySettingsViewModel(null, null, NullClipboardService.Instance);
        var viewModel = new IssueReportingViewModel(settings, NullClipboardService.Instance, stager, new RecordingLinkLauncher());

        viewModel.PasteScreenshotCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.HasScreenshot || viewModel.StatusMessage is not null);
        Assert.True(viewModel.HasScreenshot, viewModel.StatusMessage);
        var first = viewModel.ScreenshotFileName;
        Assert.NotNull(first);

        viewModel.PasteScreenshotCommand.Execute(null);
        await WaitUntilAsync(() => stager.Deleted.Count == 1 && viewModel.ScreenshotFileName != first);
        Assert.Equal(first, stager.Deleted[0]);

        viewModel.RemoveScreenshotCommand.Execute(null);
        Assert.False(viewModel.HasScreenshot);
        Assert.Equal(2, stager.Deleted.Count);

        await viewModel.CloseAsync();
        Assert.Equal(2, stager.Deleted.Count);
    }

    [AvaloniaFact]
    public async Task BrowserFailure_KeepsStagedScreenshotAndShowsCopyableAddress()
    {
        var stager = new FakeScreenshotStager();
        var settings = new WorkspaceTelemetrySettingsViewModel(null, null, NullClipboardService.Instance);
        using var viewModel = new IssueReportingViewModel(settings, NullClipboardService.Instance, stager, new FailingLinkLauncher());

        viewModel.PasteScreenshotCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.HasScreenshot || viewModel.StatusMessage is not null);
        Assert.True(viewModel.HasScreenshot, viewModel.StatusMessage);

        viewModel.OpenIssueFormCommand.Execute(null);

        Assert.True(viewModel.HasScreenshot);
        Assert.Contains(IssueReportingViewModel.IssueFormUri.ToString(), viewModel.StatusMessage);
    }

    [Fact]
    public async Task EnablingDebugModeStartsReportCapture_AndDisablingKeepsItsEntries()
    {
        using var temp = new TemporaryDirectory();
        var workspaceId = Guid.NewGuid();
        var workspaceContext = new TelemetryWorkspaceContext();
        using var service = new WorkspaceTelemetryService(new SqliteTelemetryStore(temp.GetPath("workspace.db")), workspaceContext);
        var settings = new WorkspaceTelemetrySettingsViewModel(service, workspaceContext, NullClipboardService.Instance);
        settings.SetWorkspace(workspaceId, "Report workspace");
        await settings.RefreshSettingsAsync();
        using var viewModel = new IssueReportingViewModel(settings, NullClipboardService.Instance, new FakeScreenshotStager(), new RecordingLinkLauncher());

        viewModel.DebugModeEnabled = true;
        await WaitUntilAsync(() => service.IsCaptureEnabled && viewModel.HasCapture);
        await settings.RecordAsync(new TelemetryEventRequest("Workspace", "Reproduced", "Information", "Succeeded", "reproduced"));
        Assert.True(viewModel.CanCopyDiagnostics);

        viewModel.DebugModeEnabled = false;
        await WaitUntilAsync(() => !service.IsCaptureEnabled);

        var records = await service.ReadAllAsync(workspaceId);
        Assert.Equal("Reproduced", Assert.Single(records).Name);
        Assert.True(viewModel.HasCapture);
        Assert.True(viewModel.CanCopyDiagnostics);
    }

    [AvaloniaFact]
    public async Task ReportWindow_ConstructsAndKeepsWorkspaceCaptureDisabledWithoutWorkspace()
    {
        var settings = new WorkspaceTelemetrySettingsViewModel(null, null, NullClipboardService.Instance);
        using var viewModel = new IssueReportingViewModel(settings, NullClipboardService.Instance, new FakeScreenshotStager(), new RecordingLinkLauncher());
        var window = new IssueReportingWindow { DataContext = viewModel };

        try
        {
            window.Show();
            await viewModel.InitializeAsync();
            var controls = window.GetVisualDescendants().OfType<Button>().ToArray();

            Assert.Contains(controls, button => button.Content?.ToString() == "Report Issue");
            Assert.Contains(controls, button => button.Content?.ToString() == "Paste screenshot");
            Assert.False(window.GetVisualDescendants().OfType<ToggleSwitch>().Single().IsEnabled);
        }
        finally
        {
            window.Close();
            await viewModel.CloseAsync();
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= stopAt) throw new TimeoutException("The report state did not reach the expected value.");
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private sealed class RecordingClipboard : IClipboardService
    {
        public string? Text { get; set; }
        public Task SetTextAsync(string text)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLinkLauncher : IExternalLinkLauncher
    {
        public Uri? LastOpened { get; private set; }
        public void Open(Uri uri) => LastOpened = uri;
    }

    private sealed class FailingLinkLauncher : IExternalLinkLauncher
    {
        public void Open(Uri uri) => throw new InvalidOperationException("Browser launch failed.");
    }

    private sealed class FakeScreenshotStager : IReportScreenshotStager
    {
        private int _next;
        public List<string> Deleted { get; } = [];
        public Task<StagedScreenshot> PasteFromClipboardAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new StagedScreenshot($"report-{++_next}.png", CreatePreview()));
        public void OpenLocation(string path) { }
        public void Delete(string path) => Deleted.Add(path);

        private static Avalonia.Media.Imaging.Bitmap CreatePreview() =>
            new Avalonia.Media.Imaging.WriteableBitmap(
                new Avalonia.PixelSize(1, 1),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvasIssueReportTests", Guid.NewGuid().ToString("N"));
        public string GetPath(string name) => Path.Combine(_path, name);
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_path)) Directory.Delete(_path, recursive: true);
        }
    }
}
