using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class StoreManagementViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_ShowsFirstStoreEmptyState()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));

        await viewModel.LoadAsync();

        Assert.True(viewModel.NeedsFirstStore);
        Assert.False(viewModel.HasActiveStores);
        Assert.Empty(viewModel.ActiveStores);
    }

    [Fact]
    public async Task CreateStoreAsync_SelectsNewStoreAndPopulatesEditFields()
    {
        var storeId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(),
            () => Now,
            () => storeId))
        {
            NewStoreName = "North Star Studio",
            Description = "POD brand",
            Notes = "Soft humor",
            TargetMarket = "Coffee fans",
            BrandDirection = "Warm vintage",
            PlanningContext = "Fall launch"
        };

        await viewModel.CreateStoreAsync();

        Assert.False(viewModel.NeedsFirstStore);
        Assert.Equal(storeId, viewModel.SelectedStore?.Id);
        Assert.Equal("North Star Studio", viewModel.NewStoreName);
        Assert.Equal("Soft humor", viewModel.Notes);
        Assert.True(viewModel.HasSelectedStore);
    }

    [Fact]
    public async Task SaveSelectedStoreAsync_RenamesAndUpdatesContext()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0]);
        viewModel.NewStoreName = "North Star Gifts";
        viewModel.Notes = "Sharper positioning";

        await viewModel.SaveSelectedStoreAsync();

        Assert.Equal("North Star Gifts", viewModel.SelectedStore?.Name);
        Assert.Equal("Sharper positioning", viewModel.SelectedStore?.Context.Notes);
    }

    [Fact]
    public async Task ArchiveAndRestoreFlows_ShowStoresSeparately()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0]);

        await viewModel.ArchiveSelectedStoreAsync();
        var archived = Assert.Single(viewModel.ArchivedStores);
        await viewModel.RestoreStoreAsync(archived);

        Assert.Empty(viewModel.ArchivedStores);
        Assert.Equal(store.Id, Assert.Single(viewModel.ActiveStores).Id);
    }

    [Fact]
    public async Task MainWindowViewModel_UsesSelectedStoreToFilterNavigationContexts()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var firstNiche = new Niche(Guid.NewGuid(), first.Id, "Coffee", null, false, Now, Now, "{}");
        var secondNiche = new Niche(Guid.NewGuid(), second.Id, "Cats", null, false, Now, Now, "{}");
        var firstListing = new Listing(Guid.NewGuid(), first.Id, firstNiche.Id, null, "Espresso", null, ListingStatus.Draft, false, Now, Now, "{}");
        var secondListing = new Listing(Guid.NewGuid(), second.Id, secondNiche.Id, null, "Whiskers", null, ListingStatus.Draft, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([first, second], [firstNiche, secondNiche], [], [firstListing, secondListing], [], [], [], [], []);
        var storeService = new StoreManagementService(new InMemoryWorkspaceRepository(snapshot));

        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            storeService,
            snapshot);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == firstListing.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == secondListing.Id);

        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(store => store.Id == second.Id));

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == secondListing.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == firstListing.Id);
    }

    private static Store NewStore(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
