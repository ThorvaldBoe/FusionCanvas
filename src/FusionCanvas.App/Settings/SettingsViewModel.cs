using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IApplicationSettingsStore _store;
    private readonly IApplicationThemeController _themeController;
    private readonly SynchronizationContext? _syncContext;
    private WorkspaceManagementViewModel? _workspaceManagement;
    private SettingsSection _selectedSection = SettingsSection.General;
    private bool _isOpen;
    private bool _isDarkMode;
    private string? _errorMessage;
    private string _workspaceName = "No workspace";
    private int _saveGeneration;
    private Task _saveChain = Task.CompletedTask;

    public SettingsViewModel(
        IApplicationSettingsStore store,
        IApplicationThemeController themeController,
        ApplicationSettings initialSettings,
        string? loadWarning)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _themeController = themeController ?? throw new ArgumentNullException(nameof(themeController));
        _syncContext = SynchronizationContext.Current;
        _isDarkMode = initialSettings.DarkMode;
        _errorMessage = loadWarning;
        _themeController.ApplyDarkMode(_isDarkMode);

        OpenCommand = new RelayCommand(_ => Open());
        CloseCommand = new RelayCommand(_ => Close());
        ManageWorkspacesCommand = new RelayCommand(_ => ManageWorkspaces(), () => _workspaceManagement is not null);
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
                OnPropertyChanged(nameof(IsWorkspaceSection));
            }
        }
    }

    public bool IsGeneralSection => _selectedSection == SettingsSection.General;

    public bool IsWorkspaceSection => _selectedSection == SettingsSection.Workspace;

    public IReadOnlyList<SettingsSection> Sections { get; } = new[]
    {
        SettingsSection.General,
        SettingsSection.Workspace
    };

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
                QueueSave(value);
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

    private void OnActiveWorkspaceChanged(object? sender, WorkspaceSummary? workspace)
        => UpdateWorkspaceName(workspace);

    private void UpdateWorkspaceName(WorkspaceSummary? workspace)
        => WorkspaceName = workspace is null ? "No workspace" : workspace.Name;

    private void Open()
    {
        SelectedSection = SettingsSection.General;
        IsOpen = true;
    }

    private void Close() => IsOpen = false;

    private void ManageWorkspaces()
    {
        if (_workspaceManagement is { } management && management.OpenWorkspaceManagementCommand.CanExecute(null))
        {
            management.OpenWorkspaceManagementCommand.Execute(null);
        }
    }

    private void QueueSave(bool darkMode)
    {
        var generation = Interlocked.Increment(ref _saveGeneration);
        _saveChain = _saveChain
            .ContinueWith(_ => PersistAsync(generation, darkMode), TaskScheduler.Default)
            .Unwrap();
    }

    private async Task PersistAsync(int generation, bool darkMode)
    {
        if (generation != Volatile.Read(ref _saveGeneration))
        {
            return;
        }

        try
        {
            var result = await _store.SaveAsync(new ApplicationSettings(darkMode)).ConfigureAwait(false);
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
                SetMessage("The appearance preference could not be saved and may not survive restart.");
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
}
