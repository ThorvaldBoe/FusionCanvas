using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class DesignRecordsServiceTests
{
    [Fact]
    public async Task Create_AddsDesignWithDraftState()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var designId = Guid.NewGuid();
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => designId);

        var result = await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Variant A"));

        Assert.True(result.Succeeded);
        Assert.Equal(DesignApprovalState.Draft, result.Design!.ApprovalState);
        Assert.False(result.Design.IsFinalSelected);
    }

    [Fact]
    public async Task Create_WithImplementedConceptFromSameListing()
    {
        var sample = Sample.Create();
        var conceptId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot with
        {
            Concepts = [new Concept(conceptId, sample.Store.Id, sample.Listing.Id, "Concept", null, "Idea", null, null, null, null, null, null, ConceptLifecycle.Active, false, sample.Now, sample.Now, "{}")]
        });
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design", conceptId));

        Assert.True(result.Succeeded);
        Assert.Equal(conceptId, result.Design!.ImplementedConceptId);
    }

    [Fact]
    public async Task Create_RejectsConceptFromDifferentListing()
    {
        var sample = Sample.Create();
        var otherListingId = Guid.NewGuid();
        var conceptId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot with
        {
            Listings = [sample.Listing, sample.Listing with { Id = otherListingId, Name = "Other" }],
            Concepts = [new Concept(conceptId, sample.Store.Id, otherListingId, "Concept", null, "Idea", null, null, null, null, null, null, ConceptLifecycle.Active, false, sample.Now, sample.Now, "{}")]
        });
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design", conceptId));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PromoteFinal_MarksDesignAsFinal()
    {
        var sample = Sample.Create();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => designId);
        await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design"));

        var result = await service.PromoteFinalAsync(designId);

        Assert.True(result.Succeeded);
        Assert.Contains(designId, result.State.FinalSelectedIds);
    }

    [Fact]
    public async Task PromoteFinal_DoesNotRequireSpecificApprovalState()
    {
        var sample = Sample.Create();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => designId);
        await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Draft Design"));
        await service.EditAsync(new DesignEditRequest(designId, ApprovalState: DesignApprovalState.Rejected));

        var result = await service.PromoteFinalAsync(designId);

        Assert.True(result.Succeeded);
        Assert.Contains(designId, result.State.FinalSelectedIds);
    }

    [Fact]
    public async Task DemoteFinal_RemovesFromFinalAndPreservesDesign()
    {
        var sample = Sample.Create();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => designId);
        await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design"));
        await service.PromoteFinalAsync(designId);

        var result = await service.DemoteFinalAsync(designId);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(designId, result.State.FinalSelectedIds);
        Assert.Contains(result.State.ActiveDesigns, d => d.Id == designId);
    }

    [Fact]
    public async Task Reject_MarksDesignRejectedAndRemovesFromFinal()
    {
        var sample = Sample.Create();
        var designId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => designId);
        await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design"));
        await service.PromoteFinalAsync(designId);

        var result = await service.RejectAsync(designId);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(designId, result.State.FinalSelectedIds);
        Assert.Contains(result.State.RejectedDesigns, d => d.Id == designId);
    }

    [Fact]
    public async Task SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new DesignRecordsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new DesignCreateRequest(sample.Listing.Id, "Design"));

        Assert.False(result.Succeeded);
        Assert.Empty(repository.Snapshot.Designs);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves) throw new IOException("save failed");
            Snapshot = value;
            SaveCount++;
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
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Concept, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []);
            return new(snapshot, now, store, listing);
        }
    }
}
