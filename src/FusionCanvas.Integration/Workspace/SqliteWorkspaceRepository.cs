using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Workspace;

public sealed class SqliteWorkspaceRepository(string databasePath) : IWorkspaceRepository
{
    private const int CurrentSchemaVersion = 4;

    private readonly string _databasePath = databasePath;

    public async Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_databasePath))!);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        ValidateSnapshot(snapshot);

        foreach (var table in new[] { "asset_links", "listing_tags", "prompts", "assets", "listings", "groups", "niches", "tags", "stores", "workspaces" })
        {
            await ExecuteAsync(connection, transaction, $"DELETE FROM {table};", cancellationToken);
        }

        foreach (var workspace in snapshot.Workspaces)
        {
            await InsertWorkspaceAsync(connection, transaction, workspace, cancellationToken);
        }

        foreach (var store in snapshot.Stores)
        {
            await InsertStoreAsync(connection, transaction, store, cancellationToken);
        }

        foreach (var tag in snapshot.Tags)
        {
            await InsertTagAsync(connection, transaction, tag, cancellationToken);
        }

        foreach (var niche in snapshot.Niches)
        {
            await InsertNicheAsync(connection, transaction, niche, cancellationToken);
        }

        foreach (var group in OrderGroupsForInsert(snapshot.Groups))
        {
            await InsertGroupAsync(connection, transaction, group, cancellationToken);
        }

        foreach (var listing in snapshot.Listings)
        {
            await InsertListingAsync(connection, transaction, listing, cancellationToken);
        }

        foreach (var asset in snapshot.Assets)
        {
            await InsertAssetAsync(connection, transaction, asset, cancellationToken);
        }

        foreach (var prompt in snapshot.Prompts)
        {
            await InsertPromptAsync(connection, transaction, prompt, cancellationToken);
        }

        foreach (var listingTag in snapshot.ListingTags)
        {
            await InsertListingTagAsync(connection, transaction, listingTag, cancellationToken);
        }

        foreach (var assetLink in snapshot.AssetLinks)
        {
            await InsertAssetLinkAsync(connection, transaction, assetLink, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return WorkspaceSnapshot.Empty;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        return new WorkspaceSnapshot(
            await LoadWorkspacesAsync(connection, cancellationToken),
            await LoadStoresAsync(connection, cancellationToken),
            await LoadNichesAsync(connection, cancellationToken),
            await LoadGroupsAsync(connection, cancellationToken),
            await LoadListingsAsync(connection, cancellationToken),
            await LoadAssetsAsync(connection, cancellationToken),
            await LoadPromptsAsync(connection, cancellationToken),
            await LoadTagsAsync(connection, cancellationToken),
            await LoadListingTagsAsync(connection, cancellationToken),
            await LoadAssetLinksAsync(connection, cancellationToken));
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, null, "PRAGMA foreign_keys = ON;", cancellationToken);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var schemaVersion = await ReadPragmaUserVersionAsync(connection, cancellationToken);
        if (schemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Workspace database schema version {schemaVersion} requires a newer FusionCanvas version. Current supported schema version is {CurrentSchemaVersion}.");
        }

        const string sql = """
            CREATE TABLE IF NOT EXISTS workspaces (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS stores (
                id TEXT PRIMARY KEY,
                workspace_id TEXT NOT NULL REFERENCES workspaces(id) ON DELETE RESTRICT,
                default_niche_id TEXT NULL,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS tags (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS niches (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS groups (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                niche_id TEXT NULL REFERENCES niches(id) ON DELETE SET NULL,
                parent_group_id TEXT NULL REFERENCES groups(id) ON DELETE SET NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS listings (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                niche_id TEXT NULL REFERENCES niches(id) ON DELETE SET NULL,
                group_id TEXT NULL REFERENCES groups(id) ON DELETE SET NULL,
                name TEXT NOT NULL,
                description TEXT NULL,
                status INTEGER NOT NULL,
                workflow_stage INTEGER NOT NULL DEFAULT 0,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS assets (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                kind INTEGER NOT NULL,
                workspace_relative_path TEXT NOT NULL,
                original_source_path TEXT NULL,
                is_missing INTEGER NOT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS prompts (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                listing_id TEXT NULL REFERENCES listings(id) ON DELETE SET NULL,
                name TEXT NOT NULL,
                description TEXT NULL,
                text TEXT NOT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS listing_tags (
                listing_id TEXT NOT NULL REFERENCES listings(id) ON DELETE CASCADE,
                tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY (listing_id, tag_id)
            );

            CREATE TABLE IF NOT EXISTS asset_links (
                asset_id TEXT NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
                entity_kind INTEGER NOT NULL,
                entity_id TEXT NOT NULL,
                PRIMARY KEY (asset_id, entity_kind, entity_id)
            );
            """;

        await ExecuteAsync(connection, null, sql, cancellationToken);
        if (schemaVersion < 2)
        {
            await MigrateToVersion2Async(connection, cancellationToken);
        }

        if (schemaVersion < 3)
        {
            await MigrateToVersion3Async(connection, cancellationToken);
        }

        if (schemaVersion < 4)
        {
            await MigrateToVersion4Async(connection, cancellationToken);
        }

        await SetPragmaUserVersionAsync(connection, CurrentSchemaVersion, cancellationToken);
    }

    private static async Task MigrateToVersion2Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await ExecuteAsync(connection, null, """
            INSERT OR IGNORE INTO workspaces (id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $name, NULL, 0, $created_at, $updated_at, '{}');
            """, cancellationToken,
            ("$id", WorkspaceDefaults.DefaultWorkspaceId.ToString()),
            ("$name", WorkspaceDefaults.DefaultWorkspaceName),
            ("$created_at", now),
            ("$updated_at", now));

        if (!await ColumnExistsAsync(connection, "stores", "workspace_id", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE stores ADD COLUMN workspace_id TEXT NULL REFERENCES workspaces(id) ON DELETE RESTRICT;", cancellationToken);
        }

        await ExecuteAsync(connection, null, "UPDATE stores SET workspace_id = $workspace_id WHERE workspace_id IS NULL OR workspace_id = '';", cancellationToken, ("$workspace_id", WorkspaceDefaults.DefaultWorkspaceId.ToString()));
    }

    private static async Task MigrateToVersion3Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, "stores", "default_niche_id", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE stores ADD COLUMN default_niche_id TEXT NULL;", cancellationToken);
        }

        if (!await ColumnExistsAsync(connection, "groups", "sort_order", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE groups ADD COLUMN sort_order INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }

        await ExecuteAsync(connection, null, """
            WITH ranked AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                           PARTITION BY store_id, COALESCE(niche_id, ''), COALESCE(parent_group_id, '')
                           ORDER BY name COLLATE NOCASE, id
                       ) - 1 AS position
                FROM groups
            )
            UPDATE groups
            SET sort_order = (SELECT position FROM ranked WHERE ranked.id = groups.id);
            """, cancellationToken);

        await ExecuteAsync(connection, null, """
            UPDATE stores
            SET default_niche_id = (
                SELECT MIN(niches.id)
                FROM niches
                WHERE niches.store_id = stores.id AND niches.is_archived = 0
            )
            WHERE 1 = (
                SELECT COUNT(*)
                FROM niches
                WHERE niches.store_id = stores.id AND niches.is_archived = 0
            );
            """, cancellationToken);
    }

    private static async Task MigrateToVersion4Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, "listings", "workflow_stage", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE listings ADD COLUMN workflow_stage INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }

        // Old ListingStatus ints: Active=0, Draft=1, Ready=2, Published=3, Archived=4.
        // New ListingStatus ints: Draft=0, Published=1, Paused=2, Rejected=3.
        // WorkflowStage ints: Idea=0, Concept=1, Design=2, Listing=3.
        // Translation never invents published state; stage is backfilled from the pre-v4 derivation;
        // and archived-valued rows are folded onto the archive flag.
        await ExecuteAsync(connection, null, """
            UPDATE listings
            SET status = CASE status
                    WHEN 0 THEN 0
                    WHEN 1 THEN 0
                    WHEN 2 THEN 0
                    WHEN 3 THEN 1
                    WHEN 4 THEN 0
                    ELSE 0
                END,
                workflow_stage = CASE status
                    WHEN 0 THEN 3
                    WHEN 1 THEN 0
                    WHEN 2 THEN 2
                    WHEN 3 THEN 3
                    WHEN 4 THEN 0
                    ELSE 0
                END,
                is_archived = CASE status
                    WHEN 4 THEN 1
                    ELSE is_archived
                END;
            """, cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<int> ReadPragmaUserVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static Task SetPragmaUserVersionAsync(SqliteConnection connection, int schemaVersion, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, null, $"PRAGMA user_version = {schemaVersion};", cancellationToken);

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Task InsertWorkspaceAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, FusionCanvas.Domain.Workspace.Workspace workspace, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO workspaces (id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, CommonParameters(workspace));

    private static Task InsertStoreAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Store store, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO stores (id, workspace_id, default_niche_id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $workspace_id, $default_niche_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(store), ("$workspace_id", store.WorkspaceId.ToString()), ("$default_niche_id", store.DefaultNicheId?.ToString())]);

    private static Task InsertTagAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Tag tag, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO tags (id, store_id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(tag), ("$store_id", tag.StoreId.ToString())]);

    private static Task InsertNicheAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Niche niche, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO niches (id, store_id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(niche), ("$store_id", niche.StoreId.ToString())]);

    private static Task InsertGroupAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, TopicGroup group, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO groups (id, store_id, niche_id, parent_group_id, sort_order, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $niche_id, $parent_group_id, $sort_order, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(group), ("$store_id", group.StoreId.ToString()), ("$niche_id", group.NicheId?.ToString()), ("$parent_group_id", group.ParentGroupId?.ToString()), ("$sort_order", group.SortOrder)]);

    private static IReadOnlyList<TopicGroup> OrderGroupsForInsert(IReadOnlyList<TopicGroup> groups)
    {
        var remaining = groups.ToDictionary(group => group.Id);
        var ordered = new List<TopicGroup>(groups.Count);
        var inserted = new HashSet<Guid>();

        while (remaining.Count > 0)
        {
            var ready = remaining.Values
                .Where(group => group.ParentGroupId is null || inserted.Contains(group.ParentGroupId.Value))
                .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Id)
                .ToArray();
            if (ready.Length == 0)
            {
                throw new InvalidOperationException("Group records cannot be persisted because their parent hierarchy is cyclic or incomplete.");
            }

            foreach (var group in ready)
            {
                ordered.Add(group);
                inserted.Add(group.Id);
                remaining.Remove(group.Id);
            }
        }

        return ordered;
    }

    private static Task InsertListingAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Listing listing, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO listings (id, store_id, niche_id, group_id, name, description, status, workflow_stage, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $niche_id, $group_id, $name, $description, $status, $workflow_stage, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(listing), ("$store_id", listing.StoreId.ToString()), ("$niche_id", listing.NicheId?.ToString()), ("$group_id", listing.GroupId?.ToString()), ("$status", (int)listing.Status), ("$workflow_stage", (int)listing.Stage)]);

    private static Task InsertAssetAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Asset asset, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO assets (id, store_id, name, description, kind, workspace_relative_path, original_source_path, is_missing, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $name, $description, $kind, $workspace_relative_path, $original_source_path, $is_missing, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(asset), ("$store_id", asset.StoreId.ToString()), ("$kind", (int)asset.Kind), ("$workspace_relative_path", asset.WorkspaceRelativePath), ("$original_source_path", asset.OriginalSourcePath), ("$is_missing", asset.IsMissing ? 1 : 0)]);

    private static Task InsertPromptAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Prompt prompt, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO prompts (id, store_id, listing_id, name, description, text, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $listing_id, $name, $description, $text, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(prompt), ("$store_id", prompt.StoreId.ToString()), ("$listing_id", prompt.ListingId?.ToString()), ("$text", prompt.Text)]);

    private static Task InsertListingTagAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, ListingTag listingTag, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO listing_tags (listing_id, tag_id) VALUES ($listing_id, $tag_id);", cancellationToken, ("$listing_id", listingTag.ListingId.ToString()), ("$tag_id", listingTag.TagId.ToString()));

    private static Task InsertAssetLinkAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, AssetLink assetLink, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO asset_links (asset_id, entity_kind, entity_id) VALUES ($asset_id, $entity_kind, $entity_id);", cancellationToken, ("$asset_id", assetLink.AssetId.ToString()), ("$entity_kind", (int)assetLink.EntityKind), ("$entity_id", assetLink.EntityId.ToString()));

    private static (string Name, object? Value)[] CommonParameters(WorkspaceEntity entity) =>
    [
        ("$id", entity.Id.ToString()),
        ("$name", entity.Name),
        ("$description", entity.Description),
        ("$is_archived", entity.IsArchived ? 1 : 0),
        ("$created_at", entity.CreatedAt.ToString("O")),
        ("$updated_at", entity.UpdatedAt.ToString("O")),
        ("$metadata_json", entity.MetadataJson)
    ];

    private static async Task<IReadOnlyList<FusionCanvas.Domain.Workspace.Workspace>> LoadWorkspacesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var workspaces = new List<FusionCanvas.Domain.Workspace.Workspace>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM workspaces ORDER BY name;", cancellationToken))
        {
            workspaces.Add(new FusionCanvas.Domain.Workspace.Workspace(ReadGuid(reader, "id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return workspaces;
    }

    private static async Task<IReadOnlyList<Store>> LoadStoresAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var stores = new List<Store>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM stores ORDER BY name;", cancellationToken))
        {
            stores.Add(new Store(ReadGuid(reader, "id"), ReadGuid(reader, "workspace_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json"), ReadNullableGuid(reader, "default_niche_id")));
        }

        return stores;
    }

    private static async Task<IReadOnlyList<Tag>> LoadTagsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var tags = new List<Tag>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM tags ORDER BY name;", cancellationToken))
        {
            tags.Add(new Tag(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return tags;
    }

    private static async Task<IReadOnlyList<Niche>> LoadNichesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var niches = new List<Niche>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM niches ORDER BY name;", cancellationToken))
        {
            niches.Add(new Niche(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return niches;
    }

    private static async Task<IReadOnlyList<TopicGroup>> LoadGroupsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var groups = new List<TopicGroup>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM groups ORDER BY sort_order, name;", cancellationToken))
        {
            groups.Add(new TopicGroup(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "niche_id"), ReadNullableGuid(reader, "parent_group_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json"), ReadInt(reader, "sort_order")));
        }

        return groups;
    }

    private static async Task<IReadOnlyList<Listing>> LoadListingsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var listings = new List<Listing>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM listings ORDER BY name;", cancellationToken))
        {
            listings.Add(new Listing(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "niche_id"), ReadNullableGuid(reader, "group_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), (ListingStatus)ReadInt(reader, "status"), (WorkflowStage)ReadInt(reader, "workflow_stage"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return listings;
    }

    private static async Task<IReadOnlyList<Asset>> LoadAssetsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var assets = new List<Asset>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM assets ORDER BY name;", cancellationToken))
        {
            assets.Add(new Asset(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), (AssetKind)ReadInt(reader, "kind"), ReadString(reader, "workspace_relative_path"), ReadNullableString(reader, "original_source_path"), ReadBool(reader, "is_missing"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return assets;
    }

    private static async Task<IReadOnlyList<Prompt>> LoadPromptsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var prompts = new List<Prompt>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM prompts ORDER BY name;", cancellationToken))
        {
            prompts.Add(new Prompt(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "listing_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadString(reader, "text"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return prompts;
    }

    private static async Task<IReadOnlyList<ListingTag>> LoadListingTagsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var listingTags = new List<ListingTag>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM listing_tags;", cancellationToken))
        {
            listingTags.Add(new ListingTag(ReadGuid(reader, "listing_id"), ReadGuid(reader, "tag_id")));
        }

        return listingTags;
    }

    private static async Task<IReadOnlyList<AssetLink>> LoadAssetLinksAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var assetLinks = new List<AssetLink>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM asset_links;", cancellationToken))
        {
            assetLinks.Add(new AssetLink(ReadGuid(reader, "asset_id"), (WorkspaceEntityKind)ReadInt(reader, "entity_kind"), ReadGuid(reader, "entity_id")));
        }

        return assetLinks;
    }

    private static async IAsyncEnumerable<SqliteDataReader> ReadAsync(
        SqliteConnection connection,
        string sql,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return reader;
        }
    }

    private static Guid ReadGuid(SqliteDataReader reader, string name) => Guid.Parse(ReadString(reader, name));

    private static Guid? ReadNullableGuid(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    }

    private static string ReadString(SqliteDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

    private static string? ReadNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int ReadInt(SqliteDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));

    private static bool ReadBool(SqliteDataReader reader, string name) => ReadInt(reader, name) == 1;

    private static DateTimeOffset ReadDate(SqliteDataReader reader, string name) => DateTimeOffset.Parse(ReadString(reader, name));

    private static void ValidateSnapshot(WorkspaceSnapshot snapshot)
    {
        var workspaceIds = snapshot.Workspaces.Select(workspace => workspace.Id).ToHashSet();
        foreach (var store in snapshot.Stores)
        {
            if (!workspaceIds.Contains(store.WorkspaceId))
            {
                throw new InvalidOperationException("Every store must belong to an existing workspace before saving.");
            }

            if (store.DefaultNicheId is Guid defaultNicheId &&
                !snapshot.Niches.Any(niche => niche.Id == defaultNicheId && niche.StoreId == store.Id && !niche.IsArchived))
            {
                throw new InvalidOperationException("A store default niche must reference an active niche in that store.");
            }
        }
    }
}
