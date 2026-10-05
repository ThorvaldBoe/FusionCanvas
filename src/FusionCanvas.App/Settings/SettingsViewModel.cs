using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Versioning;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Versioning;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.Application.TermsConsent;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.App.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged, IWindowGeometryStore
{
    private readonly IApplicationSettingsStore _store;
    private readonly TermsConsentService _termsConsentService;
    private readonly IApplicationThemeController _themeController;
    private readonly IApplicationVersionProvider _versionProvider;
    private readonly IClipboardService _clipboard;
    private readonly SynchronizationContext? _syncContext;
    private WorkspaceManagementViewModel? _workspaceManagement;
    private SettingsSection _selectedSection = SettingsSection.General;
    private bool _isOpen;
    private bool _isDarkMode;
    private ApplicationSettings _currentSettings;
    private bool _confirmDiscardCredentialDraft;
    private string? _errorMessage;
    private string _workspaceName = "No workspace";
    private int _saveGeneration;
    private readonly object _saveGate = new();
    private ApplicationSettings? _pendingSettings;
    private bool _retryPendingSave;
    private Task<SaveAttempt> _saveChain = Task.FromResult(SaveAttempt.Success);
    private int _busyOperationCount;

    public SettingsViewModel(
        IApplicationSettingsStore store,
        IApplicationThemeController themeController,
        ApplicationSettings initialSettings,
        string? loadWarning,
        AiSettingsViewModel? ai = null,
        IApplicationVersionProvider? versionProvider = null,
        IClipboardService? clipboard = null,
        ITelemetryService? telemetryService = null,
        ITelemetryWorkspaceContext? telemetryWorkspaceContext = null,
        TermsConsentService? termsConsentService = null,
        IUpdateService? updateService = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _termsConsentService = termsConsentService ?? new TermsConsentService(_store);
        _themeController = themeController ?? throw new ArgumentNullException(nameof(themeController));
        _versionProvider = versionProvider ?? UnknownApplicationVersionProvider.Instance;
        _clipboard = clipboard ?? NullClipboardService.Instance;
        Telemetry = new WorkspaceTelemetrySettingsViewModel(telemetryService, telemetryWorkspaceContext, _clipboard);
        _syncContext = SynchronizationContext.Current;
        _currentSettings = initialSettings;
        _isDarkMode = initialSettings.DarkMode;
        _errorMessage = loadWarning;
        _themeController.ApplyDarkMode(_isDarkMode);
        Version = _versionProvider.GetVersion();
        DiagnosticsText = ApplicationVersionDiagnostics.Format(Version, ApplicationVersionDiagnostics.BuildPlatformString());
        Updates = new UpdateViewModel(updateService ?? new DisabledUpdateService());

        OpenCommand = new RelayCommand(_ => Open());
        Ai = ai ?? CreateOfflineAi(initialSettings.Ai);
        Ai.PropertyChanged += (_, _) => OnPropertyChanged(nameof(IsBusy));
        Telemetry.PropertyChanged += (_, _) => OnPropertyChanged(nameof(IsBusy));
        Ai.SettingsChanged += (_, _) =>
        {
            _currentSettings = _currentSettings with { Ai = Ai.Current };
            QueueSave(_currentSettings);
        };

        CloseCommand = new RelayCommand(_ => RequestClose());
        ConfirmDiscardCommand = new RelayCommand(_ =>
        {
            Ai.DiscardCredentialDraft();
            ConfirmDiscardCredentialDraft = false;
            IsOpen = false;
        });
        CancelDiscardCommand = new RelayCommand(_ => ConfirmDiscardCredentialDraft = false);
        ManageWorkspacesCommand = new RelayCommand(_ => ManageWorkspaces(), () => _workspaceManagement is not null);
        CopyDiagnosticsCommand = new RelayCommand(_ => Run(CopyDiagnosticsAsync));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsSection SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetField(ref _selectedSection, value))
            {
                OnPropertyChanged(nameof(IsGeneralSection));
                OnPropertyChanged(nameof(IsAiSection));
                OnPropertyChanged(nameof(IsWorkspaceSection));
                OnPropertyChanged(nameof(IsTermsSection));
                OnPropertyChanged(nameof(IsAboutSection));
                if (value == SettingsSection.AI)
                {
                    Ai.EnsureLoaded();
                }
            }
        }
    }

    public bool IsGeneralSection => _selectedSection == SettingsSection.General;

    public bool IsWorkspaceSection => _selectedSection == SettingsSection.Workspace;

    public bool IsTermsSection => _selectedSection == SettingsSection.Terms;

    public bool IsAiSection => _selectedSection == SettingsSection.AI;

    public bool IsAboutSection => _selectedSection == SettingsSection.About;

    public IReadOnlyList<SettingsSection> Sections { get; } = new[]
    {
        SettingsSection.General,
        SettingsSection.AI,
        SettingsSection.Workspace,
        SettingsSection.Terms,
        SettingsSection.About
    };

    public ApplicationVersionInfo Version { get; }

    public string DiagnosticsText { get; }

    public AiSettingsViewModel Ai { get; }

    public WorkspaceTelemetrySettingsViewModel Telemetry { get; }

    public UpdateViewModel Updates { get; }

    internal void ReplaceUpdateService(IUpdateService service)
    {
        Updates.ReplaceService(service);
    }

    public WindowLayoutSettings? WindowLayout => _currentSettings.WindowLayout;

    public IReadOnlyDictionary<string, WindowGeometrySettings> WindowGeometry =>
        _currentSettings.WindowGeometry ?? ImmutableDictionary<string, WindowGeometrySettings>.Empty;

    public Guid? ActiveWorkspaceId => _currentSettings.ActiveWorkspaceId;

    public Guid? ActiveStoreId => _currentSettings.ActiveStoreId;

    public TermsConsentStatus TermsConsentStatus => TermsConsentStatus.From(_currentSettings.TermsConsent);

    public string TermsConsentSummary => TermsConsentStatus.Summary;

    public bool HasCurrentTermsConsent => TermsConsentStatus.IsCurrent;

    public bool ConfirmDiscardCredentialDraft
    {
        get => _confirmDiscardCredentialDraft;
        private set => SetField(ref _confirmDiscardCredentialDraft, value);
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetField(ref _isOpen, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetField(ref _isDarkMode, value))
            {
                _themeController.ApplyDarkMode(value);
                _currentSettings = _currentSettings with { DarkMode = value };
                QueueSave(_currentSettings);
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy => Volatile.Read(ref _busyOperationCount) > 0 || Ai.IsBusy || Telemetry.IsBusy;

    public string WorkspaceName
    {
        get => _workspaceName;
        private set => SetField(ref _workspaceName, value);
    }

    public ICommand OpenCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand ManageWorkspacesCommand { get; }
    public ICommand ConfirmDiscardCommand { get; }
    public ICommand CancelDiscardCommand { get; }
    public ICommand CopyDiagnosticsCommand { get; }

    public TermsConsentViewModel CreateTermsConsentViewModel(IExternalLinkLauncher? linkLauncher = null)
    {
        var viewModel = new TermsConsentViewModel(
            _currentSettings,
            _termsConsentService,
            TermsConsentPolicyDocument.Load(),
            linkLauncher);
        viewModel.Accepted += OnTermsConsentAccepted;
        return viewModel;
    }

    public void AttachWorkspaceManagement(WorkspaceManagementViewModel workspaceManagement)
    {
        ArgumentNullException.ThrowIfNull(workspaceManagement);

        if (_workspaceManagement is not null)
        {
            _workspaceManagement.ActiveWorkspaceChanged -= OnActiveWorkspaceChanged;
        }

        _workspaceManagement = workspaceManagement;
        _workspaceManagement.ActiveWorkspaceChanged += OnActiveWorkspaceChanged;
        UpdateWorkspaceName(_workspaceManagement.SelectedWorkspace);
        Telemetry.SetWorkspace(_workspaceManagement.SelectedWorkspace?.Id, _workspaceManagement.SelectedWorkspace?.Name);
    }

    public async Task FlushAsync()
    {
        Task<SaveAttempt> save;
        lock (_saveGate)
        {
            if (_retryPendingSave && _pendingSettings is { } pendingSettings)
            {
                QueueSaveCore(pendingSettings);
            }

            save = _saveChain;
        }

        var attempt = await save.ConfigureAwait(false);
        if (!attempt.Succeeded)
        {
            throw new InvalidOperationException(attempt.Warning ?? "The application settings could not be saved and may not survive restart.");
        }
    }

    public void Dispose() => Updates.Dispose();

    public void UpdateWindowLayout(WindowLayoutSettings? layout)
    {
        if (_currentSettings.WindowLayout == layout)
        {
            return;
        }

        _currentSettings = _currentSettings with { WindowLayout = layout };
        QueueSave(_currentSettings);
    }

    public void UpdateWindowGeometry(string windowKey, WindowGeometrySettings? geometry)
    {
        ArgumentNullException.ThrowIfNull(windowKey);
        var current = _currentSettings.WindowGeometry ?? ImmutableDictionary<string, WindowGeometrySettings>.Empty;
        var updated = geometry is null
            ? (current.ContainsKey(windowKey) ? current.Remove(windowKey) : current)
            : current.SetItem(windowKey, geometry);

        if (ReferenceEquals(updated, current))
        {
            return;
        }

        _currentSettings = _currentSettings with { WindowGeometry = updated };
        QueueSave(_currentSettings);
    }

    private void OnActiveWorkspaceChanged(object? sender, WorkspaceSummary? workspace)
    {
        UpdateWorkspaceName(workspace);
        UpdateActiveWorkspace(workspace?.Id);
        Telemetry.SetWorkspace(workspace?.Id, workspace?.Name);
    }

    private void OnTermsConsentAccepted(ApplicationSettings settings)
    {
        _currentSettings = settings;
        OnPropertyChanged(nameof(TermsConsentStatus));
        OnPropertyChanged(nameof(TermsConsentSummary));
        OnPropertyChanged(nameof(HasCurrentTermsConsent));
    }

    public void UpdateActiveWorkspace(Guid? workspaceId)
    {
        if (_currentSettings.ActiveWorkspaceId == workspaceId)
        {
            return;
        }

        _currentSettings = _currentSettings with { ActiveWorkspaceId = workspaceId };
        QueueSave(_currentSettings);
    }

    public void UpdateActiveStore(Guid? storeId)
    {
        if (_currentSettings.ActiveStoreId == storeId)
        {
            return;
        }

        _currentSettings = _currentSettings with { ActiveStoreId = storeId };
        QueueSave(_currentSettings);
    }

    private void UpdateWorkspaceName(WorkspaceSummary? workspace)
        => WorkspaceName = workspace is null ? "No workspace" : workspace.Name;

    private void Open()
    {
        SelectedSection = SettingsSection.General;
        IsOpen = true;
    }

    public bool RequestClose()
    {
        if (Ai.HasUnsavedCredentialDraft)
        {
            ConfirmDiscardCredentialDraft = true;
            return false;
        }

        ConfirmDiscardCredentialDraft = false;
        IsOpen = false;
        return true;
    }

    private void ManageWorkspaces()
    {
        if (_workspaceManagement is { } management && management.OpenWorkspaceManagementCommand.CanExecute(null))
        {
            management.OpenWorkspaceManagementCommand.Execute(null);
        }
    }

    private async Task CopyDiagnosticsAsync()
    {
        await _clipboard.SetTextAsync(DiagnosticsText).ConfigureAwait(true);
    }

    private void Run(Func<Task> operation) => _ = ObserveBusyAsync(operation);

    private async Task ObserveBusyAsync(Func<Task> operation)
    {
        BeginBusy();
        try
        {
            await ObserveAsync(operation).ConfigureAwait(true);
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task ObserveAsync(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            SetMessage("Diagnostics could not be copied to the clipboard.");
        }
    }

    private void QueueSave(ApplicationSettings settings)
    {
        lock (_saveGate)
        {
            QueueSaveCore(settings);
        }
    }

    private void QueueSaveCore(ApplicationSettings settings)
    {
        var generation = Interlocked.Increment(ref _saveGeneration);
        _pendingSettings = settings;
        _retryPendingSave = false;
        BeginBusy();
        _saveChain = _saveChain
            .ContinueWith(_ => PersistAndEndBusyAsync(generation, settings), TaskScheduler.Default)
            .Unwrap();
    }

    private void BeginBusy()
    {
        Interlocked.Increment(ref _busyOperationCount);
        OnPropertyChanged(nameof(IsBusy));
    }

    private void EndBusy()
    {
        Interlocked.Decrement(ref _busyOperationCount);
        OnPropertyChanged(nameof(IsBusy));
    }

    private async Task<SaveAttempt> PersistAndEndBusyAsync(int generation, ApplicationSettings settings)
    {
        try
        {
            return await PersistAsync(generation, settings).ConfigureAwait(false);
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task<SaveAttempt> PersistAsync(int generation, ApplicationSettings settings)
    {
        if (generation != Volatile.Read(ref _saveGeneration))
        {
            return SaveAttempt.Skipped;
        }

        try
        {
            var result = await _store.SaveAsync(settings).ConfigureAwait(false);
            var isCurrent = false;
            lock (_saveGate)
            {
                if (generation == Volatile.Read(ref _saveGeneration))
                {
                    isCurrent = true;
                    if (result.Saved)
                    {
                        _pendingSettings = null;
                        _retryPendingSave = false;
                    }
                    else
                    {
                        _retryPendingSave = true;
                    }
                }
            }

            if (isCurrent)
            {
                SetMessage(result.Saved ? null : result.Warning);
            }

            return result.Saved
                ? SaveAttempt.Success
                : new SaveAttempt(false, result.Warning);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            var isCurrent = false;
            lock (_saveGate)
            {
                if (generation == Volatile.Read(ref _saveGeneration))
                {
                    isCurrent = true;
                    _retryPendingSave = true;
                }
            }

            if (isCurrent)
            {
                SetMessage("The application settings could not be saved and may not survive restart.");
            }

            return new SaveAttempt(false, "The application settings could not be saved and may not survive restart.");
        }
    }

    private void SetMessage(string? message)
    {
        if (_syncContext is not null)
        {
            _syncContext.Post(_ => ErrorMessage = message, null);
        }
        else
        {
            ErrorMessage = message;
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName == nameof(ErrorMessage))
        {
            OnPropertyChanged(nameof(HasMessage));
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static AiSettingsViewModel CreateOfflineAi(AiConfigurationSettings settings)
    {
        var credentials = new OfflineCredentialStore();
        return new AiSettingsViewModel(
            settings,
            credentials,
            new OfflineCredentialValidator(),
            new OfflineCatalogProvider(),
            new OfflineCatalogCache());
    }

    private sealed class OfflineCredentialStore : IAiCredentialStore
    {
        public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiCredentialReadResult.Failure(
                AiCredentialStateKind.Unavailable,
                "Native credential storage is unavailable in this session."));
        public Task<AiCredentialOperationResult> SaveAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(AiCredentialOperationResult.Failed("Native credential storage is unavailable in this session."));
        public Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiCredentialOperationResult.Failed("Native credential storage is unavailable in this session."));
    }

    private sealed class OfflineCredentialValidator : IAiCredentialValidator
    {
        public Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCredentialValidationResult(AiCredentialValidationKind.NetworkFailure));
    }

    private sealed class OfflineCatalogProvider : IAiModelCatalogProvider
    {
        public Task<AiModelCatalog> GetModelsAsync(
            string apiKey,
            bool requireZeroDataRetention,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiModelCatalog(requireZeroDataRetention, DateTimeOffset.UtcNow, []));
    }

    private sealed class OfflineCatalogCache : IAiModelCatalogCache
    {
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult<AiModelCatalog?>(null);
        public Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private readonly record struct SaveAttempt(bool Succeeded, string? Warning)
    {
        public static SaveAttempt Success { get; } = new(true, null);

        public static SaveAttempt Skipped { get; } = new(true, null);
    }
}
