using FusionCanvas.App.StageTools;
using FusionCanvas.App.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Products;
using FusionCanvas.Application.DesignFiles;

namespace FusionCanvas.App.Tests;

public class ProductCatalogViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProductsTab_OpensAndLoadsProductsForSelectedStore()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.OpenProductsTabCommand.Execute(null);

        Assert.True(viewModel.IsProductsTabSelected);
        Assert.Single(viewModel.Products);
        Assert.Equal("Gildan 64000", viewModel.Products[0].Name);
        Assert.True(viewModel.IsCatalogOverview);
        Assert.False(viewModel.IsProductDetail);
    }

    [Fact]
    public async Task ProductAndOfferingNavigation_ExposesProgressiveDisclosureSummaries()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);

        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);

        Assert.True(viewModel.IsProductDetail);
        Assert.Equal(2, viewModel.SelectedProductOfferingCount);
        Assert.Contains("2 fulfillment offerings", viewModel.SelectedProductSummary);

        var offering = Assert.Single(product.Offerings, item => item.Kind == FulfillmentKind.FixedProvider);
        viewModel.OpenOfferingDetailCommand.Execute(offering);

        Assert.True(viewModel.IsOfferingDetail);
        Assert.Equal(1, viewModel.SelectedOfferingVariantCount);
        Assert.Equal(1, viewModel.SelectedOfferingDesignAreaCount);
        Assert.Contains("1 variant", viewModel.SelectedOfferingSummary);
    }

    [Fact]
    public async Task CatalogBackNavigation_ReturnsToOverviewAndGuardsUnsavedOffering()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(
            viewModel.SelectedProduct!.Offerings,
            offering => offering.Kind == FulfillmentKind.FixedProvider));

        viewModel.OfferingName = "Edited offering";
        viewModel.BackToProductCommand.Execute(null);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.True(viewModel.IsOfferingDetail);

        viewModel.KeepEditingCommand.Execute(null);
        Assert.True(viewModel.IsOfferingDetail);
    }

    [Fact]
    public async Task EmptyStore_ProductsTabShowsNoProductsAndCanCreateDraft()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.OpenProductsTabCommand.Execute(null);

        Assert.True(viewModel.IsProductsTabSelected);
        Assert.Empty(viewModel.Products);

        viewModel.StartCreateProductCommand.Execute(null);

        Assert.Equal("New product", Assert.Single(viewModel.EditorProducts).Name);
        Assert.True(viewModel.HasSelectedProduct);
    }

    [Fact]
    public async Task ArchivedStore_BlocksCatalogCreation()
    {
        var store = NewStore("North Star", isArchived: true);
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.CanCreateCatalogItem);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.StartCreateProductCommand.Execute(null);

        Assert.NotEmpty(viewModel.ErrorMessage ?? string.Empty);
        Assert.Empty(viewModel.EditorProducts);
    }

    [Fact]
    public async Task UnsavedCatalogDraft_PromptsDiscardOnTabSwitch()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.StartCreateProductCommand.Execute(null);
        viewModel.ProductName = "Gildan 64000";

        viewModel.SelectBasicInfoTabCommand.Execute(null);

        Assert.True(viewModel.DiscardChangesPromptVisible);
    }

    [Fact]
    public async Task NewProductDraft_RequestsProductNameFocus()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var focusRequests = 0;
        viewModel.ProductNameFocusRequested += (_, _) => focusRequests++;

        viewModel.StartCreateProductCommand.Execute(null);

        Assert.Equal(1, focusRequests);
    }

    [Fact]
    public async Task CreateProductViaEditor_PersistsToRepository()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.StartCreateProductCommand.Execute(null);
        viewModel.ProductName = "Bella Canvas 3001";
        viewModel.ProductDescription = "Soft tee";

        await viewModel.SaveSelectedProductAsync(TestContext.Current.CancellationToken);

        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var product = Assert.Single(saved.StoreProducts);
        Assert.Equal("Bella Canvas 3001", product.Name);
        Assert.Equal("Soft tee", product.Description);
        Assert.Single(viewModel.Products);
    }

    [Fact]
    public async Task DesignTool_LoadsFiles()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewDesignToolViewModel(repository);
        var itemId = Guid.NewGuid();
        await viewModel.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel);
        Assert.False(viewModel.HasConfiguration);
    }

    private static StoreManagementViewModel NewStoreManagementViewModel(InMemoryWorkspaceRepository repository) =>
        new(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new ProductSupplierSetupService(repository));

    private static DesignStageToolViewModel NewDesignToolViewModel(InMemoryWorkspaceRepository repository) =>
        new(
            new EmptyDesignStageService());

    private static WorkspaceSnapshot SnapshotWithCatalog(Store store, bool addProduct, bool addItem = false, bool choiceArea = false)
    {
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Gildan 64000", "Blank tee", null, Now, Now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        var variant = new ProductVariant(Guid.NewGuid(), offering.Id, [new VariantOption("Color", "Black")], Now, Now);
        var regularArea = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], Now, Now, "{}");
        var choiceOffering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Choice", null, FulfillmentKind.PrintifyChoiceNetwork, null, null, Now, Now, "{}");
        var choiceDesignArea = new DesignArea(Guid.NewGuid(), choiceOffering.Id, "Choice front", null, "front", "DTG", 3000, 4500, [], Now, Now, "{}");

        var products = new List<StoreProduct>();
        var offerings = new List<FulfillmentOffering>();
        var variants = new List<ProductVariant>();
        var areas = new List<DesignArea>();
        var items = new List<Item>();
        if (addProduct)
        {
            products.Add(product);
            offerings.Add(offering);
            offerings.Add(choiceOffering);
            variants.Add(variant);
            areas.Add(choiceArea ? choiceDesignArea : regularArea);
        }

        if (addItem)
        {
            items.Add(new Item(Guid.NewGuid(), store.Id, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}"));
        }

        return new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [store],
            [],
            [],
            items,
            [],
            [],
            [],
            [],
            [])
        {
            StoreProducts = products,
            FulfillmentOfferings = offerings,
            ProductVariants = variants,
            DesignAreas = areas
        };
    }

    private static Store NewStore(string name, bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, isArchived, Now, Now, "{}");

    private sealed class EmptyDesignFileService : IDesignFileService
    {
        public Task<IReadOnlyList<DesignFileSummary>> ListForItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DesignFileSummary>>([]);

        public Task<DesignFileImportResult> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignFileImportResult.Failure("No file service in tests."));

        public Task<Stream> OpenPreviewAsync(Guid assetId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExportCopyAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<DesignFileRemoveResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignFileRemoveResult.Failure("No file service in tests."));
    }

    private sealed class EmptyDesignStageService : IDesignStageService
    {
        public Task<DesignStageState> LoadDesignStageStateAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DesignStageState(itemId, false, string.Empty, null, null, null, null, [], [], [], [], []));

        public Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> AddSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> RemoveSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> MakeSpecificForColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> RemoveSpecificRowAsync(Guid itemId, Guid rowId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> AssignSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> ReplaceSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> RemoveSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<Stream> OpenSlotPreviewAsync(Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<DesignSlotSummary>> ListSupportingImagesAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DesignSlotSummary>>([]);

        public Task<DesignStageResult> ImportSupportingImageAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> RemoveSupportingImageAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public WorkspaceSnapshot Snapshot => _snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
