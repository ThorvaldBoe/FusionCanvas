using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public sealed class WorkspaceContextResolverTests
{
    [Fact]
    public void ResolveStoreId_UsesEntityOwnershipAcrossWorkspaceKinds()
    {
        var sample = Sample.Create();

        Assert.Equal(sample.Store.Id, WorkspaceContextResolver.ResolveStoreId(
            sample.Snapshot,
            new WorkspaceTreeSelection(WorkspaceEntityKind.Store, sample.Store.Id)));
        Assert.Equal(sample.Store.Id, WorkspaceContextResolver.ResolveStoreId(
            sample.Snapshot,
            new WorkspaceTreeSelection(WorkspaceEntityKind.Niche, sample.Niche.Id)));
        Assert.Equal(sample.Store.Id, WorkspaceContextResolver.ResolveStoreId(
            sample.Snapshot,
            new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Child.Id)));
        Assert.Equal(sample.Store.Id, WorkspaceContextResolver.ResolveStoreId(
            sample.Snapshot,
            new WorkspaceTreeSelection(WorkspaceEntityKind.Item, sample.Item.Id)));
    }

    [Fact]
    public void ResolveEffectiveNicheId_FollowsGroupAncestors()
    {
        var sample = Sample.Create();

        Assert.Equal(sample.Niche.Id, WorkspaceContextResolver.ResolveEffectiveNicheId(sample.Snapshot, sample.Child.Id));
    }

    private sealed record Sample(Store Store, Niche Niche, TopicGroup Child, Item Item, WorkspaceSnapshot Snapshot)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            return new Sample(store, niche, child, item, new WorkspaceSnapshot([store], [niche], [root, child], [item], [], [], [], [], []));
        }
    }
}
