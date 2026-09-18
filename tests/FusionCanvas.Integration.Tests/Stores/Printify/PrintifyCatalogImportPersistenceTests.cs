using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.Integration.Tests;

public sealed class PrintifyCatalogImportPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RepeatedImportUpdatesProviderRecordsAndPreservesLocalOnlyBlueprint()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("printify-import.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now, "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var localBlueprint = new Blueprint(Guid.NewGuid(), storeId, "Local Draft", null, false, Now, Now);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot([new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")], [store], [], [], [], [], [], [], [], [])
        {
            Blueprints = [localBlueprint]
        }, TestContext.Current.CancellationToken);

        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.ShopifyPrintify);
        var client = new ClientStub { Catalog = Catalog("First title", 3000) };
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), client, repository);

        var first = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        client.Catalog = Catalog("Changed title", 4500);
        var second = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.Single(loaded.Blueprints, value => value.Id != localBlueprint.Id);
        Assert.Equal("Brand Model", loaded.Blueprints.Single(value => value.Id != localBlueprint.Id).Name);
        Assert.Contains(loaded.Blueprints, value => value.Id == localBlueprint.Id && value.Name == "Local Draft");
        Assert.Single(loaded.PrintProviders);
        Assert.Equal("Changed provider", Assert.Single(loaded.PrintProviders).Name);
        Assert.Single(loaded.BlueprintOfferings);
        Assert.Equal("Changed title · Changed provider", Assert.Single(loaded.BlueprintOfferings).Name);
        Assert.Single(loaded.OfferingVariants);
        Assert.Equal("Changed variant", Assert.Single(loaded.OfferingVariants).Name);
        Assert.Single(loaded.OfferingPlaceholders);
        Assert.Equal(4500, Assert.Single(loaded.OfferingPlaceholders).Width);
        Assert.Contains(loaded.StoreProducts, value => value.Id != localBlueprint.Id && value.Name == "Brand Model");
        Assert.Single(loaded.FulfillmentOfferings);
        Assert.Single(loaded.ProductVariants);
        Assert.Equal(4500, Assert.Single(loaded.DesignAreas).Width);
        Assert.Single(loaded.OfferingOptions);
        Assert.Single(loaded.OfferingOptionValues);
    }

    [Fact]
    public async Task ConsolidationRoundTripsArchivedDuplicateAndMigratedMockupReferences()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("printify-consolidation.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now, "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot([new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")], [store], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);

        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.ShopifyPrintify);
        var client = new ClientStub { Catalog = Catalog("Initial title", 100) };
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), client, repository);
        var first = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var imported = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var offering = Assert.Single(imported.BlueprintOfferings);
        var area = Assert.Single(imported.OfferingPlaceholders);
        var duplicate = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "front", null, "front", "dtg", 80, 160,
            area.VariantIds, false, Now, Now, area.MetadataJson, "front");
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, duplicate.Id, "Front template", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, duplicate.Id, Now);
        var legacyDuplicate = new DesignArea(duplicate.Id, offering.Id, duplicate.Name, duplicate.Description, duplicate.Position, duplicate.DecorationMethod,
            duplicate.Width, duplicate.Height, duplicate.VariantIds, duplicate.CreatedAt, duplicate.UpdatedAt, duplicate.MetadataJson);
        await repository.SaveAsync(imported with
        {
            BlueprintOfferings = [offering with { DefaultPlaceholderId = duplicate.Id, PrimaryArtworkDesignAreaId = duplicate.Id }],
            OfferingPlaceholders = [area, duplicate],
            MockupTemplates = [template],
            MockupTemplateRevisions = [revision],
            DesignAreas = [.. imported.DesignAreas, legacyDuplicate]
        }, TestContext.Current.CancellationToken);

        client.Catalog = Catalog("Updated title", 120);
        var second = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        var canonical = Assert.Single(loaded.OfferingPlaceholders, value => !value.IsArchived);
        Assert.True(loaded.OfferingPlaceholders.Single(value => value.Id == duplicate.Id).IsArchived);
        Assert.Equal(canonical.Id, loaded.BlueprintOfferings.Single().DefaultPlaceholderId);
        Assert.Equal(canonical.Id, loaded.BlueprintOfferings.Single().PrimaryArtworkDesignAreaId);
        Assert.Equal(canonical.Id, loaded.MockupTemplates.Single().TargetPlaceholderId);
        Assert.Equal(canonical.Id, loaded.MockupTemplateRevisions.Single().TargetPlaceholderId);
        Assert.Single(loaded.DesignAreas);
        Assert.Equal(canonical.Id, loaded.DesignAreas.Single().Id);
        Assert.Equal(120, canonical.Width);
    }

    private static IReadOnlyList<PrintifyCatalogBlueprint> Catalog(string title, int width) =>
    [
        new(
            new(68, title, "Description", "Brand", "Model"),
             [new(7, "Changed provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Changed variant", true, true, [1], [new("front", "dtg", width, 4500)])])])
    ];

    private sealed class StoresStub(StoreSummary store) : IStoreManagementService
    {
        public Guid? ActiveWorkspaceId => store.WorkspaceId;
        public Guid? ActiveStoreId => store.Id;
        public void SetActiveWorkspace(Guid? workspaceId) { }
        public Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new StoreManagementState(store.WorkspaceId, [store], [], store.Id, store, false));
        public Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class CredentialStore : IStorePrintifyCredentialStore
    {
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Available, "available"), "test-key"));
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ClientStub : IPrintifyCatalogClient
    {
        public IReadOnlyList<PrintifyCatalogBlueprint> Catalog { get; set; } = [];
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "unused", []));
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: Catalog));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvas", Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(_path);
        public string GetPath(string fileName) => Path.Combine(_path, fileName);
        public void Dispose() { if (Directory.Exists(_path)) Directory.Delete(_path, true); }
    }
}
