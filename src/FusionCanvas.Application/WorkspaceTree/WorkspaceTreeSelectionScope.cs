using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public static class WorkspaceTreeSelectionScope
{
    public static IReadOnlyList<Guid> GetSelectableEntityIds(
        WorkspaceSnapshot snapshot,
        Guid storeId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Groups
            .Where(group => group.StoreId == storeId &&
                            !group.IsArchived &&
                            GroupHierarchy.IsEffectivelyActive(snapshot, group))
            .Select(group => group.Id)
            .Concat(snapshot.Items
                .Where(item => item.StoreId == storeId && !item.IsArchived)
                .Select(item => item.Id))
            .ToArray();
    }

    public static Guid? ResolveTopLevelNicheId(
        WorkspaceSnapshot snapshot,
        Guid entityId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Niches.Any(niche => niche.Id == entityId))
        {
            return entityId;
        }

        if (snapshot.Items.SingleOrDefault(item => item.Id == entityId) is { } item)
        {
            return item.NicheId;
        }

        if (snapshot.Groups.SingleOrDefault(group => group.Id == entityId) is { } group)
        {
            return group.NicheId ??
                   (group.ParentGroupId is Guid parentId
                       ? ResolveTopLevelNicheId(snapshot, parentId)
                       : null);
        }

        return null;
    }
}
