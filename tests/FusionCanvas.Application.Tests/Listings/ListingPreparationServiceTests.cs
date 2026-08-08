using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Listings;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ListingPreparationServiceTests
{
    [Fact]
    public async Task Update_PersistsProviderNeutralProfileAndKeepsProviderStateAcrossStrategyChange()
    {
        var sample = CreateSample();
        var provider = new ListingProviderState(sample.Item.Id, "Shopify", "Online Store", "42");
        var repository = new InMemoryRepository(sample.Snapshot with { ListingProviderStates = [provider] });
        var service = new ListingPreparationService(repository);

        var result = await service.UpdateAsync(new(
            sample.Item.Id,
            ListingFulfillmentStrategy.Manual,
            19.99m,
            "eur",
            ListingReadinessState.Ready,
            ListingPublicationState.NotPublished));

        Assert.True(result.Succeeded);
        Assert.Equal(ListingFulfillmentStrategy.Manual, result.State!.Profile.Strategy);
        Assert.Equal("EUR", result.State.Profile.Currency);
        Assert.Single(result.State.Providers);
        Assert.Equal("42", result.State.Providers[0].ExternalId);
    }

    [Fact]
    public async Task Update_RejectsPublishedPrintifyListingWithoutProviderIdentity()
    {
        var sample = CreateSample();
        var service = new ListingPreparationService(new InMemoryRepository(sample.Snapshot));

        var result = await service.UpdateAsync(new(
            sample.Item.Id,
            ListingFulfillmentStrategy.ShopifyPrintify,
            10,
            "USD",
            ListingReadinessState.Ready,
            ListingPublicationState.Published));

        Assert.False(result.Succeeded);
        Assert.Contains("provider identity", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BindShopify_TransitionsExistingProfileWithoutCreatingAnotherRecord()
    {
        var sample = CreateSample();
        var repository = new InMemoryRepository(sample.Snapshot);
        var service = new ListingPreparationService(repository);

        var result = await service.BindShopifyAsync(new(sample.Item.Id, "42", "Online Store"));

        Assert.True(result.Succeeded);
        Assert.Equal(ListingFulfillmentStrategy.ShopifyManual, result.State!.Profile.Strategy);
        Assert.Single(repository.Snapshot.ItemListingProfiles);
        Assert.Single(repository.Snapshot.ListingProviderStates);
        Assert.Equal("42", repository.Snapshot.ListingProviderStates[0].ExternalId);
    }

    private static Sample CreateSample()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Item", "Description", ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        return new(new WorkspaceSnapshot([store], [niche], [], [item], [], [], [], [], []), item);
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Item Item);

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
