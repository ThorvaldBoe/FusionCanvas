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
    private readonly WorkspaceTreeSelectionCoordinator _selection;
    private readonly WorkspaceTreeClipboard _clipboard;
    private readonly HashSet<Guid> _expandedIds = [];
    private HashSet<Guid>? _expandedIdsBeforeFilter;
    private WorkspaceSnapshot _snapshot;
    private Guid? _storeId;
    private WorkspaceTreeNodeViewModel? _selectedNode;
    private WorkspaceTreeNodeViewModel? _editingNode;
    private GroupParentReference? _creationAnchor;
    private string _queryText = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public WorkspaceTreeViewModel(
        IWorkspaceRepository repository,
        IGroupManagementService groups,
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelectionCoordinator? selection = null,
        WorkspaceTreeClipboard? clipboard = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _groups = groups ?? throw new ArgumentNullException(nameof(groups));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _selection = selection ?? new WorkspaceTreeSelectionCoordinator();
        _clipboard = clipboard ?? new WorkspaceTreeClipboard();
        SelectNodeCommand = new RelayCommand(parameter => Select(parameter as WorkspaceTreeNodeViewModel));
        OpenInTabCommand = new RelayCommand(parameter => OpenInTab(parameter as WorkspaceTreeNodeViewModel));
        BeginCreateCommand = new RelayCommand(_ => Run(BeginCreateAsync()));
        BeginRenameCommand = new RelayCommand(_ => BeginRename());
        CopyCommand = new RelayCommand(_ => Copy());
        CutCommand = new RelayCommand(_ => Cut());
        PasteCommand = new RelayCommand(_ => Run(PasteAsync()));
        EditPropertiesCommand = new RelayCommand(_ =>
        {
            if (_selectedNode?.EntityKind == WorkspaceEntityKind.Group)
            {
                EditPropertiesRequested?.Invoke(this, _selectedNode.EntityId);
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<WorkspaceTreeSelection>? OpenInTabRequested;
    public event EventHandler<Guid>? EditPropertiesRequested;
    public event EventHandler? StructureChanged;

    public ObservableCollection<WorkspaceTreeNodeViewModel> Roots { get; } = [];
    public ICommand SelectNodeCommand { get; }
    public ICommand OpenInTabCommand { get; }
    public ICommand BeginCreateCommand { get; }
    public ICommand BeginRenameCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand PasteCommand { get; }
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
    public bool CanManageSelection => SelectedNode?.EntityKind == WorkspaceEntityKind.Group;
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

    public void BeginRename()
    {
        if (SelectedNode is not { EntityKind: WorkspaceEntityKind.Group } node || IsBusy)
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
        GroupManagementResult result;
        if (editing.IsDraft)
        {
            result = await _groups.CreateGroupAsync(new GroupManagementCreateRequest(_creationAnchor!, editing.DraftName)).ConfigureAwait(false);
        }
        else
        {
            var group = _snapshot.Groups.Single(candidate => candidate.Id == editing.EntityId);
            result = await _groups.UpdateGroupAsync(new GroupManagementUpdateRequest(group.Id, editing.DraftName)).ConfigureAwait(false);
        }

        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            editing.IsEditing = true;
            return;
        }

        _editingNode = null;
        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(result.Group!.Id));
        StructureChanged?.Invoke(this, EventArgs.Empty);
        if (addAnotherSibling && _creationAnchor is not null)
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
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Copy, node.EntityKind, node.EntityId));
            ApplyCutState();
        }
    }

    public void Cut()
    {
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Cut, node.EntityKind, node.EntityId));
            ApplyCutState();
        }
    }

    public async Task PasteAsync()
    {
        if (_clipboard.Payload is not { Kind: WorkspaceEntityKind.Group } payload ||
            SelectedNode is not { EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group } destination)
        {
            ErrorMessage = "Copy or cut a group, then select a niche or group destination.";
            return;
        }

        var parent = new GroupParentReference(destination.EntityKind, destination.EntityId);
        IsBusy = true;
        var result = payload.Mode == WorkspaceTreeClipboardMode.Copy
            ? await _groups.CopyGroupAsync(new GroupManagementCopyRequest(payload.EntityId, parent)).ConfigureAwait(false)
            : await _groups.MoveGroupAsync(new GroupManagementMoveRequest(payload.EntityId, parent)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        if (payload.Mode == WorkspaceTreeClipboardMode.Cut)
        {
            _clipboard.Clear();
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(result.Group!.Id));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task MoveAsync(Guid sourceGroupId, WorkspaceTreeNodeViewModel target, GroupPlacement placement)
    {
        if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group) || IsBusy)
        {
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
        var result = await _groups.MoveGroupAsync(new GroupManagementMoveRequest(sourceGroupId, destination, placement)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(sourceGroupId));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Select(WorkspaceTreeNodeViewModel? node)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        SelectedNode = node;
        _selection.Select(new WorkspaceTreeSelection(node.EntityKind, node.EntityId));
    }

    private void OpenInTab(WorkspaceTreeNodeViewModel? node)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        Select(node);
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
        ApplyCutState();
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

    private void ApplyCutState()
    {
        foreach (var node in Flatten(Roots))
        {
            node.IsCut = _clipboard.Payload is { Mode: WorkspaceTreeClipboardMode.Cut } payload && payload.EntityId == node.EntityId;
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
