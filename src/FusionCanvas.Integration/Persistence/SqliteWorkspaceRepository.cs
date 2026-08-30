using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using Microsoft.Data.Sqlite;
using FusionCanvas.Application.Workspaces;
using System.Text.Json;

namespace FusionCanvas.Integration.Persistence;

public sealed class SqliteWorkspaceRepository(string databasePath, bool useConnectionPooling = true) : IWorkspaceRepository
{
    public const int CurrentSchemaVersion = SqliteDatabaseSchema.CurrentVersion;

    private readonly string _databasePath = databasePath;

    public async Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_databasePath))!);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await SqliteDatabaseSchema.EnsureAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        ValidateSnapshot(snapshot);

        foreach (var table in new[] { "mockup_template_revision_colors", "mockup_template_revisions", "mockup_template_colors", "mockup_templates", "placeholder_variants", "offering_placeholders", "offering_variant_values", "offering_variants", "offering_option_values", "offering_options", "blueprint_offerings", "print_providers", "catalog_blueprints", "design_slot_assignments", "design_variant_row_colors", "design_variant_rows", "design_selected_colors", "item_listing_configuration", "asset_links", "design_areas", "product_variants", "item_tags", "fulfillment_offerings", "prompts", "assets", "product_blueprints", "items", "ideation_rejections", "groups", "niches", "tags", "stores", "workspaces" })
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

        foreach (var product in snapshot.StoreProducts)
        {
            await InsertStoreProductAsync(connection, transaction, product, cancellationToken);
        }

        foreach (var offering in snapshot.FulfillmentOfferings)
        {
            await InsertFulfillmentOfferingAsync(connection, transaction, offering, cancellationToken);
        }

        foreach (var variant in snapshot.ProductVariants)
        {
            await InsertProductVariantAsync(connection, transaction, variant, cancellationToken);
        }

        foreach (var area in snapshot.DesignAreas)
        {
            await InsertDesignAreaAsync(connection, transaction, area, cancellationToken);
        }

        foreach (var blueprint in snapshot.Blueprints)
            await InsertBlueprintAsync(connection, transaction, blueprint, cancellationToken);
        foreach (var provider in snapshot.PrintProviders)
            await InsertPrintProviderAsync(connection, transaction, provider, cancellationToken);
        foreach (var offering in snapshot.BlueprintOfferings)
            await InsertBlueprintOfferingAsync(connection, transaction, offering, cancellationToken);
        foreach (var option in snapshot.OfferingOptions)
            await InsertOfferingOptionAsync(connection, transaction, option, cancellationToken);
        foreach (var value in snapshot.OfferingOptionValues)
            await InsertOfferingOptionValueAsync(connection, transaction, value, cancellationToken);
        foreach (var variant in snapshot.OfferingVariants)
            await InsertOfferingVariantAsync(connection, transaction, variant, cancellationToken);
        foreach (var placeholder in snapshot.OfferingPlaceholders)
            await InsertOfferingPlaceholderAsync(connection, transaction, placeholder, cancellationToken);
        foreach (var template in snapshot.MockupTemplates)
            await InsertMockupTemplateAsync(connection, transaction, template, cancellationToken);
        foreach (var color in snapshot.MockupTemplateColorVariants)
            await InsertMockupTemplateColorAsync(connection, transaction, color, cancellationToken);
        foreach (var revision in snapshot.MockupTemplateRevisions)
            await InsertMockupTemplateRevisionAsync(connection, transaction, revision, cancellationToken);
        foreach (var color in snapshot.MockupTemplateRevisionColors)
            await InsertMockupTemplateRevisionColorAsync(connection, transaction, color, cancellationToken);

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

        foreach (var rejection in snapshot.IdeationRejections)
        {
            await InsertIdeationRejectionAsync(connection, transaction, rejection, cancellationToken);
        }

        foreach (var listing in snapshot.Items)
        {
            await InsertItemAsync(connection, transaction, listing, cancellationToken);
        }

        foreach (var asset in snapshot.Assets)
        {
            await InsertAssetAsync(connection, transaction, asset, cancellationToken);
        }

        foreach (var prompt in snapshot.Prompts)
        {
            await InsertPromptAsync(connection, transaction, prompt, cancellationToken);
        }

        foreach (var listingTag in snapshot.ItemTags)
        {
            await InsertItemTagAsync(connection, transaction, listingTag, cancellationToken);
        }

        foreach (var assetLink in snapshot.AssetLinks)
        {
            await InsertAssetLinkAsync(connection, transaction, assetLink, cancellationToken);
        }

        foreach (var config in snapshot.ItemListingConfigurations)
        {
            await InsertItemListingConfigurationAsync(connection, transaction, config, cancellationToken);
        }

        foreach (var color in snapshot.DesignSelectedColors)
        {
            await InsertDesignSelectedColorAsync(connection, transaction, color, cancellationToken);
        }

        foreach (var row in snapshot.DesignVariantRows)
        {
            await InsertDesignVariantRowAsync(connection, transaction, row, cancellationToken);
        }

        foreach (var rowColor in snapshot.DesignVariantRowColors)
        {
            await InsertDesignVariantRowColorAsync(connection, transaction, rowColor, cancellationToken);
        }

        foreach (var assignment in snapshot.DesignSlotAssignments)
        {
            await InsertDesignSlotAssignmentAsync(connection, transaction, assignment, cancellationToken);
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
        await SqliteDatabaseSchema.EnsureAsync(connection, cancellationToken);

        return new WorkspaceSnapshot(
            await LoadWorkspacesAsync(connection, cancellationToken),
            await LoadStoresAsync(connection, cancellationToken),
            await LoadNichesAsync(connection, cancellationToken),
            await LoadGroupsAsync(connection, cancellationToken),
            await LoadItemsAsync(connection, cancellationToken),
            await LoadAssetsAsync(connection, cancellationToken),
            await LoadPromptsAsync(connection, cancellationToken),
            await LoadTagsAsync(connection, cancellationToken),
            await LoadItemTagsAsync(connection, cancellationToken),
            await LoadAssetLinksAsync(connection, cancellationToken))
        {
            IdeationRejections = await LoadIdeationRejectionsAsync(connection, cancellationToken),
            StoreProducts = await LoadStoreProductsAsync(connection, cancellationToken),
            FulfillmentOfferings = await LoadFulfillmentOfferingsAsync(connection, cancellationToken),
            ProductVariants = await LoadProductVariantsAsync(connection, cancellationToken),
            DesignAreas = await LoadDesignAreasAsync(connection, cancellationToken),
            ItemListingConfigurations = await LoadItemListingConfigurationsAsync(connection, cancellationToken),
            DesignSelectedColors = await LoadDesignSelectedColorsAsync(connection, cancellationToken),
            DesignVariantRows = await LoadDesignVariantRowsAsync(connection, cancellationToken),
            DesignVariantRowColors = await LoadDesignVariantRowColorsAsync(connection, cancellationToken),
            DesignSlotAssignments = await LoadDesignSlotAssignmentsAsync(connection, cancellationToken)
            ,Blueprints = await LoadBlueprintsAsync(connection, cancellationToken)
            ,PrintProviders = await LoadPrintProvidersAsync(connection, cancellationToken)
            ,BlueprintOfferings = await LoadBlueprintOfferingsAsync(connection, cancellationToken)
            ,OfferingOptions = await LoadOfferingOptionsAsync(connection, cancellationToken)
            ,OfferingOptionValues = await LoadOfferingOptionValuesAsync(connection, cancellationToken)
            ,OfferingVariants = await LoadOfferingVariantsAsync(connection, cancellationToken)
            ,OfferingPlaceholders = await LoadOfferingPlaceholdersAsync(connection, cancellationToken)
            ,MockupTemplates = await LoadMockupTemplatesAsync(connection, cancellationToken)
            ,MockupTemplateColorVariants = await LoadMockupTemplateColorsAsync(connection, cancellationToken)
            ,MockupTemplateRevisions = await LoadMockupTemplateRevisionsAsync(connection, cancellationToken)
            ,MockupTemplateRevisionColors = await LoadMockupTemplateRevisionColorsAsync(connection, cancellationToken)
        };
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = useConnectionPooling
        }.ToString();
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, null, "PRAGMA foreign_keys = ON;", cancellationToken);
        return connection;
    }

    internal static async Task EnsureSchemaCoreAsync(
        SqliteConnection connection,
        int currentSchemaVersion,
        CancellationToken cancellationToken)
    {
        var schemaVersion = await ReadPragmaUserVersionAsync(connection, cancellationToken);
        var isFreshDatabase = schemaVersion == 0
            && !await HasUserTablesAsync(connection, cancellationToken);
        if (schemaVersion > currentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Workspace database schema version {schemaVersion} requires a newer FusionCanvas version. Current supported schema version is {currentSchemaVersion}.");
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
                metadata_json TEXT NOT NULL,
                fulfillment_strategy INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS tags (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL,
                color TEXT NULL
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

            CREATE TABLE IF NOT EXISTS items (
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
                item_id TEXT NULL REFERENCES items(id) ON DELETE SET NULL,
                name TEXT NOT NULL,
                description TEXT NULL,
                text TEXT NOT NULL,
                is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS item_tags (
                item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY (item_id, tag_id)
            );

            CREATE TABLE IF NOT EXISTS asset_links (
                asset_id TEXT NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
                entity_kind INTEGER NOT NULL,
                entity_id TEXT NOT NULL,
                PRIMARY KEY (asset_id, entity_kind, entity_id)
            );

            CREATE TABLE IF NOT EXISTS product_blueprints (
                id TEXT PRIMARY KEY,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                external_product_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS fulfillment_offerings (
                id TEXT PRIMARY KEY,
                store_product_id TEXT NOT NULL REFERENCES product_blueprints(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                kind INTEGER NOT NULL,
                provider_name TEXT NULL,
                external_offering_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS product_variants (
                id TEXT PRIMARY KEY,
                fulfillment_offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                options_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS design_areas (
                id TEXT PRIMARY KEY,
                fulfillment_offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                position TEXT NOT NULL,
                decoration_method TEXT NOT NULL,
                width INTEGER NOT NULL,
                height INTEGER NOT NULL,
                variant_ids_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS item_listing_configuration (
                item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                PRIMARY KEY (item_id)
            );

            CREATE TABLE IF NOT EXISTS design_selected_colors (
                item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                color_value TEXT NOT NULL,
                PRIMARY KEY (item_id, color_value)
            );

            CREATE TABLE IF NOT EXISTS design_variant_rows (
                id TEXT PRIMARY KEY,
                item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                is_default INTEGER NOT NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS design_variant_row_colors (
                row_id TEXT NOT NULL REFERENCES design_variant_rows(id) ON DELETE CASCADE,
                color_value TEXT NOT NULL,
                PRIMARY KEY (row_id, color_value)
            );

            CREATE TABLE IF NOT EXISTS design_slot_assignments (
                row_id TEXT NOT NULL REFERENCES design_variant_rows(id) ON DELETE CASCADE,
                design_area_id TEXT NOT NULL REFERENCES design_areas(id) ON DELETE CASCADE,
                asset_id TEXT NULL REFERENCES assets(id) ON DELETE SET NULL,
                PRIMARY KEY (row_id, design_area_id)
            );

            CREATE TABLE IF NOT EXISTS catalog_blueprints (
                id TEXT PRIMARY KEY, store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL, description TEXT NULL, is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS print_providers (
                id TEXT PRIMARY KEY, store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                name TEXT NOT NULL, external_provider_id TEXT NULL, is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS blueprint_offerings (
                id TEXT PRIMARY KEY, blueprint_id TEXT NOT NULL REFERENCES catalog_blueprints(id) ON DELETE CASCADE,
                store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE, name TEXT NOT NULL,
                description TEXT NULL, kind INTEGER NOT NULL, print_provider_id TEXT NULL REFERENCES print_providers(id) ON DELETE RESTRICT,
                provider_network_code TEXT NULL, default_placeholder_id TEXT NULL, external_offering_id TEXT NULL,
                is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS offering_options (
                id TEXT PRIMARY KEY, offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                option_kind INTEGER NOT NULL, name TEXT NOT NULL, sort_order INTEGER NOT NULL, is_archived INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS offering_option_values (
                id TEXT PRIMARY KEY, option_id TEXT NOT NULL REFERENCES offering_options(id) ON DELETE CASCADE,
                offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                value TEXT NOT NULL, sort_order INTEGER NOT NULL, is_archived INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS offering_variants (
                id TEXT PRIMARY KEY, offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                name TEXT NOT NULL, is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS offering_variant_values (
                variant_id TEXT NOT NULL REFERENCES offering_variants(id) ON DELETE CASCADE,
                option_value_id TEXT NOT NULL REFERENCES offering_option_values(id) ON DELETE RESTRICT,
                PRIMARY KEY (variant_id, option_value_id)
            );
            CREATE TABLE IF NOT EXISTS offering_placeholders (
                id TEXT PRIMARY KEY, offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                name TEXT NOT NULL, description TEXT NULL, position TEXT NOT NULL, decoration_method TEXT NOT NULL,
                width INTEGER NOT NULL, height INTEGER NOT NULL, is_archived INTEGER NOT NULL,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS placeholder_variants (
                placeholder_id TEXT NOT NULL REFERENCES offering_placeholders(id) ON DELETE CASCADE,
                variant_id TEXT NOT NULL REFERENCES offering_variants(id) ON DELETE RESTRICT,
                PRIMARY KEY (placeholder_id, variant_id)
            );
            CREATE TABLE IF NOT EXISTS mockup_templates (
                id TEXT PRIMARY KEY, offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                target_placeholder_id TEXT NULL REFERENCES offering_placeholders(id) ON DELETE RESTRICT,
                name TEXT NOT NULL, description TEXT NULL, current_revision INTEGER NOT NULL,
                is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL,
                position_key TEXT NULL, future_asset_state TEXT NULL, metadata_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS mockup_template_colors (
                id TEXT PRIMARY KEY, template_id TEXT NOT NULL REFERENCES mockup_templates(id) ON DELETE CASCADE,
                color_option_value_id TEXT NOT NULL REFERENCES offering_option_values(id) ON DELETE RESTRICT,
                is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, source_asset_id TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_mockup_template_active_color ON mockup_template_colors(template_id, color_option_value_id) WHERE is_archived = 0;
            CREATE TABLE IF NOT EXISTS mockup_template_revisions (
                id TEXT PRIMARY KEY, template_id TEXT NOT NULL REFERENCES mockup_templates(id) ON DELETE RESTRICT,
                revision_number INTEGER NOT NULL, target_placeholder_id TEXT NULL REFERENCES offering_placeholders(id) ON DELETE RESTRICT,
                created_at TEXT NOT NULL, note TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS mockup_template_revision_colors (
                id TEXT PRIMARY KEY, revision_id TEXT NOT NULL REFERENCES mockup_template_revisions(id) ON DELETE RESTRICT,
                color_option_value_id TEXT NOT NULL REFERENCES offering_option_values(id) ON DELETE RESTRICT, source_asset_id TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS snowclones (
                id TEXT PRIMARY KEY,
                phrase TEXT NOT NULL,
                normalized_phrase TEXT NOT NULL UNIQUE,
                guidance TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS snowclone_library_state (
                singleton_id INTEGER PRIMARY KEY CHECK(singleton_id = 1),
                starter_initialized INTEGER NOT NULL
            );
            """;

        await ExecuteAsync(connection, null, sql, cancellationToken);

        // A database can report the current schema version while still missing
        // a column if it was created by an interrupted or pre-release build.
        // Reconcile this required column independently of user_version so the
        // startup loader never queries a column that is absent physically.
        if (!await ColumnExistsAsync(connection, "stores", "fulfillment_strategy", cancellationToken))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE stores ADD COLUMN fulfillment_strategy INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 2)
        {
            await MigrateToVersion2Async(connection, cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 3)
        {
            await MigrateToVersion3Async(connection, cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 4)
        {
            await MigrateToVersion4Async(connection, cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 5)
        {
            await MigrateToVersion5Async(connection, cancellationToken);
        }

        if (schemaVersion < 7)
        {
            await MigrateToVersion7Async(connection, cancellationToken);
        }

        if (schemaVersion < 8)
        {
            await MigrateToVersion8Async(connection, cancellationToken);
        }

        if (schemaVersion < 9)
        {
            await MigrateToVersion9Async(connection, cancellationToken);
        }

        if (schemaVersion < 10)
        {
            await MigrateToVersion10Async(connection, cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 11)
        {
            await MigrateToVersion11Async(connection, cancellationToken);
        }

        if (schemaVersion < 12)
        {
            await MigrateToVersion12Async(connection, cancellationToken);
        }

        if (!isFreshDatabase && schemaVersion < 13)
        {
            await MigrateToVersion13Async(connection, cancellationToken);
        }

        await SetPragmaUserVersionAsync(connection, currentSchemaVersion, cancellationToken);
    }

    private static async Task MigrateToVersion13Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, null, "PRAGMA foreign_keys = OFF;", cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE mockup_templates_v13 (
                    id TEXT PRIMARY KEY, offering_id TEXT NOT NULL REFERENCES blueprint_offerings(id) ON DELETE CASCADE,
                    target_placeholder_id TEXT NULL REFERENCES offering_placeholders(id) ON DELETE RESTRICT,
                    name TEXT NOT NULL, description TEXT NULL, current_revision INTEGER NOT NULL,
                    is_archived INTEGER NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL,
                    position_key TEXT NULL, future_asset_state TEXT NULL, metadata_json TEXT NOT NULL
                );
                INSERT INTO mockup_templates_v13 SELECT * FROM mockup_templates;
                CREATE TABLE mockup_template_revisions_v13 (
                    id TEXT PRIMARY KEY, template_id TEXT NOT NULL REFERENCES mockup_templates_v13(id) ON DELETE RESTRICT,
                    revision_number INTEGER NOT NULL, target_placeholder_id TEXT NULL REFERENCES offering_placeholders(id) ON DELETE RESTRICT,
                    created_at TEXT NOT NULL, note TEXT NULL, provider_mockup_reference TEXT NULL,
                    image_width INTEGER NULL, image_height INTEGER NULL, mapping_x INTEGER NULL, mapping_y INTEGER NULL,
                    mapping_width INTEGER NULL, mapping_height INTEGER NULL
                );
                INSERT INTO mockup_template_revisions_v13 SELECT * FROM mockup_template_revisions;
                DROP TABLE mockup_template_revisions;
                DROP TABLE mockup_templates;
                ALTER TABLE mockup_templates_v13 RENAME TO mockup_templates;
                ALTER TABLE mockup_template_revisions_v13 RENAME TO mockup_template_revisions;
                """, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await ExecuteAsync(connection, null, "PRAGMA foreign_keys = ON;", cancellationToken);
            await VerifyForeignKeyIntegrityAsync(connection, null, cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            await ExecuteAsync(connection, null, "PRAGMA foreign_keys = ON;", cancellationToken);
            throw new InvalidOperationException("The workspace database could not be upgraded from schema version 12 to 13. Restore a backup or use an older FusionCanvas version.", exception);
        }
    }

    private static async Task MigrateToVersion12Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var columns = new (string Table, string Name, string Definition)[]
            {
                ("offering_placeholders", "provider_reference", "TEXT NULL"),
                ("offering_placeholders", "recommended_width_px", "INTEGER NULL"),
                ("offering_placeholders", "recommended_height_px", "INTEGER NULL"),
                ("offering_placeholders", "recommended_dpi", "INTEGER NULL"),
                ("offering_placeholders", "recommended_format", "TEXT NULL"),
                ("offering_placeholders", "recommended_background", "TEXT NULL"),
                ("mockup_template_revisions", "provider_mockup_reference", "TEXT NULL"),
                ("mockup_template_revisions", "image_width", "INTEGER NULL"),
                ("mockup_template_revisions", "image_height", "INTEGER NULL"),
                ("mockup_template_revisions", "mapping_x", "INTEGER NULL"),
                ("mockup_template_revisions", "mapping_y", "INTEGER NULL"),
                ("mockup_template_revisions", "mapping_width", "INTEGER NULL"),
                ("mockup_template_revisions", "mapping_height", "INTEGER NULL")
            };

            foreach (var column in columns)
            {
                if (!await ColumnExistsAsync(connection, column.Table, column.Name, cancellationToken))
                    await ExecuteAsync(connection, transaction, $"ALTER TABLE {column.Table} ADD COLUMN {column.Name} {column.Definition};", cancellationToken);
            }

            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                "The workspace database could not be upgraded from schema version 11 to 12. Restore a backup or use an older FusionCanvas version.", exception);
        }
    }

    private static async Task MigrateToVersion11Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await ColumnExistsAsync(connection, "stores", "fulfillment_strategy", cancellationToken))
            {
                await ExecuteAsync(connection, transaction, "ALTER TABLE stores ADD COLUMN fulfillment_strategy INTEGER NOT NULL DEFAULT 0;", cancellationToken);
            }

        // Existing catalog rows remain available to the compatibility model, while
        // these inserts establish the normalized identity graph for future use.
        await ExecuteAsync(connection, transaction, """
            INSERT OR IGNORE INTO catalog_blueprints (id, store_id, name, description, is_archived, created_at, updated_at, metadata_json)
            SELECT id, store_id, name, description, 0, created_at, updated_at, metadata_json
            FROM product_blueprints;
            """, cancellationToken);

        var providerIds = new Dictionary<(Guid StoreId, string Name), Guid>();
        await foreach (var reader in ReadAsync(connection, "SELECT id, store_product_id, name, description, kind, provider_name, external_offering_id, created_at, updated_at, metadata_json FROM fulfillment_offerings;", cancellationToken, transaction))
        {
            var offeringId = ReadGuid(reader, "id");
            var blueprintId = ReadGuid(reader, "store_product_id");
            var storeId = await ReadSingleGuidInTransactionAsync(connection, "SELECT store_id FROM product_blueprints WHERE id = $id;", cancellationToken, transaction, ("$id", blueprintId.ToString()));
            var kind = ReadInt(reader, "kind") == (int)FulfillmentKind.PrintifyChoiceNetwork
                ? BlueprintOfferingKind.ProviderNetwork
                : BlueprintOfferingKind.FixedPrintProvider;
            Guid? providerId = null;
            if (kind == BlueprintOfferingKind.FixedPrintProvider)
            {
                var providerName = ReadNullableString(reader, "provider_name") ?? "Unspecified Print Provider";
                var key = (storeId, providerName.Trim().ToUpperInvariant());
                if (!providerIds.TryGetValue(key, out var existingProviderId))
                {
                    existingProviderId = Guid.NewGuid();
                    providerIds[key] = existingProviderId;
                    await ExecuteAsync(connection, transaction, "INSERT INTO print_providers (id, store_id, name, external_provider_id, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$store_id,$name,NULL,0,$created_at,$updated_at,'{}');", cancellationToken, ("$id", existingProviderId.ToString()), ("$store_id", storeId.ToString()), ("$name", providerName), ("$created_at", DateTimeOffset.UtcNow.ToString("O")), ("$updated_at", DateTimeOffset.UtcNow.ToString("O")));
                }
                providerId = existingProviderId;
            }

            await ExecuteAsync(connection, transaction, "INSERT OR IGNORE INTO blueprint_offerings (id, blueprint_id, store_id, name, description, kind, print_provider_id, provider_network_code, default_placeholder_id, external_offering_id, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$blueprint_id,$store_id,$name,$description,$kind,$print_provider_id,$provider_network_code,NULL,$external_offering_id,0,$created_at,$updated_at,$metadata_json);", cancellationToken,
                ("$id", offeringId.ToString()), ("$blueprint_id", blueprintId.ToString()), ("$store_id", storeId.ToString()), ("$name", ReadString(reader, "name")), ("$description", ReadNullableString(reader, "description")), ("$kind", (int)kind), ("$print_provider_id", providerId?.ToString()), ("$provider_network_code", kind == BlueprintOfferingKind.ProviderNetwork ? "printify-choice" : null), ("$external_offering_id", ReadNullableString(reader, "external_offering_id")), ("$created_at", ReadDate(reader, "created_at").ToString("O")), ("$updated_at", ReadDate(reader, "updated_at").ToString("O")), ("$metadata_json", ReadString(reader, "metadata_json")));
        }

        var optionIds = new Dictionary<(Guid OfferingId, OptionKind Kind), Guid>();
        var optionValueIds = new Dictionary<(Guid OptionId, string Value), Guid>();
        await foreach (var reader in ReadAsync(connection, "SELECT id, fulfillment_offering_id, options_json, created_at, updated_at FROM product_variants;", cancellationToken, transaction))
        {
            var variantId = ReadGuid(reader, "id");
            var offeringId = ReadGuid(reader, "fulfillment_offering_id");
            var optionValues = new List<Guid>();
            var displayValues = new List<string>();
            using var document = JsonDocument.Parse(ReadString(reader, "options_json"));
            foreach (var optionElement in document.RootElement.EnumerateArray())
            {
                var name = optionElement.GetProperty("Name").GetString() ?? "Other";
                var valueText = optionElement.GetProperty("Value").GetString() ?? string.Empty;
                var kind = name.Equals("Color", StringComparison.OrdinalIgnoreCase) ? OptionKind.Color : name.Equals("Size", StringComparison.OrdinalIgnoreCase) ? OptionKind.Size : OptionKind.Other;
                if (!optionIds.TryGetValue((offeringId, kind), out var optionId))
                {
                    optionId = Guid.NewGuid();
                    optionIds[(offeringId, kind)] = optionId;
                    await ExecuteAsync(connection, transaction, "INSERT INTO offering_options (id, offering_id, option_kind, name, sort_order, is_archived) VALUES ($id,$offering_id,$option_kind,$name,$sort_order,0);", cancellationToken, ("$id", optionId.ToString()), ("$offering_id", offeringId.ToString()), ("$option_kind", (int)kind), ("$name", name.Trim()), ("$sort_order", (int)kind));
                }

                var valueKey = (optionId, valueText.Trim().ToUpperInvariant());
                if (!optionValueIds.TryGetValue(valueKey, out var optionValueId))
                {
                    optionValueId = Guid.NewGuid();
                    optionValueIds[valueKey] = optionValueId;
                    await ExecuteAsync(connection, transaction, "INSERT INTO offering_option_values (id, option_id, offering_id, value, sort_order, is_archived) VALUES ($id,$option_id,$offering_id,$value,0,0);", cancellationToken, ("$id", optionValueId.ToString()), ("$option_id", optionId.ToString()), ("$offering_id", offeringId.ToString()), ("$value", valueText.Trim()));
                }

                optionValues.Add(optionValueId);
                displayValues.Add(valueText.Trim());
            }

            await ExecuteAsync(connection, transaction, "INSERT OR IGNORE INTO offering_variants (id, offering_id, name, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$offering_id,$name,0,$created_at,$updated_at,'{}');", cancellationToken, ("$id", variantId.ToString()), ("$offering_id", offeringId.ToString()), ("$name", string.Join(" / ", displayValues)), ("$created_at", ReadDate(reader, "created_at").ToString("O")), ("$updated_at", ReadDate(reader, "updated_at").ToString("O")));
            foreach (var optionValueId in optionValues)
                await ExecuteAsync(connection, transaction, "INSERT OR IGNORE INTO offering_variant_values (variant_id, option_value_id) VALUES ($variant_id,$option_value_id);", cancellationToken, ("$variant_id", variantId.ToString()), ("$option_value_id", optionValueId.ToString()));
        }

        await foreach (var reader in ReadAsync(connection, "SELECT id, fulfillment_offering_id, name, description, position, decoration_method, width, height, variant_ids_json, created_at, updated_at, metadata_json FROM design_areas;", cancellationToken, transaction))
        {
            var placeholderId = ReadGuid(reader, "id");
            var offeringId = ReadGuid(reader, "fulfillment_offering_id");
            await ExecuteAsync(connection, transaction, "INSERT OR IGNORE INTO offering_placeholders (id, offering_id, name, description, position, decoration_method, width, height, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$offering_id,$name,$description,$position,$decoration_method,$width,$height,0,$created_at,$updated_at,$metadata_json);", cancellationToken, ("$id", placeholderId.ToString()), ("$offering_id", offeringId.ToString()), ("$name", ReadString(reader, "name")), ("$description", ReadNullableString(reader, "description")), ("$position", ReadString(reader, "position")), ("$decoration_method", ReadString(reader, "decoration_method")), ("$width", ReadInt(reader, "width")), ("$height", ReadInt(reader, "height")), ("$created_at", ReadDate(reader, "created_at").ToString("O")), ("$updated_at", ReadDate(reader, "updated_at").ToString("O")), ("$metadata_json", ReadString(reader, "metadata_json")));
            var legacyVariantIds = JsonSerializer.Deserialize<Guid[]>(ReadString(reader, "variant_ids_json")) ?? [];
            if (legacyVariantIds.Length == 0)
            {
                await foreach (var variant in ReadAsync(connection, $"SELECT id FROM offering_variants WHERE offering_id = '{offeringId}';", cancellationToken, transaction))
                    legacyVariantIds = [.. legacyVariantIds, ReadGuid(variant, "id")];
            }
            foreach (var variantId in legacyVariantIds)
                await ExecuteAsync(connection, transaction, "INSERT OR IGNORE INTO placeholder_variants (placeholder_id, variant_id) VALUES ($placeholder_id,$variant_id);", cancellationToken, ("$placeholder_id", placeholderId.ToString()), ("$variant_id", variantId.ToString()));
        }

            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                "The workspace database could not be upgraded from schema version 10 to 11. Restore a backup or use an older FusionCanvas version.", exception);
        }
    }

    private static Task<Guid> ReadSingleGuidAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters) =>
        ReadSingleGuidInTransactionAsync(connection, sql, cancellationToken, null, parameters);

    private static async Task<Guid> ReadSingleGuidInTransactionAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken, System.Data.Common.DbTransaction? transaction, params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = sql;
        foreach (var (name, parameterValue) in parameters)
            command.Parameters.AddWithValue(name, parameterValue ?? DBNull.Value);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is string textValue && Guid.TryParse(textValue, out var id) ? id : throw new InvalidOperationException("Legacy catalog ownership reference is invalid.");
    }

    private static async Task<bool> HasUserTablesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite_%'
            );
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task MigrateToVersion7Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE IF NOT EXISTS ideation_rejections (
                    id TEXT PRIMARY KEY,
                    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                    niche_id TEXT NOT NULL REFERENCES niches(id) ON DELETE CASCADE,
                    group_id TEXT NULL REFERENCES groups(id) ON DELETE SET NULL,
                    text TEXT NOT NULL,
                    reason TEXT NULL,
                    mode INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NULL
                );
                """, cancellationToken);
            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task MigrateToVersion8Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, "ideation_rejections", "updated_at", cancellationToken).ConfigureAwait(false))
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await ExecuteAsync(connection, transaction, "ALTER TABLE ideation_rejections ADD COLUMN updated_at TEXT NULL;", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static async Task MigrateToVersion9Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE IF NOT EXISTS product_blueprints (
                    id TEXT PRIMARY KEY,
                    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    external_product_id TEXT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS fulfillment_offerings (
                    id TEXT PRIMARY KEY,
                    store_product_id TEXT NOT NULL REFERENCES product_blueprints(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    kind INTEGER NOT NULL,
                    provider_name TEXT NULL,
                    external_offering_id TEXT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS product_variants (
                    id TEXT PRIMARY KEY,
                    fulfillment_offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                    options_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS design_areas (
                    id TEXT PRIMARY KEY,
                    fulfillment_offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    position TEXT NOT NULL,
                    decoration_method TEXT NOT NULL,
                    width INTEGER NOT NULL,
                    height INTEGER NOT NULL,
                    variant_ids_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS item_design_area_targets (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    design_area_id TEXT NOT NULL REFERENCES design_areas(id) ON DELETE CASCADE,
                    PRIMARY KEY (item_id, design_area_id)
                );
                """, cancellationToken);
            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task MigrateToVersion10Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE IF NOT EXISTS item_listing_configuration (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    offering_id TEXT NOT NULL REFERENCES fulfillment_offerings(id) ON DELETE CASCADE,
                    PRIMARY KEY (item_id)
                );

                CREATE TABLE IF NOT EXISTS design_selected_colors (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    color_value TEXT NOT NULL,
                    PRIMARY KEY (item_id, color_value)
                );

                CREATE TABLE IF NOT EXISTS design_variant_rows (
                    id TEXT PRIMARY KEY,
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    is_default INTEGER NOT NULL,
                    sort_order INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS design_variant_row_colors (
                    row_id TEXT NOT NULL REFERENCES design_variant_rows(id) ON DELETE CASCADE,
                    color_value TEXT NOT NULL,
                    PRIMARY KEY (row_id, color_value)
                );

                CREATE TABLE IF NOT EXISTS design_slot_assignments (
                    row_id TEXT NOT NULL REFERENCES design_variant_rows(id) ON DELETE CASCADE,
                    design_area_id TEXT NOT NULL REFERENCES design_areas(id) ON DELETE CASCADE,
                    asset_id TEXT NULL REFERENCES assets(id) ON DELETE SET NULL,
                    PRIMARY KEY (row_id, design_area_id)
                );

                DROP TABLE IF EXISTS item_design_area_targets;
                """, cancellationToken);
            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
        if (!await ColumnExistsAsync(connection, "tags", "color", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE tags ADD COLUMN color TEXT NULL;", cancellationToken);
        }

        if (!await TableExistsAsync(connection, "listings", cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        if (!await ColumnExistsAsync(connection, "listings", "workflow_stage", cancellationToken).ConfigureAwait(false))
        {
            await ExecuteAsync(connection, null, "ALTER TABLE listings ADD COLUMN workflow_stage INTEGER NOT NULL DEFAULT 0;", cancellationToken);
        }

        // Old ItemStatus ints: Active=0, Draft=1, Ready=2, Published=3, Archived=4.
        // New ItemStatus ints: Draft=0, Published=1, Paused=2, Rejected=3.
        // WorkflowStage ints: Idea=0, Concept=1, Design=2, Item=3.
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

    private static async Task MigrateToVersion5Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "listings", cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE IF NOT EXISTS items (
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
                CREATE TABLE IF NOT EXISTS item_tags (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                    PRIMARY KEY (item_id, tag_id)
                );
                CREATE TABLE prompts_v5 (
                    id TEXT PRIMARY KEY,
                    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                    item_id TEXT NULL REFERENCES items(id) ON DELETE SET NULL,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    text TEXT NOT NULL,
                    is_archived INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );
                """, cancellationToken);

            await ExecuteAsync(connection, transaction, """
                INSERT INTO items (id, store_id, niche_id, group_id, name, description, status, workflow_stage, is_archived, created_at, updated_at, metadata_json)
                SELECT id, store_id, niche_id, group_id, name, description, status, workflow_stage, is_archived, created_at, updated_at, metadata_json FROM listings;
                """, cancellationToken);

            await ExecuteAsync(connection, transaction, """
                INSERT INTO item_tags (item_id, tag_id)
                SELECT listing_id, tag_id FROM listing_tags;
                """, cancellationToken);

            await ExecuteAsync(connection, transaction, """
                INSERT INTO prompts_v5 (id, store_id, item_id, name, description, text, is_archived, created_at, updated_at, metadata_json)
                SELECT id, store_id, listing_id, name, description, text, is_archived, created_at, updated_at, metadata_json FROM prompts;
                """, cancellationToken);

            await VerifyRowCountEqualAsync(connection, transaction, "listings", "items", cancellationToken);
            await VerifyRowCountEqualAsync(connection, transaction, "listing_tags", "item_tags", cancellationToken);
            await VerifyRowCountEqualAsync(connection, transaction, "prompts", "prompts_v5", cancellationToken);
            await VerifyForeignKeyIntegrityAsync(connection, transaction, cancellationToken);

            await ExecuteAsync(connection, transaction, "DROP TABLE listing_tags;", cancellationToken);
            await ExecuteAsync(connection, transaction, "DROP TABLE prompts;", cancellationToken);
            await ExecuteAsync(connection, transaction, "DROP TABLE listings;", cancellationToken);
            await ExecuteAsync(connection, transaction, "ALTER TABLE prompts_v5 RENAME TO prompts;", cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                "The workspace database could not be upgraded from schema version 4 to 5. Restore a backup or use an older FusionCanvas version.");
        }
    }

    private static async Task MigrateToVersion6Async(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, transaction, """
            CREATE TABLE IF NOT EXISTS snowclones (
                id TEXT PRIMARY KEY,
                phrase TEXT NOT NULL,
                normalized_phrase TEXT NOT NULL UNIQUE,
                guidance TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS snowclone_library_state (
                singleton_id INTEGER PRIMARY KEY CHECK(singleton_id = 1),
                starter_initialized INTEGER NOT NULL
            );

            INSERT OR IGNORE INTO snowclone_library_state (singleton_id, starter_initialized)
            VALUES (1, 0);
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task VerifyRowCountEqualAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, string sourceTable, string destinationTable, CancellationToken cancellationToken)
    {
        var sourceCount = await ReadScalarIntAsync(connection, transaction, $"SELECT COUNT(*) FROM {sourceTable};", cancellationToken);
        var destinationCount = await ReadScalarIntAsync(connection, transaction, $"SELECT COUNT(*) FROM {destinationTable};", cancellationToken);
        if (sourceCount != destinationCount)
        {
            throw new InvalidOperationException($"Migration row-count verification failed: {sourceTable} has {sourceCount} rows but {destinationTable} has {destinationCount} rows.");
        }
    }

    private static async Task VerifyForeignKeyIntegrityAsync(SqliteConnection connection, System.Data.Common.DbTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Migration foreign-key verification failed; the database has referential integrity violations.");
        }
    }

    private static async Task<int> ReadScalarIntAsync(SqliteConnection connection, System.Data.Common.DbTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
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
            INSERT INTO stores (id, workspace_id, default_niche_id, name, description, is_archived, created_at, updated_at, metadata_json, fulfillment_strategy)
            VALUES ($id, $workspace_id, $default_niche_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json, $fulfillment_strategy);
            """, cancellationToken, [.. CommonParameters(store), ("$workspace_id", store.WorkspaceId.ToString()), ("$default_niche_id", store.DefaultNicheId?.ToString()), ("$fulfillment_strategy", (int)store.FulfillmentStrategy)]);

    private static Task InsertTagAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Tag tag, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO tags (id, store_id, name, description, is_archived, created_at, updated_at, metadata_json, color)
            VALUES ($id, $store_id, $name, $description, $is_archived, $created_at, $updated_at, $metadata_json, $color);
            """, cancellationToken, [.. CommonParameters(tag), ("$store_id", tag.StoreId.ToString()), ("$color", (object?)tag.Color ?? DBNull.Value)]);

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

    private static Task InsertIdeationRejectionAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        IdeationRejection rejection,
        CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO ideation_rejections (id, store_id, niche_id, group_id, text, reason, mode, created_at, updated_at)
            VALUES ($id, $store_id, $niche_id, $group_id, $text, $reason, $mode, $created_at, $updated_at);
            """, cancellationToken,
            ("$id", rejection.Id.ToString()),
            ("$store_id", rejection.StoreId.ToString()),
            ("$niche_id", rejection.NicheId.ToString()),
            ("$group_id", rejection.GroupId?.ToString()),
            ("$text", rejection.Text),
            ("$reason", rejection.Reason),
            ("$mode", (int)rejection.Mode),
            ("$created_at", rejection.CreatedAt.ToString("O")),
            ("$updated_at", rejection.UpdatedAt?.ToString("O")));

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

    private static Task InsertItemAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Item listing, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO items (id, store_id, niche_id, group_id, name, description, status, workflow_stage, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $niche_id, $group_id, $name, $description, $status, $workflow_stage, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(listing), ("$store_id", listing.StoreId.ToString()), ("$niche_id", listing.NicheId?.ToString()), ("$group_id", listing.GroupId?.ToString()), ("$status", (int)listing.Status), ("$workflow_stage", (int)listing.Stage)]);

    private static Task InsertAssetAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Asset asset, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO assets (id, store_id, name, description, kind, workspace_relative_path, original_source_path, is_missing, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $name, $description, $kind, $workspace_relative_path, $original_source_path, $is_missing, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(asset), ("$store_id", asset.StoreId.ToString()), ("$kind", (int)asset.Kind), ("$workspace_relative_path", asset.WorkspaceRelativePath), ("$original_source_path", asset.OriginalSourcePath), ("$is_missing", asset.IsMissing ? 1 : 0)]);

    private static Task InsertPromptAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, Prompt prompt, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO prompts (id, store_id, item_id, name, description, text, is_archived, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $item_id, $name, $description, $text, $is_archived, $created_at, $updated_at, $metadata_json);
            """, cancellationToken, [.. CommonParameters(prompt), ("$store_id", prompt.StoreId.ToString()), ("$item_id", prompt.ItemId?.ToString()), ("$text", prompt.Text)]);

    private static Task InsertItemTagAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, ItemTag listingTag, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO item_tags (item_id, tag_id) VALUES ($item_id, $tag_id);", cancellationToken, ("$item_id", listingTag.ItemId.ToString()), ("$tag_id", listingTag.TagId.ToString()));

    private static Task InsertAssetLinkAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, AssetLink assetLink, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO asset_links (asset_id, entity_kind, entity_id) VALUES ($asset_id, $entity_kind, $entity_id);", cancellationToken, ("$asset_id", assetLink.AssetId.ToString()), ("$entity_kind", (int)assetLink.EntityKind), ("$entity_id", assetLink.EntityId.ToString()));

    private static Task InsertStoreProductAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, StoreProduct product, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO product_blueprints (id, store_id, name, description, external_product_id, created_at, updated_at, metadata_json)
            VALUES ($id, $store_id, $name, $description, $external_product_id, $created_at, $updated_at, $metadata_json);
            """, cancellationToken,
            ("$id", product.Id.ToString()),
            ("$store_id", product.StoreId.ToString()),
            ("$name", product.Name),
            ("$description", product.Description),
            ("$external_product_id", product.ExternalProductId),
            ("$created_at", product.CreatedAt.ToString("O")),
            ("$updated_at", product.UpdatedAt.ToString("O")),
            ("$metadata_json", product.MetadataJson));

    private static Task InsertFulfillmentOfferingAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, FulfillmentOffering offering, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO fulfillment_offerings (id, store_product_id, name, description, kind, provider_name, external_offering_id, created_at, updated_at, metadata_json)
            VALUES ($id, $store_product_id, $name, $description, $kind, $provider_name, $external_offering_id, $created_at, $updated_at, $metadata_json);
            """, cancellationToken,
            ("$id", offering.Id.ToString()),
            ("$store_product_id", offering.StoreProductId.ToString()),
            ("$name", offering.Name),
            ("$description", offering.Description),
            ("$kind", (int)offering.Kind),
            ("$provider_name", offering.ProviderName),
            ("$external_offering_id", offering.ExternalOfferingId),
            ("$created_at", offering.CreatedAt.ToString("O")),
            ("$updated_at", offering.UpdatedAt.ToString("O")),
            ("$metadata_json", offering.MetadataJson));

    private static Task InsertProductVariantAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, ProductVariant variant, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO product_variants (id, fulfillment_offering_id, options_json, created_at, updated_at)
            VALUES ($id, $fulfillment_offering_id, $options_json, $created_at, $updated_at);
            """, cancellationToken,
            ("$id", variant.Id.ToString()),
            ("$fulfillment_offering_id", variant.FulfillmentOfferingId.ToString()),
            ("$options_json", JsonSerializer.Serialize(variant.Options)),
            ("$created_at", variant.CreatedAt.ToString("O")),
            ("$updated_at", variant.UpdatedAt.ToString("O")));

    private static Task InsertDesignAreaAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, DesignArea area, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, """
            INSERT INTO design_areas (id, fulfillment_offering_id, name, description, position, decoration_method, width, height, variant_ids_json, created_at, updated_at, metadata_json)
            VALUES ($id, $fulfillment_offering_id, $name, $description, $position, $decoration_method, $width, $height, $variant_ids_json, $created_at, $updated_at, $metadata_json);
            """, cancellationToken,
            ("$id", area.Id.ToString()),
            ("$fulfillment_offering_id", area.FulfillmentOfferingId.ToString()),
            ("$name", area.Name),
            ("$description", area.Description),
            ("$position", area.Position),
            ("$decoration_method", area.DecorationMethod),
            ("$width", area.Width),
            ("$height", area.Height),
            ("$variant_ids_json", JsonSerializer.Serialize(area.VariantIds)),
            ("$created_at", area.CreatedAt.ToString("O")),
            ("$updated_at", area.UpdatedAt.ToString("O")),
            ("$metadata_json", area.MetadataJson));

    private static Task InsertBlueprintAsync(SqliteConnection c, System.Data.Common.DbTransaction t, Blueprint value, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO catalog_blueprints (id, store_id, name, description, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$store_id,$name,$description,$is_archived,$created_at,$updated_at,$metadata_json);", ct, ("$id", value.Id.ToString()), ("$store_id", value.StoreId.ToString()), ("$name", value.Name), ("$description", value.Description), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$metadata_json", value.MetadataJson));

    private static Task InsertPrintProviderAsync(SqliteConnection c, System.Data.Common.DbTransaction t, PrintProvider value, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO print_providers (id, store_id, name, external_provider_id, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$store_id,$name,$external_provider_id,$is_archived,$created_at,$updated_at,$metadata_json);", ct, ("$id", value.Id.ToString()), ("$store_id", value.StoreId.ToString()), ("$name", value.Name), ("$external_provider_id", value.ExternalProviderId), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$metadata_json", value.MetadataJson));

    private static Task InsertBlueprintOfferingAsync(SqliteConnection c, System.Data.Common.DbTransaction t, BlueprintOffering value, CancellationToken ct) =>
        ExecuteAsync(c, t, "INSERT INTO blueprint_offerings (id, blueprint_id, store_id, name, description, kind, print_provider_id, provider_network_code, default_placeholder_id, external_offering_id, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$blueprint_id,$store_id,$name,$description,$kind,$print_provider_id,$provider_network_code,$default_placeholder_id,$external_offering_id,$is_archived,$created_at,$updated_at,$metadata_json);", ct, ("$id", value.Id.ToString()), ("$blueprint_id", value.BlueprintId.ToString()), ("$store_id", value.StoreId.ToString()), ("$name", value.Name), ("$description", value.Description), ("$kind", (int)value.Kind), ("$print_provider_id", value.PrintProviderId?.ToString()), ("$provider_network_code", value.ProviderNetworkCode), ("$default_placeholder_id", value.DefaultPlaceholderId?.ToString()), ("$external_offering_id", value.ExternalOfferingId), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$metadata_json", value.MetadataJson));

    private static Task InsertOfferingOptionAsync(SqliteConnection c, System.Data.Common.DbTransaction t, OfferingOption value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO offering_options (id, offering_id, option_kind, name, sort_order, is_archived) VALUES ($id,$offering_id,$option_kind,$name,$sort_order,$is_archived);", ct, ("$id", value.Id.ToString()), ("$offering_id", value.OfferingId.ToString()), ("$option_kind", (int)value.OptionKind), ("$name", value.Name), ("$sort_order", value.SortOrder), ("$is_archived", value.IsArchived ? 1 : 0));
    private static Task InsertOfferingOptionValueAsync(SqliteConnection c, System.Data.Common.DbTransaction t, OfferingOptionValue value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO offering_option_values (id, option_id, offering_id, value, sort_order, is_archived) VALUES ($id,$option_id,$offering_id,$value,$sort_order,$is_archived);", ct, ("$id", value.Id.ToString()), ("$option_id", value.OptionId.ToString()), ("$offering_id", value.OfferingId.ToString()), ("$value", value.Value), ("$sort_order", value.SortOrder), ("$is_archived", value.IsArchived ? 1 : 0));

    private static Task InsertOfferingVariantAsync(SqliteConnection c, System.Data.Common.DbTransaction t, OfferingVariant value, CancellationToken ct) => InsertOfferingVariantCoreAsync(c, t, value, ct);
    private static async Task InsertOfferingVariantCoreAsync(SqliteConnection c, System.Data.Common.DbTransaction t, OfferingVariant value, CancellationToken ct)
    {
        await ExecuteAsync(c, t, "INSERT INTO offering_variants (id, offering_id, name, is_archived, created_at, updated_at, metadata_json) VALUES ($id,$offering_id,$name,$is_archived,$created_at,$updated_at,$metadata_json);", ct, ("$id", value.Id.ToString()), ("$offering_id", value.OfferingId.ToString()), ("$name", value.Name), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$metadata_json", value.MetadataJson));
        foreach (var optionValueId in value.OptionValueIds)
            await ExecuteAsync(c, t, "INSERT INTO offering_variant_values (variant_id, option_value_id) VALUES ($variant_id,$option_value_id);", ct, ("$variant_id", value.Id.ToString()), ("$option_value_id", optionValueId.ToString()));
    }

    private static async Task InsertOfferingPlaceholderAsync(SqliteConnection c, System.Data.Common.DbTransaction t, OfferingPlaceholder value, CancellationToken ct)
    {
        await ExecuteAsync(c, t, "INSERT INTO offering_placeholders (id, offering_id, name, description, position, decoration_method, width, height, is_archived, created_at, updated_at, metadata_json, provider_reference, recommended_width_px, recommended_height_px, recommended_dpi, recommended_format, recommended_background) VALUES ($id,$offering_id,$name,$description,$position,$decoration_method,$width,$height,$is_archived,$created_at,$updated_at,$metadata_json,$provider_reference,$recommended_width_px,$recommended_height_px,$recommended_dpi,$recommended_format,$recommended_background);", ct, ("$id", value.Id.ToString()), ("$offering_id", value.OfferingId.ToString()), ("$name", value.Name), ("$description", value.Description), ("$position", value.Position), ("$decoration_method", value.DecorationMethod), ("$width", value.Width), ("$height", value.Height), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$metadata_json", value.MetadataJson), ("$provider_reference", value.ProviderReference), ("$recommended_width_px", value.ArtworkGuidance?.RecommendedWidthPixels), ("$recommended_height_px", value.ArtworkGuidance?.RecommendedHeightPixels), ("$recommended_dpi", value.ArtworkGuidance?.DotsPerInch), ("$recommended_format", value.ArtworkGuidance?.FileFormat), ("$recommended_background", value.ArtworkGuidance?.Background));
        foreach (var variantId in value.VariantIds)
            await ExecuteAsync(c, t, "INSERT INTO placeholder_variants (placeholder_id, variant_id) VALUES ($placeholder_id,$variant_id);", ct, ("$placeholder_id", value.Id.ToString()), ("$variant_id", variantId.ToString()));
    }

    private static Task InsertMockupTemplateAsync(SqliteConnection c, System.Data.Common.DbTransaction t, MockupTemplate value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO mockup_templates (id, offering_id, target_placeholder_id, name, description, current_revision, is_archived, created_at, updated_at, position_key, future_asset_state, metadata_json) VALUES ($id,$offering_id,$target_placeholder_id,$name,$description,$current_revision,$is_archived,$created_at,$updated_at,$position_key,$future_asset_state,$metadata_json);", ct, ("$id", value.Id.ToString()), ("$offering_id", value.BlueprintOfferingId.ToString()), ("$target_placeholder_id", value.TargetPlaceholderId?.ToString()), ("$name", value.Name), ("$description", value.Description), ("$current_revision", value.CurrentRevision), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$position_key", value.PositionKey), ("$future_asset_state", value.FutureAssetState), ("$metadata_json", value.MetadataJson));
    private static Task InsertMockupTemplateColorAsync(SqliteConnection c, System.Data.Common.DbTransaction t, MockupTemplateColorVariant value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO mockup_template_colors (id, template_id, color_option_value_id, is_archived, created_at, updated_at, source_asset_id) VALUES ($id,$template_id,$color_option_value_id,$is_archived,$created_at,$updated_at,$source_asset_id);", ct, ("$id", value.Id.ToString()), ("$template_id", value.MockupTemplateId.ToString()), ("$color_option_value_id", value.ColorOptionValueId.ToString()), ("$is_archived", value.IsArchived ? 1 : 0), ("$created_at", value.CreatedAt.ToString("O")), ("$updated_at", value.UpdatedAt.ToString("O")), ("$source_asset_id", value.SourceAssetId?.ToString()));
    private static Task InsertMockupTemplateRevisionAsync(SqliteConnection c, System.Data.Common.DbTransaction t, MockupTemplateRevision value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO mockup_template_revisions (id, template_id, revision_number, target_placeholder_id, created_at, note, provider_mockup_reference, image_width, image_height, mapping_x, mapping_y, mapping_width, mapping_height) VALUES ($id,$template_id,$revision_number,$target_placeholder_id,$created_at,$note,$provider_mockup_reference,$image_width,$image_height,$mapping_x,$mapping_y,$mapping_width,$mapping_height);", ct, ("$id", value.Id.ToString()), ("$template_id", value.MockupTemplateId.ToString()), ("$revision_number", value.RevisionNumber), ("$target_placeholder_id", value.TargetPlaceholderId?.ToString()), ("$created_at", value.CreatedAt.ToString("O")), ("$note", value.Note), ("$provider_mockup_reference", value.ProviderMockupReference), ("$image_width", value.ImageMapping?.ImageWidth), ("$image_height", value.ImageMapping?.ImageHeight), ("$mapping_x", value.ImageMapping?.X), ("$mapping_y", value.ImageMapping?.Y), ("$mapping_width", value.ImageMapping?.Width), ("$mapping_height", value.ImageMapping?.Height));
    private static Task InsertMockupTemplateRevisionColorAsync(SqliteConnection c, System.Data.Common.DbTransaction t, MockupTemplateRevisionColor value, CancellationToken ct) => ExecuteAsync(c, t, "INSERT INTO mockup_template_revision_colors (id, revision_id, color_option_value_id, source_asset_id) VALUES ($id,$revision_id,$color_option_value_id,$source_asset_id);", ct, ("$id", value.Id.ToString()), ("$revision_id", value.RevisionId.ToString()), ("$color_option_value_id", value.ColorOptionValueId.ToString()), ("$source_asset_id", value.SourceAssetId?.ToString()));

    private static Task InsertItemListingConfigurationAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, ItemListingConfiguration config, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO item_listing_configuration (item_id, offering_id) VALUES ($item_id, $offering_id);", cancellationToken, ("$item_id", config.ItemId.ToString()), ("$offering_id", config.OfferingId.ToString()));

    private static Task InsertDesignSelectedColorAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, DesignSelectedColor color, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO design_selected_colors (item_id, color_value) VALUES ($item_id, $color_value);", cancellationToken, ("$item_id", color.ItemId.ToString()), ("$color_value", color.ColorValue));

    private static Task InsertDesignVariantRowAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, DesignVariantRow row, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO design_variant_rows (id, item_id, is_default, sort_order) VALUES ($id, $item_id, $is_default, $sort_order);", cancellationToken, ("$id", row.Id.ToString()), ("$item_id", row.ItemId.ToString()), ("$is_default", row.IsDefault ? 1 : 0), ("$sort_order", row.SortOrder));

    private static Task InsertDesignVariantRowColorAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, DesignVariantRowColor rowColor, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO design_variant_row_colors (row_id, color_value) VALUES ($row_id, $color_value);", cancellationToken, ("$row_id", rowColor.RowId.ToString()), ("$color_value", rowColor.ColorValue));

    private static Task InsertDesignSlotAssignmentAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, DesignSlotAssignment assignment, CancellationToken cancellationToken) =>
        ExecuteAsync(connection, transaction, "INSERT INTO design_slot_assignments (row_id, design_area_id, asset_id) VALUES ($row_id, $design_area_id, $asset_id);", cancellationToken, ("$row_id", assignment.RowId.ToString()), ("$design_area_id", assignment.DesignAreaId.ToString()), ("$asset_id", assignment.AssetId?.ToString()));

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
            stores.Add(new Store(ReadGuid(reader, "id"), ReadGuid(reader, "workspace_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json"), ReadNullableGuid(reader, "default_niche_id"), (FulfillmentStrategy)ReadInt(reader, "fulfillment_strategy")));
        }

        return stores;
    }

    private static async Task<IReadOnlyList<Blueprint>> LoadBlueprintsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<Blueprint>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM catalog_blueprints ORDER BY name;", ct))
            result.Add(new Blueprint(ReadGuid(r, "id"), ReadGuid(r, "store_id"), ReadString(r, "name"), ReadNullableString(r, "description"), ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadString(r, "metadata_json")));
        return result;
    }

    private static async Task<IReadOnlyList<PrintProvider>> LoadPrintProvidersAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<PrintProvider>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM print_providers ORDER BY name;", ct))
            result.Add(new PrintProvider(ReadGuid(r, "id"), ReadGuid(r, "store_id"), ReadString(r, "name"), ReadNullableString(r, "external_provider_id"), ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadString(r, "metadata_json")));
        return result;
    }

    private static async Task<IReadOnlyList<BlueprintOffering>> LoadBlueprintOfferingsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<BlueprintOffering>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM blueprint_offerings ORDER BY name;", ct))
            result.Add(new BlueprintOffering(ReadGuid(r, "id"), ReadGuid(r, "blueprint_id"), ReadGuid(r, "store_id"), ReadString(r, "name"), ReadNullableString(r, "description"), (BlueprintOfferingKind)ReadInt(r, "kind"), ReadNullableGuid(r, "print_provider_id"), ReadNullableString(r, "provider_network_code"), ReadNullableGuid(r, "default_placeholder_id"), ReadNullableString(r, "external_offering_id"), ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadString(r, "metadata_json")));
        return result;
    }

    private static async Task<IReadOnlyList<OfferingOption>> LoadOfferingOptionsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<OfferingOption>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM offering_options ORDER BY sort_order;", ct))
            result.Add(new OfferingOption(ReadGuid(r, "id"), ReadGuid(r, "offering_id"), (OptionKind)ReadInt(r, "option_kind"), ReadString(r, "name"), ReadInt(r, "sort_order"), ReadBool(r, "is_archived")));
        return result;
    }

    private static async Task<IReadOnlyList<OfferingOptionValue>> LoadOfferingOptionValuesAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<OfferingOptionValue>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM offering_option_values ORDER BY sort_order;", ct))
            result.Add(new OfferingOptionValue(ReadGuid(r, "id"), ReadGuid(r, "option_id"), ReadGuid(r, "offering_id"), ReadString(r, "value"), ReadInt(r, "sort_order"), ReadBool(r, "is_archived")));
        return result;
    }

    private static async Task<IReadOnlyList<OfferingVariant>> LoadOfferingVariantsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<OfferingVariant>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM offering_variants ORDER BY created_at;", ct))
        {
            var variantId = ReadGuid(r, "id");
            var ids = new List<Guid>();
            await foreach (var membership in ReadAsync(c, $"""
                SELECT membership.option_value_id
                FROM offering_variant_values AS membership
                INNER JOIN offering_option_values AS value ON value.id = membership.option_value_id
                INNER JOIN offering_options AS option ON option.id = value.option_id
                WHERE membership.variant_id = '{variantId}'
                ORDER BY option.sort_order, value.sort_order, value.id;
                """, ct))
                ids.Add(ReadGuid(membership, "option_value_id"));
            result.Add(new OfferingVariant(variantId, ReadGuid(r, "offering_id"), ReadString(r, "name"), ids, ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadString(r, "metadata_json")));
        }
        return result;
    }

    private static async Task<IReadOnlyList<OfferingPlaceholder>> LoadOfferingPlaceholdersAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<OfferingPlaceholder>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM offering_placeholders ORDER BY name;", ct))
        {
            var placeholderId = ReadGuid(r, "id");
            var ids = new List<Guid>();
            await foreach (var membership in ReadAsync(c, $"SELECT variant_id FROM placeholder_variants WHERE placeholder_id = '{placeholderId}';", ct))
                ids.Add(ReadGuid(membership, "variant_id"));
            var recommendedWidth = ReadNullableInt(r, "recommended_width_px");
            var recommendedHeight = ReadNullableInt(r, "recommended_height_px");
            var recommendedDpi = ReadNullableInt(r, "recommended_dpi");
            var recommendedFormat = ReadNullableString(r, "recommended_format");
            var recommendedBackground = ReadNullableString(r, "recommended_background");
            var guidance = recommendedWidth is null && recommendedHeight is null && recommendedDpi is null && recommendedFormat is null && recommendedBackground is null
                ? null
                : new DesignAreaArtworkGuidance(recommendedWidth, recommendedHeight, recommendedDpi, recommendedFormat, recommendedBackground);
            result.Add(new OfferingPlaceholder(placeholderId, ReadGuid(r, "offering_id"), ReadString(r, "name"), ReadNullableString(r, "description"), ReadString(r, "position"), ReadString(r, "decoration_method"), ReadInt(r, "width"), ReadInt(r, "height"), ids, ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadString(r, "metadata_json"), ReadNullableString(r, "provider_reference"), guidance));
        }
        return result;
    }

    private static async Task<IReadOnlyList<MockupTemplate>> LoadMockupTemplatesAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<MockupTemplate>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM mockup_templates ORDER BY name;", ct))
            result.Add(new MockupTemplate(ReadGuid(r, "id"), ReadGuid(r, "offering_id"), ReadNullableGuid(r, "target_placeholder_id"), ReadString(r, "name"), ReadNullableString(r, "description"), ReadInt(r, "current_revision"), ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadNullableString(r, "position_key"), ReadNullableString(r, "future_asset_state"), ReadString(r, "metadata_json")));
        return result;
    }

    private static async Task<IReadOnlyList<MockupTemplateColorVariant>> LoadMockupTemplateColorsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<MockupTemplateColorVariant>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM mockup_template_colors ORDER BY created_at;", ct))
            result.Add(new MockupTemplateColorVariant(ReadGuid(r, "id"), ReadGuid(r, "template_id"), ReadGuid(r, "color_option_value_id"), ReadBool(r, "is_archived"), ReadDate(r, "created_at"), ReadDate(r, "updated_at"), ReadNullableGuid(r, "source_asset_id")));
        return result;
    }

    private static async Task<IReadOnlyList<MockupTemplateRevision>> LoadMockupTemplateRevisionsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<MockupTemplateRevision>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM mockup_template_revisions ORDER BY revision_number;", ct))
        {
            var imageWidth = ReadNullableInt(r, "image_width");
            var mapping = imageWidth is int width
                ? new MockupImageSpaceMapping(width, ReadInt(r, "image_height"), ReadInt(r, "mapping_x"), ReadInt(r, "mapping_y"), ReadInt(r, "mapping_width"), ReadInt(r, "mapping_height"))
                : null;
            result.Add(new MockupTemplateRevision(ReadGuid(r, "id"), ReadGuid(r, "template_id"), ReadInt(r, "revision_number"), ReadNullableGuid(r, "target_placeholder_id"), ReadDate(r, "created_at"), ReadNullableString(r, "note"), ReadNullableString(r, "provider_mockup_reference"), mapping));
        }
        return result;
    }

    private static async Task<IReadOnlyList<MockupTemplateRevisionColor>> LoadMockupTemplateRevisionColorsAsync(SqliteConnection c, CancellationToken ct)
    {
        var result = new List<MockupTemplateRevisionColor>();
        await foreach (var r in ReadAsync(c, "SELECT * FROM mockup_template_revision_colors ORDER BY id;", ct))
            result.Add(new MockupTemplateRevisionColor(ReadGuid(r, "id"), ReadGuid(r, "revision_id"), ReadGuid(r, "color_option_value_id"), ReadNullableGuid(r, "source_asset_id")));
        return result;
    }

    private static async Task<IReadOnlyList<Tag>> LoadTagsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var tags = new List<Tag>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM tags ORDER BY name;", cancellationToken))
        {
            tags.Add(new Tag(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json"), ReadNullableString(reader, "color")));
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

    private static async Task<IReadOnlyList<Item>> LoadItemsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var listings = new List<Item>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM items ORDER BY name;", cancellationToken))
        {
            listings.Add(new Item(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "niche_id"), ReadNullableGuid(reader, "group_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), (ItemStatus)ReadInt(reader, "status"), (WorkflowStage)ReadInt(reader, "workflow_stage"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return listings;
    }

    private static async Task<IReadOnlyList<IdeationRejection>> LoadIdeationRejectionsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var rejections = new List<IdeationRejection>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM ideation_rejections ORDER BY created_at, id;", cancellationToken))
        {
            rejections.Add(new IdeationRejection(
                ReadGuid(reader, "id"),
                ReadGuid(reader, "store_id"),
                ReadGuid(reader, "niche_id"),
                ReadNullableGuid(reader, "group_id"),
                ReadString(reader, "text"),
                ReadNullableString(reader, "reason"),
                (IdeationMode)ReadInt(reader, "mode"),
                ReadDate(reader, "created_at"),
                ReadNullableDate(reader, "updated_at")));
        }

        return rejections;
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
            prompts.Add(new Prompt(ReadGuid(reader, "id"), ReadGuid(reader, "store_id"), ReadNullableGuid(reader, "item_id"), ReadString(reader, "name"), ReadNullableString(reader, "description"), ReadString(reader, "text"), ReadBool(reader, "is_archived"), ReadDate(reader, "created_at"), ReadDate(reader, "updated_at"), ReadString(reader, "metadata_json")));
        }

        return prompts;
    }

    private static async Task<IReadOnlyList<ItemTag>> LoadItemTagsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var listingTags = new List<ItemTag>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM item_tags;", cancellationToken))
        {
            listingTags.Add(new ItemTag(ReadGuid(reader, "item_id"), ReadGuid(reader, "tag_id")));
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

    private static async Task<IReadOnlyList<StoreProduct>> LoadStoreProductsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var products = new List<StoreProduct>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM product_blueprints ORDER BY name;", cancellationToken))
        {
            products.Add(new StoreProduct(
                ReadGuid(reader, "id"),
                ReadGuid(reader, "store_id"),
                ReadString(reader, "name"),
                ReadNullableString(reader, "description"),
                ReadNullableString(reader, "external_product_id"),
                ReadDate(reader, "created_at"),
                ReadDate(reader, "updated_at"),
                ReadString(reader, "metadata_json")));
        }

        return products;
    }

    private static async Task<IReadOnlyList<FulfillmentOffering>> LoadFulfillmentOfferingsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var offerings = new List<FulfillmentOffering>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM fulfillment_offerings ORDER BY name;", cancellationToken))
        {
            offerings.Add(new FulfillmentOffering(
                ReadGuid(reader, "id"),
                ReadGuid(reader, "store_product_id"),
                ReadString(reader, "name"),
                ReadNullableString(reader, "description"),
                (FulfillmentKind)ReadInt(reader, "kind"),
                ReadNullableString(reader, "provider_name"),
                ReadNullableString(reader, "external_offering_id"),
                ReadDate(reader, "created_at"),
                ReadDate(reader, "updated_at"),
                ReadString(reader, "metadata_json")));
        }

        return offerings;
    }

    private static async Task<IReadOnlyList<ProductVariant>> LoadProductVariantsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var variants = new List<ProductVariant>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM product_variants ORDER BY created_at, id;", cancellationToken))
        {
            variants.Add(new ProductVariant(
                ReadGuid(reader, "id"),
                ReadGuid(reader, "fulfillment_offering_id"),
                JsonSerializer.Deserialize<List<VariantOption>>(ReadString(reader, "options_json")) ?? [],
                ReadDate(reader, "created_at"),
                ReadDate(reader, "updated_at")));
        }

        return variants;
    }

    private static async Task<IReadOnlyList<DesignArea>> LoadDesignAreasAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var areas = new List<DesignArea>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM design_areas ORDER BY name;", cancellationToken))
        {
            areas.Add(new DesignArea(
                ReadGuid(reader, "id"),
                ReadGuid(reader, "fulfillment_offering_id"),
                ReadString(reader, "name"),
                ReadNullableString(reader, "description"),
                ReadString(reader, "position"),
                ReadString(reader, "decoration_method"),
                ReadInt(reader, "width"),
                ReadInt(reader, "height"),
                JsonSerializer.Deserialize<List<Guid>>(ReadString(reader, "variant_ids_json")) ?? [],
                ReadDate(reader, "created_at"),
                ReadDate(reader, "updated_at"),
                ReadString(reader, "metadata_json")));
        }

        return areas;
    }

    private static async Task<IReadOnlyList<ItemListingConfiguration>> LoadItemListingConfigurationsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var configs = new List<ItemListingConfiguration>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM item_listing_configuration;", cancellationToken))
        {
            configs.Add(new ItemListingConfiguration(ReadGuid(reader, "item_id"), ReadGuid(reader, "offering_id")));
        }

        return configs;
    }

    private static async Task<IReadOnlyList<DesignSelectedColor>> LoadDesignSelectedColorsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var colors = new List<DesignSelectedColor>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM design_selected_colors;", cancellationToken))
        {
            colors.Add(new DesignSelectedColor(ReadGuid(reader, "item_id"), ReadString(reader, "color_value")));
        }

        return colors;
    }

    private static async Task<IReadOnlyList<DesignVariantRow>> LoadDesignVariantRowsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var rows = new List<DesignVariantRow>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM design_variant_rows ORDER BY sort_order;", cancellationToken))
        {
            rows.Add(new DesignVariantRow(ReadGuid(reader, "id"), ReadGuid(reader, "item_id"), ReadBool(reader, "is_default"), ReadInt(reader, "sort_order")));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<DesignVariantRowColor>> LoadDesignVariantRowColorsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var rowColors = new List<DesignVariantRowColor>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM design_variant_row_colors;", cancellationToken))
        {
            rowColors.Add(new DesignVariantRowColor(ReadGuid(reader, "row_id"), ReadString(reader, "color_value")));
        }

        return rowColors;
    }

    private static async Task<IReadOnlyList<DesignSlotAssignment>> LoadDesignSlotAssignmentsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var assignments = new List<DesignSlotAssignment>();
        await foreach (var reader in ReadAsync(connection, "SELECT * FROM design_slot_assignments;", cancellationToken))
        {
            assignments.Add(new DesignSlotAssignment(ReadGuid(reader, "row_id"), ReadGuid(reader, "design_area_id"), ReadNullableGuid(reader, "asset_id")));
        }

        return assignments;
    }

    private static async IAsyncEnumerable<SqliteDataReader> ReadAsync(
        SqliteConnection connection,
        string sql,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        System.Data.Common.DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
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

    private static int? ReadNullableInt(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static bool ReadBool(SqliteDataReader reader, string name) => ReadInt(reader, name) == 1;

    private static DateTimeOffset ReadDate(SqliteDataReader reader, string name) => DateTimeOffset.Parse(ReadString(reader, name));

    private static DateTimeOffset? ReadNullableDate(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : DateTimeOffset.Parse(reader.GetString(ordinal));
    }

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

        foreach (var rejection in snapshot.IdeationRejections)
        {
            if (!snapshot.Stores.Any(store => store.Id == rejection.StoreId) ||
                !snapshot.Niches.Any(niche => niche.Id == rejection.NicheId && niche.StoreId == rejection.StoreId) ||
                rejection.GroupId is Guid groupId &&
                !snapshot.Groups.Any(group =>
                    group.Id == groupId &&
                    group.StoreId == rejection.StoreId &&
                    GroupHierarchy.GetEffectiveNiche(snapshot, group).Id == rejection.NicheId))
            {
                throw new InvalidOperationException("Every ideation rejection must belong to an existing store, niche, and optional group.");
            }
        }

        var storeIds = snapshot.Stores.Select(store => store.Id).ToHashSet();
        foreach (var product in snapshot.StoreProducts)
        {
            if (!storeIds.Contains(product.StoreId))
            {
                throw new InvalidOperationException("Every product blueprint must belong to an existing store before saving.");
            }
        }

        var productIds = snapshot.StoreProducts.Select(product => product.Id).ToHashSet();
        foreach (var offering in snapshot.FulfillmentOfferings)
        {
            if (!productIds.Contains(offering.StoreProductId))
            {
                throw new InvalidOperationException("Every fulfillment offering must belong to an existing product blueprint before saving.");
            }
        }

        var offeringIds = snapshot.FulfillmentOfferings.Select(offering => offering.Id).ToHashSet();
        foreach (var variant in snapshot.ProductVariants)
        {
            if (!offeringIds.Contains(variant.FulfillmentOfferingId))
            {
                throw new InvalidOperationException("Every product variant must belong to an existing fulfillment offering before saving.");
            }
        }

        foreach (var area in snapshot.DesignAreas)
        {
            if (!offeringIds.Contains(area.FulfillmentOfferingId))
            {
                throw new InvalidOperationException("Every design area must belong to an existing fulfillment offering before saving.");
            }

            if (area.VariantIds.Any(variantId => snapshot.ProductVariants.All(variant => !(variant.Id == variantId && variant.FulfillmentOfferingId == area.FulfillmentOfferingId))))
            {
                throw new InvalidOperationException("A design area may only apply to variants from its own offering.");
            }
        }

        var allOfferingIds = snapshot.FulfillmentOfferings.Select(offering => offering.Id).ToHashSet();
        foreach (var config in snapshot.ItemListingConfigurations)
        {
            if (!snapshot.Items.Any(item => item.Id == config.ItemId))
            {
                throw new InvalidOperationException("Every item listing configuration must reference an existing item.");
            }

            if (!allOfferingIds.Contains(config.OfferingId))
            {
                throw new InvalidOperationException("Every item listing configuration must reference an existing fulfillment offering.");
            }
        }

        foreach (var color in snapshot.DesignSelectedColors)
        {
            if (!snapshot.Items.Any(item => item.Id == color.ItemId))
            {
                throw new InvalidOperationException("Every design selected color must reference an existing item.");
            }
        }

        var itemIds = snapshot.Items.Select(item => item.Id).ToHashSet();
        foreach (var row in snapshot.DesignVariantRows)
        {
            if (!itemIds.Contains(row.ItemId))
            {
                throw new InvalidOperationException("Every design variant row must reference an existing item.");
            }
        }

        var rowIds = snapshot.DesignVariantRows.Select(row => row.Id).ToHashSet();
        var areaIds = snapshot.DesignAreas.Select(area => area.Id).ToHashSet();
        foreach (var rowColor in snapshot.DesignVariantRowColors)
        {
            if (!rowIds.Contains(rowColor.RowId))
            {
                throw new InvalidOperationException("Every design variant row color must reference an existing row.");
            }
        }

        foreach (var assignment in snapshot.DesignSlotAssignments)
        {
            if (!rowIds.Contains(assignment.RowId))
            {
                throw new InvalidOperationException("Every design slot assignment must reference an existing row.");
            }

            if (!areaIds.Contains(assignment.DesignAreaId))
            {
                throw new InvalidOperationException("Every design slot assignment must reference an existing design area.");
            }
        }

        var blueprints = snapshot.Blueprints.ToDictionary(value => value.Id);
        var providers = snapshot.PrintProviders.ToDictionary(value => value.Id);
        var blueprintOfferings = snapshot.BlueprintOfferings.ToDictionary(value => value.Id);
        foreach (var blueprint in snapshot.Blueprints)
        {
            if (!storeIds.Contains(blueprint.StoreId))
                throw new InvalidOperationException("Every Blueprint must belong to an existing Store.");
        }
        foreach (var provider in snapshot.PrintProviders)
        {
            if (!storeIds.Contains(provider.StoreId))
                throw new InvalidOperationException("Every Print Provider must belong to an existing Store.");
        }
        foreach (var offering in snapshot.BlueprintOfferings)
        {
            if (!blueprints.TryGetValue(offering.BlueprintId, out var blueprint) || blueprint.StoreId != offering.StoreId)
                throw new InvalidOperationException("Every Blueprint Offering must belong to its Blueprint and Store.");
            var provider = offering.PrintProviderId is Guid providerId && providers.TryGetValue(providerId, out var foundProvider) ? foundProvider : null;
            CatalogRelationshipPolicy.ValidateOffering( offering, blueprint, provider, snapshot.OfferingOptions, snapshot.OfferingOptionValues, snapshot.OfferingVariants, snapshot.OfferingPlaceholders);
        }
        foreach (var template in snapshot.MockupTemplates)
        {
            if (!blueprintOfferings.TryGetValue(template.BlueprintOfferingId, out var offering) ||
                (template.TargetPlaceholderId is not null && snapshot.OfferingPlaceholders.All(placeholder => placeholder.Id != template.TargetPlaceholderId || placeholder.OfferingId != offering.Id)))
                throw new InvalidOperationException("Every mockup template target Placeholder must belong to the template offering.");
        }
        MockupTemplatePolicy.EnsureUniqueActiveColor(snapshot.MockupTemplateColorVariants);
        foreach (var binding in snapshot.MockupTemplateColorVariants.Where(value => !value.IsArchived))
        {
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == binding.MockupTemplateId)
                ?? throw new InvalidOperationException("Every template color must reference an existing Mockup Template.");
            var colorValue = snapshot.OfferingOptionValues.SingleOrDefault(value => value.Id == binding.ColorOptionValueId)
                ?? throw new InvalidOperationException("Every template color must reference an existing Color Option Value.");
            var colorOption = snapshot.OfferingOptions.SingleOrDefault(value => value.Id == colorValue.OptionId)
                ?? throw new InvalidOperationException("Every template color must reference an existing Color Option.");
            CatalogRelationshipPolicy.ValidateMockupTemplateColor(template.BlueprintOfferingId, colorValue, colorOption, template);
        }
    }
}
