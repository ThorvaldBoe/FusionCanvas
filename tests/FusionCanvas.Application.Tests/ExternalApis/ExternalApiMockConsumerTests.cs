using System.Text.Json;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Testing;

namespace FusionCanvas.Application.Tests.ExternalApis;

public sealed class ExternalApiMockConsumerTests
{
    [Fact]
    public async Task PrintifyCatalogImportUsesTheReusableCatalogMockWithoutExposingTheKey()
    {
        var store = CreateStore();
        var client = new MockPrintifyCatalogClient();
        var service = new PrintifyCatalogImportService(
            new StoreReader(store),
            new CredentialStore(),
            client);

        var result = await service.LoadBlueprintsAsync(
            new(store.WorkspaceId, store.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("mock-product-1", Assert.Single(result.Products!).ProductId);
        var request = Assert.Single(client.Requests);
        Assert.Equal(nameof(MockPrintifyCatalogClient.LoadShopProductsAsync), request.Operation);
        Assert.Equal(9001, request.ShopId);
        Assert.DoesNotContain("synthetic-printify-key", JsonSerializer.Serialize(client.Requests), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrintifyCatalogImportPropagatesAConfiguredMockFailure()
    {
        var store = CreateStore();
        var client = new MockPrintifyCatalogClient
        {
            ShopProductsResultOverride = new(PrintifyCatalogResultKind.NetworkFailure, "Synthetic outage.")
        };
        var service = new PrintifyCatalogImportService(new StoreReader(store), new CredentialStore(), client);

        var result = await service.LoadBlueprintsAsync(
            new(store.WorkspaceId, store.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.NetworkFailure, result.Kind);
    }

    [Fact]
    public async Task StorePrintifyConfigurationUsesTheReusableVerifierMock()
    {
        var store = CreateStore();
        var verifier = new MockPrintifyCredentialVerifier();
        var service = new StorePrintifyConfigurationService(
            new StoreReader(store),
            new CredentialStore(),
            verifier);

        var result = await service.VerifyAsync(
            new(store.WorkspaceId, store.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyConfigurationKind.Verified, result.Kind);
        Assert.Equal(1, verifier.VerificationCalls);
    }

    private static StoreSummary CreateStore() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Synthetic Store",
            new(PrintifyShopId: 9001, PrintifyShopTitle: "Synthetic Shop"),
            false,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            FulfillmentStrategy.Printify);

    private sealed class StoreReader(StoreSummary store) : IStoreContextReader
    {
        public Task<StoreSummary?> ResolveActiveStoreAsync(
            Guid workspaceId,
            Guid storeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StoreSummary?>(
                workspaceId == store.WorkspaceId && storeId == store.Id ? store : null);
    }

    private sealed class CredentialStore : IStorePrintifyCredentialStore
    {
        public Task<PrintifyCredentialReadResult> ReadAsync(
            StoreCredentialScope scope,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCredentialReadResult(
                new(PrintifyConfigurationKind.Available, "Synthetic credential available."),
                "synthetic-printify-key"));

        public Task<PrintifyConfigurationResult> SaveAsync(
            StoreCredentialScope scope,
            string key,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "Synthetic credential saved."));
    }
}
