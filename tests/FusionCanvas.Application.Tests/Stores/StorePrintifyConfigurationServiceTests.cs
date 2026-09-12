using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Stores;

public class StorePrintifyConfigurationServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(FulfillmentStrategy.Manual)]
    [InlineData(FulfillmentStrategy.ShopifyManual)]
    [InlineData(FulfillmentStrategy.ShopifyPrintify)]
    [InlineData(FulfillmentStrategy.Printify)]
    public async Task Strategies_SaveAndReloadWithoutKeys(FulfillmentStrategy strategy)
    {
        var repository = new Repository();
        var stores = new StoreManagementService(repository);
        var created = await stores.CreateStoreAsync(new("Store", FulfillmentStrategy: strategy), Ct);
        Assert.True(created.Succeeded);
        Assert.Equal(strategy, (await stores.LoadAsync(Ct)).ActiveStore!.FulfillmentStrategy);
        Assert.False((await stores.CreateStoreAsync(new("Invalid", FulfillmentStrategy: (FulfillmentStrategy)99), Ct)).Succeeded);
    }

    [Fact]
    public async Task PrintifyShopSelection_SurvivesStoreReload()
    {
        var repository = new Repository();
        var stores = new StoreManagementService(repository);
        var created = (await stores.CreateStoreAsync(new("Store", new StoreContext(PrintifyShopId: 123, PrintifyShopTitle: "DevTest shop"), FulfillmentStrategy.ShopifyPrintify), Ct)).Store!;

        var loaded = await stores.LoadAsync(Ct);

        Assert.Equal(123, loaded.ActiveStore!.Context.PrintifyShopId);
        Assert.Equal("DevTest shop", loaded.ActiveStore.Context.PrintifyShopTitle);
        Assert.Equal(created.Id, loaded.ActiveStore.Id);
    }

    [Fact]
    public async Task Keys_AreIsolatedAndDoNotEnterWorkspaceData()
    {
        var repository = new Repository();
        var stores = new StoreManagementService(repository);
        var first = (await stores.CreateStoreAsync(new("A", FulfillmentStrategy: FulfillmentStrategy.ShopifyPrintify), Ct)).Store!;
        var second = (await stores.CreateStoreAsync(new("B", FulfillmentStrategy: FulfillmentStrategy.ShopifyPrintify), Ct)).Store!;
        var native = new Native();
        var verifier = new Verifier();
        var service = new StorePrintifyConfigurationService(stores, native, verifier);
        var a = new StoreCredentialScope(first.WorkspaceId, first.Id);
        var b = new StoreCredentialScope(second.WorkspaceId, second.Id);
        Assert.True((await service.SaveAsync(a, " synthetic-a ", Ct)).Succeeded);
        Assert.True((await service.SaveAsync(b, "synthetic-b", Ct)).Succeeded);
        await service.VerifyAsync(a, Ct);
        Assert.Equal("synthetic-a", verifier.LastKey);
        await service.VerifyAsync(b, Ct);
        Assert.Equal("synthetic-b", verifier.LastKey);
        await stores.UpdateStoreAsync(new(first.Id, "Renamed", first.Context, FulfillmentStrategy.ShopifyPrintify), Ct);
        Assert.Equal(PrintifyConfigurationKind.Available, (await service.ReadStatusAsync(a, Ct)).Kind);
        Assert.DoesNotContain("synthetic-", System.Text.Json.JsonSerializer.Serialize(repository.Snapshot));
        Assert.Equal(PrintifyConfigurationKind.Missing,
            (await new StorePrintifyConfigurationService(stores, new Native(), verifier).ReadStatusAsync(a, Ct)).Kind);
    }

    [Fact]
    public async Task WrongDeletedAndArchivedScopes_NeverAccessNativeStore()
    {
        var repository = new Repository();
        var stores = new StoreManagementService(repository);
        var store = (await stores.CreateStoreAsync(new("Store"), Ct)).Store!;
        var native = new Native();
        var service = new StorePrintifyConfigurationService(stores, native, new Verifier());
        foreach (var scope in new[] {
            new StoreCredentialScope(Guid.NewGuid(), store.Id),
            new StoreCredentialScope(store.WorkspaceId, Guid.NewGuid()),
            new StoreCredentialScope(store.WorkspaceId, Guid.Empty) })
        {
            Assert.Equal(PrintifyConfigurationKind.InvalidContext, (await service.ReadStatusAsync(scope, Ct)).Kind);
            Assert.Equal(PrintifyConfigurationKind.InvalidContext, (await service.SaveAsync(scope, "key", Ct)).Kind);
        }
        await stores.ArchiveStoreAsync(store.Id, Ct);
        var archived = new StoreCredentialScope(store.WorkspaceId, store.Id);
        Assert.Equal(PrintifyConfigurationKind.InvalidContext, (await service.VerifyAsync(archived, Ct)).Kind);
        Assert.Equal(PrintifyConfigurationKind.InvalidContext, (await service.ReadStatusAsync(archived, Ct)).Kind);
        Assert.Empty(native.Keys);
        Assert.Equal(0, native.Reads);
    }

    [Theory]
    [InlineData(FulfillmentStrategy.Manual)]
    [InlineData(FulfillmentStrategy.ShopifyManual)]
    public async Task NonPrintifyVerification_IsRefusedBeforeKeyLookup(FulfillmentStrategy strategy)
    {
        var stores = new StoreManagementService(new Repository());
        var store = (await stores.CreateStoreAsync(new("Store", FulfillmentStrategy: strategy), Ct)).Store!;
        var native = new Native();
        var verifier = new Verifier();
        var service = new StorePrintifyConfigurationService(stores, native, verifier);
        Assert.Equal(PrintifyConfigurationKind.InvalidContext,
            (await service.VerifyAsync(new(store.WorkspaceId, store.Id), Ct)).Kind);
        Assert.Equal(0, native.Reads);
        Assert.Null(verifier.LastKey);
    }

    [Fact]
    public async Task MissingOrUnreadableKey_DoesNotVerifyOrOverwrite()
    {
        var stores = new StoreManagementService(new Repository());
        var store = (await stores.CreateStoreAsync(new("Store", FulfillmentStrategy: FulfillmentStrategy.ShopifyPrintify), Ct)).Store!;
        var native = new Native();
        var verifier = new Verifier();
        var service = new StorePrintifyConfigurationService(stores, native, verifier);
        var scope = new StoreCredentialScope(store.WorkspaceId, store.Id);
        Assert.Equal(PrintifyConfigurationKind.Missing, (await service.VerifyAsync(scope, Ct)).Kind);
        native.Unavailable = true;
        Assert.False((await service.SaveAsync(scope, "replacement", Ct)).Succeeded);
        Assert.Null(verifier.LastKey);
        Assert.Empty(native.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("bad\r\nheader")]
    public void InvalidTokens_AreRejected(string token) => Assert.False(PrintifyToken.IsValid(token));

    private sealed class Repository : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = WorkspaceSnapshot.Empty;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        { Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class Native : IStorePrintifyCredentialStore
    {
        public Dictionary<StoreCredentialScope, string> Keys { get; } = [];
        public int Reads { get; private set; }
        public bool Unavailable { get; set; }
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Unavailable ? new PrintifyCredentialReadResult(PrintifyConfigurationResult.Unavailable) :
                Keys.TryGetValue(scope, out var key) ? new(new(PrintifyConfigurationKind.Available, "provided"), key) :
                new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Missing, "required")));
        }
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default)
        { Keys[scope] = key; return Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "saved")); }
    }

    private sealed class Verifier : IPrintifyCredentialVerifier
    {
        public string? LastKey { get; private set; }
        public Task<PrintifyConfigurationResult> VerifyAsync(string key, CancellationToken cancellationToken = default)
        { LastKey = key; return Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Verified, "verified")); }
    }
}
