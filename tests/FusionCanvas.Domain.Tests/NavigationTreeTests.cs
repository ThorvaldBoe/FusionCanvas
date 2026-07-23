using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class NavigationTreeTests
{
    [Fact]
    public void BuildTree_RepresentsStoreNicheGroupAndItemHierarchy()
    {
        var sample = NavigationSample.Create();

        var tree = WorkspaceNavigation.BuildTree(sample.Snapshot);

        var store = Assert.Single(tree.Stores);
        Assert.Equal(NavigationNodeRole.Store, store.Role);
        var niche = Assert.Single(store.Children);
        Assert.Equal(WorkspaceEntityKind.Niche, niche.EntityKind);
        var group = Assert.Single(niche.Children, child => child.EntityKind == WorkspaceEntityKind.Group);
        Assert.Equal(sample.ParentGroup.Id, group.EntityId);
        var item = Assert.Single(group.Children, child => child.EntityKind == WorkspaceEntityKind.Item);
        Assert.Equal(sample.Item.Id, item.EntityId);
    }

    [Fact]
    public void WorkspaceSnapshot_RepresentsWorkspaceStoreHierarchy()
    {
        var sample = NavigationSample.Create();

        var workspace = Assert.Single(sample.Snapshot.Workspaces);

        Assert.Equal(sample.Store.WorkspaceId, workspace.Id);
        Assert.Equal(workspace.Id, sample.Store.WorkspaceId);
        Assert.Equal(sample.Store.Id, sample.Niche.StoreId);
        Assert.Equal(sample.Store.Id, sample.Item.StoreId);
    }

    [Fact]
    public void BuildTree_PreservesNestedGroupsAtPracticalDepth()
    {
        var sample = NavigationSample.Create();

        var tree = WorkspaceNavigation.BuildTree(sample.Snapshot);

        var path = tree.GetPath(sample.DeepItem.Id);
        Assert.Equal(
            [sample.Store.Id, sample.Niche.Id, sample.ParentGroup.Id, sample.ChildGroup.Id, sample.GrandchildGroup.Id, sample.DeepItem.Id],
            path);
    }

    [Fact]
    public void MoveTopic_PreservesDescendantSubtree()
    {
        var sample = NavigationSample.Create();
        var otherNiche = NewNiche(sample.Store.Id, "Dogs");
        var snapshot = sample.Snapshot with { Niches = [.. sample.Snapshot.Niches, otherNiche] };

        var moved = WorkspaceNavigation.MoveTopic(
            snapshot,
            sample.ChildGroup.Id,
            new NavigationTopicReference(WorkspaceEntityKind.Niche, otherNiche.Id));

        var tree = WorkspaceNavigation.BuildTree(moved);
        var movedPath = tree.GetPath(sample.DeepItem.Id);
        Assert.Equal([sample.Store.Id, otherNiche.Id, sample.ChildGroup.Id, sample.GrandchildGroup.Id, sample.DeepItem.Id], movedPath);
        Assert.Contains(moved.Groups, group => group.Id == sample.GrandchildGroup.Id && group.ParentGroupId == sample.ChildGroup.Id);
    }

    [Fact]
    public void MoveItem_PreservesItemIdentityAndContext()
    {
        var sample = NavigationSample.Create();
        var otherGroup = NewGroup(sample.Store.Id, sample.Niche.Id, null, "Ready");

        var snapshot = sample.Snapshot with { Groups = [.. sample.Snapshot.Groups, otherGroup] };

        var moved = WorkspaceNavigation.MoveItem(
            snapshot,
            sample.Item.Id,
            new NavigationTopicReference(WorkspaceEntityKind.Group, otherGroup.Id));

        var item = Assert.Single(moved.Items, candidate => candidate.Id == sample.Item.Id);
        Assert.Equal(otherGroup.Id, item.GroupId);
        Assert.Equal(sample.Item.Status, item.Status);
        Assert.Equal(sample.Item.Description, item.Description);
        Assert.Equal(sample.Item.MetadataJson, item.MetadataJson);
    }

    [Fact]
    public void MoveTopic_RejectsCycleWithoutChangingHierarchy()
    {
        var sample = NavigationSample.Create();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WorkspaceNavigation.MoveTopic(
                sample.Snapshot,
                sample.ParentGroup.Id,
                new NavigationTopicReference(WorkspaceEntityKind.Group, sample.GrandchildGroup.Id)));

        Assert.Contains("descendants", exception.Message);
        Assert.Equal(sample.ParentGroup, Assert.Single(sample.Snapshot.Groups, group => group.Id == sample.ParentGroup.Id));
    }

    [Fact]
    public void BuildTree_RejectsOrphanedItem()
    {
        var sample = NavigationSample.Create();
        var orphaned = sample.Snapshot with
        {
            Items =
            [
                sample.Item with
                {
                    NicheId = null,
                    GroupId = null
                }
            ]
        };

        Assert.Throws<InvalidOperationException>(() => WorkspaceNavigation.BuildTree(orphaned));
    }

    [Fact]
    public void GroupHierarchy_ResolvesParentsNicheAncestorsDescendantsAndVisibility()
    {
        var sample = NavigationSample.Create();

        Assert.Equal(WorkspaceEntityKind.Group, GroupHierarchy.GetDirectParentKind(sample.ChildGroup));
        Assert.Equal(sample.ParentGroup.Id, GroupHierarchy.GetDirectParentId(sample.ChildGroup));
        Assert.Equal(sample.Niche.Id, GroupHierarchy.GetEffectiveNiche(sample.Snapshot, sample.GrandchildGroup).Id);
        Assert.Equal([sample.ParentGroup.Id, sample.ChildGroup.Id], GroupHierarchy.GetAncestors(sample.Snapshot, sample.GrandchildGroup).Select(group => group.Id));
        Assert.Contains(GroupHierarchy.GetDescendants(sample.Snapshot, sample.ParentGroup), group => group.Id == sample.GrandchildGroup.Id);
        Assert.True(GroupHierarchy.IsEffectivelyActive(sample.Snapshot, sample.GrandchildGroup));

        var archived = sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups.Select(group => group.Id == sample.ParentGroup.Id ? group with { IsArchived = true } : group).ToArray()
        };
        Assert.False(GroupHierarchy.IsEffectivelyActive(archived, archived.Groups.Single(group => group.Id == sample.GrandchildGroup.Id)));
    }

    [Fact]
    public void BuildTree_HidesArchivedSubtreeAndArchivedItems()
    {
        var sample = NavigationSample.Create();
        var archived = sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups.Select(group => group.Id == sample.ParentGroup.Id ? group with { IsArchived = true } : group).ToArray()
        };

        var tree = WorkspaceNavigation.BuildTree(archived);

        var niche = Assert.Single(Assert.Single(tree.Stores).Children);
        Assert.Empty(niche.Children);
    }

    [Fact]
    public void BuildTree_IncludeArchivedRevealsArchivedGroupSubtreeAsInactive()
    {
        var sample = NavigationSample.Create();
        var archived = sample.Snapshot with
        {
            Groups = sample.Snapshot.Groups.Select(group => group.Id == sample.ParentGroup.Id ? group with { IsArchived = true } : group).ToArray()
        };

        var tree = WorkspaceNavigation.BuildTree(archived, includeArchived: true);

        var niche = Assert.Single(Assert.Single(tree.Stores).Children);
        Assert.False(niche.IsInactive);
        var parent = Assert.Single(niche.Children, child => child.EntityKind == WorkspaceEntityKind.Group);
        Assert.True(parent.IsInactive);
        Assert.Equal(sample.ParentGroup.Id, parent.EntityId);
        var child = Assert.Single(parent.Children, node => node.EntityKind == WorkspaceEntityKind.Group);
        Assert.True(child.IsInactive);
        Assert.Equal(sample.ChildGroup.Id, child.EntityId);
        var grandchild = Assert.Single(child.Children, node => node.EntityKind == WorkspaceEntityKind.Group);
        Assert.True(grandchild.IsInactive);
        var deepItem = Assert.Single(grandchild.Children, node => node.EntityKind == WorkspaceEntityKind.Item);
        Assert.True(deepItem.IsInactive);
        Assert.Equal(sample.DeepItem.Id, deepItem.EntityId);
    }

    [Fact]
    public void BuildTree_IncludeArchivedRevealsArchivedItemUnderActiveParentAsInactive()
    {
        var sample = NavigationSample.Create();
        var archived = sample.Snapshot with
        {
            Items = sample.Snapshot.Items.Select(item => item.Id == sample.Item.Id ? item with { IsArchived = true } : item).ToArray()
        };

        var defaultTree = WorkspaceNavigation.BuildTree(archived);
        var parentDefault = Assert.Single(Assert.Single(Assert.Single(defaultTree.Stores).Children).Children, node => node.EntityKind == WorkspaceEntityKind.Group);
        Assert.DoesNotContain(parentDefault.Children, node => node.EntityId == sample.Item.Id);

        var includedTree = WorkspaceNavigation.BuildTree(archived, includeArchived: true);
        var niche = Assert.Single(Assert.Single(includedTree.Stores).Children);
        var parent = Assert.Single(niche.Children, node => node.EntityKind == WorkspaceEntityKind.Group);
        Assert.False(parent.IsInactive);
        var archivedItem = Assert.Single(parent.Children, node => node.EntityId == sample.Item.Id);
        Assert.True(archivedItem.IsInactive);
    }

    [Fact]
    public void BuildTree_DefaultLeavesActiveNodesMarkedActive()
    {
        var sample = NavigationSample.Create();

        var tree = WorkspaceNavigation.BuildTree(sample.Snapshot);

        foreach (var node in tree.Flatten())
        {
            Assert.False(node.IsInactive);
        }
    }

    [Fact]
    public void MoveTopic_RejectsArchivedDestinationWithoutChangingHierarchy()
    {
        var sample = NavigationSample.Create();
        var destination = NewGroup(sample.Store.Id, sample.Niche.Id, null, "Archived") with { IsArchived = true };
        var snapshot = sample.Snapshot with { Groups = [.. sample.Snapshot.Groups, destination] };

        Assert.Throws<InvalidOperationException>(() => WorkspaceNavigation.MoveTopic(
            snapshot,
            sample.ChildGroup.Id,
            new NavigationTopicReference(WorkspaceEntityKind.Group, destination.Id)));
        Assert.Equal(sample.ParentGroup.Id, snapshot.Groups.Single(group => group.Id == sample.ChildGroup.Id).ParentGroupId);
    }

    private static Niche NewNiche(Guid storeId, string name) =>
        new(Guid.NewGuid(), storeId, name, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

    private static TopicGroup NewGroup(Guid storeId, Guid? nicheId, Guid? parentGroupId, string name) =>
        new(Guid.NewGuid(), storeId, nicheId, parentGroupId, name, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

    private sealed record NavigationSample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        TopicGroup ParentGroup,
        TopicGroup ChildGroup,
        TopicGroup GrandchildGroup,
        Item Item,
        Item DeepItem)
    {
        public static NavigationSample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
            var parentGroup = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Seasonal", null, false, now, now, "{}");
            var childGroup = new TopicGroup(Guid.NewGuid(), store.Id, null, parentGroup.Id, "Autumn", null, false, now, now, "{}");
            var grandchildGroup = new TopicGroup(Guid.NewGuid(), store.Id, null, childGroup.Id, "Espresso", null, false, now, now, "{}");
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, parentGroup.Id, "Pumpkin espresso", "Cozy shirt", ItemStatus.Draft, WorkflowStage.Idea, false, now, now, """{"tags":["fall"]}""");
            var deepItem = new Item(Guid.NewGuid(), store.Id, niche.Id, grandchildGroup.Id, "Latte ghosts", "Halloween shirt", ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");

            return new NavigationSample(
                new WorkspaceSnapshot(
                    [store],
                    [niche],
                    [parentGroup, childGroup, grandchildGroup],
                    [item, deepItem],
                    [],
                    [],
                    [],
                    [],
                    []),
                store,
                niche,
                parentGroup,
                childGroup,
                grandchildGroup,
                item,
                deepItem);
        }
    }
}
