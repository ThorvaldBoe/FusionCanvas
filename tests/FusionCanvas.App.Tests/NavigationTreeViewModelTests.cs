using FusionCanvas.App.Navigation;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Tests;

public class NavigationTreeViewModelTests
{
    [Fact]
    public void PresentationState_ExpandsCollapsesAndSelectsStableNodeIds()
    {
        var nodeId = Guid.NewGuid();
        var state = new NavigationTreePresentationState();

        state.Expand(nodeId);
        state.Select(nodeId);

        Assert.True(state.IsExpanded(nodeId));
        Assert.Equal(nodeId, state.SelectedNodeId);

        state.Collapse(nodeId);

        Assert.False(state.IsExpanded(nodeId));
        Assert.Equal(nodeId, state.SelectedNodeId);
    }

    [Fact]
    public void RevealPath_ExpandsParentsAndHighlightsLeaf()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var state = new NavigationTreePresentationState();

        state.RevealPath([storeId, nicheId, groupId, itemId]);

        Assert.True(state.IsExpanded(storeId));
        Assert.True(state.IsExpanded(nicheId));
        Assert.True(state.IsExpanded(groupId));
        Assert.False(state.IsExpanded(itemId));
        Assert.Equal(itemId, state.RevealedNodeId);
        Assert.Equal(itemId, state.SelectedNodeId);
    }

    [Fact]
    public void ViewModel_RendersTreeFromApplicationProjection()
    {
        var sample = NavigationSample.Create();
        var viewModel = new NavigationTreeViewModel(sample.Snapshot, new WorkspaceNavigationService());

        viewModel.Expand(sample.Store.Id);
        viewModel.Expand(sample.Niche.Id);

        var store = Assert.Single(viewModel.Stores);
        var niche = Assert.Single(store.Children);
        var group = Assert.Single(niche.Children);

        Assert.Equal(sample.Group.Id, group.EntityId);
    }

    [Fact]
    public void ViewModel_SelectionUpdatesActiveContextThroughApplicationService()
    {
        var sample = NavigationSample.Create();
        var viewModel = new NavigationTreeViewModel(sample.Snapshot, new WorkspaceNavigationService());

        viewModel.Select(new NavigationTarget(NavigationTargetKind.Item, WorkspaceEntityKind.Item, sample.Item.Id));

        Assert.Equal(sample.Item.Id, viewModel.ActiveTarget?.EntityId);
        Assert.Equal(sample.Item.Id, viewModel.PresentationState.SelectedNodeId);
    }

    [Fact]
    public void ViewModel_RevealExpandsParentPath()
    {
        var sample = NavigationSample.Create();
        var viewModel = new NavigationTreeViewModel(sample.Snapshot, new WorkspaceNavigationService());

        viewModel.Reveal(new NavigationTarget(NavigationTargetKind.Item, WorkspaceEntityKind.Item, sample.Item.Id));

        Assert.True(viewModel.PresentationState.IsExpanded(sample.Store.Id));
        Assert.True(viewModel.PresentationState.IsExpanded(sample.Niche.Id));
        Assert.True(viewModel.PresentationState.IsExpanded(sample.Group.Id));
        Assert.Equal(sample.Item.Id, viewModel.PresentationState.RevealedNodeId);
    }

    [Fact]
    public void ViewModel_ExposesMovementEntryPointsWithoutAdvancedFeatures()
    {
        var sample = NavigationSample.Create();
        var otherNiche = NewNiche(sample.Store.Id, "Dogs");
        var snapshot = sample.Snapshot with { Niches = [.. sample.Snapshot.Niches, otherNiche] };
        var viewModel = new NavigationTreeViewModel(snapshot, new WorkspaceNavigationService());

        viewModel.MoveItem(sample.Item.Id, new NavigationTopicReference(WorkspaceEntityKind.Niche, otherNiche.Id));
        viewModel.Reveal(new NavigationTarget(NavigationTargetKind.Item, WorkspaceEntityKind.Item, sample.Item.Id));

        Assert.True(viewModel.PresentationState.IsExpanded(sample.Store.Id));
        Assert.True(viewModel.PresentationState.IsExpanded(otherNiche.Id));
        Assert.Equal(sample.Item.Id, viewModel.PresentationState.RevealedNodeId);
        var appTypeNames = typeof(NavigationTreeViewModel).Assembly.GetTypes().Select(type => type.Name).ToArray();
        Assert.DoesNotContain("SavedView", appTypeNames);
        Assert.DoesNotContain("BatchOperation", appTypeNames);
        Assert.DoesNotContain("MarketplacePublisher", appTypeNames);
        Assert.DoesNotContain("AiProvider", appTypeNames);
        Assert.DoesNotContain("PluginLoader", appTypeNames);
    }

    private static Niche NewNiche(Guid storeId, string name) =>
        new(Guid.NewGuid(), storeId, name, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

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
