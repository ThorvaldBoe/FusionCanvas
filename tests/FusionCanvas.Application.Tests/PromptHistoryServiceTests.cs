using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class PromptHistoryServiceTests
{
    [Fact]
    public async Task Save_CreatesPromptWithTypeProviderModel()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var promptId = Guid.NewGuid();
        var service = new PromptHistoryService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => promptId);

        var result = await service.SaveAsync(new PromptSaveRequest(
            sample.Store.Id, sample.Listing.Id, "Test Prompt", "input text",
            Output: "output text", PromptType: "phrase", Provider: "OpenAI", Model: "gpt-4o"));

        Assert.True(result.Succeeded);
        Assert.Equal("phrase", result.Prompt!.PromptType);
        Assert.Equal("OpenAI", result.Prompt.Provider);
        Assert.Equal("gpt-4o", result.Prompt.Model);
    }

    [Fact]
    public async Task Reject_ArchivesWithRejectedLifecycle()
    {
        var sample = Sample.Create();
        var promptId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new PromptHistoryService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => promptId);
        await service.SaveAsync(new PromptSaveRequest(sample.Store.Id, sample.Listing.Id, "Prompt", "text"));

        var result = await service.RejectAsync(promptId);

        Assert.True(result.Succeeded);
        Assert.Equal(ConceptLifecycle.Rejected, result.Prompt!.Lifecycle);
        Assert.Contains(result.State.RejectedPrompts, p => p.Id == promptId);
    }

    [Fact]
    public async Task Supersede_MarksAsSuperseded()
    {
        var sample = Sample.Create();
        var promptId = Guid.NewGuid();
        var repository = new TestRepository(sample.Snapshot);
        var service = new PromptHistoryService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => promptId);
        await service.SaveAsync(new PromptSaveRequest(sample.Store.Id, sample.Listing.Id, "Prompt", "text"));

        var result = await service.SupersedeAsync(promptId);

        Assert.True(result.Succeeded);
        Assert.Equal(ConceptLifecycle.Superseded, result.Prompt!.Lifecycle);
        Assert.Contains(result.State.SupersededPrompts, p => p.Id == promptId);
    }

    [Fact]
    public async Task SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new PromptHistoryService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.SaveAsync(new PromptSaveRequest(sample.Store.Id, sample.Listing.Id, "Prompt", "text"));

        Assert.False(result.Succeeded);
        Assert.Empty(repository.Snapshot.Prompts);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }
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
