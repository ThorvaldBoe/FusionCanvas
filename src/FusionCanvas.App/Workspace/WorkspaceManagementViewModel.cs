using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;

namespace FusionCanvas.App.Workspace;

public sealed record WorkspaceSelectorEntry(WorkspaceSummary Workspace, bool IsSelected)
{
    public Guid Id => Workspace.Id;

    public string Name => Workspace.Name;
}

public sealed class WorkspaceManagementViewModel : INotifyPropertyChanged
{
    private readonly IWorkspaceManagementService _service;
    private readonly IWorkspaceTransferService? _transferService;
    private IWorkspacePackagePicker _packagePicker;
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
    private Guid? _activeWorkspaceId;
    private bool _isTransferRunning;
    private double _transferProgress;
    private string? _transferPhase;
    private string? _transferSummary;
    private CancellationTokenSource? _transferCancellation;

    public WorkspaceManagementViewModel(
        IWorkspaceManagementService service,
        IWorkspaceTransferService? transferService = null,
        IWorkspacePackagePicker? packagePicker = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _transferService = transferService;
        _packagePicker = packagePicker ?? new NullWorkspacePackagePicker();
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
        ReviewArchivedWorkspaceCommand = new RelayCommand(parameter =>
        {
            if (parameter is WorkspaceSummary workspace && workspace.IsArchived && !IsTransferRunning)
            {
                SelectForManagement(workspace);
            }
        });
        OpenWorkspaceManagementCommand = new RelayCommand(_ => IsWorkspaceManagementOpen = true);
        CloseWorkspaceManagementCommand = new RelayCommand(_ => IsWorkspaceManagementOpen = false);
        StartCreateWorkspaceCommand = new RelayCommand(_ => StartCreateWorkspace());
        DeleteSelectedWorkspaceCommand = new RelayCommand(_ => RequestDeleteSelectedWorkspace());
        RequestDeleteSelectedWorkspaceCommand = new RelayCommand(_ => RequestDeleteSelectedWorkspace());
        ConfirmDeleteWorkspaceCommand = new RelayCommand(_ => Run(ConfirmDeleteWorkspaceAsync()));
        CancelDeleteWorkspaceCommand = new RelayCommand(_ => ClearDeleteWarning());
        ExportSelectedWorkspaceCommand = new RelayCommand(_ => Run(ExportSelectedWorkspaceAsync()));
        ImportWorkspaceCommand = new RelayCommand(_ => Run(ImportWorkspaceAsync()));
        CancelTransferCommand = new RelayCommand(_ => _transferCancellation?.Cancel());
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

    public bool ShouldShowNoWorkspaceState => NeedsFirstWorkspace || _activeWorkspaceId is null;

    public bool CanSaveSelectedWorkspace => SelectedWorkspace is not null && !IsCreatingNewWorkspace && !IsTransferRunning;

    public bool CanArchiveSelectedWorkspace => SelectedWorkspace is { IsArchived: false } && !IsCreatingNewWorkspace && !IsTransferRunning;

    public bool CanRestoreSelectedWorkspace => SelectedWorkspace is { IsArchived: true } && !IsCreatingNewWorkspace && !IsTransferRunning;

    public bool CanDeleteSelectedWorkspace => SelectedWorkspace is not null && !IsCreatingNewWorkspace && !IsTransferRunning;

    public bool CanExportSelectedWorkspace => SelectedWorkspace is not null && !IsTransferRunning && _transferService is not null;

    public bool CanImportWorkspace => !IsTransferRunning && _transferService is not null;

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
                OnPropertyChanged(nameof(CanRestoreSelectedWorkspace));
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

    public IWorkspacePackagePicker PackagePicker
    {
        get => _packagePicker;
        set => _packagePicker = value ?? new NullWorkspacePackagePicker();
    }

    public bool IsTransferRunning
    {
        get => _isTransferRunning;
        private set
        {
            if (SetField(ref _isTransferRunning, value))
            {
                RaiseTransferActionState();
            }
        }
    }

    public double TransferProgress
    {
        get => _transferProgress;
        private set => SetField(ref _transferProgress, value);
    }

    public string? TransferPhase
    {
        get => _transferPhase;
        private set => SetField(ref _transferPhase, value);
    }

    public string? TransferSummary
    {
        get => _transferSummary;
        private set
        {
            if (SetField(ref _transferSummary, value))
            {
                OnPropertyChanged(nameof(HasTransferSummary));
            }
        }
    }

    public bool HasTransferSummary => !string.IsNullOrWhiteSpace(TransferSummary);

    public ICommand ToggleWorkspaceSelectorCommand { get; }

    public ICommand SelectWorkspaceCommand { get; }

    public ICommand OpenWorkspaceManagementCommand { get; }

    public ICommand CloseWorkspaceManagementCommand { get; }

    public ICommand StartCreateWorkspaceCommand { get; }

    public ICommand CreateWorkspaceCommand { get; }

    public ICommand SaveSelectedWorkspaceCommand { get; }

    public ICommand ArchiveSelectedWorkspaceCommand { get; }

    public ICommand RestoreWorkspaceCommand { get; }

    public ICommand ReviewArchivedWorkspaceCommand { get; }

    public ICommand DeleteSelectedWorkspaceCommand { get; }

    public ICommand RequestDeleteSelectedWorkspaceCommand { get; }

    public ICommand ConfirmDeleteWorkspaceCommand { get; }

    public ICommand CancelDeleteWorkspaceCommand { get; }

    public ICommand ExportSelectedWorkspaceCommand { get; }

    public ICommand ImportWorkspaceCommand { get; }

    public ICommand CancelTransferCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(cancellationToken).ConfigureAwait(false);
        ApplyState(state);
    }

    public async Task CreateWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (IsTransferRunning) return;
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
        if (IsTransferRunning) return;
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
        if (IsTransferRunning) return;
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
        if (IsTransferRunning) return;
        var result = await _service.RestoreWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
        if (result.Succeeded)
        {
            ClearDeleteWarning();
        }
    }

    public void StartCreateWorkspace()
    {
        if (IsTransferRunning) return;
        ErrorMessage = null;
        IsCreatingNewWorkspace = true;
        WorkspaceName = "New workspace";
        Description = string.Empty;
        Notes = string.Empty;
        ClearDeleteWarning();
    }

    public void RequestDeleteSelectedWorkspace()
    {
        if (IsTransferRunning) return;
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
        if (IsTransferRunning) return;
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
        // ApplyState raises bound property and workspace-change notifications, so resume on
        // Avalonia's UI context when this operation originates from a workspace button.
        var result = await _service.SelectWorkspaceAsync(workspace.Id, cancellationToken).ConfigureAwait(true);
        ApplyResult(result);
        if (result.Succeeded)
        {
            IsCreatingNewWorkspace = false;
            ClearDeleteWarning();
        }
    }

    public async Task ExportSelectedWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (!CanExportSelectedWorkspace || SelectedWorkspace is not { } workspace || _transferService is null)
        {
            return;
        }

        var defaultName = $"{SafeFileName(workspace.Name)}-{DateTimeOffset.Now:yyyyMMdd}.fcworkspace";
        var destination = await PackagePicker.PickExportDestinationAsync(defaultName, cancellationToken).ConfigureAwait(true);
        if (destination is null)
        {
            return;
        }

        await RunTransferAsync(
            (progress, token) => _transferService.ExportWorkspaceAsync(
                new WorkspaceExportRequest(workspace.Id, destination),
                progress,
                token),
            openManagementOnFailure: false,
            cancellationToken).ConfigureAwait(true);
    }

    public async Task ImportWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (!CanImportWorkspace || _transferService is null)
        {
            return;
        }

        var packagePath = await PackagePicker.PickImportPackageAsync(cancellationToken).ConfigureAwait(true);
        if (packagePath is null)
        {
            return;
        }

        var result = await RunTransferAsync(
            (progress, token) => _transferService.ImportWorkspaceAsync(
                new WorkspaceImportRequest(packagePath),
                progress,
                token),
            openManagementOnFailure: true,
            cancellationToken).ConfigureAwait(true);
        if (result?.Succeeded == true && result.WorkspaceId is Guid workspaceId)
        {
            ApplyResult(await _service.SelectWorkspaceAsync(workspaceId, cancellationToken).ConfigureAwait(true));
        }
    }

    private async Task<WorkspaceTransferResult?> RunTransferAsync(
        Func<IProgress<WorkspaceTransferProgress>, CancellationToken, Task<WorkspaceTransferResult>> operation,
        bool openManagementOnFailure,
        CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        TransferSummary = null;
        TransferProgress = 0;
        TransferPhase = "Preparing";
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _transferCancellation = linked;
        IsTransferRunning = true;
        try
        {
            var progress = new Progress<WorkspaceTransferProgress>(value =>
            {
                TransferPhase = value.Phase;
                TransferProgress = value.Total <= 0 ? 0 : Math.Clamp((double)value.Completed / value.Total, 0, 1);
            });
            var result = await operation(progress, linked.Token).ConfigureAwait(true);
            if (result.Succeeded && result.Summary is { } summary)
            {
                TransferSummary = FormatSummary(summary);
            }
            else if (!result.Cancelled)
            {
                ErrorMessage = result.Error ?? "Workspace transfer failed.";
                if (openManagementOnFailure)
                {
                    IsWorkspaceManagementOpen = true;
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return WorkspaceTransferResult.CancelledResult();
        }
        finally
        {
            _transferCancellation = null;
            IsTransferRunning = false;
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
        _activeWorkspaceId = state.ActiveWorkspaceId;
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
        OnPropertyChanged(nameof(CanRestoreSelectedWorkspace));
        OnPropertyChanged(nameof(CanDeleteSelectedWorkspace));
        OnPropertyChanged(nameof(CanExportSelectedWorkspace));
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

    private void SelectForManagement(WorkspaceSummary workspace)
    {
        SelectedWorkspace = workspace;
        IsCreatingNewWorkspace = false;
        ErrorMessage = null;
        ApplySelectedWorkspaceFields(workspace);
        OnPropertyChanged(nameof(SelectedWorkspace));
        OnPropertyChanged(nameof(HasSelectedWorkspace));
        OnPropertyChanged(nameof(ShouldShowNoWorkspaceState));
        OnPropertyChanged(nameof(CanSaveSelectedWorkspace));
        OnPropertyChanged(nameof(CanArchiveSelectedWorkspace));
        OnPropertyChanged(nameof(CanRestoreSelectedWorkspace));
        OnPropertyChanged(nameof(CanDeleteSelectedWorkspace));
        OnPropertyChanged(nameof(CanExportSelectedWorkspace));
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

    private void RaiseTransferActionState()
    {
        OnPropertyChanged(nameof(CanSaveSelectedWorkspace));
        OnPropertyChanged(nameof(CanArchiveSelectedWorkspace));
        OnPropertyChanged(nameof(CanRestoreSelectedWorkspace));
        OnPropertyChanged(nameof(CanDeleteSelectedWorkspace));
        OnPropertyChanged(nameof(CanExportSelectedWorkspace));
        OnPropertyChanged(nameof(CanImportWorkspace));
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(name.Trim().Select(character => invalid.Contains(character) ? '-' : character).ToArray());
    }

    private static string FormatSummary(WorkspaceTransferSummary summary)
    {
        var entityCount = summary.EntityCounts.Values.Sum();
        var parts = new List<string>
        {
            $"{entityCount} records",
            $"{summary.WrittenFiles + summary.RestoredFiles} files"
        };
        if (summary.SkippedExistingFiles > 0) parts.Add($"{summary.SkippedExistingFiles} existing skipped");
        if (summary.MissingFiles.Count > 0) parts.Add($"{summary.MissingFiles.Count} missing");
        if (summary.SkippedUnsupportedFiles.Count > 0) parts.Add($"{summary.SkippedUnsupportedFiles.Count} unsupported skipped");
        if (!string.Equals(summary.OriginalWorkspaceName, summary.FinalWorkspaceName, StringComparison.Ordinal))
        {
            parts.Add($"renamed to {summary.FinalWorkspaceName}");
        }

        return string.Join(" · ", parts);
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
