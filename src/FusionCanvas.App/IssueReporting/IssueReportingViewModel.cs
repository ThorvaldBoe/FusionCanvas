using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.App.Versioning;

namespace FusionCanvas.App.IssueReporting;

public sealed class IssueReportingViewModel : INotifyPropertyChanged, IDisposable
{
    public static readonly Uri IssueFormUri = new("https://github.com/ThorvaldBoe/FusionCanvas/issues/new?template=bug_report.yml");
    private static readonly TimeSpan CopyWindow = TimeSpan.FromMinutes(10);

    private readonly WorkspaceTelemetrySettingsViewModel _telemetry;
    private readonly IClipboardService _clipboard;
    private readonly IReportScreenshotStager _screenshotStager;
    private readonly IExternalLinkLauncher _linkLauncher;
    private Guid? _observedWorkspaceId;
    private Guid? _captureWorkspaceId;
    private DateTimeOffset? _captureStartedAt;
    private bool _debugWasEnabledAtCaptureStart;
    private bool _isBusy;
    private bool _isClearConfirmationVisible;
    private bool _hasCopiedDiagnostics;
    private bool _hasDiagnosticsInCapture;
    private string _copyButtonText = "Copy diagnostics";
    private string? _statusMessage;
    private StagedScreenshot? _screenshot;
    private bool _disposed;

    public IssueReportingViewModel(
        WorkspaceTelemetrySettingsViewModel telemetry,
        IClipboardService clipboard,
        IReportScreenshotStager screenshotStager,
        IExternalLinkLauncher linkLauncher)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _screenshotStager = screenshotStager ?? throw new ArgumentNullException(nameof(screenshotStager));
        _linkLauncher = linkLauncher ?? throw new ArgumentNullException(nameof(linkLauncher));
        _observedWorkspaceId = telemetry.WorkspaceId;
        _telemetry.PropertyChanged += OnTelemetryPropertyChanged;
        _telemetry.ReportTelemetryRecorded += OnReportTelemetryRecorded;
        PasteScreenshotCommand = new RelayCommand(_ => Run(PasteScreenshotAsync), () => !IsBusy);
        RemoveScreenshotCommand = new RelayCommand(_ => RemoveScreenshot(), () => !IsBusy && HasScreenshot);
        OpenScreenshotLocationCommand = new RelayCommand(_ => OpenScreenshotLocation(), () => !IsBusy && HasScreenshot);
        CopyDiagnosticsCommand = new RelayCommand(_ => Run(CopyDiagnosticsAsync), () => CanCopyDiagnostics);
        OpenIssueFormCommand = new RelayCommand(_ => OpenIssueForm());
        RequestClearCaptureCommand = new RelayCommand(_ => IsClearConfirmationVisible = true, () => HasCapture && !IsBusy);
        ConfirmClearCaptureCommand = new RelayCommand(_ => Run(ClearCaptureAsync), () => HasCapture && !IsBusy && IsClearConfirmationVisible);
        CancelClearCaptureCommand = new RelayCommand(_ => IsClearConfirmationVisible = false);
        if (_observedWorkspaceId is not null && telemetry.DebugModeEnabled)
        {
            StartCapture(debugWasAlreadyEnabled: true);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WorkspaceName => _telemetry.WorkspaceName;
    public bool HasWorkspace => _telemetry.HasWorkspace;
    public bool HasNoWorkspace => !HasWorkspace;
    public bool DebugModeEnabled
    {
        get => _telemetry.DebugModeEnabled;
        set
        {
            if (_telemetry.DebugModeEnabled != value && HasWorkspace && !IsBusy)
            {
                Run(() => SetDebugModeAsync(value));
            }
        }
    }
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) NotifyCommands(); } }
    public bool IsClearConfirmationVisible { get => _isClearConfirmationVisible; private set { if (SetField(ref _isClearConfirmationVisible, value)) NotifyCommands(); } }
    public bool HasCopiedDiagnostics { get => _hasCopiedDiagnostics; private set { if (SetField(ref _hasCopiedDiagnostics, value)) NotifyCommands(); } }
    public string CopyButtonText { get => _copyButtonText; private set => SetField(ref _copyButtonText, value); }
    public string? StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public Bitmap? ScreenshotPreview => _screenshot?.Preview;
    public string? ScreenshotFileName => _screenshot is null ? null : Path.GetFileName(_screenshot.Path);
    public bool HasScreenshot => _screenshot is not null;
    public bool HasCapture => _captureStartedAt is not null && _captureWorkspaceId is not null;
    public bool CanCopyDiagnostics => HasCapture && _hasDiagnosticsInCapture && !IsBusy;

    public ICommand PasteScreenshotCommand { get; }
    public ICommand RemoveScreenshotCommand { get; }
    public ICommand OpenScreenshotLocationCommand { get; }
    public ICommand CopyDiagnosticsCommand { get; }
    public ICommand OpenIssueFormCommand { get; }
    public ICommand RequestClearCaptureCommand { get; }
    public ICommand ConfirmClearCaptureCommand { get; }
    public ICommand CancelClearCaptureCommand { get; }

    public async Task InitializeAsync()
    {
        if (_observedWorkspaceId is not null)
        {
            await _telemetry.RefreshSettingsAsync();
            OnPropertyChanged(nameof(DebugModeEnabled));
            if (DebugModeEnabled && _captureStartedAt is null)
            {
                StartCapture(debugWasAlreadyEnabled: true);
            }
        }
        RaiseWorkspaceProperties();
    }

    public async Task CloseAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _telemetry.PropertyChanged -= OnTelemetryPropertyChanged;
        _telemetry.ReportTelemetryRecorded -= OnReportTelemetryRecorded;
        if (_screenshot is not null)
        {
            _screenshot.Preview.Dispose();
            _screenshotStager.Delete(_screenshot.Path);
            _screenshot = null;
        }
        await Task.CompletedTask;
    }

    private void OnTelemetryPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(WorkspaceTelemetrySettingsViewModel.WorkspaceId) or nameof(WorkspaceTelemetrySettingsViewModel.WorkspaceName))
        {
            var nextWorkspaceId = _telemetry.WorkspaceId;
            if (nextWorkspaceId != _observedWorkspaceId)
            {
                var previousWorkspaceId = _observedWorkspaceId;
                _observedWorkspaceId = nextWorkspaceId;
                Run(() => HandleWorkspaceChangedAsync(previousWorkspaceId));
            }
            RaiseWorkspaceProperties();
            return;
        }

        if (args.PropertyName == nameof(WorkspaceTelemetrySettingsViewModel.DebugModeEnabled))
        {
            OnPropertyChanged(nameof(DebugModeEnabled));
            NotifyCommands();
        }
    }

    private void OnReportTelemetryRecorded(object? sender, FusionCanvas.Application.Telemetry.TelemetryEntry entry)
    {
        if (_captureStartedAt is not { } startedAt || _captureWorkspaceId != entry.WorkspaceId) return;
        if (entry.OccurredAt < startedAt || entry.OccurredAt < DateTimeOffset.UtcNow - CopyWindow) return;
        _hasDiagnosticsInCapture = true;
        OnPropertyChanged(nameof(CanCopyDiagnostics));
        NotifyCommands();
    }

    private async Task HandleWorkspaceChangedAsync(Guid? previousWorkspaceId)
    {
        if (_captureStartedAt is { } startedAt && _captureWorkspaceId is { } capturedWorkspaceId)
        {
            var endedAt = EndBoundary(startedAt, DateTimeOffset.UtcNow);
            await _telemetry.ClearReportWindowAsync(capturedWorkspaceId, startedAt, endedAt);
            if (!_debugWasEnabledAtCaptureStart && capturedWorkspaceId != _telemetry.WorkspaceId)
            {
                await _telemetry.SetDebugModeForWorkspaceAsync(capturedWorkspaceId, false);
            }
        }

        _captureStartedAt = null;
        _captureWorkspaceId = null;
        _debugWasEnabledAtCaptureStart = false;
        _hasDiagnosticsInCapture = false;
        HasCopiedDiagnostics = false;
        IsClearConfirmationVisible = false;
        CopyButtonText = "Copy diagnostics";
        StatusMessage = previousWorkspaceId is null ? null : "The report capture ended because the active workspace changed.";
        await _telemetry.RefreshSettingsAsync();
        OnPropertyChanged(nameof(DebugModeEnabled));
        OnPropertyChanged(nameof(HasCapture));
        OnPropertyChanged(nameof(CanCopyDiagnostics));
        NotifyCommands();
    }

    private async Task SetDebugModeAsync(bool enabled)
    {
        if (!HasWorkspace || IsBusy) return;
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var previous = _telemetry.DebugModeEnabled;
            await _telemetry.SetDebugModeAsync(enabled);
            if (enabled && !previous)
            {
                StartCapture(debugWasAlreadyEnabled: false);
            }
            OnPropertyChanged(nameof(DebugModeEnabled));
            StatusMessage = enabled
                ? "Debug Mode is on for this workspace. Reproduce the issue, then copy diagnostics."
                : "Debug Mode is off. Report diagnostics remain available until copied or cleared.";
        }
        catch (Exception)
        {
            StatusMessage = "Debug Mode could not be changed. Try again from Workspace Diagnostics in Settings.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    private void StartCapture(bool debugWasAlreadyEnabled)
    {
        if (_telemetry.WorkspaceId is not { } workspaceId) return;
        _captureWorkspaceId = workspaceId;
        _captureStartedAt = DateTimeOffset.UtcNow;
        _debugWasEnabledAtCaptureStart = debugWasAlreadyEnabled;
        _hasDiagnosticsInCapture = false;
        HasCopiedDiagnostics = false;
        CopyButtonText = "Copy diagnostics";
        OnPropertyChanged(nameof(HasCapture));
        OnPropertyChanged(nameof(CanCopyDiagnostics));
        NotifyCommands();
    }

    private async Task CopyDiagnosticsAsync()
    {
        if (_captureStartedAt is not { } captureStart || _captureWorkspaceId is not { } workspaceId) return;
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var from = captureStart > now - CopyWindow ? captureStart : now - CopyWindow;
            var batch = await _telemetry.ReadReportWindowAsync(workspaceId, from, EndBoundary(from, now));
            if (batch.Entries.Count == 0)
            {
                _hasDiagnosticsInCapture = false;
                OnPropertyChanged(nameof(CanCopyDiagnostics));
                NotifyCommands();
                StatusMessage = "No diagnostics were recorded in the last ten minutes. Reproduce the issue with Debug Mode on, then try again.";
                return;
            }

            var text = ReportDiagnosticsFormatter.Format(batch);
            await _clipboard.SetTextAsync(text);
            HasCopiedDiagnostics = true;
            CopyButtonText = "Copied to clipboard";
            StatusMessage = "Paste the copied diagnostics into the GitHub issue. Review them before sharing.";
        }
        catch (Exception)
        {
            StatusMessage = "Diagnostics could not be copied. The report capture remains available.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    private async Task PasteScreenshotAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var staged = await _screenshotStager.PasteFromClipboardAsync();
            var previous = _screenshot;
            _screenshot = staged;
            if (previous is not null)
            {
                previous.Preview.Dispose();
                _screenshotStager.Delete(previous.Path);
            }
            OnPropertyChanged(nameof(ScreenshotPreview));
            OnPropertyChanged(nameof(ScreenshotFileName));
            OnPropertyChanged(nameof(HasScreenshot));
            StatusMessage = "Screenshot ready. Open its folder and attach the image in GitHub when you submit the report.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    private void RemoveScreenshot()
    {
        if (_screenshot is null) return;
        _screenshot.Preview.Dispose();
        _screenshotStager.Delete(_screenshot.Path);
        _screenshot = null;
        OnPropertyChanged(nameof(ScreenshotPreview));
        OnPropertyChanged(nameof(ScreenshotFileName));
        OnPropertyChanged(nameof(HasScreenshot));
        StatusMessage = "Staged screenshot removed.";
        NotifyCommands();
    }

    private void OpenScreenshotLocation()
    {
        if (_screenshot is null) return;
        try
        {
            _screenshotStager.OpenLocation(_screenshot.Path);
            StatusMessage = "Drag the staged screenshot into GitHub's optional upload field.";
        }
        catch (Exception)
        {
            StatusMessage = "The screenshot folder could not be opened. The staged image remains available at " + _screenshot.Path;
        }
    }

    private void OpenIssueForm()
    {
        try
        {
            _linkLauncher.Open(IssueFormUri);
            StatusMessage = "GitHub opened in your browser. Sign in if asked, write the issue description, paste diagnostics, and attach the screenshot if you staged one.";
        }
        catch (Exception)
        {
            StatusMessage = $"GitHub could not be opened. Copy this address into your browser: {IssueFormUri}";
        }
    }

    private async Task ClearCaptureAsync()
    {
        if (_captureStartedAt is not { } startedAt || _captureWorkspaceId is not { } workspaceId) return;
        IsBusy = true;
        try
        {
            if (workspaceId == _telemetry.WorkspaceId && DebugModeEnabled)
            {
                await _telemetry.SetDebugModeAsync(false);
            }
            var endedAt = EndBoundary(startedAt, DateTimeOffset.UtcNow);
            var deleted = await _telemetry.ClearReportWindowAsync(workspaceId, startedAt, endedAt);
            _captureStartedAt = null;
            _captureWorkspaceId = null;
            _debugWasEnabledAtCaptureStart = false;
            _hasDiagnosticsInCapture = false;
            HasCopiedDiagnostics = false;
            IsClearConfirmationVisible = false;
            CopyButtonText = "Copy diagnostics";
            OnPropertyChanged(nameof(HasCapture));
            OnPropertyChanged(nameof(CanCopyDiagnostics));
            StatusMessage = $"Debug Mode is off and {deleted} report diagnostic records were cleared from this capture.";
        }
        catch (Exception)
        {
            StatusMessage = "The report capture could not be cleared. Existing records were left in place where possible.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
        }
    }

    private void RaiseWorkspaceProperties()
    {
        OnPropertyChanged(nameof(WorkspaceName));
        OnPropertyChanged(nameof(HasWorkspace));
        OnPropertyChanged(nameof(HasNoWorkspace));
        OnPropertyChanged(nameof(DebugModeEnabled));
        OnPropertyChanged(nameof(CanCopyDiagnostics));
        NotifyCommands();
    }

    private void NotifyCommands()
    {
        foreach (var command in new ICommand[] { PasteScreenshotCommand, RemoveScreenshotCommand, OpenScreenshotLocationCommand, CopyDiagnosticsCommand, RequestClearCaptureCommand, ConfirmClearCaptureCommand })
        {
            (command as RelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void Run(Func<Task> operation) => _ = ObserveAsync(operation);

    private async Task ObserveAsync(Func<Task> operation)
    {
        try { await operation(); }
        catch (Exception) { StatusMessage = "The report preparation action could not be completed."; }
    }

    private static DateTimeOffset EndBoundary(DateTimeOffset start, DateTimeOffset end) => end <= start ? start.AddTicks(1) : end;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose() => _ = CloseAsync();
}
