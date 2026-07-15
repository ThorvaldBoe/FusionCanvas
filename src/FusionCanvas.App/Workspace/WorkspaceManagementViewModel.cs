using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Workspace;

public sealed record WorkspaceSelectorEntry(WorkspaceSummary Workspace, bool IsSelected)
{
    public Guid Id => Workspace.Id;

    public string Name => Workspace.Name;
}

public sealed class WorkspaceManagementViewModel : INotifyPropertyChanged
{
    private readonly IWorkspaceManagementService _service;
    private bool _isSelectorExpanded;
    private bool _isWorkspaceManagementOpen;
    private bool _isCreatingNewWorkspace;
    private bool _deleteWarningVisible;
    private string _workspaceName = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _deleteConfirmationName = string.Empty;
    private string? _errorMessage;
    private WorkspaceSummary? _pendingDeleteWorkspace;

    public WorkspaceManagementViewModel(IWorkspaceManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ToggleWorkspaceSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = !IsSelectorExpanded);
        SelectWorkspaceCommand = new RelayCommand(parameter =>
        {
            if (parameter is WorkspaceSelectorEntry entry)
            {
                Run(SelectWorkspaceAsync(entry.Workspace));
            }
            else if (parameter is WorkspaceSummary workspace)
            {
                Run(SelectWorkspaceAsync(workspace));
            }
        });
        CreateWorkspaceCommand = new RelayCommand(_ => Run(CreateWorkspaceAsync()));
        SaveSelectedWorkspaceCommand = new RelayCommand(_ => Run(SaveSelectedWorkspaceAsync()));
        ArchiveSelectedWorkspaceCommand = new RelayCommand(_ => Run(ArchiveSelectedWorkspaceAsync()));
        RestoreWorkspaceCommand = new RelayCommand(parameter =>
        {
            if (parameter is WorkspaceSummary workspace)
            {
                Run(RestoreWorkspaceAsync(workspace));
            }
        });
        OpenWorkspaceManagementCommand = new RelayCommand(_ => IsWorkspaceManagementOpen = true);
        CloseWorkspaceManagementCommand = new RelayCommand(_ => IsWorkspaceManagementOpen = false);
        StartCreateWorkspaceCommand = new RelayCommand(_ => StartCreateWorkspace());
        DeleteSelectedWorkspaceCommand = new RelayCommand(_ => RequestDeleteSelectedWorkspace());
        RequestDeleteSelectedWorkspaceCommand = new RelayCommand(_ => RequestDeleteSelectedWorkspace());
        ConfirmDeleteWorkspaceCommand = new RelayCommand(_ => Run(ConfirmDeleteWorkspaceAsync()));
        CancelDeleteWorkspaceCommand = new RelayCommand(_ => ClearDeleteWarning());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<WorkspaceSummary?>? ActiveWorkspaceChanged;

    public IReadOnlyList<WorkspaceSummary> ActiveWorkspaces { get; private set; } = [];

    public IReadOnlyList<WorkspaceSummary> ArchivedWorkspaces { get; private set; } = [];

    public IReadOnlyList<WorkspaceSelectorEntry> SelectorWorkspaces { get; private set; } = [];

    public WorkspaceSummary? SelectedWorkspace { get; private set; }

    public bool NeedsFirstWorkspace { get; private set; }

    public bool HasActiveWorkspaces => ActiveWorkspaces.Count > 0;

    public bool HasArchivedWorkspaces => ArchivedWorkspaces.Count > 0;

    public bool HasSelectedWorkspace => SelectedWorkspace is not null;

    public bool ShouldShowNoWorkspaceState => NeedsFirstWorkspace || SelectedWorkspace is null;

    public bool CanSaveSelectedWorkspace => SelectedWorkspace is not null && !IsCreatingNewWorkspace;

    public bool CanArchiveSelectedWorkspace => SelectedWorkspace is { IsArchived: false } && !IsCreatingNewWorkspace;

    public bool CanDeleteSelectedWorkspace => SelectedWorkspace is not null && !IsCreatingNewWorkspace;

    public bool IsWorkspaceManagementOpen
    {
        get => _isWorkspaceManagementOpen;
        set => SetField(ref _isWorkspaceManagementOpen, value);
    }

    public bool IsCreatingNewWorkspace
    {
        get => _isCreatingNewWorkspace;
        private set
        {
            if (SetField(ref _isCreatingNewWorkspace, value))
            {
                OnPropertyChanged(nameof(CanSaveSelectedWorkspace));
                OnPropertyChanged(nameof(CanArchiveSelectedWorkspace));
                OnPropertyChanged(nameof(CanDeleteSelectedWorkspace));
            }
        }
    }

    public bool DeleteWarningVisible
    {
        get => _deleteWarningVisible;
        private set => SetField(ref _deleteWarningVisible, value);
    }

    public string DeleteConfirmationName
    {
        get => _deleteConfirmationName;
        set
        {
            if (SetField(ref _deleteConfirmationName, value))
            {
                OnPropertyChanged(nameof(CanConfirmDeleteWorkspace));
            }
        }
    }

    public bool CanConfirmDeleteWorkspace =>
        _pendingDeleteWorkspace is not null &&
        string.Equals(DeleteConfirmationName.Trim(), _pendingDeleteWorkspace.Name, StringComparison.Ordinal);

    public string DeleteWarningMessage => _pendingDeleteWorkspace is null
        ? string.Empty
        : $"Delete '{_pendingDeleteWorkspace.Name}' permanently? Type the workspace name to confirm. All stores and data in this workspace will be lost with no possibility for recovery.";

    public string SelectorToggleGlyph => IsSelectorExpanded ? "▲" : "▼";

    public string SelectorToggleTooltip => IsSelectorExpanded ? "Collapse workspaces" : "Expand workspaces";

    public bool IsSelectorExpanded
    {
        get => _isSelectorExpanded;
        private set
        {
            if (SetField(ref _isSelectorExpanded, value))
            {
                OnPropertyChanged(nameof(IsSelectorCompact));
                OnPropertyChanged(nameof(SelectorToggleGlyph));
                OnPropertyChanged(nameof(SelectorToggleTooltip));
            }
        }
    }

    public bool IsSelectorCompact => !IsSelectorExpanded;

    public string WorkspaceName
    {
        get => _workspaceName;
        set => SetField(ref _workspaceName, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand ToggleWorkspaceSelectorCommand { get; }

    public ICommand SelectWorkspaceCommand { get; }

    public ICommand OpenWorkspaceManagementCommand { get; }

    public ICommand CloseWorkspaceManagementCommand { get; }

    public ICommand StartCreateWorkspaceCommand { get; }

    public ICommand CreateWorkspaceCommand { get; }

    public ICommand SaveSelectedWorkspaceCommand { get; }

    public ICommand ArchiveSelectedWorkspaceCommand { get; }

    public ICommand RestoreWorkspaceCommand { get; }

    public ICommand DeleteSelectedWorkspaceCommand { get; }

    public ICommand RequestDeleteSelectedWorkspaceCommand { get; }

    public ICommand ConfirmDeleteWorkspaceCommand { get; }

    public ICommand CancelDeleteWorkspaceCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(cancellationToken).ConfigureAwait(false);
        ApplyState(state);
    }

    public async Task CreateWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var name = string.IsNullOrWhiteSpace(WorkspaceName) ? "New workspace" : WorkspaceName;
        var result = await _service.CreateWorkspaceAsync(new WorkspaceManagementCreateRequest(name, CurrentContext()), cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            IsCreatingNewWorkspace = false;
            ClearDeleteWarning();
        }
    }

    public async Task SaveSelectedWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedWorkspace is null)
        {
            ErrorMessage = "Select a workspace before saving.";
            return;
        }

        var result = await _service.UpdateWorkspaceAsync(new WorkspaceManagementUpdateRequest(SelectedWorkspace.Id, WorkspaceName, CurrentContext()), cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            ClearDeleteWarning();
        }
    }

    public async Task ArchiveSelectedWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedWorkspace is null)
        {
            ErrorMessage = "Select a workspace before archiving.";
            return;
        }

        var result = await _service.ArchiveWorkspaceAsync(SelectedWorkspace.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            ClearDeleteWarning();
        }
    }

    public async Task RestoreWorkspaceAsync(WorkspaceSummary workspace, CancellationToken cancellationToken = default)
    {
        var result = await _service.RestoreWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            ClearDeleteWarning();
        }
    }

    public void StartCreateWorkspace()
    {
        ErrorMessage = null;
        IsCreatingNewWorkspace = true;
        WorkspaceName = "New workspace";
        Description = string.Empty;
        Notes = string.Empty;
        ClearDeleteWarning();
    }

    public void RequestDeleteSelectedWorkspace()
    {
        if (SelectedWorkspace is null)
        {
            ErrorMessage = "Select a workspace before deleting.";
            return;
        }

        _pendingDeleteWorkspace = SelectedWorkspace;
        DeleteConfirmationName = string.Empty;
        DeleteWarningVisible = true;
        OnPropertyChanged(nameof(DeleteWarningMessage));
        OnPropertyChanged(nameof(CanConfirmDeleteWorkspace));
    }

    public async Task ConfirmDeleteWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingDeleteWorkspace is null)
        {
            return;
        }

        if (!CanConfirmDeleteWorkspace)
        {
            ErrorMessage = "Type the workspace name to confirm deletion.";
            return;
        }

        var result = await _service.DeleteWorkspaceAsync(
            new WorkspaceManagementDeleteRequest(
                _pendingDeleteWorkspace.Id,
                ConfirmPermanentDeletion: true,
                DeleteConfirmationName),
            cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            ClearDeleteWarning();
        }
    }

    public async Task SelectWorkspaceAsync(WorkspaceSummary workspace, CancellationToken cancellationToken = default)
    {
        var result = await _service.SelectWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            IsCreatingNewWorkspace = false;
            ClearDeleteWarning();
        }
    }

    private void ApplyResult(WorkspaceManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyState(result.State);
    }

    private void ApplyState(WorkspaceManagementState state)
    {
        ActiveWorkspaces = state.ActiveWorkspaces;
        ArchivedWorkspaces = state.ArchivedWorkspaces;
        SelectedWorkspace = state.ActiveWorkspace;
        NeedsFirstWorkspace = state.NeedsFirstWorkspace;
        SelectorWorkspaces = ActiveWorkspaces
            .Select(workspace => new WorkspaceSelectorEntry(workspace, workspace.Id == state.ActiveWorkspaceId))
            .ToArray();
        ApplySelectedWorkspaceFields(SelectedWorkspace);

        OnPropertyChanged(nameof(ActiveWorkspaces));
        OnPropertyChanged(nameof(ArchivedWorkspaces));
        OnPropertyChanged(nameof(SelectorWorkspaces));
        OnPropertyChanged(nameof(SelectedWorkspace));
        OnPropertyChanged(nameof(NeedsFirstWorkspace));
        OnPropertyChanged(nameof(HasActiveWorkspaces));
        OnPropertyChanged(nameof(HasArchivedWorkspaces));
        OnPropertyChanged(nameof(HasSelectedWorkspace));
        OnPropertyChanged(nameof(ShouldShowNoWorkspaceState));
        OnPropertyChanged(nameof(CanSaveSelectedWorkspace));
        OnPropertyChanged(nameof(CanArchiveSelectedWorkspace));
        OnPropertyChanged(nameof(CanDeleteSelectedWorkspace));
        ActiveWorkspaceChanged?.Invoke(this, SelectedWorkspace);
    }

    private void ApplySelectedWorkspaceFields(WorkspaceSummary? workspace)
    {
        if (workspace is null)
        {
            WorkspaceName = string.Empty;
            Description = string.Empty;
            Notes = string.Empty;
            return;
        }

        WorkspaceName = workspace.Name;
        Description = workspace.Context.Description ?? string.Empty;
        Notes = workspace.Context.Notes ?? string.Empty;
    }

    private WorkspaceContext CurrentContext() =>
        new(EmptyToNull(Description), EmptyToNull(Notes));

    private void ClearDeleteWarning()
    {
        _pendingDeleteWorkspace = null;
        DeleteConfirmationName = string.Empty;
        DeleteWarningVisible = false;
        OnPropertyChanged(nameof(DeleteWarningMessage));
        OnPropertyChanged(nameof(CanConfirmDeleteWorkspace));
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Run(Task task) => _ = task;

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
