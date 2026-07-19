using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Navigation;

public sealed class WorkspaceTreeNodeViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isEditing;
    private bool _isCut;
    private bool _isDropTarget;
    private bool _isDropBefore;
    private bool _isDropAfter;
    private bool _canPaste;
    private string _draftName;

    public WorkspaceTreeNodeViewModel(
        Guid nodeId,
        WorkspaceEntityKind entityKind,
        Guid entityId,
        string name,
        string? description,
        bool isDirectMatch,
        bool hasHiddenChildren,
        int childCount,
        IEnumerable<WorkspaceTreeNodeViewModel> children,
        bool isDraft = false)
    {
        NodeId = nodeId;
        EntityKind = entityKind;
        EntityId = entityId;
        Name = name;
        Description = description;
        IsDirectMatch = isDirectMatch;
        HasHiddenChildren = hasHiddenChildren;
        ChildCount = childCount;
        IsDraft = isDraft;
        _draftName = name;
        Children = new ObservableCollection<WorkspaceTreeNodeViewModel>(children);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid NodeId { get; }
    public WorkspaceEntityKind EntityKind { get; }
    public Guid EntityId { get; }
    public string Name { get; private set; }
    public string? Description { get; }
    public bool IsDirectMatch { get; }
    public bool HasHiddenChildren { get; }
    public int ChildCount { get; }
    public bool IsDraft { get; }
    public string Icon => EntityKind switch
    {
        WorkspaceEntityKind.Niche => "◆",
        WorkspaceEntityKind.Group => "▣",
        WorkspaceEntityKind.Listing => "●",
        _ => "•"
    };

    public string KindLabel => EntityKind switch
    {
        WorkspaceEntityKind.Niche => "Niche",
        WorkspaceEntityKind.Group => "Group",
        WorkspaceEntityKind.Listing => "Item",
        _ => EntityKind.ToString()
    };

    public string CountLabel => ChildCount == 0 ? string.Empty : ChildCount.ToString();
    public bool HasChildren => ChildCount > 0;
    public bool IsGroup => EntityKind == WorkspaceEntityKind.Group;
    public bool IsListing => EntityKind == WorkspaceEntityKind.Listing;
    public bool IsTopic => EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group;
    public bool HasContextActions => EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing;
    public ObservableCollection<WorkspaceTreeNodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetField(ref _isEditing, value);
    }

    public bool IsCut
    {
        get => _isCut;
        set => SetField(ref _isCut, value);
    }

    public bool IsDropTarget { get => _isDropTarget; set => SetField(ref _isDropTarget, value); }
    public bool IsDropBefore { get => _isDropBefore; set => SetField(ref _isDropBefore, value); }
    public bool IsDropAfter { get => _isDropAfter; set => SetField(ref _isDropAfter, value); }
    public bool CanPaste { get => _canPaste; set => SetField(ref _canPaste, value); }

    public string DraftName
    {
        get => _draftName;
        set => SetField(ref _draftName, value);
    }

    public void CommitName(string name)
    {
        Name = name;
        DraftName = name;
        OnPropertyChanged(nameof(Name));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class WorkspaceTreeViewModel : INotifyPropertyChanged
{
    private readonly IWorkspaceRepository _repository;
    private readonly IGroupManagementService _groups;
    private readonly IListingManagementService _listings;
    private readonly WorkspaceTreeSelectionCoordinator _selection;
    private readonly WorkspaceTreeClipboard _clipboard;
    private readonly HashSet<Guid> _expandedIds = [];
    private HashSet<Guid>? _expandedIdsBeforeFilter;
    private WorkspaceSnapshot _snapshot;
    private Guid? _storeId;
    private WorkspaceTreeNodeViewModel? _selectedNode;
    private WorkspaceTreeNodeViewModel? _editingNode;
    private GroupParentReference? _creationAnchor;
    private ListingTopicReference? _listingCreationAnchor;
    private string _queryText = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public WorkspaceTreeViewModel(
        IWorkspaceRepository repository,
        IGroupManagementService groups,
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelectionCoordinator? selection = null,
        WorkspaceTreeClipboard? clipboard = null,
        IListingManagementService? listings = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _groups = groups ?? throw new ArgumentNullException(nameof(groups));
        _listings = listings ?? new ListingManagementService(repository);
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _selection = selection ?? new WorkspaceTreeSelectionCoordinator();
        _clipboard = clipboard ?? new WorkspaceTreeClipboard();
        SelectNodeCommand = new RelayCommand(parameter => Select(parameter as WorkspaceTreeNodeViewModel));
        OpenInTabCommand = new RelayCommand(parameter => OpenInTab(parameter as WorkspaceTreeNodeViewModel));
        BeginCreateCommand = new RelayCommand(_ => Run(BeginCreateAsync()));
        BeginCreateListingCommand = new RelayCommand(_ => Run(BeginCreateListingAsync()));
        BeginRenameCommand = new RelayCommand(_ => BeginRename());
        CopyCommand = new RelayCommand(_ => Copy());
        CutCommand = new RelayCommand(_ => Cut());
        PasteCommand = new RelayCommand(_ => Run(PasteAsync()));
        DuplicateCommand = new RelayCommand(_ => Run(DuplicateAsync()));
        EditPropertiesCommand = new RelayCommand(_ =>
        {
            if (_selectedNode?.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing)
            {
                if (_selectedNode.EntityKind == WorkspaceEntityKind.Group)
                {
                    EditPropertiesRequested?.Invoke(this, _selectedNode.EntityId);
                }
                else
                {
                    EditListingPropertiesRequested?.Invoke(this, _selectedNode.EntityId);
                }
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<WorkspaceTreeSelection>? OpenInTabRequested;
    public event EventHandler<WorkspaceTreeSelection>? SelectionChanged;
    public event EventHandler<Guid>? EditPropertiesRequested;
    public event EventHandler<Guid>? EditListingPropertiesRequested;
    public event EventHandler? StructureChanged;
    public event EventHandler<IReadOnlySet<Guid>>? EntitiesDeleted;

    public ObservableCollection<WorkspaceTreeNodeViewModel> Roots { get; } = [];
    public ICommand SelectNodeCommand { get; }
    public ICommand OpenInTabCommand { get; }
    public ICommand BeginCreateCommand { get; }
    public ICommand BeginCreateListingCommand { get; }
    public ICommand BeginRenameCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand EditPropertiesCommand { get; }

    public WorkspaceTreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        private set
        {
            if (ReferenceEquals(_selectedNode, value))
            {
                return;
            }

            if (_selectedNode is not null)
            {
                _selectedNode.IsSelected = false;
            }

            _selectedNode = value;
            if (_selectedNode is not null)
            {
                _selectedNode.IsSelected = true;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanManageSelection));
            OnPropertyChanged(nameof(InspectorTitle));
            OnPropertyChanged(nameof(InspectorKind));
            OnPropertyChanged(nameof(InspectorDescription));
            OnPropertyChanged(nameof(InspectorPath));
        }
    }

    public bool HasSelection => SelectedNode is not null;
    public bool CanManageSelection => SelectedNode?.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing;
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get => _errorMessage; private set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public string InspectorTitle => SelectedNode?.Name ?? "No selection";
    public string InspectorKind => SelectedNode?.KindLabel ?? string.Empty;
    public string InspectorDescription => SelectedNode?.Description ?? "No additional properties have been set.";
    public string InspectorPath => SelectedNode is null ? string.Empty : BuildPath(SelectedNode.EntityId);

    public string QueryText
    {
        get => _queryText;
        set
        {
            if (string.Equals(_queryText, value, StringComparison.Ordinal))
            {
                return;
            }

            var wasFiltering = !string.IsNullOrWhiteSpace(_queryText);
            var willFilter = !string.IsNullOrWhiteSpace(value);
            if (!wasFiltering && willFilter)
            {
                CaptureExpanded(Roots);
                _expandedIdsBeforeFilter = [.. _expandedIds];
            }
            else if (wasFiltering && !willFilter && _expandedIdsBeforeFilter is not null)
            {
                _expandedIds.Clear();
                _expandedIds.UnionWith(_expandedIdsBeforeFilter);
                _expandedIdsBeforeFilter = null;
            }

            SetField(ref _queryText, value);
            RefreshProjection(captureExpanded: false);
        }
    }

    public void SetStore(Guid? storeId, WorkspaceSnapshot snapshot)
    {
        _storeId = storeId;
        _snapshot = snapshot;
        RefreshProjection();
    }

    public async Task ReloadAsync()
    {
        _snapshot = await _repository.LoadAsync().ConfigureAwait(false);
        RefreshProjection();
    }

    public void SelectEntity(Guid? entityId, bool notifySelectionChanged = true)
    {
        Select(entityId is Guid id ? FindNode(id) : null, notifySelectionChanged);
    }

    public async Task BeginCreateAsync()
    {
        if (_storeId is not Guid storeId || IsBusy)
        {
            ErrorMessage = "Select an active store before creating groups.";
            return;
        }

        var selected = SelectedNode is null ? null : new WorkspaceTreeSelection(SelectedNode.EntityKind, SelectedNode.EntityId);
        var destination = await _groups.ResolveCreateParentAsync(storeId, selected).ConfigureAwait(false);
        if (!destination.Succeeded)
        {
            ErrorMessage = destination.Error;
            return;
        }

        _creationAnchor = destination.Parent;
        InsertDraft(destination.Parent!);
    }

    public async Task BeginCreateListingAsync()
    {
        if (_storeId is not Guid storeId || IsBusy)
        {
            ErrorMessage = "Select an active store before creating listings.";
            return;
        }

        var selected = SelectedNode is null ? _selection.Selected : new WorkspaceTreeSelection(SelectedNode.EntityKind, SelectedNode.EntityId);
        var destination = await _listings.ResolveCreateTopicAsync(storeId, selected).ConfigureAwait(false);
        if (!destination.Succeeded)
        {
            ErrorMessage = destination.Error;
            return;
        }

        _listingCreationAnchor = destination.Topic;
        InsertListingDraft(destination.Topic!);
    }

    public void BeginRename()
    {
        if (SelectedNode is not { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing } node || IsBusy)
        {
            return;
        }

        CancelEdit();
        _editingNode = node;
        node.DraftName = node.Name;
        node.IsEditing = true;
    }

    public async Task CommitEditAsync(bool addAnotherSibling = false)
    {
        if (_editingNode is null || IsBusy)
        {
            return;
        }

        var editing = _editingNode;
        IsBusy = true;
        ErrorMessage = null;
        Guid selectedId;
        if (editing.IsDraft)
        {
            if (editing.EntityKind == WorkspaceEntityKind.Listing)
            {
                var result = await _listings.CreateListingAsync(new ListingManagementCreateRequest(_listingCreationAnchor!, editing.DraftName)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    IsBusy = false;
                    ErrorMessage = result.Error;
                    editing.IsEditing = true;
                    return;
                }
                selectedId = result.Listing!.Id;
            }
            else
            {
                var result = await _groups.CreateGroupAsync(new GroupManagementCreateRequest(_creationAnchor!, editing.DraftName)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    IsBusy = false;
                    ErrorMessage = result.Error;
                    editing.IsEditing = true;
                    return;
                }
                selectedId = result.Group!.Id;
            }
        }
        else if (editing.EntityKind == WorkspaceEntityKind.Listing)
        {
            var listing = _snapshot.Listings.Single(candidate => candidate.Id == editing.EntityId);
            var result = await _listings.UpdateListingAsync(new ListingManagementUpdateRequest(listing.Id, editing.DraftName)).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                IsBusy = false;
                ErrorMessage = result.Error;
                editing.IsEditing = true;
                return;
            }
            selectedId = result.Listing!.Id;
        }
        else
        {
            var group = _snapshot.Groups.Single(candidate => candidate.Id == editing.EntityId);
            var result = await _groups.UpdateGroupAsync(new GroupManagementUpdateRequest(group.Id, editing.DraftName)).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                IsBusy = false;
                ErrorMessage = result.Error;
                editing.IsEditing = true;
                return;
            }
            selectedId = result.Group!.Id;
        }

        IsBusy = false;
        _editingNode = null;
        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(selectedId));
        StructureChanged?.Invoke(this, EventArgs.Empty);
        if (addAnotherSibling && editing.EntityKind == WorkspaceEntityKind.Listing && _listingCreationAnchor is not null)
        {
            InsertListingDraft(_listingCreationAnchor);
        }
        else if (addAnotherSibling && _creationAnchor is not null)
        {
            InsertDraft(_creationAnchor);
        }
    }

    public void CancelEdit()
    {
        if (_editingNode is null)
        {
            return;
        }

        _editingNode.IsEditing = false;
        _editingNode = null;
        RefreshProjection();
    }

    public void Copy()
    {
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Copy, node.EntityKind, node.EntityId));
            ApplyClipboardState();
        }
    }

    public void Cut()
    {
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Cut, node.EntityKind, node.EntityId));
            ApplyClipboardState();
        }
    }

    public async Task PasteAsync()
    {
        if (_clipboard.Payload is not { Kind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing } payload ||
            SelectedNode is not { EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group } destination)
        {
            ErrorMessage = "Copy or cut a group or listing, then select a niche or group destination.";
            return;
        }

        IsBusy = true;
        Guid selectedId;
        bool succeeded;
        string? error;
        if (payload.Kind == WorkspaceEntityKind.Listing)
        {
            var topic = new ListingTopicReference(destination.EntityKind, destination.EntityId);
            var result = payload.Mode == WorkspaceTreeClipboardMode.Copy
                ? await _listings.DuplicateListingAsync(new ListingManagementDuplicateRequest(payload.EntityId, topic)).ConfigureAwait(false)
                : await _listings.MoveListingAsync(new ListingManagementMoveRequest(payload.EntityId, topic)).ConfigureAwait(false);
            succeeded = result.Succeeded;
            error = result.Error;
            selectedId = result.Listing?.Id ?? payload.EntityId;
        }
        else
        {
            var parent = new GroupParentReference(destination.EntityKind, destination.EntityId);
            var result = payload.Mode == WorkspaceTreeClipboardMode.Copy
                ? await _groups.CopyGroupAsync(new GroupManagementCopyRequest(payload.EntityId, parent)).ConfigureAwait(false)
                : await _groups.MoveGroupAsync(new GroupManagementMoveRequest(payload.EntityId, parent)).ConfigureAwait(false);
            succeeded = result.Succeeded;
            error = result.Error;
            selectedId = result.Group?.Id ?? payload.EntityId;
        }
        IsBusy = false;
        if (!succeeded)
        {
            ErrorMessage = error;
            return;
        }

        if (payload.Mode == WorkspaceTreeClipboardMode.Cut)
        {
            _clipboard.Clear();
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(selectedId));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DuplicateAsync()
    {
        if (SelectedNode is not { EntityKind: WorkspaceEntityKind.Listing } node || IsBusy)
        {
            return;
        }

        IsBusy = true;
        var result = await _listings.DuplicateListingAsync(new ListingManagementDuplicateRequest(node.EntityId)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(result.Listing!.Id));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public GroupDeleteImpact GetDeleteImpact(Guid groupId)
    {
        var group = _snapshot.Groups.Single(candidate => candidate.Id == groupId);
        var groupIds = GroupHierarchy.GetDescendants(_snapshot, group)
            .Select(candidate => candidate.Id)
            .Append(group.Id)
            .ToHashSet();
        var listingIds = _snapshot.Listings
            .Where(listing => listing.GroupId is Guid id && groupIds.Contains(id))
            .Select(listing => listing.Id)
            .ToHashSet();
        var promptIds = _snapshot.Prompts
            .Where(prompt => prompt.ListingId is Guid id && listingIds.Contains(id))
            .Select(prompt => prompt.Id);
        var entityIds = new HashSet<Guid>(groupIds);
        entityIds.UnionWith(listingIds);
        entityIds.UnionWith(promptIds);
        return new GroupDeleteImpact(group.Id, group.Name, groupIds.Count - 1, listingIds.Count, entityIds);
    }

    public async Task DeleteGroupAsync(Guid groupId, bool ConfirmPermanentDeletion)
    {
        if (IsBusy || _snapshot.Groups.All(group => group.Id != groupId))
        {
            return;
        }

        var impact = GetDeleteImpact(groupId);
        IsBusy = true;
        ErrorMessage = null;
        var result = await _groups.DeleteGroupAsync(new GroupManagementDeleteRequest(groupId, ConfirmPermanentDeletion)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        if (_clipboard.Payload is { } payload && impact.DeletedEntityIds.Contains(payload.EntityId))
        {
            _clipboard.Clear();
        }

        await ReloadAsync().ConfigureAwait(false);
        var fallbackId = result.State.ActiveGroupId ?? result.State.ActiveNicheId;
        Select(fallbackId is Guid id ? FindNode(id) : null);
        EntitiesDeleted?.Invoke(this, impact.DeletedEntityIds);
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task MoveAsync(Guid sourceGroupId, WorkspaceTreeNodeViewModel target, GroupPlacement placement)
        => await MoveAsync(WorkspaceEntityKind.Group, sourceGroupId, target, placement).ConfigureAwait(false);

    public async Task MoveAsync(WorkspaceEntityKind sourceKind, Guid sourceId, WorkspaceTreeNodeViewModel target, GroupPlacement placement)
    {
        if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group) || IsBusy)
        {
            return;
        }

        if (sourceKind == WorkspaceEntityKind.Listing)
        {
            IsBusy = true;
            var listingResult = await _listings.MoveListingAsync(new ListingManagementMoveRequest(
                sourceId,
                new ListingTopicReference(target.EntityKind, target.EntityId))).ConfigureAwait(false);
            IsBusy = false;
            if (!listingResult.Succeeded)
            {
                ErrorMessage = listingResult.Error;
                return;
            }

            await ReloadAsync().ConfigureAwait(false);
            Select(FindNode(sourceId));
            StructureChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!string.IsNullOrWhiteSpace(QueryText) && placement.Kind != GroupPlacementKind.Append)
        {
            ErrorMessage = "Clear filtering before positioning a group between siblings.";
            return;
        }

        var destination = placement.Kind == GroupPlacementKind.Append
            ? new GroupParentReference(target.EntityKind, target.EntityId)
            : ParentOf(target);
        IsBusy = true;
        var result = await _groups.MoveGroupAsync(new GroupManagementMoveRequest(sourceId, destination, placement)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(sourceId));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanDrop(
        Guid sourceGroupId,
        WorkspaceTreeNodeViewModel target,
        GroupPlacement placement,
        out string? error) => CanDrop(WorkspaceEntityKind.Group, sourceGroupId, target, placement, out error);

    public bool CanDrop(
        WorkspaceEntityKind sourceKind,
        Guid sourceId,
        WorkspaceTreeNodeViewModel target,
        GroupPlacement placement,
        out string? error)
    {
        if (sourceKind == WorkspaceEntityKind.Listing)
        {
            var listing = _snapshot.Listings.SingleOrDefault(candidate => candidate.Id == sourceId);
            if (listing is null || !ListingHierarchy.IsEffectivelyActive(_snapshot, listing))
            {
                error = "Only an active listing can be moved.";
                return false;
            }

            if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group))
            {
                error = "Drop the listing onto an active niche or group.";
                return false;
            }

            var listingTargetStoreId = target.EntityKind == WorkspaceEntityKind.Niche
                ? _snapshot.Niches.SingleOrDefault(niche => niche.Id == target.EntityId && !niche.IsArchived)?.StoreId
                : _snapshot.Groups.SingleOrDefault(group => group.Id == target.EntityId && GroupHierarchy.IsEffectivelyActive(_snapshot, group))?.StoreId;
            if (listingTargetStoreId != listing.StoreId)
            {
                error = "The destination must be active and belong to the same store.";
                return false;
            }

            error = null;
            return true;
        }

        var source = _snapshot.Groups.SingleOrDefault(group => group.Id == sourceId);
        if (source is null || !GroupHierarchy.IsEffectivelyActive(_snapshot, source))
        {
            error = "Only an active group can be moved.";
            return false;
        }

        if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group))
        {
            error = "Drop the group onto an active niche or group.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(QueryText) && placement.Kind != GroupPlacementKind.Append)
        {
            error = "Clear filtering before positioning a group between siblings.";
            return false;
        }

        if (target.EntityKind == WorkspaceEntityKind.Group &&
            (target.EntityId == source.Id || GroupHierarchy.IsDescendant(_snapshot, target.EntityId, source.Id)))
        {
            error = "A group cannot be moved beneath itself or one of its descendants.";
            return false;
        }

        var targetStoreId = target.EntityKind == WorkspaceEntityKind.Niche
            ? _snapshot.Niches.SingleOrDefault(niche => niche.Id == target.EntityId && !niche.IsArchived)?.StoreId
            : _snapshot.Groups.SingleOrDefault(group => group.Id == target.EntityId && GroupHierarchy.IsEffectivelyActive(_snapshot, group))?.StoreId;
        if (targetStoreId is null || targetStoreId != source.StoreId)
        {
            error = "The destination must be active and belong to the same store.";
            return false;
        }

        error = null;
        return true;
    }

    public void ShowDropFeedback(string? error) => ErrorMessage = error;

    private void Select(WorkspaceTreeNodeViewModel? node, bool notifySelectionChanged = true)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        SelectedNode = node;
        var selection = new WorkspaceTreeSelection(node.EntityKind, node.EntityId);
        _selection.Select(selection);
        if (notifySelectionChanged)
        {
            SelectionChanged?.Invoke(this, selection);
        }
    }

    private void OpenInTab(WorkspaceTreeNodeViewModel? node)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        Select(node, notifySelectionChanged: false);
        OpenInTabRequested?.Invoke(this, new WorkspaceTreeSelection(node.EntityKind, node.EntityId));
    }

    private void InsertDraft(GroupParentReference parent)
    {
        CancelEdit();
        var parentNode = FindNode(parent.Id);
        if (parentNode is null)
        {
            ErrorMessage = "The selected destination is not visible in the current tree.";
            return;
        }

        var name = UniqueDraftName(parent);
        var draft = new WorkspaceTreeNodeViewModel(Guid.NewGuid(), WorkspaceEntityKind.Group, Guid.NewGuid(), name, null, true, false, 0, [], true)
        {
            IsEditing = true
        };
        parentNode.IsExpanded = true;
        _expandedIds.Add(parentNode.EntityId);
        parentNode.Children.Add(draft);
        _editingNode = draft;
        SelectedNode = draft;
        ErrorMessage = null;
    }

    private void InsertListingDraft(ListingTopicReference topic)
    {
        CancelEdit();
        var parentNode = FindNode(topic.Id);
        if (parentNode is null)
        {
            ErrorMessage = "The selected listing destination is not visible in the current tree.";
            return;
        }

        var draft = new WorkspaceTreeNodeViewModel(
            Guid.NewGuid(),
            WorkspaceEntityKind.Listing,
            Guid.NewGuid(),
            "New listing",
            null,
            true,
            false,
            0,
            [],
            true)
        {
            IsEditing = true
        };
        parentNode.IsExpanded = true;
        _expandedIds.Add(parentNode.EntityId);
        var insertionIndex = parentNode.Children
            .TakeWhile(child => child.EntityKind == WorkspaceEntityKind.Group ||
                                string.Compare(child.Name, draft.Name, StringComparison.OrdinalIgnoreCase) < 0)
            .Count();
        parentNode.Children.Insert(insertionIndex, draft);
        _editingNode = draft;
        SelectedNode = draft;
        ErrorMessage = null;
    }

    private string UniqueDraftName(GroupParentReference parent)
    {
        var existing = _snapshot.Groups
            .Where(group => (group.NicheId == parent.Id && parent.Kind == WorkspaceEntityKind.Niche) ||
                            (group.ParentGroupId == parent.Id && parent.Kind == WorkspaceEntityKind.Group))
            .Select(group => group.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains("New group"))
        {
            return "New group";
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"New group ({suffix})";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private void RefreshProjection(bool captureExpanded = true)
    {
        if (captureExpanded && string.IsNullOrWhiteSpace(QueryText))
        {
            CaptureExpanded(Roots);
        }
        var selectedId = SelectedNode?.IsDraft == false ? SelectedNode.EntityId : _selection.Selected?.Id;
        Roots.Clear();
        if (_storeId is not Guid storeId || _snapshot.Stores.All(store => store.Id != storeId || store.IsArchived))
        {
            SelectedNode = null;
            return;
        }

        var projection = WorkspaceTreeProjector.Project(_snapshot, storeId, new WorkspaceTreeQuery(QueryText));
        foreach (var root in projection.Roots)
        {
            Roots.Add(ToNode(root));
        }

        SelectedNode = selectedId is Guid id ? FindNode(id) : null;
        ApplyClipboardState();
    }

    private WorkspaceTreeNodeViewModel ToNode(WorkspaceTreeProjectionNode projected)
    {
        var entity = FindEntity(projected.EntityKind, projected.EntityId);
        var node = new WorkspaceTreeNodeViewModel(
            projected.NodeId,
            projected.EntityKind,
            projected.EntityId,
            projected.Name,
            entity?.Description,
            projected.IsDirectMatch,
            projected.HasHiddenChildren,
            projected.Children.Count,
            projected.Children.Select(ToNode));
        node.IsExpanded = _expandedIds.Contains(node.EntityId) || !string.IsNullOrWhiteSpace(QueryText);
        return node;
    }

    private WorkspaceEntity? FindEntity(WorkspaceEntityKind kind, Guid id) => kind switch
    {
        WorkspaceEntityKind.Niche => _snapshot.Niches.SingleOrDefault(entity => entity.Id == id),
        WorkspaceEntityKind.Group => _snapshot.Groups.SingleOrDefault(entity => entity.Id == id),
        WorkspaceEntityKind.Listing => _snapshot.Listings.SingleOrDefault(entity => entity.Id == id),
        _ => null
    };

    private GroupParentReference ParentOf(WorkspaceTreeNodeViewModel node)
    {
        if (node.EntityKind == WorkspaceEntityKind.Group)
        {
            var group = _snapshot.Groups.Single(candidate => candidate.Id == node.EntityId);
            return group.NicheId is Guid nicheId
                ? new GroupParentReference(WorkspaceEntityKind.Niche, nicheId)
                : new GroupParentReference(WorkspaceEntityKind.Group, group.ParentGroupId!.Value);
        }

        throw new InvalidOperationException("Only groups support relative placement.");
    }

    private string BuildPath(Guid entityId)
    {
        var node = WorkspaceNavigation.BuildTree(_snapshot).Flatten().SingleOrDefault(candidate => candidate.EntityId == entityId);
        if (node is null)
        {
            return string.Empty;
        }

        var tree = WorkspaceNavigation.BuildTree(_snapshot);
        var names = tree.GetPath(node.Id).Select(id => tree.Find(id).Name);
        return string.Join(" / ", names);
    }

    private WorkspaceTreeNodeViewModel? FindNode(Guid entityId) => Flatten(Roots).FirstOrDefault(node => node.EntityId == entityId);

    private static IEnumerable<WorkspaceTreeNodeViewModel> Flatten(IEnumerable<WorkspaceTreeNodeViewModel> nodes) =>
        nodes.SelectMany(node => new[] { node }.Concat(Flatten(node.Children)));

    private void CaptureExpanded(IEnumerable<WorkspaceTreeNodeViewModel> nodes)
    {
        foreach (var node in Flatten(nodes))
        {
            if (node.IsExpanded)
            {
                _expandedIds.Add(node.EntityId);
            }
            else
            {
                _expandedIds.Remove(node.EntityId);
            }
        }
    }

    private void ApplyClipboardState()
    {
        foreach (var node in Flatten(Roots))
        {
            node.IsCut = _clipboard.Payload is { Mode: WorkspaceTreeClipboardMode.Cut } payload && payload.EntityId == node.EntityId;
            node.CanPaste = node.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group &&
                            _clipboard.Payload is { Kind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing };
        }
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

public sealed record GroupDeleteImpact(
    Guid GroupId,
    string GroupName,
    int DescendantGroupCount,
    int ItemCount,
    IReadOnlySet<Guid> DeletedEntityIds);
