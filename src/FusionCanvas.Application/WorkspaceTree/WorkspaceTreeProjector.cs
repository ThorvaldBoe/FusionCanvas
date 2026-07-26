using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Application.Items;

namespace FusionCanvas.Application.WorkspaceTree;

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

        if (query.ItemStatuses is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Item ||
                context.Snapshot.Items.SingleOrDefault(listing => listing.Id == node.EntityId) is not { } listing ||
                !query.ItemStatuses.Contains(listing.Status))
            {
                return false;
            }
        }

        if (query.WorkflowStages is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Item ||
                context.Snapshot.Items.SingleOrDefault(listing => listing.Id == node.EntityId) is not { } stageListing ||
                !query.WorkflowStages.Contains(stageListing.Stage))
            {
                return false;
            }
        }

        if (query.TagIds is { Count: > 0 })
        {
            if (node.EntityKind != WorkspaceEntityKind.Item)
            {
                return false;
            }

            var listingTags = context.Snapshot.ItemTags
                .Where(link => link.ItemId == node.EntityId)
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

        if (node.EntityKind != WorkspaceEntityKind.Item)
        {
            return false;
        }

        var listing = context.Snapshot.Items.SingleOrDefault(candidate => candidate.Id == node.EntityId);
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
        foreach (var listing in snapshot.Items)
        {
            var notes = ItemMetadataCodec.TryGetNotes(listing.MetadataJson);
            if (notes is not null)
            {
                listingNotes[listing.Id] = notes;
            }
        }

        var tagNamesByTagId = snapshot.Tags.ToDictionary(tag => tag.Id, tag => tag.Name);
        var tagNamesByListing = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var group in snapshot.ItemTags.GroupBy(link => link.ItemId))
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
