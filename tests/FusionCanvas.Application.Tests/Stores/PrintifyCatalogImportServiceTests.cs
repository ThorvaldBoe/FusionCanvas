using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Stores;

public sealed class PrintifyCatalogImportServiceTests
{
    [Fact]
    public async Task RefusesManualStoreWithoutReadingCredential()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Manual);
        var stores = new StoresStub(store);
        var credentials = new CredentialsStub();
        var client = new ClientStub();
        var service = new PrintifyCatalogImportService(stores, credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.InvalidRequest, result.Kind);
        Assert.Equal(0, credentials.Reads);
        Assert.Equal(0, client.Calls);
    }

    [Theory]
    [InlineData(FulfillmentStrategy.ShopifyPrintify)]
    [InlineData(FulfillmentStrategy.Printify)]
    public async Task LoadsCatalogOnlyAfterAllStoreGuardsPass(FulfillmentStrategy strategy)
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, strategy);
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var client = new ClientStub { Result = new(PrintifyCatalogResultKind.Succeeded, "loaded", []) };
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(1, credentials.Reads);
        Assert.Equal("synthetic-key", client.LastKey);
    }

    [Fact]
    public async Task LoadsProductsFromTheSelectedPrintifyShop()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var client = new ShopProductsClientStub();
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(42, client.ShopId);
        Assert.Equal("synthetic-key", client.LastKey);
        Assert.Equal(["product-a"], result.Products!.Select(value => value.ProductId));
    }

    [Fact]
    public async Task ImportsSelectedCatalogAndUpdatesExistingPrintifyRecords()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with { Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)] });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Updated Tee", null, "Gildan", "5000"),
                [new(9, "Provider", [], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])])])])
        };
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client, repository);

        var first = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);
        var second = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Single(repository.Snapshot.Blueprints);
        Assert.Single(repository.Snapshot.PrintProviders);
        Assert.Single(repository.Snapshot.BlueprintOfferings);
        var option = Assert.Single(repository.Snapshot.OfferingOptions);
        Assert.Equal(OptionKind.Color, option.OptionKind);
        Assert.Contains("\"kind\":\"option\"", option.MetadataJson);
        var optionValue = Assert.Single(repository.Snapshot.OfferingOptionValues);
        Assert.Contains("\"kind\":\"option-value\"", optionValue.MetadataJson);
        Assert.Single(repository.Snapshot.OfferingVariants);
        var variant = Assert.Single(repository.Snapshot.OfferingVariants);
        var placeholder = Assert.Single(repository.Snapshot.OfferingPlaceholders);
        Assert.Equal([variant.Id], placeholder.VariantIds);
    }

    [Fact]
    public async Task ImportsProviderIdentityWithinTargetStoreOnly()
    {
        var workspaceId = Guid.NewGuid();
        var targetStore = new StoreSummary(Guid.NewGuid(), workspaceId, "Target", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var otherStore = new StoreSummary(Guid.NewGuid(), workspaceId, "Other", new(PrintifyShopId: 84), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var otherProvider = new PrintProvider(Guid.NewGuid(), otherStore.Id, "Other provider", "7", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var repository = new RepositoryStub(new WorkspaceSnapshot([], [
            new Store(targetStore.Id, workspaceId, targetStore.Name, null, false, targetStore.CreatedAt, targetStore.UpdatedAt, "{}", null, targetStore.FulfillmentStrategy),
            new Store(otherStore.Id, workspaceId, otherStore.Name, null, false, otherStore.CreatedAt, otherStore.UpdatedAt, "{}", null, otherStore.FulfillmentStrategy)
        ], [], [], [], [], [], [], [], []) { PrintProviders = [otherProvider] });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Updated Tee", null, "Gildan", "5000"),
                [new(7, "Target provider", [], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])])])])
        };
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var service = new PrintifyCatalogImportService(new StoresStub(targetStore), credentials, client, repository);

        var result = await service.LoadSelectedAsync(new(workspaceId, targetStore.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(2, repository.Snapshot.PrintProviders.Count);
        Assert.Contains(repository.Snapshot.PrintProviders, value => value.StoreId == otherStore.Id && value.ExternalProviderId == "7");
        Assert.Contains(repository.Snapshot.PrintProviders, value => value.StoreId == targetStore.Id && value.ExternalProviderId == "7");
    }

    [Fact]
    public async Task RejectsDuplicateProviderPayloadWithoutSaving()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with { Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)] });
        var duplicateProvider = new PrintifyCatalogProvider(7, "Provider", [], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])]);
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(new(68, "Tee", null, "Brand", "Model"), [duplicateProvider, duplicateProvider])])
        };
        var service = new PrintifyCatalogImportService(new StoresStub(store), new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "key") }, client, repository);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.UnexpectedResponse, result.Kind);
        Assert.Empty(repository.Snapshot.Blueprints);
        Assert.Empty(repository.Snapshot.PrintProviders);
    }

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

    private sealed class CredentialsStub : IStorePrintifyCredentialStore
    {
        public int Reads { get; private set; }
        public PrintifyCredentialReadResult Result { get; init; } = new(new(PrintifyConfigurationKind.Missing, "missing"));
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) { Reads++; return Task.FromResult(Result); }
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ClientStub : IPrintifyCatalogClient
    {
        public int Calls { get; private set; }
        public string? LastKey { get; private set; }
        public PrintifyCatalogResult Result { get; init; } = new(PrintifyCatalogResultKind.Empty, "empty", []);
        public PrintifyCatalogResult SelectedResult { get; init; } = new(PrintifyCatalogResultKind.Empty, "empty", []);
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) { Calls++; LastKey = key; return Task.FromResult(Result); }
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => Task.FromResult(SelectedResult);
    }

    private sealed class ShopProductsClientStub : IPrintifyCatalogClient
    {
        public int ShopId { get; private set; }
        public string? LastKey { get; private set; }

        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PrintifyCatalogResult> LoadShopProductsAsync(string key, int shopId, CancellationToken cancellationToken = default)
        {
            LastKey = key;
            ShopId = shopId;
            return Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "loaded", Products: [new("product-a", "Test product", null, 68, 9)]));
        }
    }

    private sealed class RepositoryStub(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }
}
