using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record WorkspaceTreeQuery(
    string? Text = null,
    IReadOnlySet<WorkspaceEntityKind>? EntityKinds = null,
    IReadOnlySet<ListingStatus>? ListingStatuses = null,
    IReadOnlySet<Guid>? TagIds = null)
{
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text) ||
        EntityKinds is { Count: > 0 } ||
        ListingStatuses is { Count: > 0 } ||
        TagIds is { Count: > 0 };
}

public sealed record WorkspaceTreeProjectionNode(
    Guid NodeId,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    bool IsDirectMatch,
    bool HasHiddenChildren,
    IReadOnlyList<WorkspaceTreeProjectionNode> Children);

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
        var storeNode = WorkspaceNavigation.BuildTree(snapshot).Stores.Single(candidate => candidate.EntityId == store.Id);
        var visibleIds = new HashSet<Guid>();
        var roots = storeNode.Children
            .Select(node => ProjectNode(snapshot, node, query, visibleIds))
            .Where(node => node is not null)
            .Cast<WorkspaceTreeProjectionNode>()
            .ToArray();
        return new WorkspaceTreeProjection(store.Id, query, roots, visibleIds);
    }

    private static WorkspaceTreeProjectionNode? ProjectNode(
        WorkspaceSnapshot snapshot,
        NavigationNode node,
        WorkspaceTreeQuery query,
        HashSet<Guid> visibleIds)
    {
        var projectedChildren = node.Children
            .Select(child => ProjectNode(snapshot, child, query, visibleIds))
            .Where(child => child is not null)
            .Cast<WorkspaceTreeProjectionNode>()
            .ToArray();
        var directMatch = Matches(snapshot, node, query);
        if (query.IsActive && !directMatch && projectedChildren.Length == 0)
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
            projectedChildren);
    }

    private static bool Matches(WorkspaceSnapshot snapshot, NavigationNode node, WorkspaceTreeQuery query)
    {
        if (query.EntityKinds is { Count: > 0 } && !query.EntityKinds.Contains(node.EntityKind))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Text) &&
            !node.Name.Contains(query.Text.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.ListingStatuses is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Listing ||
                snapshot.Listings.Single(listing => listing.Id == node.EntityId) is not { } listing ||
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

            var listingTags = snapshot.ListingTags
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
