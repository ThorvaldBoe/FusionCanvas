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

namespace FusionCanvas.App.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IApplicationSettingsStore _store;
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
    private Task _saveChain = Task.CompletedTask;

    public SettingsViewModel(
        IApplicationSettingsStore store,
        IApplicationThemeController themeController,
        ApplicationSettings initialSettings,
        string? loadWarning,
        AiSettingsViewModel? ai = null,
        IApplicationVersionProvider? versionProvider = null,
        IClipboardService? clipboard = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _themeController = themeController ?? throw new ArgumentNullException(nameof(themeController));
        _versionProvider = versionProvider ?? UnknownApplicationVersionProvider.Instance;
        _clipboard = clipboard ?? NullClipboardService.Instance;
        _syncContext = SynchronizationContext.Current;
        _currentSettings = initialSettings;
        _isDarkMode = initialSettings.DarkMode;
        _errorMessage = loadWarning;
        _themeController.ApplyDarkMode(_isDarkMode);
        Version = _versionProvider.GetVersion();
        DiagnosticsText = ApplicationVersionDiagnostics.Format(Version, ApplicationVersionDiagnostics.BuildPlatformString());

        OpenCommand = new RelayCommand(_ => Open());
        Ai = ai ?? CreateOfflineAi(initialSettings.Ai);
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
        CopyDiagnosticsCommand = new RelayCommand(_ => CopyDiagnostics());
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
                OnPropertyChanged(nameof(IsAboutSection));
                if (value == SettingsSection.AI)
                {
                    _ = Ai.EnsureLoadedAsync();
                }
            }
        }
    }

    public bool IsGeneralSection => _selectedSection == SettingsSection.General;

    public bool IsWorkspaceSection => _selectedSection == SettingsSection.Workspace;

    public bool IsAiSection => _selectedSection == SettingsSection.AI;

    public bool IsAboutSection => _selectedSection == SettingsSection.About;

    public IReadOnlyList<SettingsSection> Sections { get; } = new[]
    {
        SettingsSection.General,
        SettingsSection.AI,
        SettingsSection.Workspace,
        SettingsSection.About
    };

    public ApplicationVersionInfo Version { get; }

    public string DiagnosticsText { get; }

    public AiSettingsViewModel Ai { get; }

    public WindowLayoutSettings? WindowLayout => _currentSettings.WindowLayout;

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
    }

    public async Task FlushAsync()
    {
        try
        {
            await _saveChain.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    public void UpdateWindowLayout(WindowLayoutSettings? layout)
    {
        if (_currentSettings.WindowLayout == layout)
        {
            return;
        }

        _currentSettings = _currentSettings with { WindowLayout = layout };
        QueueSave(_currentSettings);
    }

    private void OnActiveWorkspaceChanged(object? sender, WorkspaceSummary? workspace)
        => UpdateWorkspaceName(workspace);

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

    private void CopyDiagnostics()
    {
        var text = DiagnosticsText;
        _ = _clipboard.SetTextAsync(text);
    }

    private void QueueSave(ApplicationSettings settings)
    {
        var generation = Interlocked.Increment(ref _saveGeneration);
        _saveChain = _saveChain
            .ContinueWith(_ => PersistAsync(generation, settings), TaskScheduler.Default)
            .Unwrap();
    }

    private async Task PersistAsync(int generation, ApplicationSettings settings)
    {
        if (generation != Volatile.Read(ref _saveGeneration))
        {
            return;
        }

        try
        {
            var result = await _store.SaveAsync(settings).ConfigureAwait(false);
            if (generation == Volatile.Read(ref _saveGeneration))
            {
                SetMessage(result.Saved ? null : result.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            if (generation == Volatile.Read(ref _saveGeneration))
            {
                SetMessage("The application settings could not be saved and may not survive restart.");
            }
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
            Task.FromResult(AiCredentialReadResult.NotFound);
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
}
