using FusionCanvas.App.Stores;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace FusionCanvas.App.Tests;

public sealed class PrintifyListingImportViewModelTests
{
    [Fact]
    public async Task Empty_shop_and_missing_credential_show_distinct_dialog_states()
    {
        var empty = new Fixture([]);
        var emptyViewModel = new PrintifyListingImportViewModel(empty.Service, empty.Scope, empty.NicheId);

        await emptyViewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("This Printify shop has no products.", emptyViewModel.Message);
        Assert.False(emptyViewModel.HasError);

        var missingKey = new Fixture(["available-product"], PrintifyConfigurationKind.Missing);
        var unavailableViewModel = new PrintifyListingImportViewModel(missingKey.Service, missingKey.Scope, missingKey.NicheId);

        await unavailableViewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(unavailableViewModel.HasError);
        Assert.Contains("Printify key", unavailableViewModel.Error);
        Assert.Equal(0, missingKey.Client.ShopProductCalls);
    }

    [Fact]
    public async Task Dialog_reports_mixed_import_outcomes_and_keeps_failed_product_retryable()
    {
        var fixture = new Fixture(["available-product", "missing-product"]);
        var viewModel = new PrintifyListingImportViewModel(fixture.Service, fixture.Scope, fixture.NicheId);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        foreach (var product in viewModel.Products) product.IsSelected = true;

        await viewModel.ImportAsync();

        Assert.Equal("Imported 1 of 2 selected products.", viewModel.Message);
        Assert.Contains("missing-product", viewModel.Error);
        Assert.Equal("Imported", viewModel.Products.Single(row => row.ProductId == "available-product").ImportOutcome);
        Assert.True(viewModel.Products.Single(row => row.ProductId == "missing-product").IsSelected);
        Assert.Single(fixture.Repository.Snapshot.Items);
        Assert.Single(fixture.Repository.Snapshot.ExternalListingMappings);
    }

    [AvaloniaFact]
    public async Task Dialog_loading_state_exposes_a_cancel_action()
    {
        var fixture = new Fixture(["available-product"]);
        fixture.Client.ProductGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = new PrintifyListingImportViewModel(fixture.Service, fixture.Scope, fixture.NicheId);
        var window = new PrintifyListingImportWindow { DataContext = viewModel };

        try
        {
            window.Show();
            var loadTask = viewModel.LoadAsync(TestContext.Current.CancellationToken);
            Assert.True(viewModel.IsBusy);
            var cancelButton = window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Cancel operation"));
            Assert.True(cancelButton.IsVisible);
            Assert.True(cancelButton.IsEnabled);

            viewModel.CancelCommand.Execute(null);
            await loadTask;

            Assert.False(viewModel.IsBusy);
            Assert.Equal("Loading cancelled.", viewModel.Message);
        }
        finally { window.Close(); }
    }

    private sealed class Fixture
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        private readonly StoreSummary _store;
        public Guid NicheId { get; } = Guid.NewGuid();
        public StoreCredentialScope Scope { get; }
        public Repository Repository { get; }
        public Client Client { get; }
        public PrintifyListingImportService Service { get; }

        public Fixture(string[] products, PrintifyConfigurationKind credentialKind = PrintifyConfigurationKind.Available)
        {
            var workspaceId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            Scope = new(workspaceId, storeId);
            _store = new(storeId, workspaceId, "Store", new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.Printify);
            var niche = new Niche(NicheId, storeId, "Wildlife", null, false, Now, Now, "{}");
            Repository = new(WorkspaceSnapshot.Empty with
            {
                Stores = [new Store(storeId, workspaceId, "Store", null, false, Now, Now, "{}", fulfillmentStrategy: FulfillmentStrategy.Printify)],
                Niches = [niche]
            });
            Client = new(products);
            Service = new PrintifyListingImportService(new StoreReader(_store), new CredentialReader(credentialKind), Client, Repository, new FileStore(), clock: () => Now);
        }
    }

    private sealed class StoreReader(StoreSummary store) : IStoreContextReader
    {
        public Task<StoreSummary?> ResolveActiveStoreAsync(Guid workspaceId, Guid storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoreSummary?>(workspaceId == store.WorkspaceId && storeId == store.Id ? store : null);
    }

    private sealed class CredentialReader(PrintifyConfigurationKind kind) : IStorePrintifyCredentialStore
    {
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCredentialReadResult(new(kind, "test"), kind == PrintifyConfigurationKind.Available ? "fake-key" : null));
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class Client(string[] productIds) : IPrintifyListingImportClient
    {
        public int ShopProductCalls { get; private set; }
        public TaskCompletionSource<IReadOnlyList<PrintifyListingProductSummary>>? ProductGate { get; set; }
        public async Task<IReadOnlyList<PrintifyListingProductSummary>> GetShopProductsAsync(string apiKey, int shopId, CancellationToken cancellationToken = default)
        {
            ShopProductCalls++;
            if (ProductGate is not null) return await ProductGate.Task.WaitAsync(cancellationToken);
            return productIds.Select(id => new PrintifyListingProductSummary(id, id, "Description", true, 68, 9)).ToArray();
        }
        public Task<PrintifyListingProductDetail?> GetProductAsync(string apiKey, int shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PrintifyListingProductDetail?>(productId == "missing-product" ? null : new(productId, productId, "Description", true, 68, 9, null, [], [], []));
        public Task<PrintifyArtworkDownload> DownloadArtworkAsync(string sourceUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class Repository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class FileStore : IWorkspaceFileOutputStore
    {
        public bool Exists(string workspaceRelativePath) => false;
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public bool TryDelete(string workspaceRelativePath) => false;
    }
}
