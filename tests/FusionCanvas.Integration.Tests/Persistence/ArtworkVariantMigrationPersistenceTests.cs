using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class ArtworkVariantMigrationPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAndLoad_RoundTripsFavoriteContrastAndListingAssignments()
    {
        using var directory = new TemporaryDirectory();
        var repository = new SqliteWorkspaceRepository(directory.GetPath("artwork-variants.db"), useConnectionPooling: false);
        var snapshot = CreateSnapshot(includeLegacyColorRows: false);

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snapshot.DesignVariantRows, loaded.DesignVariantRows);
        Assert.Equal(snapshot.ListingColorSelections, loaded.ListingColorSelections);
        Assert.Equal(snapshot.OfferingOptionValues, loaded.OfferingOptionValues);
        Assert.Equal(snapshot.OfferingVariants, loaded.OfferingVariants);
    }

    [Fact]
    public async Task Migration_MovesResolvedAndUnresolvedColorsAndPreservesArtworkAssignments()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("legacy-design-colors.db");
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        var snapshot = CreateSnapshot(includeLegacyColorRows: true);
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys = OFF;
                DROP TABLE listing_color_selections;
                ALTER TABLE design_variant_rows DROP COLUMN contrast_intent;
                ALTER TABLE offering_option_values DROP COLUMN is_favorite;
                PRAGMA user_version = 23;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(loaded.DesignSelectedColors);
        Assert.Empty(loaded.DesignVariantRowColors);
        Assert.Equal(snapshot.DesignSlotAssignments, loaded.DesignSlotAssignments);
        var migrated = loaded.ListingColorSelections.OrderBy(value => value.SortOrder).ToArray();
        Assert.Equal(3, migrated.Length);
        var black = migrated.Single(value => value.ColorOptionValueId == snapshot.OfferingOptionValues.Single(optionValue => optionValue.Value == "Black").Id);
        var white = migrated.Single(value => value.ColorOptionValueId == snapshot.OfferingOptionValues.Single(optionValue => optionValue.Value == "White").Id);
        var unresolved = migrated.Single(value => value.LegacyColorLabel == "Heather Gray");
        Assert.Equal(snapshot.DesignVariantRows.Single(value => value.IsDefault).Id, black.ArtworkVariantId);
        Assert.Equal(snapshot.DesignVariantRows.Single(value => !value.IsDefault).Id, white.ArtworkVariantId);
        Assert.Null(unresolved.ColorOptionValueId);
        Assert.Equal(snapshot.DesignVariantRows.Single(value => value.IsDefault).Id, unresolved.ArtworkVariantId);

        var reloaded = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(migrated, reloaded.ListingColorSelections.OrderBy(value => value.SortOrder));
        Assert.Equal(snapshot.ExternalListingMappings, reloaded.ExternalListingMappings);
    }

    [Fact]
    public async Task Migration_RollsBackListingSelectionsWhenBackfillFails()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("legacy-design-colors-rollback.db");
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        var snapshot = CreateSnapshot(includeLegacyColorRows: true);
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys = OFF;
                DROP TABLE listing_color_selections;
                ALTER TABLE design_variant_rows DROP COLUMN contrast_intent;
                ALTER TABLE offering_option_values DROP COLUMN is_favorite;
                CREATE TABLE listing_color_selections (
                    item_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
                    color_option_value_id TEXT NULL REFERENCES offering_option_values(id) ON DELETE RESTRICT,
                    legacy_color_label TEXT NULL,
                    artwork_variant_id TEXT NOT NULL REFERENCES design_variant_rows(id) ON DELETE RESTRICT,
                    sort_order INTEGER NOT NULL,
                    CHECK ((color_option_value_id IS NOT NULL AND legacy_color_label IS NULL)
                        OR (color_option_value_id IS NULL AND legacy_color_label IS NOT NULL AND length(trim(legacy_color_label)) > 0))
                );
                CREATE TRIGGER fail_listing_color_backfill BEFORE INSERT ON listing_color_selections
                BEGIN SELECT RAISE(ABORT, 'test migration failure'); END;
                PRAGMA user_version = 23;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await Assert.ThrowsAsync<SqliteException>(() => repository.LoadAsync(TestContext.Current.CancellationToken));

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TRIGGER fail_listing_color_backfill; PRAGMA user_version;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            command.CommandText = "PRAGMA user_version;";
            Assert.Equal(24, Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
            command.CommandText = "SELECT COUNT(*) FROM design_selected_colors;";
            Assert.Equal(3, Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
            command.CommandText = "SELECT COUNT(*) FROM listing_color_selections;";
            Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
        }

        var migrated = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, migrated.ListingColorSelections.Count);
        Assert.Equal(snapshot.ExternalListingMappings, migrated.ExternalListingMappings);
    }

    private static WorkspaceSnapshot CreateSnapshot(bool includeLegacyColorRows)
    {
        var workspace = WorkspaceSnapshot.DefaultWorkspace(Now);
        var store = new Store(workspace.Id, "Studio", null, false, Now, Now, "{}");
        var itemId = Guid.NewGuid();
        var storeProductId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var item = new Item(itemId, store.Id, null, null, "Artwork variants", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        var storeProduct = new StoreProduct(storeProductId, store.Id, "T-shirt", null, null, Now, Now, "{}");
        var fulfillmentOffering = new FulfillmentOffering(offeringId, storeProduct.Id, "T-shirt provider", null, FulfillmentKind.FixedProvider, "Provider", "external", Now, Now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "Provider", "10", false, Now, Now);
        var offering = new BlueprintOffering(offeringId, blueprint.Id, store.Id, "T-shirt provider", null, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, "1:10", false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0, metadataJson: "{\"hex\":\"#000000\"}", isFavorite: true);
        var white = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "White", 1, metadataJson: "{\"hex\":\"#FFFFFF\"}");
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id], false, Now, Now) { ProviderIsAvailable = false };
        var legacyVariant = new ProductVariant(variant.Id, offeringId, [new VariantOption("Color", "Black")], Now, Now);
        var area = new DesignArea(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 3000, 4500, [legacyVariant.Id], Now, Now, "{}");
        var defaultArtwork = new DesignVariantRow(Guid.NewGuid(), itemId, true, 0, ArtworkContrastIntent.LightForDarkProduct);
        var alternateArtwork = new DesignVariantRow(Guid.NewGuid(), itemId, false, 1, ArtworkContrastIntent.DarkForLightProduct);
        var asset = new Asset(Guid.NewGuid(), store.Id, "art.png", null, AssetKind.ExportedImage, "assets/art.png", null, false, false, Now, Now, "{}");
        var assignments = new[] { new DesignSlotAssignment(defaultArtwork.Id, area.Id, asset.Id), new DesignSlotAssignment(alternateArtwork.Id, area.Id, null) };
        var snapshot = new WorkspaceSnapshot([workspace], [store], [], [], [item], [asset], [], [], [], [])
        {
            StoreProducts = [storeProduct],
            FulfillmentOfferings = [fulfillmentOffering],
            ItemListingConfigurations = [new ItemListingConfiguration(itemId, offeringId)],
            ProductVariants = [legacyVariant],
            DesignAreas = [area],
            DesignVariantRows = [defaultArtwork, alternateArtwork],
            DesignSlotAssignments = assignments,
            Blueprints = [blueprint],
            PrintProviders = [provider],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption],
            OfferingOptionValues = [black, white],
            OfferingVariants = [variant]
        };

        snapshot = snapshot with
        {
            ExternalListingMappings = [new ExternalListingMapping(store.Id, itemId, "printify", "42", "product-1", "published", null,
                ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded,
                "{\"desired\":\"unchanged\"}", null, Now, Now, Now)]
        };

        if (includeLegacyColorRows)
        {
            snapshot = snapshot with
            {
                DesignSelectedColors = [new DesignSelectedColor(itemId, "Black"), new DesignSelectedColor(itemId, "White"), new DesignSelectedColor(itemId, "Heather Gray")],
                DesignVariantRowColors = [new DesignVariantRowColor(defaultArtwork.Id, "Black"), new DesignVariantRowColor(alternateArtwork.Id, "White"), new DesignVariantRowColor(defaultArtwork.Id, "Heather Gray")]
            };
        }
        else
        {
            snapshot = snapshot with
            {
                ListingColorSelections =
                [
                    new ListingColorSelection(itemId, black.Id, defaultArtwork.Id, 0),
                    new ListingColorSelection(itemId, white.Id, alternateArtwork.Id, 1)
                ]
            };
        }

        return snapshot;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(_path);
        public string GetPath(string fileName) => Path.Combine(_path, fileName);
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_path, recursive: true);
        }
    }
}
