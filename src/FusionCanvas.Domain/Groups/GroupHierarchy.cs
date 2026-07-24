using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Groups;

public static class GroupHierarchy
{
    public static WorkspaceEntityKind GetDirectParentKind(TopicGroup group) =>
        group.NicheId is not null ? WorkspaceEntityKind.Niche : WorkspaceEntityKind.Group;

    public static Guid GetDirectParentId(TopicGroup group) =>
        group.NicheId ?? group.ParentGroupId
        ?? throw new InvalidOperationException("A group must have a direct niche or group parent.");

    public static Niche GetEffectiveNiche(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(group);

        foreach (var ancestor in GetAncestors(snapshot, group))
        {
            if (ancestor.NicheId is Guid ancestorNicheId)
            {
                return snapshot.Niches.SingleOrDefault(candidate => candidate.Id == ancestorNicheId)
                    ?? throw new InvalidOperationException("A group niche parent must exist.");
            }
        }

        if (group.NicheId is Guid nicheId)
        {
            return snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId)
                ?? throw new InvalidOperationException("A group niche parent must exist.");
        }

        throw new InvalidOperationException("A group hierarchy must resolve to a niche.");
    }

    public static IReadOnlyList<TopicGroup> GetAncestors(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(group);

        var byId = snapshot.Groups.ToDictionary(candidate => candidate.Id);
        var ancestors = new List<TopicGroup>();
        var visited = new HashSet<Guid> { group.Id };
        var parentId = group.ParentGroupId;

        while (parentId is Guid id)
        {
            if (!visited.Add(id))
            {
                throw new InvalidOperationException("A group hierarchy must not contain cycles.");
            }

            if (!byId.TryGetValue(id, out var parent))
            {
                throw new InvalidOperationException("A group parent must exist.");
            }

            ancestors.Add(parent);
            parentId = parent.ParentGroupId;
        }

        ancestors.Reverse();
        return ancestors;
    }

    public static IReadOnlyList<TopicGroup> GetDescendants(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(group);

        var childrenByParent = snapshot.Groups
            .Where(candidate => candidate.ParentGroupId is not null)
            .GroupBy(candidate => candidate.ParentGroupId!.Value)
            .ToDictionary(candidates => candidates.Key, candidates => candidates.ToArray());
        var descendants = new List<TopicGroup>();
        var visited = new HashSet<Guid> { group.Id };
        var pending = new Stack<TopicGroup>(childrenByParent.GetValueOrDefault(group.Id, []));

        while (pending.TryPop(out var descendant))
        {
            if (!visited.Add(descendant.Id))
            {
                throw new InvalidOperationException("A group hierarchy must not contain cycles.");
            }

            descendants.Add(descendant);
            foreach (var child in childrenByParent.GetValueOrDefault(descendant.Id, []))
            {
                pending.Push(child);
            }
        }

        return descendants;
    }

    public static bool IsDescendant(WorkspaceSnapshot snapshot, Guid candidateId, Guid ancestorId)
    {
        var ancestor = snapshot.Groups.SingleOrDefault(group => group.Id == ancestorId)
            ?? throw new InvalidOperationException("Ancestor group was not found.");
        return GetDescendants(snapshot, ancestor).Any(group => group.Id == candidateId);
    }

    public static bool IsEffectivelyActive(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        if (group.IsArchived || GetAncestors(snapshot, group).Any(ancestor => ancestor.IsArchived))
        {
            return false;
        }

        var niche = GetEffectiveNiche(snapshot, group);
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == group.StoreId)
            ?? throw new InvalidOperationException("A group must belong to an existing store.");
        return !niche.IsArchived && !store.IsArchived;
    }
}
