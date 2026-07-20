using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class ListingManagementPersistenceTests
{
    [Fact]
    public async Task ListingOperationsRoundTripThroughExistingSchemaWithoutSortOrder()
    {
        using var directory = new TestDirectory();
        var repository = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Group", null, false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
        await repository.SaveAsync(WorkspaceSnapshot.FromStores([store], [niche], [group], [], [], [], [tag], [], []), TestContext.Current.CancellationToken);
        var ids = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var service = new ListingManagementService(repository, clock: () => now.AddMinutes(1), newId: () => ids.Dequeue());

        var created = await service.CreateListingAsync(new(
            new(WorkspaceEntityKind.Niche, niche.Id), "Zulu",
            new ListingContext("Description", "Notes", new Dictionary<string, string> { ["custom"] = "value" }, [tag.Id])), TestContext.Current.CancellationToken);
        var moved = await service.MoveListingAsync(new(created.Listing!.Id, new(WorkspaceEntityKind.Group, group.Id)), TestContext.Current.CancellationToken);
        var duplicate = await service.DuplicateListingAsync(new(created.Listing.Id), TestContext.Current.CancellationToken);
        await service.ArchiveListingAsync(created.Listing.Id, TestContext.Current.CancellationToken);
        await service.RestoreListingAsync(new(created.Listing.Id), TestContext.Current.CancellationToken);
        var reloaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(moved.Succeeded);
        Assert.True(duplicate.Succeeded);
        Assert.Equal(2, reloaded.Listings.Count);
        Assert.All(reloaded.Listings, listing => Assert.Equal(group.Id, listing.GroupId));
        Assert.Equal(2, reloaded.ListingTags.Count);
        Assert.Contains("\"custom\":\"value\"", reloaded.Listings.Single(listing => listing.Id == created.Listing.Id).MetadataJson);
        Assert.All(reloaded.Listings, listing => Assert.False(listing.IsArchived));
    }

    [Fact]
    public async Task FailedSaveLeavesDatabaseAtLastCompleteSnapshot()
    {
        using var directory = new TestDirectory();
        var inner = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        await inner.SaveAsync(WorkspaceSnapshot.FromStores([store], [niche], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);
        var service = new ListingManagementService(new FailingRepository(inner));

        var result = await service.CreateListingAsync(new(new(WorkspaceEntityKind.Niche, niche.Id), "Idea"), TestContext.Current.CancellationToken);
        var reloaded = await inner.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(reloaded.Listings);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fusioncanvas-listing-tests", Guid.NewGuid().ToString("N"));
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
