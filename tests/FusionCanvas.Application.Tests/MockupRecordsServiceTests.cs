using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class MockupRecordsServiceTests
{
    [Fact]
    public async Task Create_AddsManualMockupWithSourceFlag()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var mockupId = Guid.NewGuid();
        var service = new MockupRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => mockupId);

        var result = await service.CreateAsync(new MockupCreateRequest(
            sample.Listing.Id, "Mockup 1", ProductType: "T-Shirt", VendorProduct: "Gildan 64000"));

        Assert.True(result.Succeeded);
        Assert.Equal("manual", result.Mockup!.SourceMethod);
        Assert.Equal("T-Shirt", result.Mockup.ProductType);
    }

    [Fact]
    public async Task Create_WithDesignFromSameListing()
    {
        var sample = Sample.Create();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot with
        {
            Designs = [new Design(designId, sample.Store.Id, sample.Listing.Id, null, "Design", null, null, null, DesignApprovalState.Draft, false, sample.Now, sample.Now, "{}")]
        });
        var service = new MockupRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new MockupCreateRequest(sample.Listing.Id, "Mockup", designId));

        Assert.True(result.Succeeded);
        Assert.Equal(designId, result.Mockup!.DesignId);
    }

    [Fact]
    public async Task Create_RejectsDesignFromDifferentListing()
    {
        var sample = Sample.Create();
        var otherListingId = Guid.NewGuid();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot with
        {
            Listings = [sample.Listing, sample.Listing with { Id = otherListingId, Name = "Other" }],
            Designs = [new Design(designId, sample.Store.Id, otherListingId, null, "Design", null, null, null, DesignApprovalState.Draft, false, sample.Now, sample.Now, "{}")]
        });
        var service = new MockupRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new MockupCreateRequest(sample.Listing.Id, "Mockup", designId));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Archive_MarksMockupArchived()
    {
        var sample = Sample.Create();
        var mockupId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new MockupRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => mockupId);
        await service.CreateAsync(new MockupCreateRequest(sample.Listing.Id, "Mockup"));

        var result = await service.ArchiveAsync(mockupId);

        Assert.True(result.Succeeded);
        Assert.True(repository.Snapshot.Mockups.Single().IsArchived);
    }

    [Fact]
    public async Task SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new MockupRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new MockupCreateRequest(sample.Listing.Id, "Mockup"));

        Assert.False(result.Succeeded);
        Assert.Empty(repository.Snapshot.Mockups);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public bool FailSaves { get; init; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves) throw new IOException("save failed");
            Snapshot = value;
            return Task.CompletedTask;
        }
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
            var snapshot = new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []);
            return new(snapshot, now, store, listing);
        }
    }
}
