using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed class ToolContextResolver : IToolContextResolver
{
    public ToolContextResolution Resolve(ToolContextResolveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Snapshot);

        if (request.RequiresSelectedItem && request.SelectionKind != ToolContextSelectionKind.Item)
        {
            return ToolContextResolution.Unavailable("This tool requires a selected item.");
        }

        var scopeKind = request.ScopeOverride ?? DefaultScope(request.SelectionKind);

        return request.SelectionKind switch
        {
            ToolContextSelectionKind.Store => ResolveStore(request, scopeKind),
            ToolContextSelectionKind.Topic => ResolveTopic(request, scopeKind),
            ToolContextSelectionKind.Item => ResolveItem(request, scopeKind),
            _ => ToolContextResolution.Unavailable("Unsupported selection kind.")
        };
    }

    public ToolContextResolution ResolveScope(ToolContextResolveRequest request, ToolContextScopeKind scope) =>
        Resolve(request with { ScopeOverride = scope });

    public ToolContextCreationDefaults ResolveCreationDefaults(ToolContextResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        var context = resolution.Context
            ?? throw new InvalidOperationException("Creation defaults require an available tool context.");

        var targetTopic = context.SelectedTopic;
        Guid? nicheId = null;
        Guid? groupId = null;

        if (targetTopic is not null)
        {
            if (targetTopic.EntityKind == WorkspaceEntityKind.Niche)
            {
                nicheId = targetTopic.EntityId;
            }
            else if (targetTopic.EntityKind == WorkspaceEntityKind.Group)
            {
                groupId = targetTopic.EntityId;
                nicheId = context.ActiveNiche?.Id;
            }
        }

        return new ToolContextCreationDefaults(
            context.ActiveStore.Id,
            nicheId,
            groupId,
            context.ScopeKind,
            context.InheritedMetadata,
            context.InheritedTags);
    }

    private static ToolContextResolution ResolveStore(ToolContextResolveRequest request, ToolContextScopeKind scopeKind)
    {
        if (request.SelectedEntityKind != WorkspaceEntityKind.Store)
        {
            return ToolContextResolution.Unavailable("Store context requires a store selection.");
        }

        var store = request.Snapshot.Stores.SingleOrDefault(candidate => candidate.Id == request.SelectedEntityId);
        if (store is null)
        {
            return ToolContextResolution.Unavailable("Selected store was not found.");
        }

        return Available(new ToolContext(
            store,
            null,
            [],
            null,
            null,
            request.WorkflowStage,
            scopeKind,
            ScopeSummary(scopeKind, store.Name, "Store scope"),
            MetadataValues(store, inherited: false),
            MetadataValues(store, inherited: false),
            [],
            [],
            NearbyForStore(request.Snapshot, store.Id, request.NearbyWorkLimit)));
    }

    private static ToolContextResolution ResolveTopic(ToolContextResolveRequest request, ToolContextScopeKind scopeKind)
    {
        var topic = ResolveTopicReference(request.Snapshot, request.SelectedEntityKind, request.SelectedEntityId);
        if (topic is null)
        {
            return ToolContextResolution.Unavailable("Selected topic was not found.");
        }

        var store = request.Snapshot.Stores.Single(candidate => candidate.Id == topic.StoreId);
        var niche = ResolveNicheForTopic(request.Snapshot, topic.EntityKind, topic.EntityId);
        var path = ResolveTopicPath(request.Snapshot, topic.EntityKind, topic.EntityId);
        var selectedTopic = path[^1];
        var metadataSources = MetadataSources(store, niche, path, selectedItem: null);

        return Available(new ToolContext(
            store,
            niche,
            path,
            selectedTopic,
            null,
            request.WorkflowStage,
            scopeKind,
            ScopeSummary(scopeKind, selectedTopic.Name, PathDescription(path)),
            InheritedMetadata(request.Snapshot, metadataSources),
            [],
            [],
            [],
            NearbyForTopic(request.Snapshot, topic.EntityKind, topic.EntityId, request.NearbyWorkLimit)));
    }

    private static ToolContextResolution ResolveItem(ToolContextResolveRequest request, ToolContextScopeKind scopeKind)
    {
        if (request.SelectedEntityKind != WorkspaceEntityKind.Item)
        {
            return ToolContextResolution.Unavailable("Item context requires a listing selection.");
        }

        var listing = request.Snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.SelectedEntityId);
        if (listing is null)
        {
            return ToolContextResolution.Unavailable("Selected item was not found.");
        }

        var store = request.Snapshot.Stores.Single(candidate => candidate.Id == listing.StoreId);
        var topic = ListingTopic(listing);
        var path = ResolveTopicPath(request.Snapshot, topic.EntityKind, topic.EntityId);
        var selectedTopic = path[^1];
        var niche = ResolveNicheForTopic(request.Snapshot, topic.EntityKind, topic.EntityId);
        var metadataSources = MetadataSources(store, niche, path, listing);
        var explicitTags = ExplicitListingTags(request.Snapshot, listing);

        return Available(new ToolContext(
            store,
            niche,
            path,
            selectedTopic,
            listing,
            request.WorkflowStage,
            scopeKind,
            ScopeSummary(scopeKind, ItemDisplayNameFormatter.Format(listing), PathDescription(path)),
            InheritedMetadata(request.Snapshot, metadataSources.Where(source => source.EntityId != listing.Id)),
            MetadataValues(listing, inherited: false),
            [],
            explicitTags,
            NearbyForTopic(request.Snapshot, topic.EntityKind, topic.EntityId, request.NearbyWorkLimit, listing.Id)));
    }

    private static ToolContextResolution Available(ToolContext context) =>
        new(true, null, context.ScopeSummary, context);

    private static ToolContextScopeKind DefaultScope(ToolContextSelectionKind selectionKind) =>
        selectionKind switch
        {
            ToolContextSelectionKind.Store => ToolContextScopeKind.Store,
            ToolContextSelectionKind.Topic => ToolContextScopeKind.CurrentTopic,
            ToolContextSelectionKind.Item => ToolContextScopeKind.CurrentItem,
            _ => ToolContextScopeKind.Unsupported
        };

    private static ToolContextScopeSummary ScopeSummary(ToolContextScopeKind scopeKind, string subject, string description)
    {
        var label = scopeKind switch
        {
            ToolContextScopeKind.CurrentItem => $"Item: {subject}",
            ToolContextScopeKind.CurrentTopic => $"Topic: {subject}",
            ToolContextScopeKind.TopicPath => $"Topic path: {subject}",
            ToolContextScopeKind.Niche => $"Niche: {subject}",
            ToolContextScopeKind.Store => $"Store: {subject}",
            ToolContextScopeKind.CurrentSubtree => $"Subtree: {subject}",
            ToolContextScopeKind.Unsupported => "Unsupported scope",
            ToolContextScopeKind.Unavailable => "Unavailable",
            _ => "Tool scope"
        };

        return new ToolContextScopeSummary(scopeKind, label, description, scopeKind is not ToolContextScopeKind.Unsupported and not ToolContextScopeKind.Unavailable);
    }

    private static string PathDescription(IReadOnlyList<ToolContextEntityReference> path) =>
        string.Join(" / ", path.Select(topic => topic.Name));

    private static TopicReference? ResolveTopicReference(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId)
    {
        if (entityKind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == entityId);
            return niche is null ? null : new TopicReference(niche.Id, WorkspaceEntityKind.Niche, niche.StoreId, niche.Name);
        }

        if (entityKind == WorkspaceEntityKind.Group)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == entityId);
            return group is null ? null : new TopicReference(group.Id, WorkspaceEntityKind.Group, group.StoreId, group.Name);
        }

        return null;
    }

    private static NavigationTopicReference ListingTopic(Item listing)
    {
        if (listing.GroupId is Guid groupId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Group, groupId);
        }

        if (listing.NicheId is Guid nicheId)
        {
            return new NavigationTopicReference(WorkspaceEntityKind.Niche, nicheId);
        }

        throw new InvalidOperationException("Listing does not have a valid topic context.");
    }

    private static Niche? ResolveNicheForTopic(WorkspaceSnapshot snapshot, WorkspaceEntityKind topicKind, Guid topicId)
    {
        if (topicKind == WorkspaceEntityKind.Niche)
        {
            return snapshot.Niches.Single(candidate => candidate.Id == topicId);
        }

        var group = snapshot.Groups.Single(candidate => candidate.Id == topicId);
        while (group.NicheId is null)
        {
            group = snapshot.Groups.Single(parent => parent.Id == group.ParentGroupId);
        }

        return snapshot.Niches.Single(candidate => candidate.Id == group.NicheId);
    }

    private static IReadOnlyList<ToolContextEntityReference> ResolveTopicPath(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind topicKind,
        Guid topicId)
    {
        var path = new List<ToolContextEntityReference>();

        if (topicKind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.Single(candidate => candidate.Id == topicId);
            path.Add(Reference(niche, WorkspaceEntityKind.Niche));
            return path;
        }

        var groupsById = snapshot.Groups.ToDictionary(group => group.Id);
        var current = groupsById[topicId];
        while (true)
        {
            path.Add(Reference(current, WorkspaceEntityKind.Group));

            if (current.ParentGroupId is Guid parentGroupId)
            {
                current = groupsById[parentGroupId];
                continue;
            }

            if (current.NicheId is Guid nicheId)
            {
                var niche = snapshot.Niches.Single(candidate => candidate.Id == nicheId);
                path.Add(Reference(niche, WorkspaceEntityKind.Niche));
            }

            break;
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<ToolContextEntityReference> MetadataSources(
        Store store,
        Niche? niche,
        IReadOnlyList<ToolContextEntityReference> topicPath,
        Item? selectedItem)
    {
        yield return Reference(store, WorkspaceEntityKind.Store);

        if (niche is not null)
        {
            yield return Reference(niche, WorkspaceEntityKind.Niche);
        }

        foreach (var topic in topicPath.Where(topic => topic.EntityKind == WorkspaceEntityKind.Group))
        {
            yield return topic;
        }

        if (selectedItem is not null)
        {
            yield return Reference(selectedItem with { Name = ItemDisplayNameFormatter.Format(selectedItem) }, WorkspaceEntityKind.Item);
        }
    }

    private static IReadOnlyList<ToolContextInheritedValue> InheritedMetadata(
        WorkspaceSnapshot snapshot,
        IEnumerable<ToolContextEntityReference> sources) =>
        sources
            .SelectMany(source => MetadataValues(snapshot, source, inherited: true))
            .ToArray();

    private static IReadOnlyList<ToolContextInheritedValue> MetadataValues(WorkspaceEntity entity, bool inherited) =>
        ParseMetadata(entity.MetadataJson)
            .Select(pair => new ToolContextInheritedValue(pair.Key, pair.Value, Reference(entity, EntityKindFor(entity)), inherited))
            .ToArray();

    private static IReadOnlyList<ToolContextInheritedValue> MetadataValues(
        WorkspaceSnapshot snapshot,
        ToolContextEntityReference source,
        bool inherited)
    {
        var metadata = FindMetadataJson(snapshot, source);
        return ParseMetadata(metadata)
            .Select(pair => new ToolContextInheritedValue(pair.Key, pair.Value, source, inherited))
            .ToArray();
    }

    private static string FindMetadataJson(WorkspaceSnapshot snapshot, ToolContextEntityReference source)
    {
        WorkspaceEntity? entity = source.EntityKind switch
        {
            WorkspaceEntityKind.Store => snapshot.Stores.SingleOrDefault(candidate => candidate.Id == source.EntityId),
            WorkspaceEntityKind.Niche => snapshot.Niches.SingleOrDefault(candidate => candidate.Id == source.EntityId),
            WorkspaceEntityKind.Group => snapshot.Groups.SingleOrDefault(candidate => candidate.Id == source.EntityId),
            WorkspaceEntityKind.Item => snapshot.Items.SingleOrDefault(candidate => candidate.Id == source.EntityId),
            _ => null
        };

        return entity?.MetadataJson ?? "{}";
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return [];
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return document.RootElement
            .EnumerateObject()
            .Select(property => new KeyValuePair<string, string>(property.Name, property.Value.ToString()))
            .ToArray();
    }

    private static IReadOnlyList<ToolContextInheritedValue> ExplicitListingTags(WorkspaceSnapshot snapshot, Item listing)
    {
        var tagIds = snapshot.ItemTags
            .Where(link => link.ItemId == listing.Id)
            .Select(link => link.TagId)
            .ToHashSet();

        var source = Reference(listing with { Name = ItemDisplayNameFormatter.Format(listing) }, WorkspaceEntityKind.Item);

        return snapshot.Tags
            .Where(tag => tagIds.Contains(tag.Id))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tag => new ToolContextInheritedValue("tag", tag.Name, source, false))
            .ToArray();
    }

    private static IReadOnlyList<ToolContextNearbyWorkSummary> NearbyForStore(WorkspaceSnapshot snapshot, Guid storeId, int limit) =>
        snapshot.Niches
            .Where(niche => niche.StoreId == storeId)
            .Take(limit)
            .Select(niche => new ToolContextNearbyWorkSummary(WorkspaceEntityKind.Niche, niche.Id, niche.Name, WorkState(niche), true))
            .ToArray();

    private static IReadOnlyList<ToolContextNearbyWorkSummary> NearbyForTopic(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind topicKind,
        Guid topicId,
        int limit,
        Guid? selectedItemId = null)
    {
        var childGroups = snapshot.Groups
            .Where(group => topicKind == WorkspaceEntityKind.Niche
                ? group.NicheId == topicId
                : group.ParentGroupId == topicId)
            .Select(group => new ToolContextNearbyWorkSummary(WorkspaceEntityKind.Group, group.Id, group.Name, WorkState(group), true));

        var childListings = snapshot.Items
            .Where(listing => selectedItemId is null || listing.Id != selectedItemId)
            .Where(listing => topicKind == WorkspaceEntityKind.Niche
                ? listing.NicheId == topicId && listing.GroupId is null
                : listing.GroupId == topicId)
            .Select(listing => new ToolContextNearbyWorkSummary(WorkspaceEntityKind.Item, listing.Id, ItemDisplayNameFormatter.Format(listing), WorkState(listing), true));

        return childGroups
            .Concat(childListings)
            .OrderBy(work => work.State)
            .ThenBy(work => work.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static NearbyWorkState WorkState(WorkspaceEntity entity) =>
        entity.IsArchived || entity is Item { Status: ItemStatus.Rejected }
            ? NearbyWorkState.RejectedOrArchived
            : NearbyWorkState.Active;

    private static ToolContextEntityReference Reference(WorkspaceEntity entity, WorkspaceEntityKind entityKind) =>
        new(entityKind, entity.Id, entity.Name);

    private static WorkspaceEntityKind EntityKindFor(WorkspaceEntity entity) =>
        entity switch
        {
            Store => WorkspaceEntityKind.Store,
            Niche => WorkspaceEntityKind.Niche,
            TopicGroup => WorkspaceEntityKind.Group,
            Item => WorkspaceEntityKind.Item,
            Asset => WorkspaceEntityKind.Asset,
            Prompt => WorkspaceEntityKind.Prompt,
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unsupported workspace entity.")
        };

    private sealed record TopicReference(Guid EntityId, WorkspaceEntityKind EntityKind, Guid StoreId, string Name);

}
