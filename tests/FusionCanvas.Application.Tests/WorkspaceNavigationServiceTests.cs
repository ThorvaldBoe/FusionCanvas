using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Tests;

public class WorkspaceNavigationServiceTests
{
    [Fact]
    public void LoadTree_ExposesWorkspaceNavigationProjection()
    {
        var sample = NavigationSample.Create();
        IWorkspaceNavigationService service = new WorkspaceNavigationService();

        var tree = service.LoadTree(sample.Snapshot);

        Assert.Equal(sample.Store.Id, Assert.Single(tree.Stores).EntityId);
    }

    [Fact]
    public void Select_TracksActiveNavigationTarget()
    {
        var sample = NavigationSample.Create();
        IWorkspaceNavigationService service = new WorkspaceNavigationService();
        var target = new NavigationTarget(NavigationTargetKind.Topic, WorkspaceEntityKind.Group, sample.Group.Id);

        var selected = service.Select(sample.Snapshot, target);

        Assert.Equal(target, selected);
    }

    [Fact]
    public void ResolveCreationScope_UsesTopicSelection()
    {
        var sample = NavigationSample.Create();
        IWorkspaceNavigationService service = new WorkspaceNavigationService();

        var scope = service.ResolveCreationScope(
            sample.Snapshot,
            new NavigationTarget(NavigationTargetKind.Topic, WorkspaceEntityKind.Group, sample.Group.Id));

        Assert.Equal(sample.Store.Id, scope.StoreId);
        Assert.Equal(sample.Group.Id, scope.TopicId);
        Assert.Equal(WorkspaceEntityKind.Group, scope.TopicKind);
    }

    [Fact]
    public void ResolveCreationScope_UsesListingParentTopic()
    {
        var sample = NavigationSample.Create();
        IWorkspaceNavigationService service = new WorkspaceNavigationService();

        var scope = service.ResolveCreationScope(
            sample.Snapshot,
            new NavigationTarget(NavigationTargetKind.Item, WorkspaceEntityKind.Item, sample.Item.Id));

        Assert.Equal(sample.Store.Id, scope.StoreId);
        Assert.Equal(sample.Group.Id, scope.TopicId);
        Assert.Equal(WorkspaceEntityKind.Group, scope.TopicKind);
    }

    [Fact]
    public void MoveOperations_AreExposedThroughApplicationBoundary()
    {
        var sample = NavigationSample.Create();
        var secondGroup = NewGroup(sample.Store.Id, sample.Niche.Id, null, "Ready");
        var snapshot = sample.Snapshot with { Groups = [.. sample.Snapshot.Groups, secondGroup] };
        IWorkspaceNavigationService service = new WorkspaceNavigationService();

        var moved = service.MoveItem(
            snapshot,
            sample.Item.Id,
            new NavigationTopicReference(WorkspaceEntityKind.Group, secondGroup.Id));

        Assert.Equal(secondGroup.Id, Assert.Single(moved.Items).GroupId);
    }

    [Fact]
    public void RevealPath_ReturnsParentPathForCollapsedUiState()
    {
        var sample = NavigationSample.Create();
        IWorkspaceNavigationService service = new WorkspaceNavigationService();

        var path = service.RevealPath(
            sample.Snapshot,
            new NavigationTarget(NavigationTargetKind.Item, WorkspaceEntityKind.Item, sample.Item.Id));

        Assert.Equal([sample.Store.Id, sample.Niche.Id, sample.Group.Id, sample.Item.Id], path);
    }

    [Fact]
    public void NavigationContracts_DoNotReferenceInfrastructureOrUiFrameworkTypes()
    {
        var applicationAssembly = typeof(WorkspaceNavigationService).Assembly;

        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();

        Assert.DoesNotContain("Avalonia", referencedAssemblies);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name is not null && name.Contains("Marketplace", StringComparison.OrdinalIgnoreCase));
    }

    private static TopicGroup NewGroup(Guid storeId, Guid? nicheId, Guid? parentGroupId, string name) =>
        new(Guid.NewGuid(), storeId, nicheId, parentGroupId, name, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

    private sealed record NavigationSample(WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup Group, Item Item)
    {
        public static NavigationSample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Seasonal", null, false, now, now, "{}");
            var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

            return new NavigationSample(
                new WorkspaceSnapshot([store], [niche], [group], [listing], [], [], [], [], []),
                store,
                niche,
                group,
                listing);
        }
    }
}
