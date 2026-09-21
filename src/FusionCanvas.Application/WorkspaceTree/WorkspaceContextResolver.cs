using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public static class WorkspaceContextResolver
{
    public static Guid? ResolveStoreId(
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelection selection)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(selection);

        return selection.Kind switch
        {
            WorkspaceEntityKind.Store => selection.Id,
            WorkspaceEntityKind.Niche => snapshot.Niches
                .SingleOrDefault(niche => niche.Id == selection.Id)?.StoreId,
            WorkspaceEntityKind.Group => snapshot.Groups
                .SingleOrDefault(group => group.Id == selection.Id)?.StoreId,
            WorkspaceEntityKind.Item => snapshot.Items
                .SingleOrDefault(item => item.Id == selection.Id)?.StoreId,
            _ => null
        };
    }

    public static Guid? ResolveEffectiveNicheId(
        WorkspaceSnapshot snapshot,
        Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Groups.SingleOrDefault(group => group.Id == groupId) is { } group
            ? GroupHierarchy.GetEffectiveNiche(snapshot, group).Id
            : null;
    }
}
