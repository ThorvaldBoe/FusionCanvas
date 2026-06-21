using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public enum NavigationTargetKind
{
    Store = 0,
    Topic = 1,
    Listing = 2
}

public sealed record NavigationTarget(NavigationTargetKind Kind, WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public WorkspaceEntityKind EntityKind { get; } = ValidateEntityKind(Kind, EntityKind);

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;

    private static WorkspaceEntityKind ValidateEntityKind(NavigationTargetKind kind, WorkspaceEntityKind entityKind) =>
        kind switch
        {
            NavigationTargetKind.Store when entityKind == WorkspaceEntityKind.Store => entityKind,
            NavigationTargetKind.Topic when entityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group => entityKind,
            NavigationTargetKind.Listing when entityKind == WorkspaceEntityKind.Listing => entityKind,
            _ => throw new ArgumentException("Navigation target kind does not match the workspace entity kind.", nameof(entityKind))
        };
}

public sealed record NavigationCreationScope(Guid StoreId, Guid? TopicId, WorkspaceEntityKind? TopicKind);

public interface IWorkspaceNavigationService
{
    NavigationTreeSnapshot LoadTree(WorkspaceSnapshot snapshot);

    NavigationTarget Select(WorkspaceSnapshot snapshot, NavigationTarget target);

    NavigationCreationScope ResolveCreationScope(WorkspaceSnapshot snapshot, NavigationTarget activeTarget);

    WorkspaceSnapshot MoveTopic(WorkspaceSnapshot snapshot, Guid groupId, NavigationTopicReference destinationTopic);

    WorkspaceSnapshot MoveListing(WorkspaceSnapshot snapshot, Guid listingId, NavigationTopicReference destinationTopic);

    IReadOnlyList<Guid> RevealPath(WorkspaceSnapshot snapshot, NavigationTarget target);
}

public sealed class WorkspaceNavigationService : IWorkspaceNavigationService
{
    public NavigationTreeSnapshot LoadTree(WorkspaceSnapshot snapshot) =>
        WorkspaceNavigation.BuildTree(snapshot);

    public NavigationTarget Select(WorkspaceSnapshot snapshot, NavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var tree = LoadTree(snapshot);
        var node = tree.Flatten().FirstOrDefault(candidate =>
            candidate.EntityKind == target.EntityKind && candidate.EntityId == target.EntityId);

        if (node is null)
        {
            throw new InvalidOperationException("The selected navigation target does not exist in the workspace tree.");
        }

        return target;
    }

    public NavigationCreationScope ResolveCreationScope(WorkspaceSnapshot snapshot, NavigationTarget activeTarget)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(activeTarget);

        return activeTarget.Kind switch
        {
            NavigationTargetKind.Store => ResolveStoreScope(snapshot, activeTarget.EntityId),
            NavigationTargetKind.Topic => ResolveTopicScope(snapshot, activeTarget.EntityKind, activeTarget.EntityId),
            NavigationTargetKind.Listing => ResolveListingScope(snapshot, activeTarget.EntityId),
            _ => throw new InvalidOperationException("Unsupported navigation target.")
        };
    }

    public WorkspaceSnapshot MoveTopic(
        WorkspaceSnapshot snapshot,
        Guid groupId,
        NavigationTopicReference destinationTopic) =>
        WorkspaceNavigation.MoveTopic(snapshot, groupId, destinationTopic);

    public WorkspaceSnapshot MoveListing(
        WorkspaceSnapshot snapshot,
        Guid listingId,
        NavigationTopicReference destinationTopic) =>
        WorkspaceNavigation.MoveListing(snapshot, listingId, destinationTopic);

    public IReadOnlyList<Guid> RevealPath(WorkspaceSnapshot snapshot, NavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var tree = LoadTree(snapshot);
        var node = tree.Flatten().FirstOrDefault(candidate =>
            candidate.EntityKind == target.EntityKind && candidate.EntityId == target.EntityId)
            ?? throw new InvalidOperationException("The reveal target does not exist in the workspace tree.");

        return tree.GetPath(node.Id);
    }

    private static NavigationCreationScope ResolveStoreScope(WorkspaceSnapshot snapshot, Guid storeId)
    {
        if (snapshot.Stores.All(store => store.Id != storeId))
        {
            throw new InvalidOperationException("Store was not found.");
        }

        return new NavigationCreationScope(storeId, null, null);
    }

    private static NavigationCreationScope ResolveTopicScope(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind topicKind,
        Guid topicId)
    {
        if (topicKind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == topicId)
                ?? throw new InvalidOperationException("Niche was not found.");

            return new NavigationCreationScope(niche.StoreId, niche.Id, WorkspaceEntityKind.Niche);
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == topicId)
            ?? throw new InvalidOperationException("Group was not found.");

        return new NavigationCreationScope(group.StoreId, group.Id, WorkspaceEntityKind.Group);
    }

    private static NavigationCreationScope ResolveListingScope(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId)
            ?? throw new InvalidOperationException("Listing was not found.");

        if (listing.GroupId is Guid groupId)
        {
            return new NavigationCreationScope(listing.StoreId, groupId, WorkspaceEntityKind.Group);
        }

        if (listing.NicheId is Guid nicheId)
        {
            return new NavigationCreationScope(listing.StoreId, nicheId, WorkspaceEntityKind.Niche);
        }

        throw new InvalidOperationException("Listing does not have a valid creation scope.");
    }
}
