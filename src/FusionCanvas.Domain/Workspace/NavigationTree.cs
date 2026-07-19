namespace FusionCanvas.Domain.Workspace;

public enum NavigationNodeRole
{
    Store = 0,
    Topic = 1,
    Item = 2
}

public sealed record NavigationNode(
    Guid Id,
    NavigationNodeRole Role,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    Guid? ParentNodeId,
    IReadOnlyList<NavigationNode> Children,
    bool IsInactive = false);

public sealed record NavigationTopicReference(WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public WorkspaceEntityKind EntityKind { get; } = EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? EntityKind
        : throw new ArgumentException("Navigation topic must reference a niche or group.", nameof(EntityKind));

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;
}

public sealed record NavigationTreeSnapshot(IReadOnlyList<NavigationNode> Stores)
{
    public IReadOnlyList<NavigationNode> Flatten() =>
        Stores.SelectMany(Flatten).ToArray();

    public NavigationNode Find(Guid nodeId) =>
        Flatten().FirstOrDefault(node => node.Id == nodeId)
        ?? throw new InvalidOperationException("Navigation node was not found.");

    public IReadOnlyList<Guid> GetPath(Guid nodeId)
    {
        var nodes = Flatten().ToDictionary(node => node.Id);
        if (!nodes.ContainsKey(nodeId))
        {
            throw new InvalidOperationException("Navigation node was not found.");
        }

        var path = new List<Guid>();
        Guid? currentId = nodeId;
        while (currentId is not null)
        {
            var current = nodes[currentId.Value];
            path.Add(current.Id);
            currentId = current.ParentNodeId;
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<NavigationNode> Flatten(NavigationNode node)
    {
        yield return node;

        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}

public static class WorkspaceNavigation
{
    public static NavigationTreeSnapshot BuildTree(WorkspaceSnapshot snapshot, bool includeArchived = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateWorkspace(snapshot);

        var activeStores = snapshot.Stores.Where(store => !store.IsArchived).ToArray();
        var storeIds = activeStores.Select(store => store.Id).ToHashSet();

        IEnumerable<Niche> nichesQuery = snapshot.Niches.Where(niche => storeIds.Contains(niche.StoreId));
        IEnumerable<TopicGroup> groupsQuery = snapshot.Groups.Where(group => storeIds.Contains(group.StoreId));
        IEnumerable<Listing> listingsQuery = snapshot.Listings.Where(listing => storeIds.Contains(listing.StoreId));

        if (!includeArchived)
        {
            nichesQuery = nichesQuery.Where(niche => !niche.IsArchived);
            groupsQuery = groupsQuery.Where(group => GroupHierarchy.IsEffectivelyActive(snapshot, group));
            listingsQuery = listingsQuery.Where(listing => !listing.IsArchived);
        }

        var niches = nichesQuery.ToArray();
        var groups = groupsQuery.ToArray();
        var groupIds = groups.Select(group => group.Id).ToHashSet();

        if (!includeArchived)
        {
            listingsQuery = listingsQuery.Where(listing =>
                (listing.NicheId is Guid nicheId && niches.Any(niche => niche.Id == nicheId)) ||
                (listing.GroupId is Guid groupId && groupIds.Contains(groupId)));
        }

        var listings = listingsQuery.ToArray();

        var nichesByStore = niches
            .GroupBy(niche => niche.StoreId)
            .ToDictionary(group => group.Key, group => group.OrderBy(niche => niche.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var groupsByNiche = groups
            .Where(group => group.NicheId is not null)
            .GroupBy(group => group.NicheId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(topicGroup => topicGroup.SortOrder).ThenBy(topicGroup => topicGroup.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var groupsByParentGroup = groups
            .Where(group => group.ParentGroupId is not null)
            .GroupBy(group => group.ParentGroupId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(topicGroup => topicGroup.SortOrder).ThenBy(topicGroup => topicGroup.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var listingsByNiche = listings
            .Where(listing => listing.NicheId is not null && listing.GroupId is null)
            .GroupBy(listing => listing.NicheId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var listingsByGroup = listings
            .Where(listing => listing.GroupId is Guid groupId)
            .GroupBy(listing => listing.GroupId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var stores = activeStores
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
            .Select(store =>
            {
                var childTopics = nichesByStore.GetValueOrDefault(store.Id, [])
                    .Select(niche => BuildNicheNode(snapshot, niche, groupsByNiche, groupsByParentGroup, listingsByNiche, listingsByGroup, includeArchived));

                return new NavigationNode(
                    store.Id,
                    NavigationNodeRole.Store,
                    WorkspaceEntityKind.Store,
                    store.Id,
                    store.Name,
                    null,
                    childTopics.ToArray(),
                    IsInactive: false);
            })
            .ToArray();

        return new NavigationTreeSnapshot(stores);
    }

    public static WorkspaceSnapshot MoveTopic(
        WorkspaceSnapshot snapshot,
        Guid groupId,
        NavigationTopicReference destinationTopic)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(destinationTopic);
        ValidateWorkspace(snapshot);

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId)
            ?? throw new InvalidOperationException("Only group topics can be moved inside the navigation tree.");

        if (destinationTopic.EntityKind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == destinationTopic.EntityId)
                ?? throw new InvalidOperationException("Destination niche was not found.");

            if (niche.StoreId != group.StoreId)
            {
                throw new InvalidOperationException("A topic cannot be moved outside its store.");
            }

            EnsureActiveDestination(snapshot, niche);

            return snapshot with
            {
                Groups = snapshot.Groups
                    .Select(candidate => candidate.Id == groupId ? candidate with { NicheId = niche.Id, ParentGroupId = null } : candidate)
                    .ToArray()
            };
        }

        var parentGroup = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == destinationTopic.EntityId)
            ?? throw new InvalidOperationException("Destination group was not found.");

        if (parentGroup.StoreId != group.StoreId)
        {
            throw new InvalidOperationException("A topic cannot be moved outside its store.");
        }

        EnsureActiveDestination(snapshot, parentGroup);

        if (parentGroup.Id == group.Id || IsDescendantGroup(snapshot.Groups, descendantId: parentGroup.Id, ancestorId: group.Id))
        {
            throw new InvalidOperationException("A topic cannot be moved under itself or its descendants.");
        }

        return snapshot with
        {
            Groups = snapshot.Groups
                .Select(candidate => candidate.Id == groupId ? candidate with { NicheId = null, ParentGroupId = parentGroup.Id } : candidate)
                .ToArray()
        };
    }

    public static WorkspaceSnapshot MoveListing(
        WorkspaceSnapshot snapshot,
        Guid listingId,
        NavigationTopicReference destinationTopic)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(destinationTopic);
        ValidateWorkspace(snapshot);

        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId)
            ?? throw new InvalidOperationException("Listing was not found.");

        if (destinationTopic.EntityKind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == destinationTopic.EntityId)
                ?? throw new InvalidOperationException("Destination niche was not found.");

            if (niche.StoreId != listing.StoreId)
            {
                throw new InvalidOperationException("A listing cannot be moved outside its store.");
            }

            EnsureActiveDestination(snapshot, niche);

            return snapshot with
            {
                Listings = snapshot.Listings
                    .Select(candidate => candidate.Id == listingId ? candidate with { NicheId = niche.Id, GroupId = null } : candidate)
                    .ToArray()
            };
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == destinationTopic.EntityId)
            ?? throw new InvalidOperationException("Destination group was not found.");

        if (group.StoreId != listing.StoreId)
        {
            throw new InvalidOperationException("A listing cannot be moved outside its store.");
        }

        EnsureActiveDestination(snapshot, group);

        return snapshot with
        {
            Listings = snapshot.Listings
                .Select(candidate => candidate.Id == listingId ? candidate with { NicheId = group.NicheId, GroupId = group.Id } : candidate)
                .ToArray()
        };
    }

    private static NavigationNode BuildNicheNode(
        WorkspaceSnapshot snapshot,
        Niche niche,
        IReadOnlyDictionary<Guid, TopicGroup[]> groupsByNiche,
        IReadOnlyDictionary<Guid, TopicGroup[]> groupsByParentGroup,
        IReadOnlyDictionary<Guid, Listing[]> listingsByNiche,
        IReadOnlyDictionary<Guid, Listing[]> listingsByGroup,
        bool includeArchived)
    {
        var childGroups = groupsByNiche.GetValueOrDefault(niche.Id, [])
            .Select(group => BuildGroupNode(snapshot, group, groupsByParentGroup, listingsByGroup, includeArchived));

        var childListings = listingsByNiche.GetValueOrDefault(niche.Id, [])
            .Select(listing => BuildListingNode(snapshot, listing, niche.Id, includeArchived));

        return new NavigationNode(
            niche.Id,
            NavigationNodeRole.Topic,
            WorkspaceEntityKind.Niche,
            niche.Id,
            niche.Name,
            niche.StoreId,
            childGroups.Concat(childListings).ToArray(),
            IsInactive: includeArchived && niche.IsArchived);
    }

    private static NavigationNode BuildGroupNode(
        WorkspaceSnapshot snapshot,
        TopicGroup group,
        IReadOnlyDictionary<Guid, TopicGroup[]> groupsByParentGroup,
        IReadOnlyDictionary<Guid, Listing[]> listingsByGroup,
        bool includeArchived)
    {
        var childGroups = groupsByParentGroup.GetValueOrDefault(group.Id, [])
            .Select(childGroup => BuildGroupNode(snapshot, childGroup, groupsByParentGroup, listingsByGroup, includeArchived));

        var childListings = listingsByGroup.GetValueOrDefault(group.Id, [])
            .Select(listing => BuildListingNode(snapshot, listing, group.Id, includeArchived));

        return new NavigationNode(
            group.Id,
            NavigationNodeRole.Topic,
            WorkspaceEntityKind.Group,
            group.Id,
            group.Name,
            group.ParentGroupId ?? group.NicheId,
            childGroups.Concat(childListings).ToArray(),
            IsInactive: includeArchived && !GroupHierarchy.IsEffectivelyActive(snapshot, group));
    }

    private static NavigationNode BuildListingNode(WorkspaceSnapshot snapshot, Listing listing, Guid parentNodeId, bool includeArchived) =>
        new(
            listing.Id,
            NavigationNodeRole.Item,
            WorkspaceEntityKind.Listing,
            listing.Id,
            listing.Name,
            parentNodeId,
            [],
            IsInactive: includeArchived && !ListingHierarchy.IsEffectivelyActive(snapshot, listing));

    private static void ValidateWorkspace(WorkspaceSnapshot snapshot)
    {
        EnsureUniqueIds(snapshot.Stores.Select(store => store.Id), "stores");
        EnsureUniqueIds(snapshot.Niches.Select(niche => niche.Id), "niches");
        EnsureUniqueIds(snapshot.Groups.Select(group => group.Id), "groups");
        EnsureUniqueIds(snapshot.Listings.Select(listing => listing.Id), "listings");

        var storeIds = snapshot.Stores.Select(store => store.Id).ToHashSet();
        var nicheIds = snapshot.Niches.Select(niche => niche.Id).ToHashSet();
        var groupIds = snapshot.Groups.Select(group => group.Id).ToHashSet();

        foreach (var niche in snapshot.Niches)
        {
            if (!storeIds.Contains(niche.StoreId))
            {
                throw new InvalidOperationException("A niche must belong to an existing store.");
            }
        }

        foreach (var group in snapshot.Groups)
        {
            if (!storeIds.Contains(group.StoreId))
            {
                throw new InvalidOperationException("A group must belong to an existing store.");
            }

            if ((group.NicheId is null) == (group.ParentGroupId is null))
            {
                throw new InvalidOperationException("A group must belong under exactly one niche or group.");
            }

            if (group.NicheId is Guid nicheId && !nicheIds.Contains(nicheId))
            {
                throw new InvalidOperationException("A group niche parent must exist.");
            }

            if (group.ParentGroupId is Guid parentGroupId && !groupIds.Contains(parentGroupId))
            {
                throw new InvalidOperationException("A group parent must exist.");
            }
        }

        foreach (var group in snapshot.Groups)
        {
            if (IsDescendantGroup(snapshot.Groups, descendantId: group.Id, ancestorId: group.Id))
            {
                throw new InvalidOperationException("A group hierarchy must not contain cycles.");
            }
        }

        foreach (var listing in snapshot.Listings)
        {
            if (!storeIds.Contains(listing.StoreId))
            {
                throw new InvalidOperationException("A listing must belong to an existing store.");
            }

            if (listing.NicheId is null && listing.GroupId is null)
            {
                throw new InvalidOperationException("A listing must belong under a niche or group.");
            }

            if (listing.NicheId is Guid nicheId && !nicheIds.Contains(nicheId))
            {
                throw new InvalidOperationException("A listing niche parent must exist.");
            }

            if (listing.GroupId is Guid groupId && !groupIds.Contains(groupId))
            {
                throw new InvalidOperationException("A listing group parent must exist.");
            }
        }
    }

    private static bool IsDescendantGroup(IEnumerable<TopicGroup> groups, Guid descendantId, Guid ancestorId)
    {
        var parentByGroup = groups.ToDictionary(group => group.Id, group => group.ParentGroupId);
        var visited = new HashSet<Guid>();
        var current = descendantId;

        while (parentByGroup.TryGetValue(current, out var parentId) && parentId is not null)
        {
            if (!visited.Add(current))
            {
                return true;
            }

            if (parentId == ancestorId)
            {
                return true;
            }

            current = parentId.Value;
        }

        return false;
    }

    private static void EnsureActiveDestination(WorkspaceSnapshot snapshot, Niche niche)
    {
        var store = snapshot.Stores.Single(candidate => candidate.Id == niche.StoreId);
        if (niche.IsArchived || store.IsArchived)
        {
            throw new InvalidOperationException("Destination topic and its ancestors must be active.");
        }
    }

    private static void EnsureActiveDestination(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        if (!GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            throw new InvalidOperationException("Destination topic and its ancestors must be active.");
        }
    }

    private static void EnsureUniqueIds(IEnumerable<Guid> ids, string collectionName)
    {
        var seen = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (id == Guid.Empty)
            {
                throw new InvalidOperationException($"The {collectionName} collection contains an empty identifier.");
            }

            if (!seen.Add(id))
            {
                throw new InvalidOperationException($"The {collectionName} collection contains duplicate identifiers.");
            }
        }
    }
}
