using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Workspace;

public sealed class SqliteWorkspaceRepository(string databasePath) : IWorkspaceRepository
{
    private const int CurrentSchemaVersion = 1;

    private readonly string _databasePath = databasePath;

    public async Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_databasePath))!);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var table in new[] { "asset_links", "listing_tags", "prompts", "assets", "listings", "groups", "niches", "tags", "stores" })
        {
            await ExecuteAsync(connection, transaction, $"DELETE FROM {table};", cancellationToken);
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

        foreach (var group in snapshot.Groups)
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
            CREATE TABLE IF NOT EXISTS stores (
                id TEXT PRIMARY KEY,
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
        await SetPragmaUserVersionAsync(connection, CurrentSchemaVersion, cancellationToken);
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

    private static Task InsertStoreAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Store store, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO stores (id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, CommonParameters(store));

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
            INSERT INTO groups (id, store_id, niche_id, parent_group_id, name, description, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $niche_id, $parent_group_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(group), ("$store_id", group.StoreId.ToString()), ("$niche_id", group.NicheId?.ToString()), ("$parent_group_id", group.ParentGroupId?.ToString())]);

    private static Task InsertListingAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Listing listing, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO listings (id, store_id, niche_id, group_id, name, description, status, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $niche_id, $group_id, $name, $description, $status, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(listing), ("$store_id", listing.StoreId.ToString()), ("$niche_id", listing.NicheId?.ToString()), ("$group_id", listing.GroupId?.ToString()), ("$status", (int)listing.Status)]);

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

    private static async Task<IReadOnlyList<Store>> LoadStoresAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var stores = new List<Store>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM stores ORDER BY name;", cancellationToken))
        {
            stores.Add(new Store(ReadGuid(reader, "id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
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
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM groups ORDER BY name;", cancellationToken))
        {
            groups.Add(new TopicGroup(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "niche_id"), ReadNullableGuid(reader, "parent_group_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return groups;
    }

    private static async Task<IReadOnlyList<Listing>> LoadListingsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var listings = new List<Listing>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM listings ORDER BY name;", cancellationToken))
        {
            listings.Add(new Listing(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "niche_id"), ReadNullableGuid(reader, "group_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), (ListingStatus)ReadInt(reader, "status"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
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
}
