using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.App.Settings;

public sealed class WorkspaceTelemetrySettingsViewModel : INotifyPropertyChanged
{
    private readonly ITelemetryService? _service;
    private readonly ITelemetryWorkspaceContext? _workspaceContext;
    private readonly IClipboardService _clipboard;
    private readonly SynchronizationContext? _syncContext = SynchronizationContext.Current;
    private Guid? _workspaceId;
    private string _workspaceName = "No workspace";
    private WorkspaceTelemetrySettings _settings = WorkspaceTelemetrySettings.Default;
    private string _fromText = string.Empty;
    private string _toText = string.Empty;
    private string _selectedArea = string.Empty;
    private string _resultText = string.Empty;
    private string _windowText = string.Empty;
    private string? _statusMessage;
    private bool _isBusy;
    private bool _confirmDelete;
    private ITelemetryExportFilePicker? _filePicker;

    public WorkspaceTelemetrySettingsViewModel(
        ITelemetryService? service,
        ITelemetryWorkspaceContext? workspaceContext,
        IClipboardService clipboard)
    {
        _service = service;
        _workspaceContext = workspaceContext;
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        if (_service is not null) _service.EntryRecorded += OnEntryRecorded;
        SearchCommand = new RelayCommand(_ => Run(SearchAsync()), () => HasWorkspace && !IsBusy);
        ExportCommand = new RelayCommand(_ => Run(ExportAsync()), () => HasWorkspace && !IsBusy && _service is not null && _filePicker is not null);
        RequestDeleteCommand = new RelayCommand(_ => ConfirmDelete = true, () => HasWorkspace && !IsBusy);
        ConfirmDeleteCommand = new RelayCommand(_ => Run(DeleteAsync()), () => HasWorkspace && !IsBusy && ConfirmDelete);
        CancelDeleteCommand = new RelayCommand(_ => ConfirmDelete = false);
        CopyWindowCommand = new RelayCommand(_ => Run(CopyWindowAsync()), () => WindowText.Length > 0);
        ClearWindowCommand = new RelayCommand(_ => WindowText = string.Empty, () => WindowText.Length > 0);
        CloseWindowCommand = new RelayCommand(_ => SetShowDebugWindow(false), () => IsDebugWindowOpen);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TelemetryRetentionPeriod> RetentionPeriods { get; } =
        [TelemetryRetentionPeriod.OneHour, TelemetryRetentionPeriod.OneDay, TelemetryRetentionPeriod.OneWeek];

    public IReadOnlyList<string> Areas { get; } = ["All areas", .. TelemetryAreas.All];

    public Guid? WorkspaceId => _workspaceId;
    public string WorkspaceName => _workspaceName;
    public bool HasWorkspace => _workspaceId is not null;
    public bool HasNoWorkspace => !HasWorkspace;
    public string DeleteConfirmationText => $"Delete all telemetry for {_workspaceName}? This cannot be undone.";
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public string? StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string FromText { get => _fromText; set => SetField(ref _fromText, value); }
    public string ToText { get => _toText; set => SetField(ref _toText, value); }
    public string SelectedArea { get => _selectedArea; set => SetField(ref _selectedArea, value); }
    public string ResultText { get => _resultText; private set => SetField(ref _resultText, value); }
    public string WindowText
    {
        get => _windowText;
        private set
        {
            if (SetField(ref _windowText, value))
            {
                NotifyCommandState();
            }
        }
    }

    public bool ConfirmDelete
    {
        get => _confirmDelete;
        private set
        {
            if (SetField(ref _confirmDelete, value)) NotifyCommandState();
        }
    }

    public bool DebugModeEnabled
    {
        get => _settings.DebugModeEnabled;
        set
        {
            if (_settings.DebugModeEnabled == value) return;
            _settings = _settings with { DebugModeEnabled = value, ShowDebugWindow = value && _settings.ShowDebugWindow };
            RaiseSettingsChanged();
            Run(SaveSettingsAsync());
        }
    }

    public TelemetryRetentionPeriod RetentionPeriod
    {
        get => _settings.RetentionPeriod;
        set
        {
            if (_settings.RetentionPeriod == value) return;
            _settings = _settings with { RetentionPeriod = value };
            RaiseSettingsChanged();
            Run(SaveSettingsAsync());
        }
    }

    public bool ShowDebugWindow
    {
        get => _settings.ShowDebugWindow;
        set => SetShowDebugWindow(value);
    }

    public bool IsDebugWindowOpen => HasWorkspace && _settings.DebugModeEnabled && _settings.ShowDebugWindow;

    public ICommand SearchCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RequestDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand CopyWindowCommand { get; }
    public ICommand ClearWindowCommand { get; }
    public ICommand CloseWindowCommand { get; }

    public void SetExportFilePicker(ITelemetryExportFilePicker? picker)
    {
        _filePicker = picker;
        NotifyCommandState();
    }

    public void SetWorkspace(Guid? workspaceId, string? workspaceName)
    {
        if (_workspaceId == workspaceId && _workspaceName == (workspaceName ?? "No workspace")) return;
        var wasOpen = IsDebugWindowOpen;
        _workspaceId = workspaceId;
        _workspaceName = workspaceName ?? "No workspace";
        _workspaceContext?.SetActiveWorkspace(workspaceId);
        _settings = WorkspaceTelemetrySettings.Default;
        ResultText = string.Empty;
        WindowText = string.Empty;
        StatusMessage = null;
        OnPropertyChanged(nameof(WorkspaceId));
        OnPropertyChanged(nameof(WorkspaceName));
        OnPropertyChanged(nameof(HasWorkspace));
        OnPropertyChanged(nameof(HasNoWorkspace));
        OnPropertyChanged(nameof(DeleteConfirmationText));
        OnPropertyChanged(nameof(DebugModeEnabled));
        OnPropertyChanged(nameof(RetentionPeriod));
        OnPropertyChanged(nameof(ShowDebugWindow));
        OnPropertyChanged(nameof(IsDebugWindowOpen));
        NotifyCommandState();
        if (wasOpen) OnPropertyChanged(nameof(IsDebugWindowOpen));
        if (workspaceId is { } id && _service is not null) Run(LoadSettingsAsync(id));
    }

    public void CloseDebugWindow() => SetShowDebugWindow(false);

    public Task RecordAsync(TelemetryEventRequest request) =>
        _service?.RecordAsync(request) ?? Task.CompletedTask;

    private void SetShowDebugWindow(bool value)
    {
        if (_settings.ShowDebugWindow == value && (!value || _settings.DebugModeEnabled)) return;
        _settings = _settings with
        {
            DebugModeEnabled = value || _settings.DebugModeEnabled,
            ShowDebugWindow = value
        };
        RaiseSettingsChanged();
        Run(SaveSettingsAsync(recordWindowOpened: value));
    }

    private async Task LoadSettingsAsync(Guid workspaceId)
    {
        try
        {
            var settings = await _service!.GetSettingsAsync(workspaceId).ConfigureAwait(false);
            Dispatch(() =>
            {
                if (_workspaceId != workspaceId) return;
                _settings = settings;
                OnPropertyChanged(nameof(DebugModeEnabled));
                OnPropertyChanged(nameof(RetentionPeriod));
                OnPropertyChanged(nameof(ShowDebugWindow));
                OnPropertyChanged(nameof(IsDebugWindowOpen));
                NotifyCommandState();
            });
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Workspace diagnostics settings could not be loaded.");
        }
    }

    private async Task SaveSettingsAsync(bool recordWindowOpened = false)
    {
        if (_workspaceId is not { } id || _service is null) return;
        var settings = _settings;
        try
        {
            await _service.SaveSettingsAsync(id, settings).ConfigureAwait(false);
            if (recordWindowOpened && settings.DebugModeEnabled && settings.ShowDebugWindow)
            {
                await _service.RecordAsync(new TelemetryEventRequest(
                    "Workspace", "DebugWindowOpened", "Information", "Succeeded",
                    "Opened the live telemetry window.")).ConfigureAwait(false);
            }
            Dispatch(() => StatusMessage = null);
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Workspace diagnostics settings could not be saved.");
        }
    }

    private async Task SearchAsync()
    {
        if (_workspaceId is not { } id || _service is null) return;
        if (!TryParseInstant(FromText, out var from) || !TryParseInstant(ToText, out var to))
        {
            StatusMessage = "Enter valid UTC timestamps in ISO 8601 format, or leave either field blank.";
            return;
        }
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var areas = SelectedArea is "" or "All areas" ? null : new[] { SelectedArea };
            var entries = await _service.SearchAsync(id, new TelemetryQuery(from, to, areas, 500)).ConfigureAwait(false);
            var text = string.Join(Environment.NewLine + Environment.NewLine, entries.Select(entry => entry.FormatForDisplay()));
            Dispatch(() =>
            {
                ResultText = text;
                StatusMessage = entries.Count == 0 ? "No telemetry records match this search." : $"Showing {entries.Count} telemetry records.";
            });
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Telemetry search could not be completed.");
        }
        finally
        {
            Dispatch(() => IsBusy = false);
        }
    }

    private async Task ExportAsync()
    {
        if (_workspaceId is not { } id || _service is null || _filePicker is null) return;
        var path = await _filePicker.PickPathAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(path)) return;
        IsBusy = true;
        try
        {
            var json = await _service.ExportJsonAsync(id).ConfigureAwait(false);
            await File.WriteAllTextAsync(path, json, new System.Text.UTF8Encoding(false)).ConfigureAwait(false);
            Dispatch(() => StatusMessage = "Telemetry export completed.");
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Telemetry export could not be completed.");
        }
        finally
        {
            Dispatch(() => IsBusy = false);
        }
    }

    private async Task DeleteAsync()
    {
        if (_workspaceId is not { } id || _service is null) return;
        IsBusy = true;
        try
        {
            var count = await _service.DeleteAllAsync(id).ConfigureAwait(false);
            Dispatch(() =>
            {
                ConfirmDelete = false;
                ResultText = string.Empty;
                StatusMessage = $"Deleted {count} telemetry records from {_workspaceName}.";
            });
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Telemetry records could not be deleted.");
        }
        finally
        {
            Dispatch(() => IsBusy = false);
        }
    }

    private async Task CopyWindowAsync()
    {
        try
        {
            await _clipboard.SetTextAsync(WindowText).ConfigureAwait(false);
            Dispatch(() => StatusMessage = "Debug output copied.");
        }
        catch (Exception)
        {
            Dispatch(() => StatusMessage = "Debug output could not be copied.");
        }
    }

    private void OnEntryRecorded(object? sender, TelemetryEntry entry)
    {
        void Append()
        {
            if (entry.WorkspaceId != _workspaceId || !IsDebugWindowOpen) return;
            WindowText = string.IsNullOrEmpty(WindowText)
                ? entry.FormatForDisplay()
                : WindowText + Environment.NewLine + Environment.NewLine + entry.FormatForDisplay();
        }
        Dispatch(Append);
    }

    private static bool TryParseInstant(string value, out DateTimeOffset? instant)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            instant = null;
            return true;
        }
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            instant = parsed;
            return true;
        }
        instant = null;
        return false;
    }

    private void RaiseSettingsChanged()
    {
        OnPropertyChanged(nameof(DebugModeEnabled));
        OnPropertyChanged(nameof(RetentionPeriod));
        OnPropertyChanged(nameof(ShowDebugWindow));
        OnPropertyChanged(nameof(IsDebugWindowOpen));
        NotifyCommandState();
    }

    private void NotifyCommandState()
    {
        foreach (var command in new ICommand?[] { SearchCommand, ExportCommand, RequestDeleteCommand, ConfirmDeleteCommand, CopyWindowCommand, ClearWindowCommand, CloseWindowCommand })
        {
            (command as RelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void Run(Task task) => _ = ObserveAsync(task);

    private async Task ObserveAsync(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (Exception) { Dispatch(() => StatusMessage = "The diagnostics operation could not be completed."); }
    }

    private void Dispatch(Action action)
    {
        if (_syncContext is null || SynchronizationContext.Current == _syncContext) action();
        else _syncContext.Post(_ => action(), null);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
