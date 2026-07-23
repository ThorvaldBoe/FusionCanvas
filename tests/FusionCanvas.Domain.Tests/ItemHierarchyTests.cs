using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class ItemHierarchyTests
{
    [Fact]
    public void ResolvesNestedGroupTopicAndEffectiveNiche()
    {
        var sample = Sample.Create();

        Assert.Equal(WorkspaceEntityKind.Group, ItemHierarchy.GetTopic(sample.Item).EntityKind);
        Assert.Equal(sample.Child.Id, ItemHierarchy.GetTopic(sample.Item).EntityId);
        Assert.Equal(sample.Niche.Id, ItemHierarchy.GetEffectiveNiche(sample.Snapshot, sample.Item).Id);
        Assert.True(ItemHierarchy.IsEffectivelyActive(sample.Snapshot, sample.Item));
    }

    [Fact]
    public void ArchivedItemOrAncestorIsEffectivelyInactive()
    {
        var sample = Sample.Create();

        Assert.False(ItemHierarchy.IsEffectivelyActive(sample.Snapshot, sample.Item with { IsArchived = true }));
        Assert.False(ItemHierarchy.IsEffectivelyActive(sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        }, sample.Item));
        Assert.False(ItemHierarchy.IsEffectivelyActive(sample.Snapshot with
        {
            Niches = [sample.Niche with { IsArchived = true }]
        }, sample.Item));
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Niche Niche, TopicGroup Root, TopicGroup Child, Item Item)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            return new(new WorkspaceSnapshot([store], [niche], [root, child], [item], [], [], [], [], []), niche, root, child, item);
        }
    }
}
