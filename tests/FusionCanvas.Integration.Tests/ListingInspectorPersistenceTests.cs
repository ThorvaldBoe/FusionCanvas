using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class ListingInspectorPersistenceTests
{
    [Fact]
    public async Task InspectorSave_RoundTripsCreativeFieldsNotesAndTagsThroughExistingSchema()
    {
        using var directory = new TestDirectory();
        var repository = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await repository.SaveAsync(new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []), TestContext.Current.CancellationToken);
        var service = new ListingInspectorService(repository, clock: () => now.AddMinutes(1), newId: Guid.NewGuid);

        var saved = await service.SaveAsync(new(
            listing.Id, "Updated", "Idea text", "Phrase text", "Graphic direction", "Notes text", ["First", "Second"]), TestContext.Current.CancellationToken);
        var reloaded = await service.LoadAsync(listing.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.Succeeded);
        Assert.NotNull(saved.State);
        Assert.Equal("Updated", saved.State!.Title);
        Assert.Equal("Idea text", saved.State.Creative.Idea);
        Assert.Equal("Phrase text", saved.State.Creative.Phrase);
        Assert.Equal("Graphic direction", saved.State.Creative.GraphicDirection);
        Assert.Equal("Notes text", saved.State.Notes);
        Assert.Equal(2, saved.State.Tags.Count);

        Assert.Equal("Updated", reloaded!.Title);
        Assert.Equal("Idea text", reloaded.Creative.Idea);
        Assert.Equal("Phrase text", reloaded.Creative.Phrase);
        Assert.Equal("Graphic direction", reloaded.Creative.GraphicDirection);
        Assert.Equal("Notes text", reloaded.Notes);
        Assert.Equal(2, reloaded.Tags.Count);
    }

    [Fact]
    public async Task InspectorSave_TagResolveOrCreateReusesExistingAcrossListingsAndPreservesReusableTag()
    {
        using var directory = new TestDirectory();
        var repository = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var first = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "First", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var second = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Second", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await repository.SaveAsync(new WorkspaceSnapshot([store], [niche], [], [first, second], [], [], [], [], []), TestContext.Current.CancellationToken);
        var id = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var service = new ListingInspectorService(repository, clock: () => now.AddMinutes(1), newId: () => id.Dequeue());

        await service.SaveAsync(new(first.Id, first.Name, null, null, null, null, ["Shared Tag"]), TestContext.Current.CancellationToken);
        await service.SaveAsync(new(second.Id, second.Name, null, null, null, null, ["shared tag"]), TestContext.Current.CancellationToken);
        await service.SaveAsync(new(first.Id, first.Name, null, null, null, null, []), TestContext.Current.CancellationToken);
        var reloaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        var sharedTag = reloaded.Tags.Single(tag => tag.Name == "Shared Tag");
        Assert.DoesNotContain(reloaded.ListingTags, link => link.ListingId == first.Id);
        Assert.Contains(reloaded.ListingTags, link => link.ListingId == second.Id && link.TagId == sharedTag.Id);
        Assert.Single(reloaded.Tags);
    }

    [Fact]
    public async Task InspectorSave_FailureLeavesDatabaseAtLastCompleteSnapshot()
    {
        using var directory = new TestDirectory();
        var inner = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await inner.SaveAsync(new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []), TestContext.Current.CancellationToken);
        var service = new ListingInspectorService(new FailingRepository(inner));

        var result = await service.SaveAsync(new(listing.Id, "Updated", "idea", null, null, null, ["New Tag"]), TestContext.Current.CancellationToken);
        var reloaded = await inner.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Idea", reloaded.Listings.Single().Name);
        Assert.Empty(reloaded.Tags);
        Assert.Empty(reloaded.ListingTags);
    }

    private sealed class FailingRepository(IWorkspaceRepository inner) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => inner.LoadAsync(cancellationToken);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) => throw new IOException("save failed");
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fusioncanvas-inspector-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }
        public string Path { get; }
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(Path, true);
        }
    }
}
