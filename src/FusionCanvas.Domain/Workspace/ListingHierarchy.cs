namespace FusionCanvas.Domain.Workspace;

public static class ListingHierarchy
{
    public static NavigationTopicReference GetTopic(Listing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        if (listing.GroupId is Guid groupId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Group, groupId);
        }

        if (listing.NicheId is Guid nicheId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Niche, nicheId);
        }

        throw new InvalidOperationException("A listing must belong beneath a niche or group.");
    }

    public static Niche GetEffectiveNiche(WorkspaceSnapshot snapshot, Listing listing)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(listing);

        if (listing.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId)
                ?? throw new InvalidOperationException("A listing group parent must exist.");
            return GroupHierarchy.GetEffectiveNiche(snapshot, group);
        }

        if (listing.NicheId is Guid nicheId)
        {
            return snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId)
                ?? throw new InvalidOperationException("A listing niche parent must exist.");
        }

        throw new InvalidOperationException("A listing must belong beneath a niche or group.");
    }

    public static bool IsEffectivelyActive(WorkspaceSnapshot snapshot, Listing listing)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(listing);

        if (listing.IsArchived)
        {
            return false;
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == listing.StoreId)
            ?? throw new InvalidOperationException("A listing must belong to an existing store.");
        if (store.IsArchived)
        {
            return false;
        }

        if (listing.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId)
                ?? throw new InvalidOperationException("A listing group parent must exist.");
            return group.StoreId == listing.StoreId && GroupHierarchy.IsEffectivelyActive(snapshot, group);
        }

        var niche = GetEffectiveNiche(snapshot, listing);
        return niche.StoreId == listing.StoreId && !niche.IsArchived;
    }
}
