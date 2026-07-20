using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ConceptVersionsServiceTests
{
    [Fact]
    public async Task Create_FirstConceptIsSelectedAutomatically()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var conceptId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => conceptId);

        var result = await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Funny pug"));

        Assert.True(result.Succeeded);
        Assert.Equal(conceptId, result.Concept!.Id);
        Assert.Equal(ConceptLifecycle.Active, result.Concept.Lifecycle);
        Assert.NotNull(result.State.SelectedConcept);
        Assert.Equal(conceptId, result.State.SelectedConcept!.Id);
    }

    [Fact]
    public async Task Create_SecondConceptDoesNotChangeSelection()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => firstId);

        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "First"));
        service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(2), newId: () => secondId);

        var second = await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Second"));

        Assert.True(second.Succeeded);
        Assert.Equal(firstId, second.State.SelectedConcept!.Id);
    }

    [Fact]
    public async Task Supersede_SelectsNewVersionAndDemotesPrior()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => firstId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "First"));

        service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(2), newId: () => secondId);
        var result = await service.SupersedeAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Second"));

        Assert.True(result.Succeeded);
        Assert.Equal(secondId, result.State.SelectedConcept!.Id);
        Assert.Single(result.State.SupersededConcepts);
        Assert.Equal(firstId, result.State.SupersededConcepts[0].Id);
    }

    [Fact]
    public async Task Reject_ClearsSelectionWhenSelectedConceptRejected()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var conceptId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => conceptId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Test"));

        var result = await service.RejectAsync(conceptId);

        Assert.True(result.Succeeded);
        Assert.Null(result.State.SelectedConcept);
        Assert.Single(result.State.RejectedConcepts);
    }

    [Fact]
    public async Task Select_OnlyAcceptsActiveConcepts()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => firstId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "First"));
        await service.RejectAsync(firstId);

        service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(2), newId: () => secondId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Second"));

        var selectRejected = await service.SelectAsync(firstId);
        Assert.False(selectRejected.Succeeded);

        var selectActive = await service.SelectAsync(secondId);
        Assert.True(selectActive.Succeeded);
        Assert.Equal(secondId, selectActive.State.SelectedConcept!.Id);
    }

    [Fact]
    public async Task Edit_PreservesIdentityAndUpdatesFields()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var conceptId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => conceptId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Original"));

        var result = await service.EditAsync(new ConceptEditRequest(conceptId, Idea: "Updated", Phrase: "New phrase"));

        Assert.True(result.Succeeded);
        Assert.Equal("Updated", result.Concept!.Idea);
        Assert.Equal("New phrase", result.Concept.Phrase);
    }

    [Fact]
    public async Task UpdateScore_StoresScoreAndDoesNotBlockAdvancement()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var conceptId = Guid.NewGuid();
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => conceptId);
        await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Test"));

        var result = await service.UpdateScoreAsync(new ConceptScoreRequest(conceptId, """{"overall":5,"weakElement":"phrase"}"""));

        Assert.True(result.Succeeded);
        Assert.Contains("overall", result.Concept!.ScoreJson!);
    }

    [Fact]
    public async Task SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new ConceptCreateRequest(sample.Listing.Id, Idea: "Test"));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.Snapshot.Concepts);
    }

    [Fact]
    public async Task Create_RejectsMissingListing()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ConceptVersionsService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.CreateAsync(new ConceptCreateRequest(Guid.NewGuid(), Idea: "Test"));

        Assert.False(result.Succeeded);
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

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, Listing Listing)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []);
            return new(snapshot, now, store, niche, listing);
        }
    }
}
