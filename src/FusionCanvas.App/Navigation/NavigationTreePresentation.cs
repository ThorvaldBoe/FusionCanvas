using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Navigation;

public sealed class NavigationTreePresentationState
{
    private readonly HashSet<Guid> _expandedNodeIds = [];

    public Guid? SelectedNodeId { get; private set; }

    public Guid? RevealedNodeId { get; private set; }

    public IReadOnlyCollection<Guid> ExpandedNodeIds => _expandedNodeIds;

    public bool IsExpanded(Guid nodeId) => _expandedNodeIds.Contains(nodeId);

    public void Select(Guid nodeId) => SelectedNodeId = RequireNodeId(nodeId);

    public void Expand(Guid nodeId) => _expandedNodeIds.Add(RequireNodeId(nodeId));

    public void Collapse(Guid nodeId) => _expandedNodeIds.Remove(RequireNodeId(nodeId));

    public void RevealPath(IReadOnlyList<Guid> nodePath)
    {
        ArgumentNullException.ThrowIfNull(nodePath);

        if (nodePath.Count == 0)
        {
            throw new ArgumentException("Reveal path must include at least one node.", nameof(nodePath));
        }

        foreach (var nodeId in nodePath.Take(nodePath.Count - 1))
        {
            Expand(nodeId);
        }

        RevealedNodeId = RequireNodeId(nodePath[^1]);
        SelectedNodeId = RevealedNodeId;
    }

    private static Guid RequireNodeId(Guid nodeId) =>
        nodeId == Guid.Empty
            ? throw new ArgumentException("Identifier must not be empty.", nameof(nodeId))
            : nodeId;
}

public sealed record NavigationTreeNodeViewModel(
    Guid NodeId,
    NavigationNodeRole Role,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    bool IsExpanded,
    bool IsSelected,
    bool IsRevealed,
    IReadOnlyList<NavigationTreeNodeViewModel> Children);

public sealed class NavigationTreeViewModel
{
    private readonly IWorkspaceNavigationService _navigationService;
    private WorkspaceSnapshot _snapshot;

    public NavigationTreeViewModel(
        WorkspaceSnapshot snapshot,
        IWorkspaceNavigationService navigationService,
        NavigationTreePresentationState? presentationState = null)
    {
        _snapshot = snapshot;
        _navigationService = navigationService;
        PresentationState = presentationState ?? new NavigationTreePresentationState();
        Refresh();
    }

    public NavigationTreePresentationState PresentationState { get; }

    public IReadOnlyList<NavigationTreeNodeViewModel> Stores { get; private set; } = [];

    public NavigationTarget? ActiveTarget { get; private set; }

    public void Select(NavigationTarget target)
    {
        ActiveTarget = _navigationService.Select(_snapshot, target);
        var tree = _navigationService.LoadTree(_snapshot);
        var node = tree.Flatten().Single(candidate => candidate.EntityKind == target.EntityKind && candidate.EntityId == target.EntityId);
        PresentationState.Select(node.Id);
        Refresh();
    }

    public void Expand(Guid nodeId)
    {
        PresentationState.Expand(nodeId);
        Refresh();
    }

    public void Collapse(Guid nodeId)
    {
        PresentationState.Collapse(nodeId);
        Refresh();
    }

    public void Reveal(NavigationTarget target)
    {
        var path = _navigationService.RevealPath(_snapshot, target);
        PresentationState.RevealPath(path);
        ActiveTarget = _navigationService.Select(_snapshot, target);
        Refresh();
    }

    public void MoveTopic(Guid groupId, NavigationTopicReference destinationTopic)
    {
        _snapshot = _navigationService.MoveTopic(_snapshot, groupId, destinationTopic);
        Refresh();
    }

    public void MoveItem(Guid itemId, NavigationTopicReference destinationTopic)
    {
        _snapshot = _navigationService.MoveItem(_snapshot, itemId, destinationTopic);
        Refresh();
    }

    private void Refresh()
    {
        Stores = _navigationService.LoadTree(_snapshot)
            .Stores
            .Select(ToViewModel)
            .ToArray();
    }

    private NavigationTreeNodeViewModel ToViewModel(NavigationNode node)
    {
        var isExpanded = PresentationState.IsExpanded(node.Id);
        var children = isExpanded
            ? node.Children.Select(ToViewModel).ToArray()
            : [];

        return new NavigationTreeNodeViewModel(
            node.Id,
            node.Role,
            node.EntityKind,
            node.EntityId,
            node.Name,
            isExpanded,
            PresentationState.SelectedNodeId == node.Id,
            PresentationState.RevealedNodeId == node.Id,
            children);
    }
}
