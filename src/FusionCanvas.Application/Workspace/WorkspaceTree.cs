using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record WorkspaceTreeQuery(
    string? Text = null,
    IReadOnlySet<WorkspaceEntityKind>? EntityKinds = null,
    IReadOnlySet<ListingStatus>? ListingStatuses = null,
    IReadOnlySet<Guid>? TagIds = null,
    NavigationTopicReference? ScopeTopic = null,
    bool IncludeArchived = false)
{
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text) ||
        EntityKinds is { Count: > 0 } ||
        ListingStatuses is { Count: > 0 } ||
        TagIds is { Count: > 0 } ||
        ScopeTopic is not null ||
        IncludeArchived;
}

public sealed record WorkspaceTreeProjectionNode(
    Guid NodeId,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    bool IsDirectMatch,
    bool HasHiddenChildren,
    IReadOnlyList<WorkspaceTreeProjectionNode> Children,
    bool IsInactive = false);

public sealed record WorkspaceTreeProjection(
    Guid StoreId,
    WorkspaceTreeQuery Query,
    IReadOnlyList<WorkspaceTreeProjectionNode> Roots,
    IReadOnlySet<Guid> VisibleEntityIds)
{
    public bool CanReorderBetweenSiblings => !Query.IsActive;
}

public static class WorkspaceTreeProjector
{
    public static WorkspaceTreeProjection Project(WorkspaceSnapshot snapshot, Guid storeId, WorkspaceTreeQuery? query = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        query ??= new WorkspaceTreeQuery();
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId && !candidate.IsArchived)
            ?? throw new InvalidOperationException("Active store was not found.");
        var navigationTree = WorkspaceNavigation.BuildTree(snapshot, includeArchived: query.IncludeArchived);
        var storeNode = navigationTree.Stores.Single(candidate => candidate.EntityId == store.Id);
        var scopedRoot = query.ScopeTopic is null
            ? null
            : FindTopicNode(storeNode, query.ScopeTopic.EntityKind, query.ScopeTopic.EntityId);
        var rootsSource = scopedRoot is null ? storeNode.Children : new[] { scopedRoot };

        var context = BuildContext(snapshot, query);
        var visibleIds = new HashSet<Guid>();
        var roots = rootsSource
            .Select(node => ProjectNode(node, context, visibleIds))
            .Where(node => node is not null)
            .Cast<WorkspaceTreeProjectionNode>()
            .ToArray();
        return new WorkspaceTreeProjection(store.Id, query, roots, visibleIds);
    }

    private static WorkspaceTreeProjectionNode? ProjectNode(
        NavigationNode node,
        ProjectionContext context,
        HashSet<Guid> visibleIds)
    {
        var projectedChildren = node.Children
            .Select(child => ProjectNode(child, context, visibleIds))
            .Where(child => child is not null)
            .Cast<WorkspaceTreeProjectionNode>()
            .ToArray();
        var directMatch = Matches(context, node);
        if (context.Query.IsActive && !directMatch && projectedChildren.Length == 0)
        {
            return null;
        }

        visibleIds.Add(node.EntityId);
        return new WorkspaceTreeProjectionNode(
            node.Id,
            node.EntityKind,
            node.EntityId,
            node.Name,
            directMatch,
            projectedChildren.Length < node.Children.Count,
            projectedChildren,
            node.IsInactive);
    }

    private static bool Matches(ProjectionContext context, NavigationNode node)
    {
        var query = context.Query;

        if (query.EntityKinds is { Count: > 0 } && !query.EntityKinds.Contains(node.EntityKind))
        {
            return false;
        }

        if (context.TrimmedText is not null &&
            !NodeTextMatches(context, node, context.TrimmedText))
        {
            return false;
        }

        if (query.ListingStatuses is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Listing ||
                context.Snapshot.Listings.SingleOrDefault(listing => listing.Id == node.EntityId) is not { } listing ||
                !query.ListingStatuses.Contains(listing.Status))
            {
                return false;
            }
        }

        if (query.TagIds is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Listing)
            {
                return false;
            }

            var listingTags = context.Snapshot.ListingTags
                .Where(link => link.ListingId == node.EntityId)
                .Select(link => link.TagId)
                .ToHashSet();
            if (!query.TagIds.All(listingTags.Contains))
            {
                return false;
            }
        }

        return true;
    }

    private static bool NodeTextMatches(ProjectionContext context, NavigationNode node, string trimmedText)
    {
        if (node.Name.Contains(trimmedText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (node.EntityKind != WorkspaceEntityKind.Listing)
        {
            return false;
        }

        var listing = context.Snapshot.Listings.SingleOrDefault(candidate => candidate.Id == node.EntityId);
        if (listing is null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(listing.Description) &&
            listing.Description.Contains(trimmedText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.ListingNotes.TryGetValue(listing.Id, out var notes) &&
            notes.Contains(trimmedText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.TagNamesByListing.TryGetValue(listing.Id, out var tagNames) &&
            tagNames.Any(tag => tag.Contains(trimmedText, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static NavigationNode? FindTopicNode(NavigationNode node, WorkspaceEntityKind kind, Guid entityId)
    {
        if (node.EntityKind == kind && node.EntityId == entityId)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindTopicNode(child, kind, entityId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static ProjectionContext BuildContext(WorkspaceSnapshot snapshot, WorkspaceTreeQuery query)
    {
        var trimmed = string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim();

        var listingNotes = new Dictionary<Guid, string>();
        foreach (var listing in snapshot.Listings)
        {
            var notes = ListingMetadata.TryGetNotes(listing.MetadataJson);
            if (notes is not null)
            {
                listingNotes[listing.Id] = notes;
            }
        }

        var tagNamesByTagId = snapshot.Tags.ToDictionary(tag => tag.Id, tag => tag.Name);
        var tagNamesByListing = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var group in snapshot.ListingTags.GroupBy(link => link.ListingId))
        {
            var names = group
                .Select(link => tagNamesByTagId.GetValueOrDefault(link.TagId, string.Empty))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();
            tagNamesByListing[group.Key] = names;
        }

        return new ProjectionContext(snapshot, query, trimmed, listingNotes, tagNamesByListing);
    }

    private sealed record ProjectionContext(
        WorkspaceSnapshot Snapshot,
        WorkspaceTreeQuery Query,
        string? TrimmedText,
        IReadOnlyDictionary<Guid, string> ListingNotes,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> TagNamesByListing);
}

public sealed class WorkspaceTreeSelectionCoordinator
{
    public event EventHandler<WorkspaceTreeSelection?>? SelectionChanged;

    public WorkspaceTreeSelection? Selected { get; private set; }

    public void Select(WorkspaceTreeSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Id == Guid.Empty)
        {
            throw new ArgumentException("Selection identifier must not be empty.", nameof(selection));
        }

        if (Selected == selection)
        {
            return;
        }

        Selected = selection;
        SelectionChanged?.Invoke(this, Selected);
    }

    public void Clear()
    {
        if (Selected is null)
        {
            return;
        }

        Selected = null;
        SelectionChanged?.Invoke(this, null);
    }
}

public enum WorkspaceTreeClipboardMode
{
    Copy = 0,
    Cut = 1
}

public sealed record WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode Mode, WorkspaceEntityKind Kind, Guid EntityId);

public sealed class WorkspaceTreeClipboard
{
    public WorkspaceTreeClipboardPayload? Payload { get; private set; }

    public void Set(WorkspaceTreeClipboardPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Clipboard entity identifier must not be empty.", nameof(payload));
        }

        Payload = payload;
    }

    public void Clear() => Payload = null;
}
