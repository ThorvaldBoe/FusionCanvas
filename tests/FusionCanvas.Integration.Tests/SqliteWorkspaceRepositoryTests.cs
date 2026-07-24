using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
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
        Assert.Equal(snapshot.Items[0], Assert.Single(loaded.Items));
        Assert.Equal(snapshot.Assets[0], Assert.Single(loaded.Assets));
        Assert.Equal(snapshot.Prompts[0], Assert.Single(loaded.Prompts));
        Assert.Equal(snapshot.Tags[0], Assert.Single(loaded.Tags));
        Assert.Equal(snapshot.ItemTags[0], Assert.Single(loaded.ItemTags));
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

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, loaded.Stores.Count);
        Assert.Equal(active, loaded.Stores.Single(store => store.Id == active.Id));
        Assert.Equal(archived, loaded.Stores.Single(store => store.Id == archived.Id));
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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

        await repository.SaveAsync(new WorkspaceSnapshot([first, deleted], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);
        await repository.SaveAsync(new WorkspaceSnapshot([first], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first.Id, Assert.Single(loaded.Stores).Id);
        Assert.DoesNotContain(loaded.Stores, store => store.Id == deleted.Id);
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, loaded.Niches.Count);
        Assert.Equal(active, loaded.Niches.Single(niche => niche.Id == active.Id));
        Assert.Equal(archived, loaded.Niches.Single(niche => niche.Id == archived.Id));
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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

        await repository.SaveAsync(new WorkspaceSnapshot([store], [kept, deleted], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);
        await repository.SaveAsync(new WorkspaceSnapshot([store], [kept], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(kept.Id, Assert.Single(loaded.Niches).Id);
        Assert.DoesNotContain(loaded.Niches, niche => niche.Id == deleted.Id);
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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
        var listing = new Item(Guid.NewGuid(), store.Id, sourceNiche.Id, child.Group!.Id, "Cozy shirt", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await repository.SaveAsync(withListing with { Items = [listing] }, TestContext.Current.CancellationToken);

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
        var loadedListing = Assert.Single(loaded.Items);
        Assert.Equal(destinationNiche.Id, loadedRoot.NicheId);
        Assert.False(loadedRoot.IsArchived);
        Assert.Equal(root.Group.Id, loadedChild.ParentGroupId);
        Assert.Equal("Apparel", loadedChild.Name);
        Assert.Equal("Product split", loadedChild.Description);
        Assert.Contains("Ready for art", loadedChild.MetadataJson);
        Assert.Equal(child.Group.Id, loadedListing.GroupId);
        Assert.Equal(destinationNiche.Id, loadedListing.NicheId);
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
    }


    [Fact]
    public async Task LoadAsync_UpgradesUnversionedDatabaseToCurrentSchemaVersion()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        await SetUserVersionAsync(databasePath, 0);

        var repository = new SqliteWorkspaceRepository(databasePath);

        await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
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

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
        Assert.Equal(nicheId, Assert.Single(loaded.Stores).DefaultNicheId);
        Assert.Equal(["Alpha", "Zulu"], loaded.Groups.OrderBy(group => group.SortOrder).Select(group => group.Name));
        Assert.Equal([0, 1], loaded.Groups.OrderBy(group => group.SortOrder).Select(group => group.SortOrder));
    }

    [Fact]
    public async Task LoadAsync_MigratesV3LifecycleValuesToV4Vocabulary()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var workspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE workspaces (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE stores (id TEXT PRIMARY KEY, workspace_id TEXT NOT NULL, default_niche_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE niches (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, parent_group_id TEXT NULL, sort_order INTEGER NOT NULL DEFAULT 0, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listings (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, group_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, status INTEGER NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listing_tags (listing_id TEXT NOT NULL, tag_id TEXT NOT NULL, PRIMARY KEY (listing_id, tag_id));
                CREATE TABLE tags (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE assets (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, kind INTEGER NOT NULL, workspace_relative_path TEXT NOT NULL, original_source_path TEXT NULL, is_missing INTEGER NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE prompts (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, listing_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, text TEXT NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE asset_links (asset_id TEXT NOT NULL, entity_kind INTEGER NOT NULL, entity_id TEXT NOT NULL, PRIMARY KEY (asset_id, entity_kind, entity_id));
                INSERT INTO workspaces VALUES ($workspace_id, 'Personal', NULL, 0, $now, $now, '{}');
                INSERT INTO stores VALUES ($store_id, $workspace_id, NULL, 'Store', NULL, 0, $now, $now, '{}');
                INSERT INTO niches VALUES ($niche_id, $store_id, 'Niche', NULL, 0, $now, $now, '{}');
                UPDATE stores SET default_niche_id = $niche_id WHERE id = $store_id;
                INSERT INTO listings VALUES ($active_id, $store_id, $niche_id, NULL, 'Active', NULL, 0, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($draft_id, $store_id, $niche_id, NULL, 'Draft', NULL, 1, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($ready_id, $store_id, $niche_id, NULL, 'Ready', NULL, 2, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($published_id, $store_id, $niche_id, NULL, 'Published', NULL, 3, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($archived_id, $store_id, $niche_id, NULL, 'Archived', NULL, 4, 0, $now, $now, '{}');
                PRAGMA user_version = 3;
                """;
            command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString());
            command.Parameters.AddWithValue("$store_id", storeId.ToString());
            command.Parameters.AddWithValue("$niche_id", nicheId.ToString());
            command.Parameters.AddWithValue("$active_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$draft_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$ready_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$published_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$archived_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
        var byName = loaded.Items.ToDictionary(listing => listing.Name);
        Assert.Equal(ItemStatus.Draft, byName["Active"].Status);
        Assert.Equal(WorkflowStage.Listing, byName["Active"].Stage);
        Assert.False(byName["Active"].IsArchived);
        Assert.Equal(ItemStatus.Draft, byName["Draft"].Status);
        Assert.Equal(WorkflowStage.Idea, byName["Draft"].Stage);
        Assert.Equal(ItemStatus.Draft, byName["Ready"].Status);
        Assert.Equal(WorkflowStage.Design, byName["Ready"].Stage);
        Assert.Equal(ItemStatus.Published, byName["Published"].Status);
        Assert.Equal(WorkflowStage.Listing, byName["Published"].Stage);
        Assert.Equal(ItemStatus.Draft, byName["Archived"].Status);
        Assert.Equal(WorkflowStage.Idea, byName["Archived"].Stage);
        Assert.True(byName["Archived"].IsArchived);
    }

    [Fact]
    public async Task LoadAsync_WhenDatabaseVersionIsNewer_ThrowsClearError()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        await SetUserVersionAsync(databasePath, 6);
        var repository = new SqliteWorkspaceRepository(databasePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Contains("requires a newer FusionCanvas version", exception.Message);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsTagColor()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "Studio", null, false, now, now, "{}");
        var withColor = new Tag(Guid.NewGuid(), store.Id, "evergreen", "Always relevant", false, now, now, "{}", "#1abc9c");
        var nullColor = new Tag(Guid.NewGuid(), store.Id, "draft", null, false, now, now, "{}", null);
        var expandedColor = new Tag(Guid.NewGuid(), store.Id, "risky", null, false, now, now, "{}", "#f00");
        var snapshot = new WorkspaceSnapshot([store], [], [], [], [], [], [withColor, nullColor, expandedColor], [], []);

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(withColor, loaded.Tags.Single(tag => tag.Id == withColor.Id));
        Assert.Equal("#1ABC9C", loaded.Tags.Single(tag => tag.Id == withColor.Id).Color);
        Assert.Equal(nullColor, loaded.Tags.Single(tag => tag.Id == nullColor.Id));
        Assert.Null(loaded.Tags.Single(tag => tag.Id == nullColor.Id).Color);
        Assert.Equal("#FF0000", loaded.Tags.Single(tag => tag.Id == expandedColor.Id).Color);
        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task LoadAsync_UpgradesVersion3DatabaseTo4PreservingTags()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
        var workspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        var storeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE workspaces (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE stores (id TEXT PRIMARY KEY, workspace_id TEXT NOT NULL, default_niche_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE niches (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, parent_group_id TEXT NULL, sort_order INTEGER NOT NULL DEFAULT 0, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listings (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, group_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, status INTEGER NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE assets (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, kind INTEGER NOT NULL, workspace_relative_path TEXT NOT NULL, original_source_path TEXT NULL, is_missing INTEGER NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE prompts (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, listing_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, text TEXT NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE tags (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listing_tags (listing_id TEXT NOT NULL, tag_id TEXT NOT NULL, PRIMARY KEY (listing_id, tag_id));
                CREATE TABLE asset_links (asset_id TEXT NOT NULL, entity_kind INTEGER NOT NULL, entity_id TEXT NOT NULL, PRIMARY KEY (asset_id, entity_kind, entity_id));
                INSERT INTO workspaces VALUES ($workspace_id, 'Personal', NULL, 0, $now, $now, '{}');
                INSERT INTO stores VALUES ($store_id, $workspace_id, NULL, 'Studio', NULL, 0, $now, $now, '{}');
                INSERT INTO tags VALUES ($tag_id, $store_id, 'Evergreen', 'Always relevant', 0, $now, $now, '{}');
                PRAGMA user_version = 3;
                """;
            command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString());
            command.Parameters.AddWithValue("$store_id", storeId.ToString());
            command.Parameters.AddWithValue("$tag_id", tagId.ToString());
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
        var tag = Assert.Single(loaded.Tags);
        Assert.Equal(tagId, tag.Id);
        Assert.Equal(storeId, tag.StoreId);
        Assert.Equal("Evergreen", tag.Name);
        Assert.Null(tag.Color);
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidTagColor()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "Studio", null, false, now, now, "{}");

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.SaveAsync(
                new WorkspaceSnapshot([store], [], [], [], [], [], [new Tag(Guid.NewGuid(), store.Id, "bad", null, false, now, now, "{}", "not-a-color")], [], []),
                TestContext.Current.CancellationToken));
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

    [Fact]
    public async Task LoadAsync_MigratesVersion4ListingSchemaToVersion5ItemSchema()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var now = new DateTimeOffset(2026, 7, 23, 9, 0, 0, TimeSpan.Zero);
        var workspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var rootGroupId = Guid.NewGuid();
        var childGroupId = Guid.NewGuid();
        var namedItemId = Guid.NewGuid();
        var emptyItemId = Guid.NewGuid();
        var archivedItemId = Guid.NewGuid();
        var publishedItemId = Guid.NewGuid();
        var rejectedItemId = Guid.NewGuid();
        var pausedItemId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE workspaces (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE stores (id TEXT PRIMARY KEY, workspace_id TEXT NOT NULL, default_niche_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE niches (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, parent_group_id TEXT NULL, sort_order INTEGER NOT NULL DEFAULT 0, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listings (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, group_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, status INTEGER NOT NULL, workflow_stage INTEGER NOT NULL DEFAULT 0, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE assets (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, kind INTEGER NOT NULL, workspace_relative_path TEXT NOT NULL, original_source_path TEXT NULL, is_missing INTEGER NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE prompts (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, listing_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, text TEXT NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE tags (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL, color TEXT NULL);
                CREATE TABLE listing_tags (listing_id TEXT NOT NULL, tag_id TEXT NOT NULL, PRIMARY KEY (listing_id, tag_id));
                CREATE TABLE asset_links (asset_id TEXT NOT NULL, entity_kind INTEGER NOT NULL, entity_id TEXT NOT NULL, PRIMARY KEY (asset_id, entity_kind, entity_id));
                INSERT INTO workspaces VALUES ($workspace_id, 'Personal', NULL, 0, $now, $now, '{}');
                INSERT INTO stores VALUES ($store_id, $workspace_id, $niche_id, 'Studio', NULL, 0, $now, $now, '{}');
                INSERT INTO niches VALUES ($niche_id, $store_id, 'Coffee', NULL, 0, $now, $now, '{}');
                INSERT INTO groups VALUES ($root_group_id, $store_id, $niche_id, NULL, 0, 'Fall', NULL, 0, $now, $now, '{"priority":2}');
                INSERT INTO groups VALUES ($child_group_id, $store_id, $niche_id, $root_group_id, 1, 'Shirts', NULL, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($named_item_id, $store_id, $niche_id, $child_group_id, 'Pumpkin espresso', 'Cozy shirt', 0, 0, 0, $now, $now, '{"idea":"warm","inheritedFrom:niche":"Coffee","unknownKey":"preserve"}');
                INSERT INTO listings VALUES ($empty_item_id, $store_id, $niche_id, NULL, '', NULL, 0, 1, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($archived_item_id, $store_id, $niche_id, NULL, 'Archived one', NULL, 0, 2, 1, $now, $now, '{"notes":"keep"}');
                INSERT INTO listings VALUES ($published_item_id, $store_id, $niche_id, NULL, 'Live one', NULL, 1, 3, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($paused_item_id, $store_id, $niche_id, NULL, 'Paused one', NULL, 2, 3, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($rejected_item_id, $store_id, $niche_id, NULL, 'Rejected one', NULL, 3, 0, 0, $now, $now, '{}');
                INSERT INTO tags VALUES ($tag_id, $store_id, 'seasonal', NULL, 0, $now, $now, '{}', NULL);
                INSERT INTO listing_tags VALUES ($named_item_id, $tag_id);
                INSERT INTO assets VALUES ($asset_id, $store_id, 'design.png', NULL, 1, 'assets/design.png', NULL, 0, 0, $now, $now, '{}');
                INSERT INTO asset_links VALUES ($asset_id, 3, $named_item_id);
                INSERT INTO prompts VALUES ($prompt_id, $store_id, $named_item_id, 'Prompt', NULL, 'Text', 0, $now, $now, '{}');
                PRAGMA user_version = 4;
                """;
            command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString());
            command.Parameters.AddWithValue("$store_id", storeId.ToString());
            command.Parameters.AddWithValue("$niche_id", nicheId.ToString());
            command.Parameters.AddWithValue("$root_group_id", rootGroupId.ToString());
            command.Parameters.AddWithValue("$child_group_id", childGroupId.ToString());
            command.Parameters.AddWithValue("$named_item_id", namedItemId.ToString());
            command.Parameters.AddWithValue("$empty_item_id", emptyItemId.ToString());
            command.Parameters.AddWithValue("$archived_item_id", archivedItemId.ToString());
            command.Parameters.AddWithValue("$published_item_id", publishedItemId.ToString());
            command.Parameters.AddWithValue("$paused_item_id", pausedItemId.ToString());
            command.Parameters.AddWithValue("$rejected_item_id", rejectedItemId.ToString());
            command.Parameters.AddWithValue("$tag_id", tagId.ToString());
            command.Parameters.AddWithValue("$asset_id", assetId.ToString());
            command.Parameters.AddWithValue("$prompt_id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, await ReadUserVersionAsync(databasePath));
        Assert.Equal(6, loaded.Items.Count);
        var byId = loaded.Items.ToDictionary(item => item.Id);
        Assert.Equal("Pumpkin espresso", byId[namedItemId].Name);
        Assert.Equal(WorkflowStage.Idea, byId[namedItemId].Stage);
        Assert.Equal(ItemStatus.Draft, byId[namedItemId].Status);
        Assert.Contains("\"unknownKey\":\"preserve\"", byId[namedItemId].MetadataJson);
        Assert.Contains("\"inheritedFrom:niche\":\"Coffee\"", byId[namedItemId].MetadataJson);
        Assert.Equal(string.Empty, byId[emptyItemId].Name);
        Assert.Equal(WorkflowStage.Concept, byId[emptyItemId].Stage);
        Assert.True(byId[archivedItemId].IsArchived);
        Assert.Equal(WorkflowStage.Listing, byId[publishedItemId].Stage);
        Assert.Equal(ItemStatus.Published, byId[publishedItemId].Status);
        Assert.Equal(ItemStatus.Paused, byId[pausedItemId].Status);
        Assert.Equal(ItemStatus.Rejected, byId[rejectedItemId].Status);
        Assert.Equal(namedItemId, Assert.Single(loaded.ItemTags).ItemId);
        Assert.Equal(tagId, Assert.Single(loaded.ItemTags).TagId);
        Assert.Equal(namedItemId, Assert.Single(loaded.Prompts).ItemId);
        var link = Assert.Single(loaded.AssetLinks);
        Assert.Equal(WorkspaceEntityKind.Item, link.EntityKind);
        Assert.Equal(3, (int)link.EntityKind);
        Assert.Equal(namedItemId, link.EntityId);
        await AssertOldListingTablesRemovedAsync(databasePath);

        await new SqliteWorkspaceRepository(databasePath).SaveAsync(loaded, TestContext.Current.CancellationToken);
        var reopened = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(loaded.Items.Count, reopened.Items.Count);
        Assert.Equal(loaded.ItemTags.Count, reopened.ItemTags.Count);
        Assert.Equal(loaded.Prompts.Count, reopened.Prompts.Count);
        Assert.Equal(loaded.AssetLinks.Count, reopened.AssetLinks.Count);
        Assert.Equal(byId[namedItemId], reopened.Items.Single(item => item.Id == namedItemId));
        Assert.Equal(byId[namedItemId].MetadataJson, reopened.Items.Single(item => item.Id == namedItemId).MetadataJson);
    }

    [Fact]
    public async Task LoadAsync_MigrationFailureRollsBackToV4State()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("workspace.db");
        var now = new DateTimeOffset(2026, 7, 23, 9, 0, 0, TimeSpan.Zero);
        var workspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var orphanItemId = Guid.NewGuid();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys = OFF;
                CREATE TABLE workspaces (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE stores (id TEXT PRIMARY KEY, workspace_id TEXT NOT NULL, default_niche_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE niches (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, parent_group_id TEXT NULL, sort_order INTEGER NOT NULL DEFAULT 0, name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listings (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, niche_id TEXT NULL, group_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, status INTEGER NOT NULL, workflow_stage INTEGER NOT NULL DEFAULT 0, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                CREATE TABLE listing_tags (listing_id TEXT NOT NULL, tag_id TEXT NOT NULL, PRIMARY KEY (listing_id, tag_id));
                CREATE TABLE prompts (id TEXT PRIMARY KEY, store_id TEXT NOT NULL, listing_id TEXT NULL, name TEXT NOT NULL, description TEXT NULL, text TEXT NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL);
                INSERT INTO workspaces VALUES ($workspace_id, 'Personal', NULL, 0, $now, $now, '{}');
                INSERT INTO stores VALUES ($store_id, $workspace_id, NULL, 'Studio', NULL, 0, $now, $now, '{}');
                INSERT INTO niches VALUES ($niche_id, $store_id, 'Coffee', NULL, 0, $now, $now, '{}');
                INSERT INTO listings VALUES ($orphan_item_id, '00000000-0000-0000-0000-000000000000', $niche_id, NULL, 'Orphan', NULL, 0, 0, 0, $now, $now, '{}');
                PRAGMA foreign_keys = ON;
                PRAGMA user_version = 4;
                """;
            command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString());
            command.Parameters.AddWithValue("$store_id", storeId.ToString());
            command.Parameters.AddWithValue("$niche_id", nicheId.ToString());
            command.Parameters.AddWithValue("$orphan_item_id", orphanItemId.ToString());
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var repository = new SqliteWorkspaceRepository(databasePath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(4, await ReadUserVersionAsync(databasePath));
        await using (var verifyConnection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await verifyConnection.OpenAsync(TestContext.Current.CancellationToken);
            await using var verifyCommand = verifyConnection.CreateCommand();
            verifyCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'listings';";
            var tableResult = await verifyCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal("listings", tableResult);
            verifyCommand.CommandText = "SELECT COUNT(*) FROM listings;";
            Assert.Equal(1, Convert.ToInt32(await verifyCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
        }
    }

    // Fault point: a v4 listings row referencing a non-existent store triggers PRAGMA foreign_key_check
    // failure inside the transactional v5 migration. Deeper SQLite I/O faults (disk full, locked file)
    // are not injectable in-process and are covered by desktop/manual evidence rather than faked here.

    private static async Task AssertOldListingTablesRemovedAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('listings', 'listing_tags');";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var remaining = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            remaining.Add(reader.GetString(0));
        }
        Assert.Empty(remaining);
    }

    private static WorkspaceSnapshot CreateCompleteSnapshot()
    {
        var createdAt = new DateTimeOffset(2026, 6, 18, 7, 30, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(5);
        var store = new Store(Guid.NewGuid(), "North Star Studio", "POD brand", false, createdAt, updatedAt, """{"currency":"USD"}""");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", "Warm drink designs", false, createdAt, updatedAt, """{"season":"fall"}""");
        store = store with { DefaultNicheId = niche.Id };
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Fall launch", "Seasonal rollout", true, createdAt, updatedAt, """{"priority":2}""", 4);
        var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", "Cozy shirt idea", ItemStatus.Draft, WorkflowStage.Idea, false, createdAt, updatedAt, """{"audience":"baristas"}""");
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
            [new ItemTag(listing.Id, tag.Id)],
            [new AssetLink(asset.Id, WorkspaceEntityKind.Item, listing.Id)]);
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
