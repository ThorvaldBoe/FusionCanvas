using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public sealed class WorkspaceTreeSelectionScopeTests
{
    [Fact]
    public void GetSelectableEntityIds_ExcludesArchivedAndInactiveGroups()
    {
        var sample = Sample.Create();

        var ids = WorkspaceTreeSelectionScope.GetSelectableEntityIds(sample.Snapshot, sample.Store.Id);

        Assert.Contains(sample.ActiveGroup.Id, ids);
        Assert.Contains(sample.Item.Id, ids);
        Assert.DoesNotContain(sample.ArchivedGroup.Id, ids);
        Assert.DoesNotContain(sample.InactiveGroup.Id, ids);
    }

    [Fact]
    public void ResolveTopLevelNicheId_ResolvesNestedGroupAncestry()
    {
        var sample = Sample.Create();

        Assert.Equal(sample.Niche.Id,
            WorkspaceTreeSelectionScope.ResolveTopLevelNicheId(sample.Snapshot, sample.ChildGroup.Id));
    }

    private sealed record Sample(
        Store Store,
        Niche Niche,
        TopicGroup ActiveGroup,
        TopicGroup ChildGroup,
        TopicGroup ArchivedGroup,
        TopicGroup InactiveGroup,
        Item Item,
        WorkspaceSnapshot Snapshot)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var activeGroup = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Active", null, false, now, now, "{}", 0);
            var childGroup = new TopicGroup(Guid.NewGuid(), store.Id, null, activeGroup.Id, "Child", null, false, now, now, "{}", 0);
            var archivedGroup = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Archived", null, true, now, now, "{}", 1);
            var inactiveGroup = new TopicGroup(Guid.NewGuid(), store.Id, null, archivedGroup.Id, "Inactive", null, false, now, now, "{}", 0);
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, activeGroup.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store],
                [niche],
                [activeGroup, childGroup, archivedGroup, inactiveGroup],
                [item],
                [], [], [], [], []);
            return new Sample(store, niche, activeGroup, childGroup, archivedGroup, inactiveGroup, item, snapshot);
        }
    }
}
