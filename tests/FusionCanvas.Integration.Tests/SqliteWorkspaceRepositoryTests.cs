using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class SqliteWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadAsync_PreservesCoreEntitiesAndRelationships()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCompleteSnapshot();

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snapshot.Stores[0], Assert.Single(loaded.Stores));
        Assert.Equal(snapshot.Niches[0], Assert.Single(loaded.Niches));
        Assert.Equal(snapshot.Groups[0], Assert.Single(loaded.Groups));
        Assert.Equal(snapshot.Listings[0], Assert.Single(loaded.Listings));
        Assert.Equal(snapshot.Assets[0], Assert.Single(loaded.Assets));
        Assert.Equal(snapshot.Prompts[0], Assert.Single(loaded.Prompts));
        Assert.Equal(snapshot.Tags[0], Assert.Single(loaded.Tags));
        Assert.Equal(snapshot.ListingTags[0], Assert.Single(loaded.ListingTags));
        Assert.Equal(snapshot.AssetLinks[0], Assert.Single(loaded.AssetLinks));
    }

    [Fact]
    public async Task LoadAsync_WhenDatabaseDoesNotExist_ReturnsEmptyWorkspace()
    {
        using var tempDirectory = new TemporaryDirectory();
        var repository = new SqliteWorkspaceRepository(tempDirectory.GetPath("missing.db"));

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Same(WorkspaceSnapshot.Empty, loaded);
    }

    [Fact]
    public async Task SaveAsync_CreatesSchemaAndSetsCurrentSchemaVersion()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);

        await repository.SaveAsync(CreateCompleteSnapshot(), TestContext.Current.CancellationToken);

        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsStoreManagementFieldsWithoutSchemaChanges()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var createdAt = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(15);
        var active = new Store(
            Guid.NewGuid(),
            "North Star Studio",
            "POD brand",
            false,
            createdAt,
            updatedAt,
            """{"notes":"Soft humor","targetMarket":"Coffee fans","brandDirection":"Warm vintage","planningContext":"Fall launch"}""");
        var archived = new Store(
            Guid.NewGuid(),
            "Paused Studio",
            "Inactive brand",
            true,
            createdAt,
            updatedAt,
            """{"notes":"Review later"}""");
        var snapshot = new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], []);

        await repository.SaveAsync(snapshot);
        var loaded = await repository.LoadAsync();

        Assert.Equal(2, loaded.Stores.Count);
        Assert.Equal(active, loaded.Stores.Single(store => store.Id == active.Id));
        Assert.Equal(archived, loaded.Stores.Single(store => store.Id == archived.Id));
        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsStoreDeletionThroughSnapshotReplacement()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var createdAt = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        var first = new Store(Guid.NewGuid(), "First Studio", null, false, createdAt, createdAt, "{}");
        var deleted = new Store(Guid.NewGuid(), "Deleted Studio", null, false, createdAt, createdAt, "{}");

        await repository.SaveAsync(new WorkspaceSnapshot([first, deleted], [], [], [], [], [], [], [], []));
        await repository.SaveAsync(new WorkspaceSnapshot([first], [], [], [], [], [], [], [], []));
        var loaded = await repository.LoadAsync();

        Assert.Equal(first.Id, Assert.Single(loaded.Stores).Id);
        Assert.DoesNotContain(loaded.Stores, store => store.Id == deleted.Id);
        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsNicheManagementFieldsWithoutSchemaChanges()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var createdAt = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(15);
        var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, createdAt, updatedAt, "{}");
        var active = new Niche(
            Guid.NewGuid(),
            store.Id,
            "Coffee",
            "Warm drink designs",
            false,
            createdAt,
            updatedAt,
            """{"audience":"Coffee fans","humorStyle":"Gentle","visualStyleGuidance":"Vintage badges","constraints":"No logos","risks":"Crowded","researchNotes":"Fall works","notes":"Try cozy textures"}""");
        var archived = new Niche(
            Guid.NewGuid(),
            store.Id,
            "Paused",
            "Inactive niche",
            true,
            createdAt,
            updatedAt,
            """{"notes":"Review later"}""");
        var snapshot = new WorkspaceSnapshot([store], [active, archived], [], [], [], [], [], [], []);

        await repository.SaveAsync(snapshot);
        var loaded = await repository.LoadAsync();

        Assert.Equal(2, loaded.Niches.Count);
        Assert.Equal(active, loaded.Niches.Single(niche => niche.Id == active.Id));
        Assert.Equal(archived, loaded.Niches.Single(niche => niche.Id == archived.Id));
        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsNicheDeletionThroughSnapshotReplacement()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var createdAt = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, createdAt, createdAt, "{}");
        var kept = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, createdAt, createdAt, "{}");
        var deleted = new Niche(Guid.NewGuid(), store.Id, "Deleted", null, false, createdAt, createdAt, "{}");

        await repository.SaveAsync(new WorkspaceSnapshot([store], [kept, deleted], [], [], [], [], [], [], []));
        await repository.SaveAsync(new WorkspaceSnapshot([store], [kept], [], [], [], [], [], [], []));
        var loaded = await repository.LoadAsync();

        Assert.Equal(kept.Id, Assert.Single(loaded.Niches).Id);
        Assert.DoesNotContain(loaded.Niches, niche => niche.Id == deleted.Id);
        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task GroupManagement_RoundTripsHierarchyContextMovesArchiveAndListingRelationshipsWithoutSchemaChange()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var now = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, now, now, "{}");
        var sourceNiche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var destinationNiche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, now, now, "{}");
        await repository.SaveAsync(
            new WorkspaceSnapshot([store], [sourceNiche, destinationNiche], [], [], [], [], [], [], []),
            TestContext.Current.CancellationToken);
        var ids = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var service = new GroupManagementService(repository, () => now.AddMinutes(1), () => ids.Dequeue());

        var root = await service.CreateGroupAsync(
            new GroupManagementCreateRequest(
                new GroupParentReference(WorkspaceEntityKind.Niche, sourceNiche.Id),
                "Campaign",
                new GroupContext("Fall launch", "Keep variants together")),
            TestContext.Current.CancellationToken);
        var child = await service.CreateGroupAsync(
            new GroupManagementCreateRequest(
                new GroupParentReference(WorkspaceEntityKind.Group, root.Group!.Id),
                "Shirts"),
            TestContext.Current.CancellationToken);
        var withListing = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var listing = new Listing(Guid.NewGuid(), store.Id, sourceNiche.Id, child.Group!.Id, "Cozy shirt", null, ListingStatus.Draft, false, now, now, "{}");
        await repository.SaveAsync(withListing with { Listings = [listing] }, TestContext.Current.CancellationToken);

        var updated = await service.UpdateGroupAsync(
            new GroupManagementUpdateRequest(child.Group.Id, "Apparel", new GroupContext("Product split", "Ready for art")),
            TestContext.Current.CancellationToken);
        var moved = await service.MoveGroupAsync(
            new GroupManagementMoveRequest(root.Group.Id, new GroupParentReference(WorkspaceEntityKind.Niche, destinationNiche.Id)),
            TestContext.Current.CancellationToken);
        var archived = await service.ArchiveGroupAsync(root.Group.Id, TestContext.Current.CancellationToken);
        var archivedSnapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var restored = await service.RestoreGroupAsync(root.Group.Id, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(updated.Succeeded, updated.Error);
        Assert.True(moved.Succeeded, moved.Error);
        Assert.True(archived.Succeeded, archived.Error);
        Assert.True(restored.Succeeded, restored.Error);
        Assert.True(archivedSnapshot.Groups.Single(group => group.Id == root.Group.Id).IsArchived);
        var loadedRoot = loaded.Groups.Single(group => group.Id == root.Group.Id);
        var loadedChild = loaded.Groups.Single(group => group.Id == child.Group.Id);
        var loadedListing = Assert.Single(loaded.Listings);
        Assert.Equal(destinationNiche.Id, loadedRoot.NicheId);
        Assert.False(loadedRoot.IsArchived);
        Assert.Equal(root.Group.Id, loadedChild.ParentGroupId);
        Assert.Equal("Apparel", loadedChild.Name);
        Assert.Equal("Product split", loadedChild.Description);
        Assert.Contains("Ready for art", loadedChild.MetadataJson);
        Assert.Equal(child.Group.Id, loadedListing.GroupId);
        Assert.Equal(destinationNiche.Id, loadedListing.NicheId);
        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }


    [Fact]
    public async Task LoadAsync_UpgradesUnversionedDatabaseToCurrentSchemaVersion()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        await SetUserVersionAsync(databasePath, 0);

        var repository = new SqliteWorkspaceRepository(databasePath);

        await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task LoadAsync_UpgradesVersion2WithDefaultNicheAndAlphabeticalSiblingOrder()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var now = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
        var workspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE workspaces (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE stores (id TEXT PRIMARY KEY, workspace_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE niches (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, parent_group_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                INSERT INTO workspaces VALUES ($workspace_id, 'Personal', NULL, 0, $now, $now, '{}');
                INSERT INTO stores VALUES ($store_id, $workspace_id, 'Store', NULL, 0, $now, $now, '{}');
                INSERT INTO niches VALUES ($niche_id, $store_id, 'Niche', NULL, 0, $now, $now, '{}');
                INSERT INTO groups VALUES ($z_id, $store_id, $niche_id, NULL, 'Zulu', NULL, 0, $now, $now, '{}');
                INSERT INTO groups VALUES ($a_id, $store_id, $niche_id, NULL, 'Alpha', NULL, 0, $now, $now, '{}');
                PRAGMA user_version = 2;
                """;
            command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString());
            command.Parameters.AddWithValue("$store_id", storeId.ToString());
            command.Parameters.AddWithValue("$niche_id", nicheId.ToString());
            command.Parameters.AddWithValue("$z_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$a_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, await ReadUserVersionAsync(databasePath));
        Assert.Equal(nicheId, Assert.Single(loaded.Stores).DefaultNicheId);
        Assert.Equal(["Alpha", "Zulu"], loaded.Groups.OrderBy(group => group.SortOrder).Select(group => group.Name));
        Assert.Equal([0, 1], loaded.Groups.OrderBy(group => group.SortOrder).Select(group => group.SortOrder));
    }

    [Fact]
    public async Task LoadAsync_WhenDatabaseVersionIsNewer_ThrowsClearError()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        await SetUserVersionAsync(databasePath, 4);
        var repository = new SqliteWorkspaceRepository(databasePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Contains("requires a newer FusionCanvas version", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_WhenSnapshotViolatesForeignKeys_DoesNotPartiallyCommit()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var original = CreateCompleteSnapshot();
        var invalidStore = new Store(Guid.NewGuid(), "Invalid replacement", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var invalidSnapshot = new WorkspaceSnapshot(
            [invalidStore],
            [],
            [],
            [],
            [],
            [],
            [new Tag(Guid.NewGuid(), Guid.NewGuid(), "orphaned", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}")],
            [],
            []);

        await repository.SaveAsync(original, TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.SaveAsync(invalidSnapshot, TestContext.Current.CancellationToken));
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(original.Stores[0], Assert.Single(loaded.Stores));
        Assert.Equal(original.Tags[0], Assert.Single(loaded.Tags));
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsWorkspacesAndStoreOwnership()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var personal = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Personal", null, false, now, now, "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Client", "Client work", false, now, now, """{"notes":"Retainer"}""");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var clientStore = new Store(Guid.NewGuid(), client.Id, "Client Store", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore, clientStore], [], [], [], [], [], [], [], []);

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, loaded.Workspaces.Count);
        Assert.Equal(personal, loaded.Workspaces.Single(workspace => workspace.Id == personal.Id));
        Assert.Equal(client, loaded.Workspaces.Single(workspace => workspace.Id == client.Id));
        Assert.Equal(personal.Id, loaded.Stores.Single(store => store.Id == personalStore.Id).WorkspaceId);
        Assert.Equal(client.Id, loaded.Stores.Single(store => store.Id == clientStore.Id).WorkspaceId);
    }

    private static WorkspaceSnapshot CreateCompleteSnapshot()
    {
        var createdAt = new DateTimeOffset(2026, 6, 18, 7, 30, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(5);
        var store = new Store(Guid.NewGuid(), "North Star Studio", "POD brand", false, createdAt, updatedAt, """{"currency":"USD"}""");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", "Warm drink designs", false, createdAt, updatedAt, """{"season":"fall"}""");
        store = store with { DefaultNicheId = niche.Id };
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Fall launch", "Seasonal rollout", true, createdAt, updatedAt, """{"priority":2}""", 4);
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", "Cozy shirt idea", ListingStatus.Draft, false, createdAt, updatedAt, """{"audience":"baristas"}""");
        var asset = new Asset(Guid.NewGuid(), store.Id, "design.png", "Primary export", AssetKind.ExportedImage, "assets/2026/06/design.png", "C:/imports/design.png", false, false, createdAt, updatedAt, """{"width":4500}""");
        var prompt = new Prompt(Guid.NewGuid(), store.Id, listing.Id, "Listing prompt", "Phrase prompt", "Create a warm fall coffee phrase.", false, createdAt, updatedAt, """{"model":"manual"}""");
        var tag = new Tag(Guid.NewGuid(), store.Id, "seasonal", "Seasonal work", false, createdAt, updatedAt, """{"color":"orange"}""");

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [group],
            [listing],
            [asset],
            [prompt],
            [tag],
            [new ListingTag(listing.Id, tag.Id)],
            [new AssetLink(asset.Id, WorkspaceEntityKind.Listing, listing.Id)]);
    }

    private static async Task<int> ReadUserVersionAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private static async Task SetUserVersionAsync(string databasePath, int version)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string fileName) => Path.Combine(_directory.FullName, fileName);

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _directory.Delete(recursive: true);
                    return;
                }
                catch (IOException) when (attempt < 3)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
