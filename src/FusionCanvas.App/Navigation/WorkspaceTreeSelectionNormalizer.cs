using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Navigation;

/// <summary>
/// Session-only selection state for the workspace tree. The canonical active
/// selection remains owned by <see cref="WorkspaceTreeSelectionCoordinator"/>.
/// </summary>
public static class WorkspaceTreeSelectionNormalizer
{
    public static IReadOnlyList<WorkspaceTreeSelection> Normalize(
        WorkspaceSnapshot snapshot,
        IEnumerable<WorkspaceTreeSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(selections);

        var selected = selections
            .Where(selection => selection.Kind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item)
            .Distinct()
            .ToArray();
        var selectedGroupIds = selected
            .Where(selection => selection.Kind == WorkspaceEntityKind.Group)
            .Select(selection => selection.Id)
            .ToHashSet();

        return selected
            .Where(selection => selection.Kind == WorkspaceEntityKind.Group ||
                                !selectedGroupIds.Any(groupId => IsWithinGroup(snapshot, selection.Id, groupId)))
            .ToArray();
    }

    public static bool IsWithinGroup(WorkspaceSnapshot snapshot, Guid entityId, Guid groupId)
    {
        if (entityId == groupId)
        {
            return true;
        }

        if (snapshot.Groups.SingleOrDefault(group => group.Id == entityId) is { } entityGroup)
        {
            return entityGroup.Id == groupId ||
                   GroupHierarchy.GetAncestors(snapshot, entityGroup).Any(ancestor => ancestor.Id == groupId);
        }

        return snapshot.Items.SingleOrDefault(item => item.Id == entityId) is { GroupId: Guid itemGroupId } &&
               (itemGroupId == groupId || GroupHierarchy.IsDescendant(snapshot, itemGroupId, groupId));
    }
}
