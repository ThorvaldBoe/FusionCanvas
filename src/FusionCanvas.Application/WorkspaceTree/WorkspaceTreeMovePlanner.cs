using FusionCanvas.Application.Groups;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public static class WorkspaceTreeMovePlanner
{
    public static IReadOnlyList<GroupDestination> BuildGroupDestinations(
        WorkspaceSnapshot snapshot,
        Guid? storeId,
        IReadOnlyList<WorkspaceTreeSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(selections);

        if (storeId is not Guid activeStoreId)
        {
            return [];
        }

        var effectiveSelections = WorkspaceTreeSelectionRules.Normalize(snapshot, selections);
        var selectedGroups = effectiveSelections
            .Where(selection => selection.Kind == WorkspaceEntityKind.Group)
            .Select(selection => selection.Id)
            .ToHashSet();
        var excluded = selectedGroups
            .SelectMany(id => GroupHierarchy.GetDescendants(snapshot, snapshot.Groups.Single(group => group.Id == id))
                .Select(group => group.Id)
                .Append(id))
            .ToHashSet();
        var destinations = new List<GroupDestination>();

        foreach (var niche in snapshot.Niches.Where(niche => niche.StoreId == activeStoreId && !niche.IsArchived))
        {
            destinations.Add(new GroupDestination(
                new GroupParentReference(WorkspaceEntityKind.Niche, niche.Id),
                activeStoreId,
                niche.Id,
                niche.Name));

            foreach (var group in snapshot.Groups
                         .Where(group => group.StoreId == activeStoreId
                                         && !group.IsArchived
                                         && !excluded.Contains(group.Id)
                                         && GroupHierarchy.IsEffectivelyActive(snapshot, group)
                                         && GroupHierarchy.GetEffectiveNiche(snapshot, group).Id == niche.Id)
                         .OrderBy(group => group.SortOrder)
                         .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase))
            {
                var path = GroupHierarchy.GetAncestors(snapshot, group)
                    .Select(ancestor => ancestor.Name)
                    .Append(group.Name);
                destinations.Add(new GroupDestination(
                    new GroupParentReference(WorkspaceEntityKind.Group, group.Id),
                    activeStoreId,
                    niche.Id,
                    $"{niche.Name} / {string.Join(" / ", path)}"));
            }
        }

        return destinations;
    }

    public static GroupDestination? ResolveDefaultGroupDestination(
        WorkspaceSnapshot snapshot,
        IReadOnlyList<WorkspaceTreeSelection> selections,
        IReadOnlyList<GroupDestination> destinations)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(destinations);

        var effectiveSelections = WorkspaceTreeSelectionRules.Normalize(snapshot, selections);
        if (effectiveSelections.Count == 0)
        {
            return destinations.FirstOrDefault();
        }

        var parents = effectiveSelections
            .Select(selection => ResolveParent(snapshot, selection))
            .Distinct()
            .ToArray();
        return parents.Length == 1
            ? destinations.SingleOrDefault(destination => destination.Parent == parents[0]) ?? destinations.FirstOrDefault()
            : destinations.FirstOrDefault();
    }

    private static GroupParentReference ResolveParent(
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelection selection) => selection.Kind switch
        {
            WorkspaceEntityKind.Group => ResolveGroupParent(snapshot, selection.Id),
            WorkspaceEntityKind.Item => ResolveItemParent(snapshot, selection.Id),
            _ => throw new InvalidOperationException("Only groups and items can be grouped.")
        };

    private static GroupParentReference ResolveGroupParent(WorkspaceSnapshot snapshot, Guid groupId)
    {
        var group = snapshot.Groups.Single(group => group.Id == groupId);
        return group.NicheId is Guid nicheId
            ? new GroupParentReference(WorkspaceEntityKind.Niche, nicheId)
            : new GroupParentReference(WorkspaceEntityKind.Group, group.ParentGroupId!.Value);
    }

    private static GroupParentReference ResolveItemParent(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.Single(item => item.Id == itemId);
        return item.GroupId is Guid groupId
            ? new GroupParentReference(WorkspaceEntityKind.Group, groupId)
            : new GroupParentReference(WorkspaceEntityKind.Niche, item.NicheId!.Value);
    }
}
