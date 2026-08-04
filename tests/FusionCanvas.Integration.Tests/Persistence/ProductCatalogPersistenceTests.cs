using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class ProductCatalogPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsCatalogAndTargets()
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
        Assert.Equal(snapshot.ItemDesignAreaTargets, loaded.ItemDesignAreaTargets);
    }

    [Fact]
    public async Task SaveAndLoadAsync_CreatesSchemaVersionNine()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);

        await repository.SaveAsync(CreateCatalogSnapshot(), TestContext.Current.CancellationToken);

        Assert.Equal(9, await ReadUserVersionAsync(databasePath));
    }

    [Fact]
    public async Task LoadAsync_MigratesVersionEightDatabaseToNine()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        await CreateVersionEightDatabaseAsync(databasePath);
        Assert.Equal(8, await ReadUserVersionAsync(databasePath));

        var loaded = await new SqliteWorkspaceRepository(databasePath)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(9, await ReadUserVersionAsync(databasePath));
        Assert.Empty(loaded.StoreProducts);
        Assert.Empty(loaded.DesignAreas);
        Assert.Empty(loaded.ItemDesignAreaTargets);
    }

    [Fact]
    public async Task SaveAsync_RejectsCrossOfferingApplicableVariant()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
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
    }

    [Fact]
    public async Task SaveAsync_RejectsTargetToMissingArea()
    {
        using var tempDirectory = new TemporaryDirectory();
        var databasePath = tempDirectory.GetPath("catalog.db");
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = CreateCatalogSnapshot();
        snapshot = snapshot with
        {
            ItemDesignAreaTargets = [.. snapshot.ItemDesignAreaTargets, new ItemDesignAreaTarget(snapshot.Items[0].Id, Guid.NewGuid())]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveAsync(snapshot, TestContext.Current.CancellationToken));

        Assert.Contains("existing design area", exception.Message);
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
            DesignAreas = [],
            ItemDesignAreaTargets = []
        };
        await repository.SaveAsync(reduced, TestContext.Current.CancellationToken);

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Empty(loaded.StoreProducts);
        Assert.Empty(loaded.ItemDesignAreaTargets);
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
        var target = new ItemDesignAreaTarget(item.Id, area.Id);

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
            DesignAreas = [area],
            ItemDesignAreaTargets = [target]
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
                    niche_id TEXT NOT NULL REFERENCES niches(id) ON DELETE CASCADE,
                    group_id TEXT NULL REFERENCES groups(id) ON DELETE SET NULL,
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

    private static async Task<int> ReadUserVersionAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
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
