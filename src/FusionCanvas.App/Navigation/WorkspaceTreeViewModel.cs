using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Navigation;

public sealed class WorkspaceTreeNodeViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isMultiSelected;
    private bool _hasMultiSelectionContext;
    private int _selectionCount;
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
        bool isDraft = false,
        IReadOnlyList<string>? appliedTagColors = null,
        bool isInactive = false)
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
        AppliedTagColors = appliedTagColors ?? [];
        IsInactive = isInactive;
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
    public bool IsInactive { get; }
    public string Icon => EntityKind switch
    {
        WorkspaceEntityKind.Niche => "◆",
        WorkspaceEntityKind.Group => "▣",
        WorkspaceEntityKind.Item => "●",
        _ => "•"
    };

    public string KindLabel => EntityKind switch
    {
        WorkspaceEntityKind.Niche => "Niche",
        WorkspaceEntityKind.Group => "Group",
        WorkspaceEntityKind.Item => "Item",
        _ => EntityKind.ToString()
    };

    public string CountLabel => ChildCount == 0 ? string.Empty : ChildCount.ToString();
    public bool HasChildren => ChildCount > 0;
    public bool HasAppliedTags => AppliedTagColors.Count > 0;
    public IReadOnlyList<string> AppliedTagColors { get; }
    public int VisibleTagChipCount => Math.Min(AppliedTagColors.Count, 3);
    public int HiddenTagCount => Math.Max(0, AppliedTagColors.Count - 3);
    public string HiddenTagLabel => HiddenTagCount > 0 ? $"+{HiddenTagCount}" : string.Empty;
    public bool HasHiddenTags => HiddenTagCount > 0;
    public IEnumerable<string> VisibleTagColorSequence => AppliedTagColors.Take(3);
    public bool IsGroup => EntityKind == WorkspaceEntityKind.Group;
    public bool IsItem => EntityKind == WorkspaceEntityKind.Item;
    public bool IsTopic => EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group;
    public bool HasContextActions => EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item;
    public bool HasAssetActions => EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Item;
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

    public bool IsMultiSelected
    {
        get => _isMultiSelected;
        set => SetField(ref _isMultiSelected, value);
    }

    public bool HasMultiSelectionContext
    {
        get => _hasMultiSelectionContext;
        set
        {
            if (_hasMultiSelectionContext == value)
            {
                return;
            }

            SetField(ref _hasMultiSelectionContext, value);
            OnPropertyChanged(nameof(IsSingleSelectionContext));
            OnPropertyChanged(nameof(IsGroupAndSingleSelection));
            OnPropertyChanged(nameof(IsItemAndSingleSelection));
        }
    }

    public bool IsSingleSelectionContext => !HasMultiSelectionContext;
    public bool IsGroupAndSingleSelection => IsGroup && IsSingleSelectionContext;
    public bool IsItemAndSingleSelection => IsItem && IsSingleSelectionContext;
    public bool HasContextActionsAndSingleSelection => HasContextActions && IsSingleSelectionContext;
    public bool IsTopicAndSingleSelection => IsTopic && IsSingleSelectionContext;

    public int SelectionCount
    {
        get => _selectionCount;
        set
        {
            if (_selectionCount != value)
            {
                SetField(ref _selectionCount, value);
                OnPropertyChanged(nameof(SelectionCountLabel));
            }
        }
    }

    public string SelectionCountLabel => $"{SelectionCount} selected";

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
    private readonly IItemManagementService _items;
    private readonly WorkspaceTreeSelectionCoordinator _selection;
    private readonly WorkspaceTreeMultiSelection _multiSelection = new();
    private readonly WorkspaceTreeClipboard _clipboard;
    private readonly IItemCsvExportService _csvExport;
    private IItemCsvCodec _csvCodec;
    private readonly HashSet<Guid> _expandedIds = [];
    private HashSet<Guid>? _expandedIdsBeforeFilter;
    private readonly ObservableCollection<TagFilterEntryViewModel> _availableTags = [];
    private WorkspaceSnapshot _snapshot;
    private Guid? _storeId;
    private WorkspaceTreeNodeViewModel? _selectedNode;
    private WorkspaceTreeNodeViewModel? _editingNode;
    private GroupParentReference? _creationAnchor;
    private ItemTopicReference? _itemCreationAnchor;
    private string _queryText = string.Empty;
    private HashSet<Guid> _selectedTagIds = [];
    private NavigationTopicReference? _scopedTopic;
    private bool _scopeToCurrentTopic;
    private bool _includeArchived;
    private int _stageFilterIndex;
    private int _statusFilterIndex;
    private bool _nextToggleExpands = true;
    private string? _errorMessage;
    private bool _isBusy;

    public WorkspaceTreeViewModel(
        IWorkspaceRepository repository,
        IGroupManagementService groups,
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelectionCoordinator? selection = null,
        WorkspaceTreeClipboard? clipboard = null,
        IItemManagementService? items = null,
        IItemCsvExportService? csvExport = null,
        IItemCsvCodec? csvCodec = null,
        IItemCsvFilePicker? filePicker = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _groups = groups ?? throw new ArgumentNullException(nameof(groups));
        _items = items ?? new ItemManagementService(repository);
        _csvExport = csvExport ?? new ItemCsvExportService();
        _csvCodec = csvCodec ?? NullItemCsvCodec.Instance;
        FilePicker = filePicker ?? new NullItemCsvFilePicker();
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _selection = selection ?? new WorkspaceTreeSelectionCoordinator();
        _clipboard = clipboard ?? new WorkspaceTreeClipboard();
        SelectNodeCommand = new RelayCommand(parameter => Select(parameter as WorkspaceTreeNodeViewModel));
        OpenInTabCommand = new RelayCommand(parameter => OpenInTab(parameter as WorkspaceTreeNodeViewModel));
        OpenSelectedInTabsCommand = new RelayCommand(_ => OpenSelectedInTabs());
        ExportSelectedCommand = new RelayCommand(_ => Run(ExportSelectedAsync()));
        BeginCreateCommand = new RelayCommand(_ => Run(BeginCreateAsync()));
        BeginCreateItemCommand = new RelayCommand(_ => Run(BeginCreateItemAsync()));
        BeginRenameCommand = new RelayCommand(_ => BeginRename());
        CopyCommand = new RelayCommand(_ => Copy());
        CutCommand = new RelayCommand(_ => Cut());
        PasteCommand = new RelayCommand(_ => Run(PasteAsync()));
        DuplicateCommand = new RelayCommand(_ => Run(DuplicateAsync()));
        ToggleTagFilterCommand = new RelayCommand(parameter =>
        {
            if (parameter is TagSummary tag) ToggleTagFilter(tag.Id);
            else if (parameter is Guid id) ToggleTagFilter(id);
        });
        ClearTagFiltersCommand = new RelayCommand(_ => ClearTagFilters());
        ClearTagFilterOrRevealSelectionCommand = new RelayCommand(_ => ClearTagFilters());
        ManageAssetsCommand = new RelayCommand(_ =>
        {
            if (_selectedNode?.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Item)
            {
                ManageAssetsRequested?.Invoke(this, new WorkspaceTreeSelection(_selectedNode.EntityKind, _selectedNode.EntityId));
            }
        });
        ClearFiltersCommand = new RelayCommand(_ => ClearAllFilters());
        ToggleExpandCollapseAllCommand = new RelayCommand(_ => ToggleExpandCollapseAll());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<WorkspaceTreeSelection>? OpenInTabRequested;
    public event EventHandler<IReadOnlyList<WorkspaceTreeSelection>>? OpenSelectedInTabsRequested;
    public event EventHandler<WorkspaceTreeSelection>? SelectionChanged;
    public event EventHandler<WorkspaceTreeSelection>? ManageAssetsRequested;
    public event EventHandler? StructureChanged;
    public event EventHandler<IReadOnlySet<Guid>>? EntitiesDeleted;

    public ObservableCollection<WorkspaceTreeNodeViewModel> Roots { get; } = [];
    public ICommand SelectNodeCommand { get; }
    public ICommand OpenInTabCommand { get; }
    public ICommand OpenSelectedInTabsCommand { get; }
    public ICommand ExportSelectedCommand { get; }
    public ICommand BeginCreateCommand { get; }
    public ICommand BeginCreateItemCommand { get; }
    public ICommand BeginRenameCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand ManageAssetsCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ToggleExpandCollapseAllCommand { get; }

    public IReadOnlyList<TagSummary> AvailableTagFilters { get; private set; } = [];
    public IReadOnlyList<Guid> SelectedTagFilterIds => [.. _selectedTagIds];
    public bool IsTagFilterActive => _selectedTagIds.Count > 0;
    public bool HasTagFiltersAvailable => AvailableTagFilters.Count > 0;
    public bool HasFilteredOutSelection => SelectedNode is null && _selection.Selected is { } selected && HasEntityInStore(selected.Id);
    public string? FilteredOutSelectionName => HasFilteredOutSelection ? FindEntityName(_selection.Selected!.Id) : null;
    public string FilteredOutSelectionMessage => HasFilteredOutSelection
        ? $"Selection '{FilteredOutSelectionName}' is hidden by the active tag filter."
        : string.Empty;
    public ICommand ToggleTagFilterCommand { get; }
    public ICommand ClearTagFiltersCommand { get; }
    public ICommand ClearTagFilterOrRevealSelectionCommand { get; }

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

            ApplyMultiSelectionVisualState();

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanManageSelection));
            OnPropertyChanged(nameof(InspectorTitle));
            OnPropertyChanged(nameof(InspectorKind));
            OnPropertyChanged(nameof(InspectorDescription));
            OnPropertyChanged(nameof(InspectorPath));
            OnPropertyChanged(nameof(CanScopeToCurrentTopic));
        }
    }

    public bool HasSelection => SelectedNode is not null;
    public IReadOnlyList<Guid> SelectedEntityIds => _multiSelection.SelectedIds;
    public int SelectedEntityCount => _multiSelection.Count;
    public bool HasMultiSelection => _multiSelection.Count > 1;
    public bool IsNodeMultiSelected(WorkspaceTreeNodeViewModel? node) =>
        node is not null && _multiSelection.Contains(node.EntityId);
    public bool CanManageSelection => SelectedNode?.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item;
    public bool HasEditingNode => _editingNode is not null;
    public IItemCsvFilePicker FilePicker { get; set; }

    public IItemCsvCodec CsvCodec { get => _csvCodec; set => _csvCodec = value; }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get => _errorMessage; private set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public string InspectorTitle => SelectedNode?.Name ?? "No selection";
    public string InspectorKind => SelectedNode?.KindLabel ?? string.Empty;
    public string InspectorDescription => SelectedNode?.Description ?? "No additional properties have been set.";
    public string InspectorPath => SelectedNode is null ? string.Empty : BuildPath(SelectedNode.EntityId);

    public void SelectNodeWithModifiers(
        WorkspaceTreeNodeViewModel? node,
        bool toggle,
        bool range,
        bool extendRange = false)
    {
        if (node is null || node.IsDraft || node.EntityKind is not (WorkspaceEntityKind.Group or WorkspaceEntityKind.Item))
        {
            return;
        }

        var visibleIds = SelectableVisibleNodes().Select(candidate => candidate.EntityId).ToArray();
        if (range)
        {
            _multiSelection.SelectRange(visibleIds, node.EntityId, extendRange);
        }
        else if (toggle)
        {
            _multiSelection.Toggle(node.EntityId);
        }
        else
        {
            _multiSelection.Replace(node.EntityId);
        }

        var activeId = _multiSelection.ActiveId;
        if (activeId is Guid id && FindNode(id) is { } activeNode)
        {
            Select(activeNode, notifySelectionChanged: true, replaceMultiSelection: false);
        }
        else
        {
            ApplyMultiSelectionVisualState();
        }
    }

    public void SelectAllVisibleEntities()
    {
        _multiSelection.SelectAll(SelectableVisibleNodes().Select(node => node.EntityId).ToArray());
        if (_multiSelection.ActiveId is Guid id && FindNode(id) is { } activeNode)
        {
            Select(activeNode, notifySelectionChanged: true, replaceMultiSelection: false);
        }
        else
        {
            ApplyMultiSelectionVisualState();
        }
    }

    public void PrepareContextSelection(WorkspaceTreeNodeViewModel? node)
    {
        if (node is null || node.IsDraft || node.EntityKind is not (WorkspaceEntityKind.Group or WorkspaceEntityKind.Item))
        {
            return;
        }

        if (!_multiSelection.Contains(node.EntityId))
        {
            Select(node);
        }
    }

    public string QueryText
    {
        get => _queryText;
        set
        {
            if (string.Equals(_queryText, value, StringComparison.Ordinal))
            {
                return;
            }

            SetField(ref _queryText, value);
            ApplyFilterTransition();
        }
    }

    public IReadOnlySet<Guid> SelectedTagIds => _selectedTagIds;
    public bool IncludeArchived
    {
        get => _includeArchived;
        set
        {
            if (_includeArchived == value)
            {
                return;
            }

            SetField(ref _includeArchived, value);
            ApplyFilterTransition();
        }
    }

    public bool ScopeToCurrentTopic
    {
        get => _scopeToCurrentTopic;
        set
        {
            if (_scopeToCurrentTopic == value)
            {
                return;
            }

            _scopeToCurrentTopic = value;
            _scopedTopic = value ? ResolveScopeTopic() : null;
            OnPropertyChanged(nameof(ScopedTopicName));
            ApplyFilterTransition();
        }
    }

    public bool CanScopeToCurrentTopic => ResolveScopeTopic() is not null;

    public string ScopedTopicName
    {
        get
        {
            if (_scopedTopic is not { } topic)
            {
                return string.Empty;
            }

            return topic.EntityKind switch
            {
                WorkspaceEntityKind.Niche => _snapshot.Niches.SingleOrDefault(niche => niche.Id == topic.EntityId)?.Name ?? string.Empty,
                WorkspaceEntityKind.Group => _snapshot.Groups.SingleOrDefault(group => group.Id == topic.EntityId)?.Name ?? string.Empty,
                _ => string.Empty
            };
        }
    }

    public bool HasActiveFilters => BuildQuery().IsActive;
    public bool HasNonTextFilters =>
        _selectedTagIds.Count > 0 ||
        _scopeToCurrentTopic ||
        _includeArchived ||
        _stageFilterIndex > 0 ||
        _statusFilterIndex > 0;
    public int ActiveFilterCount =>
        (_selectedTagIds.Count > 0 ? 1 : 0) +
        (_scopeToCurrentTopic ? 1 : 0) +
        (_includeArchived ? 1 : 0) +
        (_stageFilterIndex > 0 ? 1 : 0) +
        (_statusFilterIndex > 0 ? 1 : 0);
    public bool HasVisibleResults => Roots.Count > 0;
    public bool HasEmptyFilterResults => HasActiveFilters && !HasVisibleResults;
    public bool IsFiltering => BuildQuery().IsActive;

    public bool NextToggleExpands => _nextToggleExpands;

    public bool CanToggleExpandCollapseAll => !IsFiltering && Flatten(Roots).Any(node => node.HasChildren);

    public string ExpandCollapseAllTooltip
    {
        get
        {
            if (IsFiltering) return "Filtering already expands all groups";
            if (!Flatten(Roots).Any(node => node.HasChildren)) return "No groups to expand or collapse";
            return _nextToggleExpands ? "Expand all groups" : "Collapse all groups";
        }
    }

    public ObservableCollection<TagFilterEntryViewModel> AvailableTags => _availableTags;

    public bool IsTagSelected(Guid tagId) => _selectedTagIds.Contains(tagId);

    public void SetTagSelected(Guid tagId, bool value)
    {
        var changed = value ? _selectedTagIds.Add(tagId) : _selectedTagIds.Remove(tagId);
        if (!changed)
        {
            return;
        }

        RaiseTagSelectionChanged(tagId);
        ApplyFilterTransition();
    }

    public void ToggleTagFilter(Guid tagId) => SetTagSelected(tagId, !IsTagSelected(tagId));

    public void ClearTagFilters()
    {
        var hadTagFilters = _selectedTagIds.Count > 0;
        _selectedTagIds.Clear();
        OnPropertyChanged(nameof(SelectedTagFilterIds));
        OnPropertyChanged(nameof(IsTagFilterActive));
        RaiseAllTagSelectionsChanged();
        if (hadTagFilters)
        {
            RestorePreFilterExpansion();
        }
        RefreshProjection(captureExpanded: false);
    }

    public bool HasEntityInStore(Guid entityId)
    {
        if (_storeId is not Guid storeId) return false;
        return _snapshot.Niches.Any(n => n.Id == entityId && n.StoreId == storeId) ||
               _snapshot.Groups.Any(g => g.Id == entityId && g.StoreId == storeId) ||
               _snapshot.Items.Any(l => l.Id == entityId && l.StoreId == storeId);
    }

    public string? FindEntityName(Guid entityId)
    {
        if (_snapshot.Niches.SingleOrDefault(n => n.Id == entityId) is { } niche) return niche.Name;
        if (_snapshot.Groups.SingleOrDefault(g => g.Id == entityId) is { } group) return group.Name;
        if (_snapshot.Items.SingleOrDefault(l => l.Id == entityId) is { } item) return item.Name;
        return null;
    }

    public void ClearAllFilters()
    {
        var hadFilters = HasActiveFilters;
        _queryText = string.Empty;
        var tagIdsCleared = _selectedTagIds.Count > 0;
        _selectedTagIds.Clear();
        _scopeToCurrentTopic = false;
        _scopedTopic = null;
        _includeArchived = false;
        _stageFilterIndex = 0;
        _statusFilterIndex = 0;
        OnPropertyChanged(nameof(QueryText));
        OnPropertyChanged(nameof(IncludeArchived));
        OnPropertyChanged(nameof(ScopeToCurrentTopic));
        OnPropertyChanged(nameof(ScopedTopicName));
        OnPropertyChanged(nameof(SelectedTagIds));
        OnPropertyChanged(nameof(StageFilterIndex));
        OnPropertyChanged(nameof(StatusFilterIndex));
        if (tagIdsCleared)
        {
            RaiseAllTagSelectionsChanged();
        }

        if (hadFilters)
        {
            RestorePreFilterExpansion();
        }

        RefreshProjection(captureExpanded: false);
    }

    private void RebuildAvailableTags()
    {
        _availableTags.Clear();
        if (_storeId is not Guid storeId)
        {
            return;
        }

        foreach (var tag in _snapshot.Tags.Where(tag => tag.StoreId == storeId && !tag.IsArchived)
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            _availableTags.Add(new TagFilterEntryViewModel(this, tag.Id, tag.Name));
        }
    }

    private void RaiseTagSelectionChanged(Guid tagId)
    {
        foreach (var entry in _availableTags)
        {
            if (entry.Id == tagId)
            {
                entry.RaiseIsSelectedChanged();
                return;
            }
        }
    }

    private void RaiseAllTagSelectionsChanged()
    {
        foreach (var entry in _availableTags)
        {
            entry.RaiseIsSelectedChanged();
        }
    }

    private void ApplyFilterTransition()
    {
        var active = HasActiveFilters;
        if (active && _expandedIdsBeforeFilter is null)
        {
            CaptureExpanded(Roots);
            _expandedIdsBeforeFilter = [.. _expandedIds];
        }
        else if (!active && _expandedIdsBeforeFilter is not null)
        {
            RestorePreFilterExpansion();
        }

        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(HasNonTextFilters));
        OnPropertyChanged(nameof(ActiveFilterCount));
        OnPropertyChanged(nameof(IsFiltering));
        OnPropertyChanged(nameof(CanScopeToCurrentTopic));
        RefreshProjection(captureExpanded: false);
    }

    private void RestorePreFilterExpansion()
    {
        if (_expandedIdsBeforeFilter is null)
        {
            return;
        }

        _expandedIds.Clear();
        _expandedIds.UnionWith(_expandedIdsBeforeFilter);
        _expandedIdsBeforeFilter = null;
    }

    private NavigationTopicReference? ResolveScopeTopic()
    {
        var selection = _selection.Selected;
        if (selection is null)
        {
            return null;
        }

        if (selection.Kind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group)
        {
            return new NavigationTopicReference(selection.Kind, selection.Id);
        }

        if (selection.Kind == WorkspaceEntityKind.Item)
        {
            var item = _snapshot.Items.SingleOrDefault(candidate => candidate.Id == selection.Id);
            if (item is null || item.IsArchived)
            {
                return null;
            }

            return ItemHierarchy.GetTopic(item);
        }

        return null;
    }

    public int StageFilterIndex
    {
        get => _stageFilterIndex;
        set
        {
            if (_stageFilterIndex == value)
            {
                return;
            }

            SetField(ref _stageFilterIndex, value);
            ApplyFilterTransition();
        }
    }

    public int StatusFilterIndex
    {
        get => _statusFilterIndex;
        set
        {
            if (_statusFilterIndex == value)
            {
                return;
            }

            SetField(ref _statusFilterIndex, value);
            ApplyFilterTransition();
        }
    }

    private WorkspaceTreeQuery BuildQuery() => new(
        Text: _queryText,
        WorkflowStages: _stageFilterIndex > 0 ? new HashSet<WorkflowStage> { WorkflowStages.Ordered[_stageFilterIndex - 1] } : null,
        ItemStatuses: _statusFilterIndex > 0 ? new HashSet<ItemStatus> { ItemStatuses.Ordered[_statusFilterIndex - 1] } : null,
        TagIds: _selectedTagIds.Count > 0 ? _selectedTagIds : null,
        ScopeTopic: _scopeToCurrentTopic ? _scopedTopic : null,
        IncludeArchived: _includeArchived);

    public void SetStore(Guid? storeId, WorkspaceSnapshot snapshot)
    {
        _storeId = storeId;
        _snapshot = snapshot;
        RebuildAvailableTags();
        RefreshProjection();
    }

    public async Task ReloadAsync()
    {
        _snapshot = await _repository.LoadAsync().ConfigureAwait(false);
        RebuildAvailableTags();
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

    public async Task BeginCreateItemAsync()
    {
        if (_storeId is not Guid storeId || IsBusy)
        {
            ErrorMessage = "Select an active store before creating items.";
            return;
        }

        var selected = SelectedNode is null ? _selection.Selected : new WorkspaceTreeSelection(SelectedNode.EntityKind, SelectedNode.EntityId);
        var destination = await _items.ResolveCreateTopicAsync(storeId, selected).ConfigureAwait(false);
        if (!destination.Succeeded)
        {
            ErrorMessage = destination.Error;
            return;
        }

        _itemCreationAnchor = destination.Topic;
        InsertItemDraft(destination.Topic!);
    }

    public void BeginRename()
    {
        if (SelectedNode is not { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item } node || IsBusy)
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
            if (editing.EntityKind == WorkspaceEntityKind.Item)
            {
                var result = await _items.CreateItemAsync(new ItemManagementCreateRequest(_itemCreationAnchor!, editing.DraftName)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    IsBusy = false;
                    ErrorMessage = result.Error;
                    editing.IsEditing = true;
                    return;
                }
                selectedId = result.Item!.Id;
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
        else if (editing.EntityKind == WorkspaceEntityKind.Item)
        {
            var item = _snapshot.Items.Single(candidate => candidate.Id == editing.EntityId);
            var result = await _items.UpdateItemAsync(new ItemManagementUpdateRequest(item.Id, editing.DraftName)).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                IsBusy = false;
                ErrorMessage = result.Error;
                editing.IsEditing = true;
                return;
            }
            selectedId = result.Item!.Id;
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
        if (addAnotherSibling && editing.EntityKind == WorkspaceEntityKind.Item && _itemCreationAnchor is not null)
        {
            InsertItemDraft(_itemCreationAnchor);
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
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Copy, node.EntityKind, node.EntityId));
            ApplyClipboardState();
        }
    }

    public void Cut()
    {
        if (SelectedNode is { EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item } node)
        {
            _clipboard.Set(new WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode.Cut, node.EntityKind, node.EntityId));
            ApplyClipboardState();
        }
    }

    public async Task PasteAsync()
    {
        if (_clipboard.Payload is not { Kind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item } payload ||
            SelectedNode is not { EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group } destination)
        {
            ErrorMessage = "Copy or cut a group or item, then select a niche or group destination.";
            return;
        }

        IsBusy = true;
        Guid selectedId;
        bool succeeded;
        string? error;
        if (payload.Kind == WorkspaceEntityKind.Item)
        {
            var topic = new ItemTopicReference(destination.EntityKind, destination.EntityId);
            var result = payload.Mode == WorkspaceTreeClipboardMode.Copy
                ? await _items.DuplicateItemAsync(new ItemManagementDuplicateRequest(payload.EntityId, topic)).ConfigureAwait(false)
                : await _items.MoveItemAsync(new ItemManagementMoveRequest(payload.EntityId, topic)).ConfigureAwait(false);
            succeeded = result.Succeeded;
            error = result.Error;
            selectedId = result.Item?.Id ?? payload.EntityId;
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
        if (SelectedNode is not { EntityKind: WorkspaceEntityKind.Item } node || IsBusy)
        {
            return;
        }

        IsBusy = true;
        var result = await _items.DuplicateItemAsync(new ItemManagementDuplicateRequest(node.EntityId)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await ReloadAsync().ConfigureAwait(false);
        Select(FindNode(result.Item!.Id));
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DuplicateSelectedAsync()
    {
        if (IsBusy || !HasMultiSelection)
        {
            return;
        }

        var sources = WorkspaceTreeSelectionNormalizer.Normalize(
            _snapshot,
            _multiSelection.SelectedIds.Select(id => FindNode(id))
                .Where(node => node is { IsDraft: false })
                .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId)));
        var originalSnapshot = _snapshot;
        var originalSelectedIds = _multiSelection.SelectedIds.ToArray();
        var originalActiveId = _multiSelection.ActiveId;
        var originalAnchorId = _multiSelection.AnchorId;
        IsBusy = true;
        ErrorMessage = null;

        foreach (var source in sources)
        {
            if (source.Kind == WorkspaceEntityKind.Item)
            {
                var result = await _items.DuplicateItemAsync(new ItemManagementDuplicateRequest(source.Id)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, result.Error).ConfigureAwait(false);
                    return;
                }
            }
            else if (source.Kind == WorkspaceEntityKind.Group && FindNode(source.Id) is { } node)
            {
                var result = await _groups.CopyGroupAsync(new GroupManagementCopyRequest(source.Id, ParentOf(node))).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, result.Error).ConfigureAwait(false);
                    return;
                }
            }
        }

        IsBusy = false;
        await ReloadAsync().ConfigureAwait(false);
        _multiSelection.Restore(originalSelectedIds, originalActiveId, originalAnchorId);
        ApplyMultiSelectionVisualState();
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RestoreBatchFailureAsync(
        WorkspaceSnapshot originalSnapshot,
        IReadOnlyList<Guid> selectedIds,
        Guid? activeId,
        Guid? anchorId,
        string? error)
    {
        try
        {
            await _repository.SaveAsync(originalSnapshot).ConfigureAwait(false);
            _snapshot = originalSnapshot;
            IsBusy = false;
            await ReloadAsync().ConfigureAwait(false);
            _multiSelection.Restore(selectedIds, activeId, anchorId);
            ApplyMultiSelectionVisualState();
            ErrorMessage = error ?? "The group action failed; the confirmed workspace was restored.";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            IsBusy = false;
            ErrorMessage = $"The group action failed and could not be restored: {ex.Message}";
        }
    }

    public GroupDeleteImpact GetDeleteImpact(Guid groupId)
    {
        var group = _snapshot.Groups.Single(candidate => candidate.Id == groupId);
        var groupIds = GroupHierarchy.GetDescendants(_snapshot, group)
            .Select(candidate => candidate.Id)
            .Append(group.Id)
            .ToHashSet();
        var itemIds = _snapshot.Items
            .Where(item => item.GroupId is Guid id && groupIds.Contains(id))
            .Select(item => item.Id)
            .ToHashSet();
        var promptIds = _snapshot.Prompts
            .Where(prompt => prompt.ItemId is Guid id && itemIds.Contains(id))
            .Select(prompt => prompt.Id);
        var entityIds = new HashSet<Guid>(groupIds);
        entityIds.UnionWith(itemIds);
        entityIds.UnionWith(promptIds);
        return new GroupDeleteImpact(group.Id, group.Name, groupIds.Count - 1, itemIds.Count, entityIds);
    }

    public async Task ExportCsvAsync(WorkspaceTreeNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.EntityKind is not (WorkspaceEntityKind.Group or WorkspaceEntityKind.Niche) || IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        var rows = _csvExport.Project(_snapshot, node.EntityKind, node.EntityId);
        var stream = await FilePicker.OpenExportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        try
        {
            await using (stream)
            {
                await CsvCodec.WriteAsync(stream, rows).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = "The items could not be exported to CSV.";
        }
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

    public async Task MoveSelectionAsync(
        IReadOnlyList<WorkspaceTreeSelection> sources,
        WorkspaceTreeNodeViewModel target,
        GroupPlacement placement)
    {
        string? validationError = null;
        if (IsBusy || !CanDrop(sources, target, placement, out validationError))
        {
            ErrorMessage = validationError;
            return;
        }

        var originalSnapshot = _snapshot;
        var originalSelectedIds = _multiSelection.SelectedIds.ToArray();
        var originalActiveId = _multiSelection.ActiveId;
        var originalAnchorId = _multiSelection.AnchorId;
        ErrorMessage = null;
        var effectiveSources = WorkspaceTreeSelectionNormalizer.Normalize(_snapshot, sources);
        foreach (var source in effectiveSources)
        {
            await MoveAsync(source.Kind, source.Id, target, placement).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                try
                {
                    await _repository.SaveAsync(originalSnapshot).ConfigureAwait(false);
                    _snapshot = originalSnapshot;
                    await ReloadAsync().ConfigureAwait(false);
                    _multiSelection.Restore(originalSelectedIds, originalActiveId, originalAnchorId);
                    ApplyMultiSelectionVisualState();
                    ErrorMessage = "The multi-entity move could not be saved; the confirmed hierarchy was restored.";
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException)
                {
                    ErrorMessage = $"The multi-entity move failed and could not be restored: {ex.Message}";
                }

                return;
            }
        }

        var storeId = target.EntityKind == WorkspaceEntityKind.Niche
            ? _snapshot.Niches.Single(niche => niche.Id == target.EntityId).StoreId
            : _snapshot.Groups.Single(group => group.Id == target.EntityId).StoreId;
        _multiSelection.Restore(originalSelectedIds, originalActiveId, originalAnchorId);
        _multiSelection.Reconcile(SelectableEntityIdsForStore(storeId));
        ApplyMultiSelectionVisualState();
    }

    public async Task MoveAsync(WorkspaceEntityKind sourceKind, Guid sourceId, WorkspaceTreeNodeViewModel target, GroupPlacement placement)
    {
        if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group) || IsBusy)
        {
            return;
        }

        if (sourceKind == WorkspaceEntityKind.Item)
        {
            IsBusy = true;
            var itemResult = await _items.MoveItemAsync(new ItemManagementMoveRequest(
                sourceId,
                new ItemTopicReference(target.EntityKind, target.EntityId))).ConfigureAwait(false);
            IsBusy = false;
            if (!itemResult.Succeeded)
            {
                ErrorMessage = itemResult.Error;
                return;
            }

            await ReloadAsync().ConfigureAwait(false);
            Select(FindNode(sourceId));
            StructureChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (IsFiltering && placement.Kind != GroupPlacementKind.Append)
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

    public IReadOnlyList<WorkspaceTreeSelection> GetDragSelections(WorkspaceTreeNodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var selected = _multiSelection.Contains(node.EntityId)
            ? _multiSelection.SelectedIds
                .Select(FindNode)
                .Where(candidate => candidate is { IsDraft: false })
                .Select(candidate => new WorkspaceTreeSelection(candidate!.EntityKind, candidate.EntityId))
            : [new WorkspaceTreeSelection(node.EntityKind, node.EntityId)];
        return WorkspaceTreeSelectionNormalizer.Normalize(_snapshot, selected);
    }

    public bool CanDrop(
        IReadOnlyList<WorkspaceTreeSelection> sources,
        WorkspaceTreeNodeViewModel target,
        GroupPlacement placement,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(target);

        var effectiveSources = WorkspaceTreeSelectionNormalizer.Normalize(_snapshot, sources);
        if (effectiveSources.Count == 0)
        {
            error = "Select an active Item or group before dragging.";
            return false;
        }

        if (effectiveSources.Any(source => source.Id == target.EntityId))
        {
            error = "The destination must be outside the selected hierarchy.";
            return false;
        }

        if (effectiveSources.Any(source =>
                source.Kind == WorkspaceEntityKind.Group &&
                target.EntityKind == WorkspaceEntityKind.Group &&
                GroupHierarchy.IsDescendant(_snapshot, target.EntityId, source.Id)))
        {
            error = "The destination must be outside the selected hierarchy.";
            return false;
        }

        if (effectiveSources.Any(source => source.Kind == WorkspaceEntityKind.Item) &&
            placement.Kind != GroupPlacementKind.Append)
        {
            error = "Items can only be moved inside a niche or group.";
            return false;
        }

        foreach (var source in effectiveSources)
        {
            if (!CanDrop(source.Kind, source.Id, target, placement, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    public bool CanDrop(
        WorkspaceEntityKind sourceKind,
        Guid sourceId,
        WorkspaceTreeNodeViewModel target,
        GroupPlacement placement,
        out string? error)
    {
        if (sourceKind == WorkspaceEntityKind.Item)
        {
            var item = _snapshot.Items.SingleOrDefault(candidate => candidate.Id == sourceId);
            if (item is null || !ItemHierarchy.IsEffectivelyActive(_snapshot, item))
            {
                error = "Only an active item can be moved.";
                return false;
            }

            if (target.EntityKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group))
            {
                error = "Drop the item onto an active niche or group.";
                return false;
            }

            var listingTargetStoreId = target.EntityKind == WorkspaceEntityKind.Niche
                ? _snapshot.Niches.SingleOrDefault(niche => niche.Id == target.EntityId && !niche.IsArchived)?.StoreId
                : _snapshot.Groups.SingleOrDefault(group => group.Id == target.EntityId && GroupHierarchy.IsEffectivelyActive(_snapshot, group))?.StoreId;
            if (listingTargetStoreId != item.StoreId)
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

        if (IsFiltering && placement.Kind != GroupPlacementKind.Append)
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

    private void Select(
        WorkspaceTreeNodeViewModel? node,
        bool notifySelectionChanged = true,
        bool replaceMultiSelection = true)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        if (replaceMultiSelection)
        {
            if (node.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item)
            {
                _multiSelection.Replace(node.EntityId);
            }
            else
            {
                _multiSelection.Clear();
            }
        }

        SelectedNode = node;
        if (node.IsInactive)
        {
            return;
        }

        var selection = new WorkspaceTreeSelection(node.EntityKind, node.EntityId);
        _selection.Select(selection);
        if (notifySelectionChanged)
        {
            SelectionChanged?.Invoke(this, selection);
        }

        ApplyMultiSelectionVisualState();
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

    public void OpenInTabPreservingSelection(WorkspaceTreeNodeViewModel? node)
    {
        if (node is null || node.IsDraft)
        {
            return;
        }

        OpenInTabRequested?.Invoke(this, new WorkspaceTreeSelection(node.EntityKind, node.EntityId));
    }

    private void OpenSelectedInTabs()
    {
        var selections = _multiSelection.SelectedIds
            .Select(FindNode)
            .Where(node => node is { IsDraft: false, EntityKind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item })
            .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId))
            .ToArray();
        if (selections.Length > 0)
        {
            OpenSelectedInTabsRequested?.Invoke(this, selections);
        }
    }

    public async Task ExportSelectedAsync()
    {
        if (IsBusy || !HasMultiSelection)
        {
            return;
        }

        var itemIds = new HashSet<Guid>();
        var sources = WorkspaceTreeSelectionNormalizer.Normalize(
            _snapshot,
            _multiSelection.SelectedIds.Select(id => FindNode(id))
                .Where(node => node is { IsDraft: false })
                .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId)));
        foreach (var source in sources)
        {
            if (source.Kind == WorkspaceEntityKind.Item)
            {
                itemIds.Add(source.Id);
                continue;
            }

            if (_snapshot.Groups.SingleOrDefault(group => group.Id == source.Id) is { } group)
            {
                var groupIds = GroupHierarchy.GetDescendants(_snapshot, group)
                    .Append(group)
                    .Select(candidate => candidate.Id)
                    .ToHashSet();
                foreach (var item in _snapshot.Items.Where(item => item.GroupId is Guid groupId && groupIds.Contains(groupId)))
                {
                    itemIds.Add(item.Id);
                }
            }
        }

        var stream = await FilePicker.OpenExportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        try
        {
            await using (stream)
            {
                await CsvCodec.WriteAsync(stream, _csvExport.ProjectSelected(_snapshot, [.. itemIds])).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = "The selected items could not be exported to CSV.";
        }
    }

    public IReadOnlyList<GroupDestination> GetGroupDestinationsForSelection()
    {
        if (_storeId is not Guid storeId)
        {
            return [];
        }

        var selectedGroups = _multiSelection.SelectedIds
            .Select(id => _snapshot.Groups.SingleOrDefault(group => group.Id == id))
            .Where(group => group is not null)
            .Select(group => group!.Id)
            .ToHashSet();
        var excluded = selectedGroups
            .SelectMany(id => GroupHierarchy.GetDescendants(_snapshot, _snapshot.Groups.Single(group => group.Id == id)).Select(group => group.Id).Append(id))
            .ToHashSet();
        var destinations = new List<GroupDestination>();
        foreach (var niche in _snapshot.Niches.Where(niche => niche.StoreId == storeId && !niche.IsArchived))
        {
            destinations.Add(new GroupDestination(new GroupParentReference(WorkspaceEntityKind.Niche, niche.Id), storeId, niche.Id, niche.Name));
            foreach (var group in _snapshot.Groups
                         .Where(group => group.StoreId == storeId && !group.IsArchived && !excluded.Contains(group.Id) &&
                                         GroupHierarchy.IsEffectivelyActive(_snapshot, group) &&
                                         GroupHierarchy.GetEffectiveNiche(_snapshot, group).Id == niche.Id)
                         .OrderBy(group => group.SortOrder)
                         .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase))
            {
                var path = GroupHierarchy.GetAncestors(_snapshot, group).Select(ancestor => ancestor.Name).Append(group.Name);
                destinations.Add(new GroupDestination(new GroupParentReference(WorkspaceEntityKind.Group, group.Id), storeId, niche.Id, $"{niche.Name} / {string.Join(" / ", path)}"));
            }
        }

        return destinations;
    }

    public GroupDestination? GetDefaultGroupDestination(IReadOnlyList<GroupDestination> destinations)
    {
        var sources = WorkspaceTreeSelectionNormalizer.Normalize(
            _snapshot,
            _multiSelection.SelectedIds.Select(id => FindNode(id))
                .Where(node => node is { IsDraft: false })
                .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId)));
        if (sources.Count == 0)
        {
            return destinations.FirstOrDefault();
        }

        var parents = sources.Select(source => source.Kind == WorkspaceEntityKind.Group
                ? new GroupParentReference(
                    _snapshot.Groups.Single(group => group.Id == source.Id).NicheId is not null ? WorkspaceEntityKind.Niche : WorkspaceEntityKind.Group,
                    _snapshot.Groups.Single(group => group.Id == source.Id).NicheId ?? _snapshot.Groups.Single(group => group.Id == source.Id).ParentGroupId!.Value)
                : _snapshot.Items.Single(item => item.Id == source.Id).GroupId is Guid groupId
                    ? new GroupParentReference(WorkspaceEntityKind.Group, groupId)
                    : new GroupParentReference(WorkspaceEntityKind.Niche, _snapshot.Items.Single(item => item.Id == source.Id).NicheId!.Value))
            .Distinct()
            .ToArray();
        return parents.Length == 1
            ? destinations.SingleOrDefault(destination => destination.Parent == parents[0]) ?? destinations.FirstOrDefault()
            : destinations.FirstOrDefault();
    }

    public async Task GroupSelectedAsync(string name, GroupDestination destination)
    {
        if (IsBusy || !HasMultiSelection || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var sources = WorkspaceTreeSelectionNormalizer.Normalize(
            _snapshot,
            _multiSelection.SelectedIds.Select(id => FindNode(id))
                .Where(node => node is { IsDraft: false })
                .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId)));
        if (sources.Count == 0)
        {
            ErrorMessage = "Select at least one active Item or group to group.";
            return;
        }

        var originalSnapshot = _snapshot;
        var originalSelectedIds = _multiSelection.SelectedIds.ToArray();
        var originalActiveId = _multiSelection.ActiveId;
        var originalAnchorId = _multiSelection.AnchorId;
        IsBusy = true;
        ErrorMessage = null;
        var created = await _groups.CreateGroupAsync(new GroupManagementCreateRequest(destination.Parent, name.Trim())).ConfigureAwait(false);
        if (!created.Succeeded || created.Group is null)
        {
            IsBusy = false;
            ErrorMessage = created.Error ?? "The group could not be created.";
            return;
        }

        var parent = new GroupParentReference(WorkspaceEntityKind.Group, created.Group.Id);
        foreach (var source in sources)
        {
            var error = source.Kind == WorkspaceEntityKind.Item
                ? (await _items.MoveItemAsync(new ItemManagementMoveRequest(source.Id, new ItemTopicReference(WorkspaceEntityKind.Group, created.Group.Id))).ConfigureAwait(false)).Error
                : (await _groups.MoveGroupAsync(new GroupManagementMoveRequest(source.Id, parent)).ConfigureAwait(false)).Error;
            if (!string.IsNullOrWhiteSpace(error))
            {
                await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, error).ConfigureAwait(false);
                return;
            }
        }

        IsBusy = false;
        await ReloadAsync().ConfigureAwait(false);
        _multiSelection.Restore(originalSelectedIds, originalActiveId, originalAnchorId);
        ApplyMultiSelectionVisualState();
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ArchiveSelectedAsync()
    {
        if (IsBusy || !HasMultiSelection)
        {
            return;
        }

        var sources = GetEffectiveSelectedSources();
        var originalSnapshot = _snapshot;
        var originalSelectedIds = _multiSelection.SelectedIds.ToArray();
        var originalActiveId = _multiSelection.ActiveId;
        var originalAnchorId = _multiSelection.AnchorId;
        IsBusy = true;
        ErrorMessage = null;
        foreach (var source in sources)
        {
            var error = source.Kind == WorkspaceEntityKind.Item
                ? (await _items.ArchiveItemAsync(source.Id).ConfigureAwait(false)).Error
                : (await _groups.ArchiveGroupAsync(source.Id).ConfigureAwait(false)).Error;
            if (!string.IsNullOrWhiteSpace(error))
            {
                await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, error).ConfigureAwait(false);
                return;
            }
        }

        IsBusy = false;
        await ReloadAsync().ConfigureAwait(false);
        _multiSelection.Restore(originalSelectedIds, originalActiveId, originalAnchorId);
        _multiSelection.Reconcile(SelectableEntityIdsForStore(_storeId!.Value));
        ApplyMultiSelectionVisualState();
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteSelectedAsync()
    {
        if (IsBusy || !HasMultiSelection)
        {
            return;
        }

        var sources = GetEffectiveSelectedSources();
        var deletedIds = new HashSet<Guid>();
        var originalSnapshot = _snapshot;
        var originalSelectedIds = _multiSelection.SelectedIds.ToArray();
        var originalActiveId = _multiSelection.ActiveId;
        var originalAnchorId = _multiSelection.AnchorId;
        IsBusy = true;
        ErrorMessage = null;
        foreach (var source in sources)
        {
            if (source.Kind == WorkspaceEntityKind.Item)
            {
                var result = await _items.DeleteItemAsync(new ItemManagementDeleteRequest(source.Id, true)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, result.Error).ConfigureAwait(false);
                    return;
                }

                deletedIds.Add(source.Id);
            }
            else
            {
                var impact = GetDeleteImpact(source.Id);
                var result = await _groups.DeleteGroupAsync(new GroupManagementDeleteRequest(source.Id, true)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    await RestoreBatchFailureAsync(originalSnapshot, originalSelectedIds, originalActiveId, originalAnchorId, result.Error).ConfigureAwait(false);
                    return;
                }

                deletedIds.UnionWith(impact.DeletedEntityIds);
            }
        }

        IsBusy = false;
        await ReloadAsync().ConfigureAwait(false);
        _multiSelection.Reconcile(SelectableEntityIdsForStore(_storeId!.Value));
        ApplyMultiSelectionVisualState();
        EntitiesDeleted?.Invoke(this, deletedIds);
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<WorkspaceTreeSelection> GetEffectiveSelectedSources() =>
        WorkspaceTreeSelectionNormalizer.Normalize(
            _snapshot,
            _multiSelection.SelectedIds.Select(id => FindNode(id))
                .Where(node => node is { IsDraft: false })
                .Select(node => new WorkspaceTreeSelection(node!.EntityKind, node.EntityId)));

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

    private void InsertItemDraft(ItemTopicReference topic)
    {
        CancelEdit();
        var parentNode = FindNode(topic.Id);
        if (parentNode is null)
        {
            ErrorMessage = "The selected item destination is not visible in the current tree.";
            return;
        }

        var draft = new WorkspaceTreeNodeViewModel(
            Guid.NewGuid(),
            WorkspaceEntityKind.Item,
            Guid.NewGuid(),
            "New Item",
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

    private void ToggleExpandCollapseAll()
    {
        if (!CanToggleExpandCollapseAll) return;
        if (_nextToggleExpands)
        {
            ExpandAllNodes();
        }
        else
        {
            CollapseAllNodes();
        }

        _nextToggleExpands = !_nextToggleExpands;
        OnPropertyChanged(nameof(NextToggleExpands));
        OnPropertyChanged(nameof(ExpandCollapseAllTooltip));
    }

    private void ExpandAllNodes()
    {
        foreach (var node in Flatten(Roots))
        {
            if (node.HasChildren && !node.IsDraft)
            {
                node.IsExpanded = true;
                _expandedIds.Add(node.EntityId);
            }
        }
    }

    private void CollapseAllNodes()
    {
        var ancestorIds = _editingNode is not null
            ? CollectEditingNodeAncestorIds(Roots, _editingNode)
            : new HashSet<Guid>();
        foreach (var node in Flatten(Roots))
        {
            if (node.HasChildren && !node.IsDraft && !ancestorIds.Contains(node.EntityId))
            {
                node.IsExpanded = false;
                _expandedIds.Remove(node.EntityId);
            }
        }
    }

    private static HashSet<Guid> CollectEditingNodeAncestorIds(
        IEnumerable<WorkspaceTreeNodeViewModel> roots, WorkspaceTreeNodeViewModel target)
    {
        var ancestors = new HashSet<Guid>();
        FindAncestors(roots, target, ancestors);
        return ancestors;
    }

    private static bool FindAncestors(
        IEnumerable<WorkspaceTreeNodeViewModel> nodes,
        WorkspaceTreeNodeViewModel target,
        HashSet<Guid> ancestors)
    {
        foreach (var node in nodes)
        {
            if (ReferenceEquals(node, target))
            {
                return true;
            }

            if (FindAncestors(node.Children, target, ancestors))
            {
                ancestors.Add(node.EntityId);
                return true;
            }
        }

        return false;
    }

    private void RefreshProjection(bool captureExpanded = true)
    {
        if (captureExpanded && !IsFiltering)
        {
            CaptureExpanded(Roots);
        }
        var selectedId = SelectedNode?.IsDraft == false ? SelectedNode.EntityId : _selection.Selected?.Id;
        Roots.Clear();
        if (_storeId is not Guid storeId || _snapshot.Stores.All(store => store.Id != storeId || store.IsArchived))
        {
            SelectedNode = null;
            OnPropertyChanged(nameof(HasVisibleResults));
            OnPropertyChanged(nameof(HasEmptyFilterResults));
            OnPropertyChanged(nameof(CanToggleExpandCollapseAll));
            OnPropertyChanged(nameof(ExpandCollapseAllTooltip));
            return;
        }

        var projection = WorkspaceTreeProjector.Project(_snapshot, storeId, BuildQuery());
        foreach (var root in projection.Roots)
        {
            Roots.Add(ToNode(root));
        }

        SelectedNode = selectedId is Guid id ? FindNode(id) : null;
        _multiSelection.Reconcile(SelectableEntityIdsForStore(storeId));
        ApplyMultiSelectionVisualState();
        ApplyClipboardState();
        OnPropertyChanged(nameof(HasVisibleResults));
        OnPropertyChanged(nameof(HasEmptyFilterResults));
        OnPropertyChanged(nameof(CanToggleExpandCollapseAll));
        OnPropertyChanged(nameof(ExpandCollapseAllTooltip));
    }

    private WorkspaceTreeNodeViewModel ToNode(WorkspaceTreeProjectionNode projected)
    {
        var entity = FindEntity(projected.EntityKind, projected.EntityId);
        var tagColors = projected.EntityKind == WorkspaceEntityKind.Item
            ? ResolveItemTagColors(projected.EntityId)
            : [];
        var node = new WorkspaceTreeNodeViewModel(
            projected.NodeId,
            projected.EntityKind,
            projected.EntityId,
            projected.Name,
            entity?.Description,
            projected.IsDirectMatch,
            projected.HasHiddenChildren,
            projected.Children.Count,
            projected.Children.Select(ToNode),
            appliedTagColors: tagColors,
            isInactive: projected.IsInactive || (entity is Item item && item.Status == ItemStatus.Rejected));
        node.IsExpanded = _expandedIds.Contains(node.EntityId) || IsFiltering;
        return node;
    }

    private IEnumerable<WorkspaceTreeNodeViewModel> SelectableVisibleNodes() =>
        Flatten(Roots).Where(node =>
            !node.IsDraft &&
            !node.IsInactive &&
            node.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item);

    private IEnumerable<Guid> SelectableEntityIdsForStore(Guid storeId) =>
        _snapshot.Groups
            .Where(group => group.StoreId == storeId && !group.IsArchived && GroupHierarchy.IsEffectivelyActive(_snapshot, group))
            .Select(group => group.Id)
            .Concat(_snapshot.Items.Where(item => item.StoreId == storeId && !item.IsArchived).Select(item => item.Id));

    private void ApplyMultiSelectionVisualState()
    {
        foreach (var node in Flatten(Roots))
        {
            node.IsMultiSelected = _multiSelection.Contains(node.EntityId);
            node.HasMultiSelectionContext = HasMultiSelection && node.IsMultiSelected;
            node.SelectionCount = SelectedEntityCount;
            node.IsSelected = _multiSelection.ActiveId == node.EntityId;
        }

        OnPropertyChanged(nameof(SelectedEntityIds));
        OnPropertyChanged(nameof(SelectedEntityCount));
        OnPropertyChanged(nameof(HasMultiSelection));
    }

    private IReadOnlyList<string> ResolveItemTagColors(Guid itemId)
    {
        var tagIds = _snapshot.ItemTags
            .Where(link => link.ItemId == itemId)
            .Select(link => link.TagId)
            .ToHashSet();
        return _snapshot.Tags
            .Where(tag => tagIds.Contains(tag.Id))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tag => tag.Color ?? "#243447")
            .ToArray();
    }

    private WorkspaceEntity? FindEntity(WorkspaceEntityKind kind, Guid id) => kind switch
    {
        WorkspaceEntityKind.Niche => _snapshot.Niches.SingleOrDefault(entity => entity.Id == id),
        WorkspaceEntityKind.Group => _snapshot.Groups.SingleOrDefault(entity => entity.Id == id),
        WorkspaceEntityKind.Item => _snapshot.Items.SingleOrDefault(entity => entity.Id == id),
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
                            _clipboard.Payload is { Kind: WorkspaceEntityKind.Group or WorkspaceEntityKind.Item };
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

public sealed class TagFilterEntryViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceTreeViewModel _owner;

    public TagFilterEntryViewModel(WorkspaceTreeViewModel owner, Guid id, string name)
    {
        _owner = owner;
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _owner.IsTagSelected(Id);
        set => _owner.SetTagSelected(Id, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RaiseIsSelectedChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
}
