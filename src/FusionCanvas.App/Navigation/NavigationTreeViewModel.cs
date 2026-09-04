using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Application.Navigation;

namespace FusionCanvas.App.Navigation;

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
