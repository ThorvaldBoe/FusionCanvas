using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class ListingHierarchyTests
{
    [Fact]
    public void ResolvesNestedGroupTopicAndEffectiveNiche()
    {
        var sample = Sample.Create();

        Assert.Equal(WorkspaceEntityKind.Group, ListingHierarchy.GetTopic(sample.Listing).EntityKind);
        Assert.Equal(sample.Child.Id, ListingHierarchy.GetTopic(sample.Listing).EntityId);
        Assert.Equal(sample.Niche.Id, ListingHierarchy.GetEffectiveNiche(sample.Snapshot, sample.Listing).Id);
        Assert.True(ListingHierarchy.IsEffectivelyActive(sample.Snapshot, sample.Listing));
    }

    [Fact]
    public void ArchivedListingOrAncestorIsEffectivelyInactive()
    {
        var sample = Sample.Create();

        Assert.False(ListingHierarchy.IsEffectivelyActive(sample.Snapshot, sample.Listing with { IsArchived = true }));
        Assert.False(ListingHierarchy.IsEffectivelyActive(sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        }, sample.Listing));
        Assert.False(ListingHierarchy.IsEffectivelyActive(sample.Snapshot with
        {
            Niches = [sample.Niche with { IsArchived = true }]
        }, sample.Listing));
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Niche Niche, TopicGroup Root, TopicGroup Child, Listing Listing)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", null, ListingStatus.Draft, false, now, now, "{}");
            return new(new WorkspaceSnapshot([store], [niche], [root, child], [listing], [], [], [], [], []), niche, root, child, listing);
        }
    }
}
