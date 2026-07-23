namespace FusionCanvas.Domain.Workspace;

public static class ItemHierarchy
{
    public static NavigationTopicReference GetTopic(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.GroupId is Guid groupId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Group, groupId);
        }

        if (item.NicheId is Guid nicheId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Niche, nicheId);
        }

        throw new InvalidOperationException("An item must belong beneath a niche or group.");
    }

    public static Niche GetEffectiveNiche(WorkspaceSnapshot snapshot, Item item)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(item);

        if (item.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId)
                ?? throw new InvalidOperationException("An item group parent must exist.");
            return GroupHierarchy.GetEffectiveNiche(snapshot, group);
        }

        if (item.NicheId is Guid nicheId)
        {
            return snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId)
                ?? throw new InvalidOperationException("An item niche parent must exist.");
        }

        throw new InvalidOperationException("An item must belong beneath a niche or group.");
    }

    public static bool IsEffectivelyActive(WorkspaceSnapshot snapshot, Item item)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsArchived)
        {
            return false;
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == item.StoreId)
            ?? throw new InvalidOperationException("An item must belong to an existing store.");
        if (store.IsArchived)
        {
            return false;
        }

        if (item.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId)
                ?? throw new InvalidOperationException("An item group parent must exist.");
            return group.StoreId == item.StoreId && GroupHierarchy.IsEffectivelyActive(snapshot, group);
        }

        var niche = GetEffectiveNiche(snapshot, item);
        return niche.StoreId == item.StoreId && !niche.IsArchived;
    }
}
