using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ManualListingDetailsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAsync_PersistsSeparateCopyAndVariantTerms()
    {
        var (snapshot, item, source, _, variant) = CreateWorkspace();
        var repository = new InMemoryRepository(snapshot);
        var service = new ManualListingDetailsService(repository);

        await service.SaveAsync(item.Id, "Public title", "Public body", "USD", "Standard", 5m, 3m, "3-5 days",
            [new ItemVariantListingTerms(item.Id, variant.Id, 24m, 8m)], TestContext.Current.CancellationToken);

        var loaded = await service.LoadAsync(item.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Working title", item.Name);
        Assert.Equal(source.Id, loaded!.OfferingId);
        Assert.Equal("Public title", loaded.Details!.Title);
        Assert.Equal(24m, Assert.Single(loaded.VariantTerms).SellingPrice);
        Assert.Equal(5m, loaded.Details.CustomerShippingCharge);
    }

    [Fact]
    public async Task ConfirmMigration_ArchivesSourceAndResetsSetupSpecificShipping()
    {
        var (snapshot, item, source, destination, variant) = CreateWorkspace(withSavedDetails: true);
        var repository = new InMemoryRepository(snapshot);
        var service = new ManualListingDetailsService(repository, () => Now.AddMinutes(1), Guid.NewGuid);
        var preview = service.PreviewMigration(snapshot, item.Id, destination.Id);

        var state = await service.ConfirmMigrationAsync(preview, TestContext.Current.CancellationToken);

        Assert.Equal(destination.Id, state.OfferingId);
        Assert.Null(state.Details!.ShippingOptionName);
        Assert.Null(state.Details.CustomerShippingCharge);
        Assert.Empty(state.VariantTerms); // Legacy Variants have no semantic option signatures.
        var archived = Assert.Single(state.History);
        Assert.Equal(source.Id, archived.OfferingId);
        Assert.Equal("Previous title", archived.Title);
        Assert.Equal(5m, archived.CustomerShippingCharge);
    }

    [Fact]
    public async Task ConfirmMigration_PersistenceFailureLeavesSourceSetupActive()
    {
        var (snapshot, item, source, destination, _) = CreateWorkspace(withSavedDetails: true);
        var repository = new InMemoryRepository(snapshot, failSave: true);
        var service = new ManualListingDetailsService(repository);
        var preview = service.PreviewMigration(snapshot, item.Id, destination.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmMigrationAsync(preview, TestContext.Current.CancellationToken));

        Assert.Equal(source.Id, repository.Snapshot.ItemListingConfigurations.Single().OfferingId);
        Assert.Equal("Previous title", repository.Snapshot.ItemListingDetails.Single().Title);
        Assert.Empty(repository.Snapshot.ItemListingSetupHistory);
    }

    private static (WorkspaceSnapshot Snapshot, Item Item, FulfillmentOffering Source, FulfillmentOffering Destination, ProductVariant Variant) CreateWorkspace(bool withSavedDetails = false)
    {
        var store = new Store(Guid.NewGuid(), "Studio", null, false, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Working title", "Working description", ItemStatus.Draft, WorkflowStage.Listing, false, Now, Now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Tee", null, null, Now, Now, "{}");
        var source = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Source setup", null, FulfillmentKind.FixedProvider, "manual", null, Now, Now, "{}");
        var destination = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Destination setup", null, FulfillmentKind.FixedProvider, "manual", null, Now, Now, "{}");
        var variant = new ProductVariant(Guid.NewGuid(), source.Id, [new VariantOption("Size", "M")], Now, Now);
        var destinationVariant = new ProductVariant(Guid.NewGuid(), destination.Id, [new VariantOption("Size", "M")], Now, Now);
        var snapshot = new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product], FulfillmentOfferings = [source, destination], ProductVariants = [variant, destinationVariant],
            ItemListingConfigurations = [new ItemListingConfiguration(item.Id, source.Id)],
            ItemListingDetails = withSavedDetails ? [new ItemListingDetails(item.Id, source.Id, "Previous title", "Previous body", "USD", "Tracked", 5m, 3m, "5 days")] : [],
            ItemVariantListingTerms = withSavedDetails ? [new ItemVariantListingTerms(item.Id, variant.Id, 20m, 8m)] : []
        };
        return (snapshot, item, source, destination, variant);
    }

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot, bool failSave = false) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (failSave) throw new InvalidOperationException("Simulated persistence failure.");
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
