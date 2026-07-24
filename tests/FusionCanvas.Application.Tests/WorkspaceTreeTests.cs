using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Tests;

public class WorkspaceTreeTests
{
    [Fact]
    public void Project_FreeTextMatchIncludesAncestorPathAndDisablesRelativeReorder()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Campaign", null, false, now, now, "{}", 0);
        var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Espresso", null, false, now, now, "{}", 0);
        var snapshot = new WorkspaceSnapshot([store], [niche], [root, child], [], [], [], [], [], []);

        var projection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("espresso"));

        var nicheNode = Assert.Single(projection.Roots);
        var rootNode = Assert.Single(nicheNode.Children);
        var childNode = Assert.Single(rootNode.Children);
        Assert.False(nicheNode.IsDirectMatch);
        Assert.False(rootNode.IsDirectMatch);
        Assert.True(childNode.IsDirectMatch);
        Assert.False(projection.CanReorderBetweenSiblings);
        Assert.Contains(child.Id, projection.VisibleEntityIds);
    }

    [Fact]
    public void SelectionCoordinator_ChangesCanonicalSelectionWithoutDocumentState()
    {
        var coordinator = new WorkspaceTreeSelectionCoordinator();
        WorkspaceTreeSelection? observed = null;
        coordinator.SelectionChanged += (_, selection) => observed = selection;
        var selection = new WorkspaceTreeSelection(WorkspaceEntityKind.Group, Guid.NewGuid());

        coordinator.Select(selection);

        Assert.Equal(selection, coordinator.Selected);
        Assert.Equal(selection, observed);
    }

    [Fact]
    public void Project_RepresentativeDeepAndWideTreePreservesStablePaths()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var groups = new List<TopicGroup>();
        Guid? parentId = null;
        for (var depth = 0; depth < 25; depth++)
        {
            var group = new TopicGroup(
                Guid.NewGuid(), store.Id, parentId is null ? niche.Id : null, parentId,
                $"Depth {depth}", null, false, now, now, "{}", depth);
            groups.Add(group);
            parentId = group.Id;
        }

        for (var index = 0; index < 200; index++)
        {
            groups.Add(new TopicGroup(
                Guid.NewGuid(), store.Id, niche.Id, null,
                $"Wide {index:D3}", null, false, now, now, "{}", index + 25));
        }

        var snapshot = new WorkspaceSnapshot([store], [niche], groups, [], [], [], [], [], []);
        var complete = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery());
        var filtered = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("Depth 24"));

        Assert.Equal(225, complete.VisibleEntityIds.Count(id => id != niche.Id));
        Assert.Equal(25, Descendants(Assert.Single(filtered.Roots)).Count(node => node.EntityKind == WorkspaceEntityKind.Group));
        Assert.Equal(parentId, Descendants(Assert.Single(filtered.Roots)).Last().EntityId);
    }

    private static IEnumerable<WorkspaceTreeProjectionNode> Descendants(WorkspaceTreeProjectionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Descendants))
        {
            yield return child;
        }
    }

    [Fact]
    public void Project_TextMatchesListingDescriptionAndNotes()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var byDescription = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Title A", "Cozy autumn mug", ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var byNotes = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Title B", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, """{"notes":"remember to add a scarf"}""");
        var noMatch = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Title C", "Plain text", ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [byDescription, byNotes, noMatch], [], [], [], [], []);

        var byDesc = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("autumn"));
        Assert.Single(byDesc.Roots.Single().Children, node => node.EntityId == byDescription.Id);

        var byNotesResult = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("scarf"));
        Assert.Single(byNotesResult.Roots.Single().Children, node => node.EntityId == byNotes.Id);
    }

    [Fact]
    public void Project_TextMatchesListingByAttachedTagName()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Untitled", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Halloween", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot(
            [store], [niche], [], [listing], [], [], [tag], [new ItemTag(listing.Id, tag.Id)], []);

        var projection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("hallo"));

        Assert.Single(projection.Roots.Single().Children, node => node.EntityId == listing.Id);
    }

    [Fact]
    public void Project_TagFilterRequiresAllSelectedTagsAndKeepsAncestorContext()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Campaign", null, false, now, now, "{}");
        var oneTag = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "One", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var bothTags = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Both", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var fallTag = new Tag(Guid.NewGuid(), store.Id, "Fall", null, false, now, now, "{}");
        var cozyTag = new Tag(Guid.NewGuid(), store.Id, "Cozy", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot(
            [store], [niche], [group], [oneTag, bothTags], [], [], [fallTag, cozyTag],
            [new ItemTag(oneTag.Id, fallTag.Id), new ItemTag(bothTags.Id, fallTag.Id), new ItemTag(bothTags.Id, cozyTag.Id)],
            []);

        var fallOnly = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery(TagIds: new HashSet<Guid> { fallTag.Id }));
        Assert.Equal(2, fallOnly.Roots.Single().Children.Single().Children.Count);

        var both = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery(TagIds: new HashSet<Guid> { fallTag.Id, cozyTag.Id }));
        var matchedListing = Assert.Single(both.Roots.Single().Children.Single().Children, node => node.EntityKind == WorkspaceEntityKind.Item);
        Assert.Equal(bothTags.Id, matchedListing.EntityId);
    }

    [Fact]
    public void Project_SubtreeScopeRestrictsResultsToScopedTopicSubtree()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var outerGroup = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Outer", null, false, now, now, "{}");
        var innerGroup = new TopicGroup(Guid.NewGuid(), store.Id, null, outerGroup.Id, "Inner", null, false, now, now, "{}");
        var outerListing = new Item(Guid.NewGuid(), store.Id, niche.Id, outerGroup.Id, "Outer listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var innerListing = new Item(Guid.NewGuid(), store.Id, niche.Id, innerGroup.Id, "Inner listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var outsideListing = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Outside listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [outerGroup, innerGroup], [outerListing, innerListing, outsideListing], [], [], [], [], []);

        var scoped = WorkspaceTreeProjector.Project(
            snapshot,
            store.Id,
            new WorkspaceTreeQuery(ScopeTopic: new NavigationTopicReference(WorkspaceEntityKind.Group, outerGroup.Id)));

        var root = Assert.Single(scoped.Roots);
        Assert.Equal(outerGroup.Id, root.EntityId);
        Assert.All(FlattenProjection(root), node => Assert.NotEqual(outsideListing.Id, node.EntityId));
        Assert.Contains(root.Children, node => node.EntityId == outerListing.Id);
        var innerNode = Assert.Single(root.Children, node => node.EntityId == innerGroup.Id);
        Assert.Contains(innerNode.Children, node => node.EntityId == innerListing.Id);
        Assert.DoesNotContain(scoped.VisibleEntityIds, id => id == niche.Id);
    }

    private static IEnumerable<WorkspaceTreeProjectionNode> FlattenProjection(WorkspaceTreeProjectionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(FlattenProjection))
        {
            yield return child;
        }
    }

    [Fact]
    public void Project_ScopeWithTextCombinesAcrossDimensions()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Campaign", null, false, now, now, "{}");
        var matching = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var other = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Pumpkin elsewhere", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [group], [matching, other], [], [], [], [], []);

        var projection = WorkspaceTreeProjector.Project(
            snapshot,
            store.Id,
            new WorkspaceTreeQuery(Text: "pumpkin", ScopeTopic: new NavigationTopicReference(WorkspaceEntityKind.Group, group.Id)));

        var root = Assert.Single(projection.Roots);
        Assert.Equal(group.Id, root.EntityId);
        var matched = Assert.Single(root.Children, node => node.EntityKind == WorkspaceEntityKind.Item);
        Assert.Equal(matching.Id, matched.EntityId);
    }

    [Fact]
    public void Project_IncludeArchivedRevealsArchivedListingInactive()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var archived = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Ghost listing", null, ItemStatus.Draft, WorkflowStage.Idea, true, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [archived], [], [], [], [], []);

        var defaultProjection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("ghost"));
        Assert.Empty(defaultProjection.Roots);

        var included = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery(Text: "ghost", IncludeArchived: true));
        var listingNode = Assert.Single(included.Roots.Single().Children);
        Assert.Equal(archived.Id, listingNode.EntityId);
        Assert.True(listingNode.IsInactive);
    }

    [Fact]
    public void Project_ActiveQueryWithNoMatchesProducesEmptyRoots()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []);

        var projection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("nothing matches"));

        Assert.Empty(projection.Roots);
        Assert.True(projection.Query.IsActive);
        Assert.False(projection.CanReorderBetweenSiblings);
    }

    [Fact]
    public void Project_BlankTextDoesNotRestrictAndStaysInactive()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Title", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []);

        var projection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("   "));

        Assert.False(projection.Query.IsActive);
        Assert.True(projection.CanReorderBetweenSiblings);
        Assert.Single(projection.Roots);
    }
}
