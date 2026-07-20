using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ListingMetadataEditorServiceTests
{
    [Fact]
    public async Task Save_PersistsMarketplaceMetadata()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingMetadataEditorService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.SaveAsync(new ListingMetadataEditRequest(sample.Listing.Id,
            new ListingMarketplaceMetadata("24.99", "USD", "Marketplace notes", "T-Shirt", "Printify-123", "Standard", "dnd, shirt")));

        Assert.True(result.Succeeded);
        Assert.Equal("24.99", result.Metadata!.Price);
        Assert.Equal("T-Shirt", result.Metadata.ProductType);
    }

    [Fact]
    public async Task Save_RejectsNegativePrice()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingMetadataEditorService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.SaveAsync(new ListingMetadataEditRequest(sample.Listing.Id,
            new ListingMarketplaceMetadata("-5.00", null, null, null, null, null, null)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Load_ReturnsExistingMetadata()
    {
        var sample = Sample.Create();
        var metadataJson = """{"listing.price":"19.99","listing.productType":"Hoodie"}""";
        var repository = new TestRepository(sample.Snapshot with
        {
            Listings = [sample.Listing with { MetadataJson = metadataJson }]
        });
        var service = new ListingMetadataEditorService(repository);

        var metadata = await service.LoadAsync(sample.Listing.Id);

        Assert.Equal("19.99", metadata.Price);
        Assert.Equal("Hoodie", metadata.ProductType);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default) { Snapshot = value; return Task.CompletedTask; }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Listing Listing)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
            var snapshot = WorkspaceSnapshot.FromStores([store], [niche], [], [listing], [], [], [], [], []);
            return new(snapshot, now, store, listing);
        }
    }
}
