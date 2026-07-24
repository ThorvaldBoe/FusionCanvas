using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;

namespace FusionCanvas.Application.Navigation;

public interface IWorkspaceNavigationService
{
    NavigationTreeSnapshot LoadTree(WorkspaceSnapshot snapshot);

    NavigationTarget Select(WorkspaceSnapshot snapshot, NavigationTarget target);

    NavigationCreationScope ResolveCreationScope(WorkspaceSnapshot snapshot, NavigationTarget activeTarget);

    WorkspaceSnapshot MoveTopic(WorkspaceSnapshot snapshot, Guid groupId, NavigationTopicReference destinationTopic);

    WorkspaceSnapshot MoveItem(WorkspaceSnapshot snapshot, Guid itemId, NavigationTopicReference destinationTopic);

    IReadOnlyList<Guid> RevealPath(WorkspaceSnapshot snapshot, NavigationTarget target);
}
