using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Groups;

namespace FusionCanvas.App.Groups;

public sealed class GroupDetailsViewModel : INotifyPropertyChanged
{
    private readonly IGroupManagementService _service;
    private GroupSummary? _group;
    private Guid _storeId;
    private Guid? _nicheId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _originalName = string.Empty;
    private string _originalDescription = string.Empty;
    private string _originalNotes = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _archiveConfirmationVisible;
    private GroupDestination? _selectedDestination;
    private IReadOnlyList<GroupSummary> _activeGroups = [];

    public GroupDetailsViewModel(IGroupManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        MoveCommand = new RelayCommand(_ => Run(MoveAsync()), () => CanMove);
        RequestArchiveCommand = new RelayCommand(_ => ArchiveConfirmationVisible = true, () => CanArchive);
        ConfirmArchiveCommand = new RelayCommand(_ => Run(ConfirmArchiveAsync()));
        CancelArchiveCommand = new RelayCommand(_ => ArchiveConfirmationVisible = false);
        RestoreCommand = new RelayCommand(_ => Run(RestoreAsync()), () => CanRestore);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<GroupSummary?>? StructureChanged;

    public GroupSummary? Group
    {
        get => _group;
        private set
        {
            if (SetField(ref _group, value))
            {
                OnPropertyChanged(nameof(HasState));
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsArchived));
                OnPropertyChanged(nameof(InactiveNotice));
                OnPropertyChanged(nameof(DisplayPath));
                OnPropertyChanged(nameof(CanEdit));
                RaiseActionProperties();
            }
        }
    }

    public IReadOnlyList<GroupDestination> Destinations { get; private set; } = [];

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

    public bool HasState => _group is not null;

    public bool IsArchived => _group?.IsArchived == true;

    public bool IsReadOnly => _group is not { IsEffectivelyActive: true };

    public bool CanEdit => _group is { IsEffectivelyActive: true } && !IsBusy;

    public string InactiveNotice => IsArchived
        ? "This group is archived. Restore it to edit its details."
        : _group is not null && !_group.IsEffectivelyActive
            ? "This group is inactive because an ancestor is archived. Restore the archived ancestor to edit its details."
            : string.Empty;

    public string DisplayPath => _group?.DisplayPath ?? string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                RaiseDirty();
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
                RaiseDirty();
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
                RaiseDirty();
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                RaiseDirty();
                RaiseActionProperties();
            }
        }
    }

    public bool ArchiveConfirmationVisible
    {
        get => _archiveConfirmationVisible;
        private set => SetField(ref _archiveConfirmationVisible, value);
    }

    public bool HasUnsavedChanges =>
        Name != _originalName
        || Description != _originalDescription
        || Notes != _originalNotes;

    public bool CanMove =>
        _group is { IsEffectivelyActive: true }
        && _selectedDestination is not null
        && _selectedDestination.Parent != _group.Parent
        && !IsBusy;

    public bool CanArchive => _group is { IsArchived: false, IsEffectivelyActive: true } && !IsBusy;

    public bool CanRestore => _group is { IsArchived: true } && !IsBusy;

    public ICommand MoveCommand { get; }
    public ICommand RequestArchiveCommand { get; }
    public ICommand ConfirmArchiveCommand { get; }
    public ICommand CancelArchiveCommand { get; }
    public ICommand RestoreCommand { get; }

    public async Task LoadAsync(Guid groupId, Guid storeId, Guid? nicheId, CancellationToken cancellationToken = default)
    {
        _storeId = storeId;
        _nicheId = nicheId;
        var state = await _service.LoadAsync(storeId, nicheId, cancellationToken).ConfigureAwait(false);
        ApplyState(state);
        var group = _activeGroups.Concat(state.ArchivedGroups).SingleOrDefault(candidate => candidate.Id == groupId);
        if (group is null)
        {
            Clear();
            return;
        }

        ApplyGroup(group);
    }

    public void Clear()
    {
        Group = null;
        Destinations = [];
        SelectedDestination = null;
        _activeGroups = [];
        Name = string.Empty;
        Description = string.Empty;
        Notes = string.Empty;
        ErrorMessage = null;
        ArchiveConfirmationVisible = false;
        ResetBaselines();
        OnPropertyChanged(nameof(Destinations));
    }

    public async Task CommitEditsAsync(CancellationToken cancellationToken = default)
    {
        if (_group is not { } group || IsBusy || IsReadOnly || !HasUnsavedChanges)
        {
            return;
        }

        string? nameError = null;
        var normalizedName = Name.Trim();
        if (normalizedName.Length == 0 || normalizedName.Contains('\n') || normalizedName.Contains('\r'))
        {
            nameError = "The group name must be a non-empty single line. It was reverted to its last saved value.";
        }
        else if (_activeGroups.Any(candidate =>
                     candidate.Id != group.Id
                     && candidate.Parent == group.Parent
                     && string.Equals(candidate.Name.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            nameError = "Another group at this location already uses that name. It was reverted to its last saved value.";
        }

        if (nameError is not null)
        {
            Name = _originalName;
            if (!HasUnsavedChanges)
            {
                ErrorMessage = nameError;
                return;
            }
        }

        IsBusy = true;
        var result = await _service.UpdateGroupAsync(
            new GroupManagementUpdateRequest(group.Id, Name, CurrentContext()),
            cancellationToken).ConfigureAwait(false);
        IsBusy = false;

        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyState(result.State);
        if (result.Group is not null)
        {
            ApplyGroup(result.Group);
        }

        ErrorMessage = nameError;
        StructureChanged?.Invoke(this, result.Group);
    }

    private async Task MoveAsync()
    {
        if (_group is not { } group || _selectedDestination is not { } destination || IsBusy)
        {
            return;
        }

        await CommitEditsAsync().ConfigureAwait(false);
        if (HasError)
        {
            return;
        }

        IsBusy = true;
        var result = await _service.MoveGroupAsync(
            new GroupManagementMoveRequest(group.Id, destination.Parent)).ConfigureAwait(false);
        IsBusy = false;
        ApplyMutationResult(result);
    }

    private async Task ConfirmArchiveAsync()
    {
        if (_group is not { } group || IsBusy)
        {
            return;
        }

        ArchiveConfirmationVisible = false;
        IsBusy = true;
        var result = await _service.ArchiveGroupAsync(group.Id).ConfigureAwait(false);
        IsBusy = false;
        ApplyMutationResult(result);
    }

    private async Task RestoreAsync()
    {
        if (_group is not { } group || IsBusy)
        {
            return;
        }

        IsBusy = true;
        var result = await _service.RestoreGroupAsync(group.Id).ConfigureAwait(false);
        IsBusy = false;
        ApplyMutationResult(result);
    }

    private void ApplyMutationResult(GroupManagementResult result)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ErrorMessage = null;
        ApplyState(result.State);
        if (result.Group is not null)
        {
            var refreshed = _activeGroups.Concat(result.State.ArchivedGroups).SingleOrDefault(candidate => candidate.Id == result.Group.Id) ?? result.Group;
            ApplyGroup(refreshed);
        }

        StructureChanged?.Invoke(this, result.Group);
    }

    private void ApplyState(GroupManagementState state)
    {
        _activeGroups = state.ActiveGroups;
        Destinations = state.ValidDestinations;
        OnPropertyChanged(nameof(Destinations));
    }

    private void ApplyGroup(GroupSummary group)
    {
        Group = group;
        Name = group.Name;
        Description = group.Context.Description ?? string.Empty;
        Notes = group.Context.Notes ?? string.Empty;
        ErrorMessage = null;
        ArchiveConfirmationVisible = false;
        SelectedDestination = Destinations.SingleOrDefault(destination => destination.Parent == group.Parent)
            ?? Destinations.FirstOrDefault();
        ResetBaselines();
    }

    private GroupContext CurrentContext() => new(
        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

    private void ResetBaselines()
    {
        _originalName = Name;
        _originalDescription = Description;
        _originalNotes = Notes;
        RaiseDirty();
    }

    private void RaiseActionProperties()
    {
        OnPropertyChanged(nameof(CanMove));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanRestore));
    }

    private void RaiseDirty()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    private static void Run(Task task) => _ = task;
}
