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
