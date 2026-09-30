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
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Catalog;

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
    public async Task ProductCatalogEditor_OwnsStoreScopedLoadDraftAndSaveWorkflow()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(repository));
        editor.SetScope(new StoreManagementScope(store.WorkspaceId, store.Id));

        await editor.LoadAsync(TestContext.Current.CancellationToken);
        editor.StartCreateProduct();
        editor.ProductName = "Bella Canvas 3001";
        editor.ProductDescription = "Soft tee";
        await editor.SaveSelectedProductAsync(TestContext.Current.CancellationToken);

        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var product = Assert.Single(saved.StoreProducts);
        Assert.Equal(store.Id, product.StoreId);
        Assert.Equal("Bella Canvas 3001", product.Name);
        Assert.Equal("Soft tee", product.Description);
        Assert.Equal(product.Id, editor.SelectedProduct?.Id);
        Assert.Single(editor.Products);
        Assert.False(editor.HasUnsavedProductChanges);
    }

    [Fact]
    public async Task LateProductCreateDoesNotClobberReplacementDraftInSameScope()
    {
        var store = NewStore("North Star");
        var repository = new SaveGatedWorkspaceRepository(SnapshotWithCatalog(store, addProduct: false));
        var workspaceChanged = false;
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(repository), workspaceChanged: () => workspaceChanged = true);
        editor.SetScope(new StoreManagementScope(store.WorkspaceId, store.Id));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        editor.StartCreateProduct();
        editor.ProductName = "Old draft";

        var save = editor.SaveSelectedProductAsync(TestContext.Current.CancellationToken);
        await repository.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.DiscardUnsavedChanges();
        editor.StartCreateProduct();
        editor.ProductName = "Replacement draft";
        repository.ReleaseSave();
        await save;

        Assert.True(editor.IsCreatingNewProduct);
        Assert.Equal("Replacement draft", editor.ProductName);
        Assert.True(workspaceChanged);
    }

    [Fact]
    public async Task LateOfferingCreateDoesNotClobberReplacementDraftInSameScope()
    {
        var store = NewStore("North Star");
        var repository = new SaveGatedWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var workspaceChanged = false;
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(repository), workspaceChanged: () => workspaceChanged = true);
        editor.SetScope(new StoreManagementScope(store.WorkspaceId, store.Id));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        editor.SelectProductForEditing(Assert.Single(editor.Products));
        editor.StartCreateOffering();
        editor.OfferingName = "Old draft";
        editor.OfferingProviderName = "Local provider";

        var save = editor.SaveSelectedOfferingAsync(TestContext.Current.CancellationToken);
        await repository.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.DiscardUnsavedChanges();
        editor.StartCreateOffering();
        editor.OfferingName = "Replacement draft";
        repository.ReleaseSave();
        await save;

        Assert.True(editor.IsCreatingNewOffering);
        Assert.Equal("Replacement draft", editor.OfferingName);
        Assert.True(workspaceChanged);
    }

    [Fact]
    public void ProductCatalogEditor_NavigationRequestsDiscardForUnsavedProduct()
    {
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(new InMemoryWorkspaceRepository()));
        editor.SetScope(new StoreManagementScope(Guid.NewGuid(), Guid.NewGuid()));
        editor.StartCreateProduct();
        editor.ProductName = "Unsaved product";
        Action? discard = null;
        var navigationRequested = false;
        editor.DiscardRequested += action => discard = action;
        editor.NavigationRequested += _ => navigationRequested = true;

        editor.NavigateCatalog(CatalogEditorLevel.Overview);

        Assert.NotNull(discard);
        Assert.False(navigationRequested);
    }

    [Fact]
    public void ChangingProductEditorScopeRaisesAvailabilityPropertyNotifications()
    {
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(new InMemoryWorkspaceRepository()));
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        editor.SetScope(new StoreManagementScope(workspaceId, storeId, IsStoreArchived: true));
        Assert.False(editor.CanCreateCatalogItem);
        var changed = new HashSet<string?>();
        editor.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        editor.SetScope(new StoreManagementScope(workspaceId, storeId, IsStoreArchived: false));

        Assert.True(editor.CanCreateCatalogItem);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanCreateCatalogItem), changed);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanSaveSelectedProduct), changed);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanSaveSelectedOffering), changed);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanDeleteSelectedProduct), changed);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanArchiveSelectedProduct), changed);
        Assert.Contains(nameof(ProductCatalogEditorViewModel.CanDeleteSelectedOffering), changed);
    }

    [Fact]
    public async Task ChangingWorkspaceUpdatesProductEditorScopeBeforeLoadingNewWorkspace()
    {
        var firstStore = NewStore("First store");
        var repository = new DelayedFirstLoadWorkspaceRepository(SnapshotWithCatalog(firstStore, addProduct: true));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        var newWorkspaceId = Guid.NewGuid();
        viewModel.ProductCatalogEditor.SetScope(new StoreManagementScope(firstStore.WorkspaceId, firstStore.Id));

        var switchWorkspace = viewModel.SetActiveWorkspaceAsync(newWorkspaceId, TestContext.Current.CancellationToken);
        await repository.FirstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var scopeDuringLoad = viewModel.ProductCatalogEditor.Scope;
        repository.ReleaseFirstLoad();
        await switchWorkspace;

        Assert.Equal(new StoreManagementScope(newWorkspaceId, null), scopeDuringLoad);
    }

    [Fact]
    public async Task ProductCatalogEditor_IgnoresProductLoadFromPreviousStoreScope()
    {
        var firstStore = NewStore("First store");
        var secondStore = NewStore("Second store");
        var repository = new DelayedFirstLoadWorkspaceRepository(SnapshotWithCatalog(firstStore, addProduct: true));
        var editor = new ProductCatalogEditorViewModel(new ProductSupplierSetupService(repository));
        editor.SetScope(new StoreManagementScope(firstStore.WorkspaceId, firstStore.Id));
        var oldLoad = editor.LoadAsync(TestContext.Current.CancellationToken);
        await repository.FirstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(secondStore.WorkspaceId, secondStore.Id));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Empty(editor.Products);

        repository.ReleaseFirstLoad();
        await oldLoad;

        Assert.Equal(secondStore.Id, editor.Scope.StoreId);
        Assert.Empty(editor.Products);
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
    public async Task SwitchingProductWhileOfferingDraftIsActiveRequestsDiscard()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithTwoProducts(store));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);

        var firstProduct = viewModel.Products.Single(value => value.Name == "Gildan 64000");
        var secondProduct = viewModel.Products.Single(value => value.Name == "Alternate product");
        viewModel.OpenProductDetailCommand.Execute(firstProduct);
        viewModel.StartCreateOfferingCommand.Execute(null);
        viewModel.OfferingName = "Unsaved offering";

        viewModel.OpenProductDetailCommand.Execute(secondProduct);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(firstProduct.Id, viewModel.SelectedProduct!.Id);
        Assert.True(viewModel.IsCreatingNewOffering);
    }

    [Fact]
    public async Task StartingProductCreationWhileOfferingDraftIsActiveRequestsDiscard()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.StartCreateOfferingCommand.Execute(null);
        viewModel.OfferingName = "Unsaved offering";

        viewModel.StartCreateProductCommand.Execute(null);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.True(viewModel.IsCreatingNewOffering);
        Assert.False(viewModel.ProductCatalogEditor.IsCreatingNewProduct);
    }

    [Fact]
    public async Task SwitchingOfferingAfterDiscardingVariantDraftClearsItsFields()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var offerings = product.Offerings.ToArray();
        var firstOffering = offerings[0];
        var secondOffering = offerings[1];
        viewModel.OpenOfferingDetailCommand.Execute(firstOffering);
        viewModel.ProductCatalogEditor.StartAddVariant();
        viewModel.ProductCatalogEditor.VariantColor = "Indigo";

        viewModel.OpenOfferingDetailCommand.Execute(secondOffering);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(firstOffering.Id, viewModel.SelectedOffering?.Id);
        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.Equal(secondOffering.Id, viewModel.SelectedOffering?.Id);
        Assert.False(viewModel.ProductCatalogEditor.IsAddingVariant);
        Assert.Equal(string.Empty, viewModel.ProductCatalogEditor.VariantColor);
        Assert.Equal(string.Empty, viewModel.ProductCatalogEditor.VariantSize);
    }

    [Fact]
    public async Task SwitchingOfferingAfterDiscardingDesignAreaDraftClearsItsFields()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var offerings = product.Offerings.ToArray();
        var firstOffering = offerings[0];
        var secondOffering = offerings[1];
        viewModel.OpenOfferingDetailCommand.Execute(firstOffering);
        viewModel.ProductCatalogEditor.StartAddDesignArea();
        viewModel.ProductCatalogEditor.AreaName = "Front print";
        viewModel.ProductCatalogEditor.AreaPosition = "front";

        viewModel.OpenOfferingDetailCommand.Execute(secondOffering);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(firstOffering.Id, viewModel.SelectedOffering?.Id);
        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.Equal(secondOffering.Id, viewModel.SelectedOffering?.Id);
        Assert.False(viewModel.ProductCatalogEditor.IsAddingDesignArea);
        Assert.Equal(string.Empty, viewModel.ProductCatalogEditor.AreaName);
        Assert.Equal(string.Empty, viewModel.ProductCatalogEditor.AreaPosition);
    }

    [Fact]
    public async Task SelectingOfferingWhileProductDraftIsDirtyRequestsDiscard()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var firstOffering = product.Offerings[0];
        var secondOffering = product.Offerings[1];
        viewModel.OpenOfferingDetailCommand.Execute(firstOffering);
        viewModel.ProductName = "Unsaved product name";

        viewModel.OpenOfferingDetailCommand.Execute(secondOffering);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(firstOffering.Id, viewModel.SelectedOffering?.Id);
        Assert.Equal("Unsaved product name", viewModel.ProductName);
        viewModel.ConfirmDiscardChangesCommand.Execute(null);
        Assert.Equal(secondOffering.Id, viewModel.SelectedOffering?.Id);
        Assert.Equal(product.Name, viewModel.ProductName);
    }

    [Fact]
    public async Task StartingOfferingWhileProductDraftIsDirtyRequestsDiscard()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.ProductName = "Unsaved product edit";

        viewModel.StartCreateOfferingCommand.Execute(null);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.False(viewModel.ProductCatalogEditor.IsCreatingNewOffering);
        Assert.Equal("Unsaved product edit", viewModel.ProductName);
    }

    [Fact]
    public async Task SelectingAnotherOfferingPromptsForActiveCatalogSetupDraft()
    {
        var store = NewStore("North Star");
        var productRepository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var catalogRepository = new InMemoryWorkspaceRepository(SnapshotForCatalogSetup(store, productRepository.Snapshot.FulfillmentOfferings));
        var viewModel = NewStoreManagementViewModelWithCatalog(productRepository, catalogRepository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.CatalogSetup!.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var fixedOffering = product.Offerings.Single(value => value.Kind == FulfillmentKind.FixedProvider);
        var networkOffering = product.Offerings.Single(value => value.Kind == FulfillmentKind.PrintifyChoiceNetwork);
        viewModel.OpenOfferingDetailCommand.Execute(fixedOffering);
        Assert.True(viewModel.CatalogSetup!.IsAvailable, viewModel.CatalogSetup.ErrorMessage);
        Assert.True(viewModel.CatalogSetup.CanEdit);
        Assert.Equal(fixedOffering.Id, viewModel.CatalogSetup.SelectedOfferingId);
        Assert.True(viewModel.CatalogSetup.StartAddTemplateCommand.CanExecute(null));
        viewModel.CatalogSetup.StartAddTemplateCommand.Execute(null);
        Assert.True(viewModel.CatalogSetup.IsAddingTemplate);

        viewModel.OpenOfferingDetailCommand.Execute(networkOffering);

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(fixedOffering.Id, viewModel.SelectedOffering!.Id);
        Assert.True(viewModel.CatalogSetup.IsAddingTemplate);
    }

    [Fact]
    public async Task CatalogSetupOnlyDraftCountsAsDirtyWhenClosingEditor()
    {
        var store = NewStore("North Star");
        var productRepository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var catalogRepository = new InMemoryWorkspaceRepository(SnapshotForCatalogSetup(store, productRepository.Snapshot.FulfillmentOfferings));
        var viewModel = NewStoreManagementViewModelWithCatalog(productRepository, catalogRepository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var fixedOffering = product.Offerings.Single(value => value.Kind == FulfillmentKind.FixedProvider);
        viewModel.OpenOfferingDetailCommand.Execute(fixedOffering);
        var dirtyNotifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StoreManagementViewModel.HasAnyUnsavedChanges)) dirtyNotifications++;
        };

        viewModel.CatalogSetup!.StartAddPrintProviderCommand.Execute(null);

        Assert.True(viewModel.CatalogSetup.HasActiveDraft);
        Assert.True(viewModel.HasAnyCatalogUnsavedChanges);
        Assert.True(viewModel.HasAnyUnsavedChanges);
        Assert.True(dirtyNotifications > 0);
        viewModel.CloseStoreEditorCommand.Execute(null);
        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.True(viewModel.IsStoreEditorOpen);
    }

    [Fact]
    public async Task ChangingProductEditorScopeCancelsCatalogSetupDrafts()
    {
        var store = NewStore("North Star");
        var productRepository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var catalogRepository = new InMemoryWorkspaceRepository(SnapshotForCatalogSetup(store, productRepository.Snapshot.FulfillmentOfferings));
        var viewModel = NewStoreManagementViewModelWithCatalog(productRepository, catalogRepository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        var product = Assert.Single(viewModel.Products);
        viewModel.OpenProductDetailCommand.Execute(product);
        var fixedOffering = product.Offerings.Single(value => value.Kind == FulfillmentKind.FixedProvider);
        viewModel.OpenOfferingDetailCommand.Execute(fixedOffering);
        viewModel.CatalogSetup!.StartAddOptionCommand.Execute(null);
        Assert.True(viewModel.CatalogSetup.IsAddingOption);
        Assert.True(viewModel.CatalogSetup.HasActiveDraft);

        viewModel.ProductCatalogEditor.SetScope(viewModel.ProductCatalogEditor.Scope with { StoreId = Guid.NewGuid() });

        Assert.False(viewModel.CatalogSetup.HasActiveDraft);
        Assert.False(viewModel.CatalogSetup.IsAddingOption);
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
    public async Task OfferingOverview_RoutesToFocusedManagementWithoutChangingContext()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        var offering = Assert.Single(viewModel.SelectedProduct!.Offerings, item => item.Kind == FulfillmentKind.FixedProvider);
        viewModel.OpenOfferingDetailCommand.Execute(offering);

        viewModel.OpenVariantManagementCommand.Execute(null);
        Assert.True(viewModel.IsVariantManagement);
        Assert.Equal(offering.Id, viewModel.SelectedOffering!.Id);

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenDesignAreaManagementCommand.Execute(null);
        Assert.True(viewModel.IsDesignAreaManagement);
        Assert.Equal(offering.Id, viewModel.SelectedOffering!.Id);

        viewModel.BackToOfferingOverviewCommand.Execute(null);
        viewModel.OpenMockupTemplateManagementCommand.Execute(null);
        Assert.True(viewModel.IsMockupTemplateManagement);
        Assert.Equal(offering.Id, viewModel.SelectedOffering!.Id);
    }

    [Fact]
    public async Task UnsavedOfferingBasics_BlockFocusedManagementRoute()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        viewModel.OpenOfferingDetailCommand.Execute(Assert.Single(viewModel.SelectedProduct!.Offerings, item => item.Kind == FulfillmentKind.FixedProvider));
        viewModel.OfferingName = "Unsaved name";

        viewModel.OpenVariantManagementCommand.Execute(null);

        Assert.True(viewModel.IsOfferingDetail);
        Assert.True(viewModel.DiscardChangesPromptVisible);

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
    public async Task NewOfferingDraft_RequestsFocusAndCanBeCancelled()
    {
        var store = NewStore("North Star");
        var repository = new InMemoryWorkspaceRepository(SnapshotWithCatalog(store, addProduct: true));
        var viewModel = NewStoreManagementViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenProductsTabCommand.Execute(null);
        viewModel.OpenProductDetailCommand.Execute(Assert.Single(viewModel.Products));
        var focusRequests = 0;
        viewModel.OfferingNameFocusRequested += (_, _) => focusRequests++;

        viewModel.StartCreateOfferingCommand.Execute(null);

        Assert.True(viewModel.IsCreatingNewOffering);
        Assert.Equal(1, focusRequests);

        viewModel.CancelNewOfferingCommand.Execute(null);

        Assert.False(viewModel.IsCreatingNewOffering);
        Assert.True(viewModel.IsProductDetail);
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

    private static StoreManagementViewModel NewStoreManagementViewModelWithCatalog(InMemoryWorkspaceRepository productRepository, InMemoryWorkspaceRepository catalogRepository) =>
        new(
            new StoreManagementService(productRepository),
            new NicheManagementService(productRepository),
            new TagManagementService(productRepository),
            new ProductSupplierSetupService(productRepository),
            new CatalogSetupService(catalogRepository),
            new MockupTemplateSetupService(catalogRepository));

    private static StoreManagementViewModel NewStoreManagementViewModel(InMemoryWorkspaceRepository repository) =>
        new(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new ProductSupplierSetupService(repository));

    private static DesignStageToolViewModel NewDesignToolViewModel(InMemoryWorkspaceRepository repository) =>
        new(
            new EmptyDesignStageService());

    private static WorkspaceSnapshot SnapshotForCatalogSetup(Store store, IReadOnlyList<FulfillmentOffering> productOfferings)
    {
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "Printful", "printful", false, Now, Now);
        var blueprintOfferings = productOfferings.Select(offering => new BlueprintOffering(
            offering.Id,
            blueprint.Id,
            store.Id,
            offering.Name,
            offering.Description,
            offering.Kind == FulfillmentKind.PrintifyChoiceNetwork ? BlueprintOfferingKind.ProviderNetwork : BlueprintOfferingKind.FixedPrintProvider,
            offering.Kind == FulfillmentKind.FixedProvider ? provider.Id : null,
            offering.Kind == FulfillmentKind.PrintifyChoiceNetwork ? "printify-choice" : null,
            null,
            null,
            false,
            Now,
            Now)).ToArray();
        return WorkspaceSnapshot.Empty with
        {
            Workspaces = [WorkspaceSnapshot.DefaultWorkspace(Now)],
            Stores = [store],
            Blueprints = [blueprint],
            BlueprintOfferings = blueprintOfferings,
            PrintProviders = [provider]
        };
    }

    private static WorkspaceSnapshot SnapshotWithTwoProducts(Store store)
    {
        var snapshot = SnapshotWithCatalog(store, addProduct: true);
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Alternate product", null, null, Now, Now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Alternate offering", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        return snapshot with
        {
            StoreProducts = [.. snapshot.StoreProducts, product],
            FulfillmentOfferings = [.. snapshot.FulfillmentOfferings, offering]
        };
    }

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

        public Task<DesignStageResult> RecoverStaleConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure("Not implemented in tests."));

        public Task<DesignStageResult> SaveArtworkPreferencesAsync(Guid itemId, Guid? designAreaId, bool transparentBackground, CancellationToken cancellationToken = default) =>
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

    private sealed class DelayedFirstLoadWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private int _loadCount;
        private readonly TaskCompletionSource _releaseFirstLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstLoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseFirstLoad() => _releaseFirstLoad.TrySetResult();
        public Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public async Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _loadCount) == 1)
            {
                FirstLoadStarted.TrySetResult();
                await _releaseFirstLoad.Task.WaitAsync(cancellationToken);
            }
            return snapshot;
        }
    }

    private sealed class SaveGatedWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;
        private readonly TaskCompletionSource _releaseSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseSave() => _releaseSave.TrySetResult();

        public async Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default)
        {
            SaveStarted.TrySetResult();
            await _releaseSave.Task.WaitAsync(cancellationToken);
            _snapshot = updated;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
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
