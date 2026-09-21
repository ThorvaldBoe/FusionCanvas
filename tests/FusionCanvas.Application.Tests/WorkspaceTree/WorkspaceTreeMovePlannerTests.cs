using FusionCanvas.Application.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public sealed class WorkspaceTreeMovePlannerTests
{
    [Fact]
    public void BuildGroupDestinations_ExcludesSelectedGroupSubtreeAndKeepsPaths()
    {
        var sample = Sample.Create();
        var destinations = WorkspaceTreeMovePlanner.BuildGroupDestinations(
            sample.Snapshot,
            sample.Store.Id,
            [new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Root.Id)]);

        Assert.Contains(destinations, destination => destination.Parent == new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id));
        Assert.Contains(destinations, destination => destination.Parent == new GroupParentReference(WorkspaceEntityKind.Group, sample.Sibling.Id));
        Assert.DoesNotContain(destinations, destination => destination.Parent == new GroupParentReference(WorkspaceEntityKind.Group, sample.Root.Id));
        Assert.DoesNotContain(destinations, destination => destination.Parent == new GroupParentReference(WorkspaceEntityKind.Group, sample.Child.Id));
        Assert.Contains(destinations, destination => destination.DisplayPath == "Niche / Sibling");
    }

    [Fact]
    public void ResolveDefaultGroupDestination_UsesSingleSharedParent()
    {
        var sample = Sample.Create();
        var destinations = WorkspaceTreeMovePlanner.BuildGroupDestinations(sample.Snapshot, sample.Store.Id, []);

        var destination = WorkspaceTreeMovePlanner.ResolveDefaultGroupDestination(
            sample.Snapshot,
            [new WorkspaceTreeSelection(WorkspaceEntityKind.Item, sample.Item.Id)],
            destinations);

        Assert.Equal(new GroupParentReference(WorkspaceEntityKind.Group, sample.Root.Id), destination?.Parent);
    }

    private sealed record Sample(
        Store Store,
        Niche Niche,
        TopicGroup Root,
        TopicGroup Child,
        TopicGroup Sibling,
        Item Item,
        WorkspaceSnapshot Snapshot)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}", 0);
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}", 0);
            var sibling = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Sibling", null, false, now, now, "{}", 1);
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, root.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot([store], [niche], [root, child, sibling], [item], [], [], [], [], []);
            return new Sample(store, niche, root, child, sibling, item, snapshot);
        }
    }
}
