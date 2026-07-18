using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Groups;

public sealed class GroupManagementViewModel : INotifyPropertyChanged
{
    private enum PendingAction
    {
        None,
        Close,
        StartCreate,
        Select,
        Move,
        Archive,
        Restore
    }

    private sealed record EditorState(string Name, string Description, string Notes);

    private readonly IGroupManagementService _service;
    private PendingAction _pendingAction;
    private GroupSummary? _pendingGroup;
    private GroupDestination? _pendingDestination;
    private EditorState _originalState = new(string.Empty, string.Empty, string.Empty);
    private bool _isOpen;
    private bool _isBusy;
    private bool _isCreating;
    private bool _discardPromptVisible;
    private bool _archiveWarningVisible;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string? _errorMessage;
    private GroupSummary? _selectedGroup;
    private GroupDestination? _selectedDestination;
    private GroupParentReference? _draftParent;

    public GroupManagementViewModel(IGroupManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        SelectGroupCommand = new RelayCommand(parameter =>
        {
            if (parameter is GroupSummary group)
            {
                Run(TrySelectAsync(group));
            }
        });
        StartCreateCommand = new RelayCommand(_ => TryBeginCreate());
        SaveCommand = new RelayCommand(_ => Run(SaveAsync()));
        MoveCommand = new RelayCommand(_ => Run(TryMoveAsync()));
        RequestArchiveCommand = new RelayCommand(_ => TryRequestArchive());
        ConfirmArchiveCommand = new RelayCommand(_ => Run(ConfirmArchiveAsync()));
        CancelArchiveCommand = new RelayCommand(_ => ArchiveWarningVisible = false);
        RestoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is GroupSummary group)
            {
                Run(TryRestoreAsync(group));
            }
        });
        CloseCommand = new RelayCommand(_ => TryClose());
        SaveAndContinueCommand = new RelayCommand(_ => Run(SaveAndContinueAsync()));
        DiscardAndContinueCommand = new RelayCommand(_ => Run(DiscardAndContinueAsync()));
        KeepEditingCommand = new RelayCommand(_ => ClearPendingAction());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? GroupNameFocusRequested;
    public event EventHandler? GroupListFocusRequested;
    public event EventHandler<GroupSummary?>? WorkspaceStructureChanged;

    public IReadOnlyList<GroupSummary> ActiveGroups { get; private set; } = [];
    public IReadOnlyList<GroupSummary> ArchivedGroups { get; private set; } = [];
    public IReadOnlyList<GroupDestination> Destinations { get; private set; } = [];

    public GroupSummary? SelectedGroup
    {
        get => _selectedGroup;
        private set
        {
            if (SetField(ref _selectedGroup, value))
            {
                RaiseActionProperties();
            }
        }
    }

    public GroupDestination? SelectedDestination
    {
        get => _selectedDestination;
        set
        {
            if (SetField(ref _selectedDestination, value))
            {
                OnPropertyChanged(nameof(CanMove));
            }
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetField(ref _isOpen, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseActionProperties();
            }
        }
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set
        {
            if (SetField(ref _isCreating, value))
            {
                RaiseActionProperties();
            }
        }
    }

    public bool DiscardPromptVisible
    {
        get => _discardPromptVisible;
        private set => SetField(ref _discardPromptVisible, value);
    }

    public bool ArchiveWarningVisible
    {
        get => _archiveWarningVisible;
        private set => SetField(ref _archiveWarningVisible, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                RaiseEditProperties();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetField(ref _description, value))
            {
                RaiseEditProperties();
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            if (SetField(ref _notes, value))
            {
                RaiseEditProperties();
            }
        }
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
    public bool HasActiveGroups => ActiveGroups.Count > 0;
    public bool HasArchivedGroups => ArchivedGroups.Count > 0;
    public bool HasSelection => SelectedGroup is not null || IsCreating;
    public bool HasUnsavedChanges => CurrentState() != _originalState;
    public bool CanSave => HasSelection && HasUnsavedChanges && !IsBusy && !string.IsNullOrWhiteSpace(Name);
    public bool CanMove => SelectedGroup is { IsEffectivelyActive: true } && SelectedDestination is not null && !IsBusy && !IsCreating;
    public bool CanArchive => SelectedGroup is { IsEffectivelyActive: true } && !IsBusy && !IsCreating;
    public bool CanRestore => SelectedGroup is { IsArchived: true } && !IsBusy && !IsCreating;
    public bool ShowsEmptyState => !HasActiveGroups && !IsCreating;

    public ICommand SelectGroupCommand { get; }
    public ICommand StartCreateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand RequestArchiveCommand { get; }
    public ICommand ConfirmArchiveCommand { get; }
    public ICommand CancelArchiveCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SaveAndContinueCommand { get; }
    public ICommand DiscardAndContinueCommand { get; }
    public ICommand KeepEditingCommand { get; }

    public void SetActiveWorkspace(Guid? workspaceId) => _service.SetActiveWorkspace(workspaceId);

    public async Task OpenForCreateAsync(Guid storeId, Guid nicheId, GroupParentReference parent, CancellationToken cancellationToken = default)
    {
        IsOpen = true;
        await LoadStateAsync(storeId, nicheId, cancellationToken).ConfigureAwait(false);
        BeginCreate(parent);
    }

    public async Task OpenForEditAsync(Guid storeId, Guid nicheId, Guid groupId, CancellationToken cancellationToken = default)
    {
        IsOpen = true;
        await LoadStateAsync(storeId, nicheId, cancellationToken).ConfigureAwait(false);
        var group = ActiveGroups.Concat(ArchivedGroups).SingleOrDefault(candidate => candidate.Id == groupId);
        if (group is not null)
        {
            await SelectImmediatelyAsync(group, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RefreshAsync(Guid? storeId, Guid? nicheId, CancellationToken cancellationToken = default)
    {
        if (!IsOpen)
        {
            return;
        }

        await LoadStateAsync(storeId, nicheId, cancellationToken).ConfigureAwait(false);
    }

    public void BeginCreate(GroupParentReference parent)
    {
        _draftParent = parent;
        SelectedGroup = null;
        IsCreating = true;
        Name = string.Empty;
        Description = string.Empty;
        Notes = string.Empty;
        SelectedDestination = Destinations.SingleOrDefault(destination => destination.Parent == parent) ?? Destinations.FirstOrDefault();
        _originalState = CurrentState();
        ErrorMessage = null;
        GroupNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await SaveCoreAsync(cancellationToken).ConfigureAwait(false);

    public void TryBeginCreate()
    {
        if (SelectedDestination is null)
        {
            ErrorMessage = "Select a niche or group destination before creating a group.";
            return;
        }

        if (QueueIfDirty(PendingAction.StartCreate, destination: SelectedDestination))
        {
            return;
        }

        BeginCreate(SelectedDestination.Parent);
    }

    public void TryClose()
    {
        if (QueueIfDirty(PendingAction.Close))
        {
            return;
        }

        CloseImmediately();
    }

    public async Task TrySelectAsync(GroupSummary group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (QueueIfDirty(PendingAction.Select, group))
        {
            return;
        }

        await SelectImmediatelyAsync(group, cancellationToken).ConfigureAwait(false);
    }

    public async Task TryMoveAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedGroup is null || SelectedDestination is null)
        {
            ErrorMessage = "Select an active group and destination before moving.";
            return;
        }

        if (QueueIfDirty(PendingAction.Move, SelectedGroup, SelectedDestination))
        {
            return;
        }

        await MoveImmediatelyAsync(SelectedGroup, SelectedDestination, cancellationToken).ConfigureAwait(false);
    }

    public void TryRequestArchive()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        if (QueueIfDirty(PendingAction.Archive, SelectedGroup))
        {
            return;
        }

        ArchiveWarningVisible = true;
    }

    public async Task TryRestoreAsync(GroupSummary group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (QueueIfDirty(PendingAction.Restore, group))
        {
            return;
        }

        await RestoreImmediatelyAsync(group, cancellationToken).ConfigureAwait(false);
    }

    public async Task ConfirmArchiveAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var group = SelectedGroup;
        ArchiveWarningVisible = false;
        await ExecuteAsync(
            () => _service.ArchiveGroupAsync(group.Id, cancellationToken),
            selectResult: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAndContinueAsync(CancellationToken cancellationToken = default)
    {
        var action = CapturePending();
        var saved = await SaveCoreAsync(cancellationToken).ConfigureAwait(false);
        if (saved)
        {
            await ExecutePendingAsync(action, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DiscardAndContinueAsync(CancellationToken cancellationToken = default)
    {
        var action = CapturePending();
        RestoreOriginalFields();
        await ExecutePendingAsync(action, cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadStateAsync(Guid? storeId, Guid? nicheId, CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            ApplyState(await _service.LoadAsync(storeId, nicheId, cancellationToken).ConfigureAwait(false));
            ErrorMessage = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"Groups could not be loaded: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> SaveCoreAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Group name is required.";
            GroupNameFocusRequested?.Invoke(this, EventArgs.Empty);
            return false;
        }

        GroupManagementResult result;
        IsBusy = true;
        try
        {
            if (IsCreating)
            {
                var parent = SelectedDestination?.Parent ?? _draftParent;
                if (parent is null)
                {
                    ErrorMessage = "Select a niche or group destination.";
                    return false;
                }

                result = await _service.CreateGroupAsync(
                    new GroupManagementCreateRequest(parent, Name, CurrentContext()),
                    cancellationToken).ConfigureAwait(false);
            }
            else if (SelectedGroup is not null)
            {
                result = await _service.UpdateGroupAsync(
                    new GroupManagementUpdateRequest(SelectedGroup.Id, Name, CurrentContext()),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                ErrorMessage = "Select or create a group before saving.";
                return false;
            }

            ApplyResult(result, selectResult: result.Succeeded);
            if (result.Succeeded)
            {
                IsCreating = false;
                WorkspaceStructureChanged?.Invoke(this, result.Group);
                GroupListFocusRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                GroupNameFocusRequested?.Invoke(this, EventArgs.Empty);
            }

            return result.Succeeded;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectImmediatelyAsync(GroupSummary group, CancellationToken cancellationToken)
    {
        if (group.IsEffectivelyActive)
        {
            IsBusy = true;
            try
            {
                var result = await _service.SelectGroupAsync(group.Id, cancellationToken).ConfigureAwait(false);
                ApplyResult(result, selectResult: result.Succeeded);
            }
            finally
            {
                IsBusy = false;
            }
        }
        else
        {
            SelectedGroup = group;
            ApplyFields(group);
            ErrorMessage = null;
        }
    }

    private async Task MoveImmediatelyAsync(GroupSummary group, GroupDestination destination, CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => _service.MoveGroupAsync(new GroupManagementMoveRequest(group.Id, destination.Parent), cancellationToken),
            selectResult: true,
            cancellationToken).ConfigureAwait(false);

    private async Task RestoreImmediatelyAsync(GroupSummary group, CancellationToken cancellationToken) =>
        await ExecuteAsync(
            () => _service.RestoreGroupAsync(group.Id, cancellationToken),
            selectResult: true,
            cancellationToken).ConfigureAwait(false);

    private async Task ExecuteAsync(
        Func<Task<GroupManagementResult>> operation,
        bool selectResult,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await operation().ConfigureAwait(false);
            ApplyResult(result, selectResult && result.Succeeded);
            if (result.Succeeded)
            {
                WorkspaceStructureChanged?.Invoke(this, result.Group);
                GroupListFocusRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyResult(GroupManagementResult result, bool selectResult)
    {
        ErrorMessage = result.Error;
        ApplyState(result.State);
        if (selectResult && result.Group is not null)
        {
            SelectedGroup = ActiveGroups.Concat(ArchivedGroups).SingleOrDefault(group => group.Id == result.Group.Id) ?? result.Group;
            ApplyFields(SelectedGroup);
        }
        else if (result.Succeeded && result.Group is { IsArchived: true })
        {
            SelectedGroup = null;
            ClearFields();
        }
    }

    private void ApplyState(GroupManagementState state)
    {
        ActiveGroups = state.ActiveGroups;
        ArchivedGroups = state.ArchivedGroups;
        Destinations = state.ValidDestinations;
        OnPropertyChanged(nameof(ActiveGroups));
        OnPropertyChanged(nameof(ArchivedGroups));
        OnPropertyChanged(nameof(Destinations));
        OnPropertyChanged(nameof(HasActiveGroups));
        OnPropertyChanged(nameof(HasArchivedGroups));
        OnPropertyChanged(nameof(ShowsEmptyState));
        if (SelectedGroup is not null)
        {
            SelectedGroup = ActiveGroups.Concat(ArchivedGroups).SingleOrDefault(group => group.Id == SelectedGroup.Id);
        }
        RaiseActionProperties();
    }

    private void ApplyFields(GroupSummary? group)
    {
        IsCreating = false;
        Name = group?.Name ?? string.Empty;
        Description = group?.Context.Description ?? string.Empty;
        Notes = group?.Context.Notes ?? string.Empty;
        SelectedDestination = group is null
            ? Destinations.FirstOrDefault()
            : Destinations.SingleOrDefault(destination => destination.Parent == group.Parent) ?? Destinations.FirstOrDefault();
        _originalState = CurrentState();
        RaiseEditProperties();
    }

    private bool QueueIfDirty(PendingAction action, GroupSummary? group = null, GroupDestination? destination = null)
    {
        if (!HasUnsavedChanges)
        {
            return false;
        }

        _pendingAction = action;
        _pendingGroup = group;
        _pendingDestination = destination;
        DiscardPromptVisible = true;
        return true;
    }

    private (PendingAction Action, GroupSummary? Group, GroupDestination? Destination) CapturePending()
    {
        var captured = (_pendingAction, _pendingGroup, _pendingDestination);
        ClearPendingAction();
        return captured;
    }

    private async Task ExecutePendingAsync(
        (PendingAction Action, GroupSummary? Group, GroupDestination? Destination) pending,
        CancellationToken cancellationToken)
    {
        switch (pending.Action)
        {
            case PendingAction.Close:
                CloseImmediately();
                break;
            case PendingAction.StartCreate when pending.Destination is not null:
                BeginCreate(pending.Destination.Parent);
                break;
            case PendingAction.Select when pending.Group is not null:
                await SelectImmediatelyAsync(pending.Group, cancellationToken).ConfigureAwait(false);
                break;
            case PendingAction.Move when pending.Group is not null && pending.Destination is not null:
                await MoveImmediatelyAsync(pending.Group, pending.Destination, cancellationToken).ConfigureAwait(false);
                break;
            case PendingAction.Archive when pending.Group is not null:
                SelectedGroup = pending.Group;
                ArchiveWarningVisible = true;
                break;
            case PendingAction.Restore when pending.Group is not null:
                await RestoreImmediatelyAsync(pending.Group, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private void ClearPendingAction()
    {
        _pendingAction = PendingAction.None;
        _pendingGroup = null;
        _pendingDestination = null;
        DiscardPromptVisible = false;
    }

    private void RestoreOriginalFields()
    {
        Name = _originalState.Name;
        Description = _originalState.Description;
        Notes = _originalState.Notes;
    }

    private void CloseImmediately()
    {
        IsOpen = false;
        ArchiveWarningVisible = false;
        ClearPendingAction();
    }

    private void ClearFields()
    {
        IsCreating = false;
        Name = string.Empty;
        Description = string.Empty;
        Notes = string.Empty;
        _originalState = CurrentState();
    }

    private EditorState CurrentState() => new(Name, Description, Notes);

    private GroupContext CurrentContext() => new(EmptyToNull(Description), EmptyToNull(Notes));

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RaiseEditProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanSave));
    }

    private void RaiseActionProperties()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanMove));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanRestore));
        OnPropertyChanged(nameof(ShowsEmptyState));
    }

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
