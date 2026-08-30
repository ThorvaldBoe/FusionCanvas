using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class ProductCatalogPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsCatalog()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snapshot.StoreProducts, loaded.StoreProducts);
        Assert.Equal(snapshot.FulfillmentOfferings.OrderBy(o => o.Id), loaded.FulfillmentOfferings.OrderBy(o => o.Id));
        Assert.Equal(snapshot.ProductVariants, loaded.ProductVariants);
        Assert.Equal(snapshot.DesignAreas, loaded.DesignAreas);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsNormalizedOfferingAndMockupModel()
    {
        using var tempDirectory = new TemporaryDirectory();
        var repository = new SqliteWorkspaceRepository(tempDirectory.GetPath("normalized.db"));
        var store = new Store(Guid.NewGuid(), "Catalog Store", null, false, Now, Now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "Printful", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Printful Tee", null, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, null, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, Now, Now);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], false, Now, Now, providerReference: "front-print-area", artworkGuidance: new DesignAreaArtworkGuidance(4500, 5400, 300, "PNG", "Transparent"));
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, placeholder.Id, "Front mockup", null, 1, false, Now, Now);
        var templateColor = new MockupTemplateColorVariant(Guid.NewGuid(), template.Id, black.Id, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, placeholder.Id, Now, providerMockupReference: "provider-image-front-black", imageMapping: new MockupImageSpaceMapping(1200, 1200, 360, 240, 480, 600));
        var revisionColor = new MockupTemplateRevisionColor(Guid.NewGuid(), revision.Id, black.Id);
        var asset = new Asset(Guid.NewGuid(), store.Id, "front-black.png", null, AssetKind.MockupImage, "assets/front-black.png", "C:\\imports\\front-black.png", false, false, Now, Now, "{}");
        var sourceImage = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, asset.Id, new MockupImageSpaceMapping(1200, 1200, 0, 0, 1200, 1200), false, Now, Now);
        var sourceCondition = new MockupTemplateSourceImageOptionValue(sourceImage.Id, black.Id);
        var revisionSourceImage = new MockupTemplateRevisionSourceImage(Guid.NewGuid(), revision.Id, asset.Id, sourceImage.ImageMapping);
        var revisionSourceCondition = new MockupTemplateRevisionSourceImageOptionValue(revisionSourceImage.Id, black.Id);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [asset], [], [], [], [new AssetLink(asset.Id, WorkspaceEntityKind.Store, store.Id)])
        {
            Blueprints = [blueprint], PrintProviders = [provider], BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption], OfferingOptionValues = [black, medium],
            OfferingVariants = [variant], OfferingPlaceholders = [placeholder], MockupTemplates = [template],
            MockupTemplateColorVariants = [templateColor], MockupTemplateRevisions = [revision], MockupTemplateRevisionColors = [revisionColor],
            MockupTemplateSourceImages = [sourceImage], MockupTemplateSourceImageOptionValues = [sourceCondition],
            MockupTemplateRevisionSourceImages = [revisionSourceImage], MockupTemplateRevisionSourceImageOptionValues = [revisionSourceCondition]
        };

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snapshot.Blueprints, loaded.Blueprints);
        Assert.Equal(snapshot.PrintProviders, loaded.PrintProviders);
        Assert.Equal(snapshot.BlueprintOfferings, loaded.BlueprintOfferings);
        Assert.Equal(snapshot.OfferingOptions, loaded.OfferingOptions);
        Assert.Equal(snapshot.OfferingOptionValues, loaded.OfferingOptionValues);
        Assert.Equal(snapshot.OfferingVariants.Select(value => value.Id).OrderBy(value => value), loaded.OfferingVariants.Select(value => value.Id).OrderBy(value => value));
        Assert.Equal(snapshot.OfferingVariants[0].OptionValueIds.OrderBy(value => value), loaded.OfferingVariants[0].OptionValueIds.OrderBy(value => value));
        Assert.Equal(snapshot.OfferingPlaceholders.Select(value => value.Id).OrderBy(value => value), loaded.OfferingPlaceholders.Select(value => value.Id).OrderBy(value => value));
        Assert.Equal(snapshot.OfferingPlaceholders[0].VariantIds, loaded.OfferingPlaceholders[0].VariantIds);
        Assert.Equal(placeholder.ProviderReference, loaded.OfferingPlaceholders[0].ProviderReference);
        Assert.Equal(placeholder.ArtworkGuidance, loaded.OfferingPlaceholders[0].ArtworkGuidance);
        Assert.Equal(snapshot.MockupTemplates, loaded.MockupTemplates);
        Assert.Equal(snapshot.MockupTemplateColorVariants, loaded.MockupTemplateColorVariants);
        Assert.Equal(snapshot.MockupTemplateRevisions, loaded.MockupTemplateRevisions);
        Assert.Equal(revision.ProviderMockupReference, loaded.MockupTemplateRevisions[0].ProviderMockupReference);
        Assert.Equal(revision.ImageMapping, loaded.MockupTemplateRevisions[0].ImageMapping);
        Assert.Equal(snapshot.MockupTemplateRevisionColors, loaded.MockupTemplateRevisionColors);
        Assert.Equal(snapshot.MockupTemplateSourceImages, loaded.MockupTemplateSourceImages);
        Assert.Equal(snapshot.MockupTemplateSourceImageOptionValues, loaded.MockupTemplateSourceImageOptionValues);
        Assert.Equal(snapshot.MockupTemplateRevisionSourceImages, loaded.MockupTemplateRevisionSourceImages);
        Assert.Equal(snapshot.MockupTemplateRevisionSourceImageOptionValues, loaded.MockupTemplateRevisionSourceImageOptionValues);
    }

    [Fact]
    public async Task SaveAndLoadAsync_CreatesSchemaVersionCurrent()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);

        await repository.SaveAsync(CreateCatalogSnapshot(), TestContext.Current.CancellationToken);

        Assert.Equal(SqliteWorkspaceRepository.CurrentSchemaVersion, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsNameOnlyDraft()
    {
        using var tempDirectory = new TemporaryDirectory();
        var repository = new SqliteWorkspaceRepository(tempDirectory.GetPath("draft.db"));
        var store = new Store(Guid.NewGuid(), "Catalog Store", null, false, Now, Now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Unfinished front", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, null, Now);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], MockupTemplates = [template], MockupTemplateRevisions = [revision]
        };

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(template, Assert.Single(loaded.MockupTemplates));
        Assert.Equal(revision, Assert.Single(loaded.MockupTemplateRevisions));
        Assert.Equal(13, SqliteWorkspaceRepository.CurrentSchemaVersion);
    }

    [Fact]
    public async Task Migration_DropsItemDesignAreaTargetsTable()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        await CreateVersionNineDatabaseAsync(databasePath);

        // Verify old table exists before migration
        using (var conn = new SqliteConnection($"Data Source={databasePath}"))
        {
            await conn.OpenAsync(TestContext.Current.CancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='item_design_area_targets'";
            var before = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(1, before);
        }

        // Run migration (load triggers migration to current)
        await new SqliteWorkspaceRepository(databasePath)
            .LoadAsync(TestContext.Current.CancellationToken);

        // Verify table is dropped
        using (var conn = new SqliteConnection($"Data Source={databasePath}"))
        {
            await conn.OpenAsync(TestContext.Current.CancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='item_design_area_targets'";
            var after = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(0, after);
        }

        Assert.Equal(SqliteWorkspaceRepository.CurrentSchemaVersion, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task LoadAsync_MigratesSchemaElevenWithExplicitUnconfiguredUxFields()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("schema-eleven.db");
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(CreateCatalogSnapshot(), TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                ALTER TABLE offering_placeholders DROP COLUMN provider_reference;
                ALTER TABLE offering_placeholders DROP COLUMN recommended_width_px;
                ALTER TABLE offering_placeholders DROP COLUMN recommended_height_px;
                ALTER TABLE offering_placeholders DROP COLUMN recommended_dpi;
                ALTER TABLE offering_placeholders DROP COLUMN recommended_format;
                ALTER TABLE offering_placeholders DROP COLUMN recommended_background;
                ALTER TABLE mockup_template_revisions DROP COLUMN provider_mockup_reference;
                ALTER TABLE mockup_template_revisions DROP COLUMN image_width;
                ALTER TABLE mockup_template_revisions DROP COLUMN image_height;
                ALTER TABLE mockup_template_revisions DROP COLUMN mapping_x;
                ALTER TABLE mockup_template_revisions DROP COLUMN mapping_y;
                ALTER TABLE mockup_template_revisions DROP COLUMN mapping_width;
                ALTER TABLE mockup_template_revisions DROP COLUMN mapping_height;
                PRAGMA user_version = 11;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SqliteWorkspaceRepository.CurrentSchemaVersion, await ReadUserVersionAsync(databasePath));
        Assert.All(loaded.OfferingPlaceholders, value =>
        {
            Assert.Null(value.ProviderReference);
            Assert.Null(value.ArtworkGuidance);
        });
        Assert.All(loaded.MockupTemplateRevisions, value =>
        {
            Assert.Null(value.ProviderMockupReference);
            Assert.Null(value.ImageMapping);
        });
        Assert.Contains("recommended_width_px", await ReadColumnNamesAsync(databasePath, "offering_placeholders"));
        Assert.Contains("mapping_width", await ReadColumnNamesAsync(databasePath, "mockup_template_revisions"));
    }

    [Fact]
    public async Task LoadAsync_RepairsCurrentVersionDatabaseMissingFulfillmentStrategyColumn()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("missing-fulfillment-column.db");
        var snapshot = CreateCatalogSnapshot();
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(snapshot, TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE stores DROP COLUMN fulfillment_strategy; PRAGMA user_version = {SqliteWorkspaceRepository.CurrentSchemaVersion};";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        var store = Assert.Single(loaded.Stores);
        Assert.Equal(snapshot.Stores[0].Id, store.Id);
        Assert.Equal(FulfillmentStrategy.Manual, store.FulfillmentStrategy);
    }

    [Fact]
    public async Task NewSchemaHasNoRenderingOverrideOrExternalIntegrationTables()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("scope.db");
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(CreateCatalogSnapshot(), TestContext.Current.CancellationToken);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken)) tables.Add(reader.GetString(0));

        Assert.DoesNotContain(tables, name => name.Contains("coordinate", StringComparison.OrdinalIgnoreCase) || name.Contains("override", StringComparison.OrdinalIgnoreCase) || name.Contains("generated_mockup", StringComparison.OrdinalIgnoreCase) || name.Contains("shopify", StringComparison.OrdinalIgnoreCase) || name.Contains("credential", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LoadAsync_MigratesPopulatedSchemaTenCatalogToNormalizedModel()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("legacy-catalog.db");
        var legacy = CreateCatalogSnapshot();
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(legacy, TestContext.Current.CancellationToken);
        await DowngradeStoreTableAndSetSchemaTenAsync(databasePath);

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SqliteWorkspaceRepository.CurrentSchemaVersion, await ReadUserVersionAsync(databasePath));
        Assert.Equal(legacy.StoreProducts.Select(value => value.Id), loaded.Blueprints.Select(value => value.Id));
        Assert.Equal(legacy.FulfillmentOfferings.Select(value => value.Id).OrderBy(value => value), loaded.BlueprintOfferings.Select(value => value.Id).OrderBy(value => value));
        Assert.Equal(legacy.ProductVariants.Select(value => value.Id), loaded.OfferingVariants.Select(value => value.Id));
        Assert.Equal(legacy.DesignAreas.Select(value => value.Id), loaded.OfferingPlaceholders.Select(value => value.Id));
        Assert.Equal("printify-choice", loaded.BlueprintOfferings.Single(value => value.Kind == BlueprintOfferingKind.ProviderNetwork).ProviderNetworkCode);
        Assert.Equal(2, loaded.OfferingOptions.Count);
        Assert.Equal(2, loaded.OfferingOptionValues.Count);
        Assert.Equal(2, loaded.OfferingVariants.Single().OptionValueIds.Count);
        Assert.Single(loaded.OfferingPlaceholders.Single().VariantIds);
    }

    [Fact]
    public async Task LoadAsync_MigrationPreservesArchivedStoreAndItemDesignRelationships()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("legacy-relations.db");
        var legacy = CreateCatalogSnapshot();
        var item = legacy.Items[0];
        var offering = legacy.FulfillmentOfferings[0];
        var area = legacy.DesignAreas[0];
        var rowId = Guid.NewGuid();
        var archivedStore = new Store(Guid.NewGuid(), "Archived Store", null, true, Now, Now, "{}");
        var archivedItem = item with { Id = Guid.NewGuid(), StoreId = archivedStore.Id, IsArchived = true };
        legacy = legacy with
        {
            Stores = [.. legacy.Stores, archivedStore],
            Items = [.. legacy.Items, archivedItem],
            ItemListingConfigurations = [new ItemListingConfiguration(item.Id, offering.Id)],
            DesignVariantRows = [new DesignVariantRow(rowId, item.Id, true, 0)],
            DesignVariantRowColors = [new DesignVariantRowColor(rowId, "Black")],
            DesignSlotAssignments = [new DesignSlotAssignment(rowId, area.Id, null)]
        };
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(legacy, TestContext.Current.CancellationToken);
        await DowngradeStoreTableAndSetSchemaTenAsync(databasePath);

        var loaded = await new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(loaded.Stores.Single(store => store.Id == archivedStore.Id).IsArchived);
        Assert.Contains(loaded.Items, value => value.Id == archivedItem.Id && value.StoreId == archivedStore.Id);
        Assert.Contains(loaded.ItemListingConfigurations, value => value.ItemId == item.Id && value.OfferingId == offering.Id);
        Assert.Contains(loaded.DesignSlotAssignments, value => value.RowId == rowId && value.DesignAreaId == area.Id);
    }

    [Fact]
    public async Task LoadAsync_MalformedSchemaTenCatalogRollsBackNormalizedMigration()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("malformed-legacy-catalog.db");
        await new SqliteWorkspaceRepository(databasePath).SaveAsync(CreateCatalogSnapshot(), TestContext.Current.CancellationToken);
        await DowngradeStoreTableAndSetSchemaTenAsync(databasePath);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE product_variants SET options_json = 'not-json';";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => new SqliteWorkspaceRepository(databasePath).LoadAsync(TestContext.Current.CancellationToken));

        await using var verification = new SqliteConnection($"Data Source={databasePath}");
        await verification.OpenAsync(TestContext.Current.CancellationToken);
        await using var versionCommand = verification.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        Assert.Equal(10, Convert.ToInt32(await versionCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
        await using var blueprintCommand = verification.CreateCommand();
        blueprintCommand.CommandText = "SELECT COUNT(*) FROM catalog_blueprints;";
        Assert.Equal(0, Convert.ToInt32(await blueprintCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task LoadAsync_MigratesVersionEightDatabaseToCurrent()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        await CreateVersionEightDatabaseAsync(databasePath);
        Assert.Equal(8, await ReadUserVersionAsync(databasePath));

        var loaded = await new SqliteWorkspaceRepository(databasePath)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(SqliteWorkspaceRepository.CurrentSchemaVersion, await ReadUserVersionAsync(databasePath));
        Assert.Empty(loaded.StoreProducts);
        Assert.Empty(loaded.DesignAreas);
    }

    [Fact]
    public async Task SaveAsync_RejectsCrossOfferingApplicableVariant()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var choiceOffering = snapshot.FulfillmentOfferings.Single(o => o.Kind == FulfillmentKind.PrintifyChoiceNetwork);
        var fixedOfferingVariant = snapshot.ProductVariants[0];
        var crossArea = new DesignArea(
            Guid.NewGuid(), choiceOffering.Id, "Invalid", null, "front", "DTG", 3000, 4500,
            [fixedOfferingVariant.Id], Now, Now, "{}");
        snapshot = snapshot with
        {
            DesignAreas = [.. snapshot.DesignAreas, crossArea]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveAsync(snapshot, TestContext.Current.CancellationToken));

        Assert.Contains("own offering", exception.Message);
        var unchanged = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(unchanged.DesignAreas, value => value.Id == crossArea.Id);
    }

    [Fact]
    public async Task LoadAsync_RejectsPersistedMockupMappingOutsideImageBounds()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("invalid-mapping.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var store = new Store(Guid.NewGuid(), "Store", null, false, Now, Now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now, providerMockupReference: "front", imageMapping: new MockupImageSpaceMapping(1200, 1200, 300, 200, 500, 650));
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingPlaceholders = [area], MockupTemplates = [template], MockupTemplateRevisions = [revision]
        };
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE mockup_template_revisions SET mapping_width = image_width WHERE id = $id;";
            command.Parameters.AddWithValue("$id", revision.Id.ToString());
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_RoundTripsDesignStageDomain()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
        var itemId = snapshot.Items[0].Id;
        var offeringId = snapshot.FulfillmentOfferings[0].Id;
        var rowId = Guid.NewGuid();
        var areaId = snapshot.DesignAreas[0].Id;

        snapshot = snapshot with
        {
            ItemListingConfigurations = [new ItemListingConfiguration(itemId, offeringId)],
            DesignSelectedColors = [new DesignSelectedColor(itemId, "Black")],
            DesignVariantRows = [new DesignVariantRow(rowId, itemId, true, 0)],
            DesignVariantRowColors = [new DesignVariantRowColor(rowId, "Black")],
            DesignSlotAssignments = [new DesignSlotAssignment(rowId, areaId, null)]
        };

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Single(loaded.ItemListingConfigurations);
        Assert.Single(loaded.DesignSelectedColors);
        Assert.Single(loaded.DesignVariantRows);
        Assert.Single(loaded.DesignVariantRowColors);
        Assert.Single(loaded.DesignSlotAssignments);
        Assert.Equal(itemId, loaded.ItemListingConfigurations[0].ItemId);
        Assert.Equal(offeringId, loaded.ItemListingConfigurations[0].OfferingId);
    }

    [Fact]
    public async Task SaveAsync_RejectsOrphanOfferingWithoutProduct()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
        snapshot = snapshot with
        {
            FulfillmentOfferings = [.. snapshot.FulfillmentOfferings,
                new FulfillmentOffering(Guid.NewGuid(), Guid.NewGuid(), "Orphan", null, FulfillmentKind.FixedProvider, "P", null, Now, Now, "{}")]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveAsync(snapshot, TestContext.Current.CancellationToken));

        Assert.Contains("existing product blueprint", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_DeleteCascadesCleanlyWhenRecordsRemoved()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        var reduced = snapshot with
        {
            StoreProducts = [],
            FulfillmentOfferings = [],
            ProductVariants = [],
            DesignAreas = []
        };
        await repository.SaveAsync(reduced, TestContext.Current.CancellationToken);

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Empty(loaded.StoreProducts);
        Assert.Equal(snapshot.Items, loaded.Items);
    }

    private static WorkspaceSnapshot CreateCatalogSnapshot()
    {
        var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, Now, Now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Gildan 64000", "Blank tee", "printify-1", Now, Now, "{}");
        var fixedOffering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", "ext-1", Now, Now, "{}");
        var choiceOffering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Choice", null, FulfillmentKind.PrintifyChoiceNetwork, null, "ext-2", Now, Now, "{}");
        var variant = new ProductVariant(Guid.NewGuid(), fixedOffering.Id, [new VariantOption("Color", "Black"), new VariantOption("Size", "M")], Now, Now);
        var area = new DesignArea(Guid.NewGuid(), fixedOffering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");

        return new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [store],
            [],
            [],
            [item],
            [],
            [],
            [],
            [],
            [])
        {
            IdeationRejections = [],
            StoreProducts = [product],
            FulfillmentOfferings = [fixedOffering, choiceOffering],
            ProductVariants = [variant],
            DesignAreas = [area]
        };
    }

    private static async Task CreateVersionEightDatabaseAsync(string databasePath)
    {
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE workspaces (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    is_archived INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );
                CREATE TABLE stores (
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
                CREATE TABLE items (
                    id TEXT PRIMARY KEY,
                    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                    niche_id TEXT NULL,
                    group_id TEXT NULL,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    status INTEGER NOT NULL,
                    workflow_stage INTEGER NOT NULL DEFAULT 0,
                    is_archived INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    metadata_json TEXT NOT NULL
                );
                CREATE TABLE ideation_rejections (
                    id TEXT PRIMARY KEY,
                    store_id TEXT NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
                    niche_id TEXT NULL,
                    group_id TEXT NULL,
                    text TEXT NOT NULL,
                    reason TEXT NULL,
                    mode INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NULL
                );
                PRAGMA user_version = 8;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task CreateVersionNineDatabaseAsync(string databasePath)
    {
        // First create v8, then add the item_design_area_targets table and set version to 9
        await CreateVersionEightDatabaseAsync(databasePath);
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS item_design_area_targets (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    design_area_id TEXT NOT NULL REFERENCES design_areas(id) ON DELETE CASCADE,
                    PRIMARY KEY (item_id, design_area_id)
                );
                PRAGMA user_version = 9;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task DowngradeStoreTableAndSetSchemaTenAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version = 10;";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> ReadUserVersionAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<IReadOnlyList<string>> ReadColumnNamesAsync(string databasePath, string table)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        var result = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            result.Add(reader.GetString(1));
        return result;
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
