using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Products;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Stores;

/// <summary>Owns product/offering drafts and store-scoped catalog workflows.</summary>
public sealed class ProductCatalogEditorViewModel : INotifyPropertyChanged
{
    private sealed record ProductDraft(string Name, string Description, string ExternalId);
    private sealed record OfferingDraft(string Name, string Description, string ExternalId, int KindIndex, string ProviderName, string AreaName, string AreaPosition, string AreaDecorationMethod, string AreaWidth, string AreaHeight, string VariantColor, string VariantSize);

    private readonly IProductSupplierSetupService? _productsService;
    private readonly ICatalogSetupService? _catalogService;
    private readonly IOfferingManagementService? _offeringService;
    private readonly Action<Func<CancellationToken, Task>> _runOperation;
    private readonly Action<string?> _reportError;
    private readonly Action _workspaceChanged;
    private StoreManagementScope _scope = StoreManagementScope.Empty;
    private ProductDraft _originalProductDraft = new(string.Empty, string.Empty, string.Empty);
    private OfferingDraft _originalOfferingDraft = new(string.Empty, string.Empty, string.Empty, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    private IReadOnlyList<StoreProductSummary> _products = [];
    private IReadOnlyList<StoreProductSummary> _archivedProducts = [];
    private StoreProductSummary? _selectedProduct;
    private FulfillmentOfferingSummary? _selectedOffering;
    private bool _showArchivedProducts;
    private bool _showArchivedOfferings;
    private bool _isCreatingNewProduct;
    private bool _isCreatingNewOffering;
    private Guid? _draftProductId;
    private Guid? _draftOfferingId;
    private StoreProductSummary? _pendingDeleteProduct;
    private StoreProductSummary? _pendingArchiveProduct;
    private FulfillmentOfferingSummary? _pendingDeleteOffering;
    private bool _productDeleteWarningVisible;
    private bool _productArchiveWarningVisible;
    private bool _offeringDeleteWarningVisible;
    private bool _isAddingVariant;
    private bool _isAddingDesignArea;
    private bool _isBlueprintBasicsExpanded;
    private string _productName = string.Empty;
    private string _productDescription = string.Empty;
    private string _externalProductId = string.Empty;
    private string _offeringName = string.Empty;
    private string _offeringDescription = string.Empty;
    private string _offeringExternalOfferingId = string.Empty;
    private int _offeringKindIndex;
    private string _offeringProviderName = string.Empty;
    private string _areaName = string.Empty;
    private string _areaPosition = string.Empty;
    private string _areaDecorationMethod = string.Empty;
    private string _areaWidth = string.Empty;
    private string _areaHeight = string.Empty;
    private string _variantColor = string.Empty;
    private string _variantSize = string.Empty;
    private long _scopeVersion;
    private long _cardLoadVersion;
    private long _draftGeneration;
    private CatalogEditorLevel _navigationLevel;
    private CatalogSetupViewModel? _catalogSetup;

    public ProductCatalogEditorViewModel(
        IProductSupplierSetupService? productsService = null,
        ICatalogSetupService? catalogService = null,
        IOfferingManagementService? offeringService = null,
        Action<Func<CancellationToken, Task>>? runOperation = null,
        Action<string?>? reportError = null,
        Action? workspaceChanged = null)
    {
        _productsService = productsService;
        _catalogService = catalogService;
        _offeringService = offeringService;
        _runOperation = runOperation ?? (operation => _ = operation(CancellationToken.None));
        _reportError = reportError ?? (_ => { });
        _workspaceChanged = workspaceChanged ?? (() => { });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<Action>? DiscardRequested;
    public event EventHandler? ProductNameFocusRequested;
    public event EventHandler? OfferingNameFocusRequested;
    public event Action<CatalogEditorLevel>? NavigationRequested;

    public StoreManagementScope Scope { get => _scope; set => SetScope(value); }
    public CatalogSetupViewModel? CatalogSetup
    {
        get => _catalogSetup;
        internal set
        {
            if (ReferenceEquals(_catalogSetup, value)) return;
            if (_catalogSetup is not null) _catalogSetup.PropertyChanged -= OnCatalogSetupPropertyChanged;
            _catalogSetup = value;
            if (_catalogSetup is not null) _catalogSetup.PropertyChanged += OnCatalogSetupPropertyChanged;
            Raise(nameof(CatalogSetup), nameof(HasAnyCatalogUnsavedChanges));
        }
    }
    public IReadOnlyList<StoreProductSummary> Products => _products;
    public IReadOnlyList<StoreProductSummary> ArchivedProducts => _archivedProducts;
    public ObservableCollection<BlueprintOfferingCardViewModel> BlueprintOfferingCards { get; } = [];
    public ObservableCollection<ApplicableVariantViewModel> ApplicableVariants { get; } = [];
    public bool HasBlueprintOfferingCards => BlueprintOfferingCards.Count > 0;
    public bool HasProducts => EditorProducts.Count > 0;
    public bool ShowArchivedProducts { get => _showArchivedProducts; set { if (Set(ref _showArchivedProducts, value)) Raise(nameof(EditorProducts), nameof(HasProducts)); } }
    public bool ShowArchivedOfferings { get => _showArchivedOfferings; set { if (Set(ref _showArchivedOfferings, value)) _runOperation(RefreshBlueprintOfferingCardsAsync); } }
    public IReadOnlyList<StoreProductSummary> EditorProducts => (_showArchivedProducts ? _products.Concat(_archivedProducts) : _products).Concat(_isCreatingNewProduct && DraftProduct() is { } draft ? [draft] : []).ToArray();
    public StoreProductSummary? SelectedProduct { get => _selectedProduct; private set { if (Set(ref _selectedProduct, value)) Raise(nameof(HasSelectedProduct), nameof(CanDeleteSelectedProduct), nameof(CanArchiveSelectedProduct), nameof(SelectedProductOfferingCount), nameof(SelectedProductSummary)); } }
    public FulfillmentOfferingSummary? SelectedOffering { get => _selectedOffering; private set { if (Set(ref _selectedOffering, value)) { Raise(nameof(HasSelectedOffering), nameof(CanDeleteSelectedOffering), nameof(OfferingVariants), nameof(OfferingDesignAreas), nameof(HasOfferingVariants), nameof(HasOfferingDesignAreas), nameof(HasSelectedProductOfferings), nameof(SelectedOfferingVariantCount), nameof(SelectedOfferingDesignAreaCount), nameof(SelectedOfferingSummary)); RefreshApplicableVariants(); } } }
    public IReadOnlyList<ProductVariantSummary> OfferingVariants => SelectedOffering?.Variants ?? [];
    public IReadOnlyList<DesignAreaSummary> OfferingDesignAreas => SelectedOffering?.DesignAreas ?? [];
    public bool HasOfferingVariants => OfferingVariants.Count > 0;
    public bool HasOfferingDesignAreas => OfferingDesignAreas.Count > 0;
    public bool HasSelectedProductOfferings => SelectedProduct?.Offerings.Count > 0;
    public int SelectedProductOfferingCount => SelectedProduct?.Offerings.Count ?? 0;
    public int SelectedOfferingVariantCount => SelectedOffering?.Variants.Count ?? 0;
    public int SelectedOfferingDesignAreaCount => SelectedOffering?.DesignAreas.Count ?? 0;
    public string SelectedProductSummary => $"{SelectedProductOfferingCount} fulfillment offering{(SelectedProductOfferingCount == 1 ? string.Empty : "s")}";
    public string SelectedOfferingSummary => $"{SelectedOfferingVariantCount} variant{(SelectedOfferingVariantCount == 1 ? string.Empty : "s")}  ·  {SelectedOfferingDesignAreaCount} printable area{(SelectedOfferingDesignAreaCount == 1 ? string.Empty : "s")}";
    public bool HasSelectedProduct => SelectedProduct is not null;
    public bool HasSelectedOffering => SelectedOffering is not null;
    public bool IsCreatingNewProduct => _isCreatingNewProduct;
    public bool IsCreatingNewOffering => _isCreatingNewOffering;
    public bool HasSelectedVariant => SelectedOffering?.Variants.Any() == true;
    public bool HasSelectedDesignArea => SelectedOffering?.DesignAreas.Any() == true;
    public bool HasAnyCatalogUnsavedChanges => HasUnsavedProductChanges || HasUnsavedOfferingChanges || CatalogSetup?.HasActiveDraft == true;
    public bool HasUnsavedProductChanges => _isCreatingNewProduct || (SelectedProduct is not null && _draftProductId is null && HasUnsavedProductDraft);
    public bool HasUnsavedOfferingChanges => _isCreatingNewOffering || (SelectedOffering is not null && _draftOfferingId is null && HasUnsavedOfferingDraft);
    public bool HasUnsavedProductDraft => CurrentProductDraft() != _originalProductDraft;
    public bool HasUnsavedOfferingDraft => CurrentOfferingDraft() != _originalOfferingDraft;
    internal void CaptureOriginalProductDraft() => _originalProductDraft = CurrentProductDraft();
    internal void CaptureOriginalOfferingDraft() => _originalOfferingDraft = CurrentOfferingDraft();
    public bool CanSaveSelectedProduct => _productsService is not null && CanManageScope && HasUnsavedProductChanges;
    public bool CanSaveSelectedOffering => _productsService is not null && CanManageScope && SelectedProduct is not null && HasUnsavedOfferingChanges;
    public bool CanDeleteSelectedProduct => _catalogService is not null && SelectedProduct is { IsArchived: true } && !_isCreatingNewProduct && CanManageScope;
    public bool CanArchiveSelectedProduct => _catalogService is not null && SelectedProduct is { IsArchived: false } && !_isCreatingNewProduct && !HasUnsavedProductChanges && CanManageScope;
    public bool CanDeleteSelectedOffering => _productsService is not null && SelectedOffering is not null && !_isCreatingNewOffering && CanManageScope;
    public bool CanCreateCatalogItem => _productsService is not null && CanManageScope;
    public bool IsAddingVariant { get => _isAddingVariant; set => Set(ref _isAddingVariant, value); }
    public bool IsAddingDesignArea { get => _isAddingDesignArea; set => Set(ref _isAddingDesignArea, value); }
    public bool IsBlueprintBasicsExpanded { get => _isBlueprintBasicsExpanded; set => Set(ref _isBlueprintBasicsExpanded, value); }
    public bool ProductDeleteWarningVisible { get => _productDeleteWarningVisible; private set => Set(ref _productDeleteWarningVisible, value); }
    public bool ProductArchiveWarningVisible { get => _productArchiveWarningVisible; private set => Set(ref _productArchiveWarningVisible, value); }
    public bool OfferingDeleteWarningVisible { get => _offeringDeleteWarningVisible; private set => Set(ref _offeringDeleteWarningVisible, value); }
    public string ProductDeleteWarningMessage => _pendingDeleteProduct is null ? "Permanent deletion cannot be undone." : $"Delete archived Blueprint '{_pendingDeleteProduct.Name}' and its catalog records permanently? This cannot be undone.";
    public string ProductArchiveWarningMessage => _pendingArchiveProduct is null ? "Archiving this Blueprint will affect its connected catalog configuration." : $"This is a high-impact action. Archiving Blueprint '{_pendingArchiveProduct.Name}' will also archive its fulfillment offerings, variants, design areas, and mockup configuration. Existing listing and design relationships will be preserved, but this Blueprint will leave active use.";
    public string OfferingDeleteWarningMessage => _pendingDeleteOffering is null ? "Permanent deletion cannot be undone." : $"Delete offering '{_pendingDeleteOffering.Name}' permanently? This cannot be undone.";
    public string ProductName { get => _productName; set { if (Set(ref _productName, value)) RaiseDraft(); } }
    public string ProductDescription { get => _productDescription; set { if (Set(ref _productDescription, value)) RaiseDraft(); } }
    public string ExternalProductId { get => _externalProductId; set { if (Set(ref _externalProductId, value)) RaiseDraft(); } }
    public string OfferingName { get => _offeringName; set { if (Set(ref _offeringName, value)) RaiseOfferingDraft(); } }
    public string OfferingDescription { get => _offeringDescription; set { if (Set(ref _offeringDescription, value)) RaiseOfferingDraft(); } }
    public string OfferingExternalOfferingId { get => _offeringExternalOfferingId; set { if (Set(ref _offeringExternalOfferingId, value)) RaiseOfferingDraft(); } }
    public int OfferingKindIndex { get => _offeringKindIndex; set { if (Set(ref _offeringKindIndex, value)) RaiseOfferingDraft(); } }
    public string OfferingProviderName { get => _offeringProviderName; set { if (Set(ref _offeringProviderName, value)) RaiseOfferingDraft(); } }
    public string AreaName { get => _areaName; set => Set(ref _areaName, value); }
    public string AreaPosition { get => _areaPosition; set => Set(ref _areaPosition, value); }
    public string AreaDecorationMethod { get => _areaDecorationMethod; set => Set(ref _areaDecorationMethod, value); }
    public string AreaWidth { get => _areaWidth; set => Set(ref _areaWidth, value); }
    public string AreaHeight { get => _areaHeight; set => Set(ref _areaHeight, value); }
    public string VariantColor { get => _variantColor; set => Set(ref _variantColor, value); }
    public string VariantSize { get => _variantSize; set => Set(ref _variantSize, value); }
    public CatalogEditorLevel NavigationLevel { get => _navigationLevel; private set { if (Set(ref _navigationLevel, value)) NavigationRequested?.Invoke(value); } }

    internal void SynchronizeNavigationLevel(CatalogEditorLevel level) => _navigationLevel = level;

    private bool CanManageScope => _scope.StoreId is not null && !_scope.IsStoreArchived && !_scope.IsCreatingNewStore;

    public void SetScope(StoreManagementScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (_scope == scope) return;
        _scope = scope;
        _scopeVersion++;
        _draftGeneration++;
        _cardLoadVersion++;
        NavigationLevel = CatalogEditorLevel.Overview;
        _isCreatingNewProduct = false; _isCreatingNewOffering = false;
        _draftProductId = null; _draftOfferingId = null;
        _products = []; _archivedProducts = []; SelectedProduct = null; SelectedOffering = null;
        ClearProductFields(); ClearOfferingFields(); ClearProductDeleteWarning(); ClearProductArchiveWarning(); ClearOfferingDeleteWarning();
        BlueprintOfferingCards.Clear();
        CatalogSetup?.CancelActiveDrafts();
        CatalogSetup?.SelectOffering(null);
        CaptureOriginalProductDraft(); CaptureOriginalOfferingDraft();
        Raise(nameof(Scope), nameof(Products), nameof(ArchivedProducts), nameof(EditorProducts), nameof(HasProducts), nameof(HasSelectedProduct), nameof(HasSelectedOffering), nameof(CanSaveSelectedProduct), nameof(CanSaveSelectedOffering), nameof(CanDeleteSelectedProduct), nameof(CanArchiveSelectedProduct), nameof(CanDeleteSelectedOffering), nameof(CanCreateCatalogItem));
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null || !CanManageScope || _scope.StoreId is not { } storeId) return;
        var scope = _scope; var version = _scopeVersion;
        var state = await _productsService.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version)) return;
        ApplyProductState(state);
    }

    public void SelectProductForEditing(StoreProductSummary product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!CanManageScope || product.StoreId != _scope.StoreId) return;
        if (_isCreatingNewProduct && product.Id == _draftProductId) return;
        if (SelectedProduct?.Id != product.Id && (HasAnyCatalogUnsavedChanges || CatalogSetup?.HasActiveDraft == true))
        {
            var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) PerformSelectProduct(product); }); return;
        }
        PerformSelectProduct(product);
    }

    public void StartCreateProduct()
    {
        if (!CanCreateCatalogItem) { _reportError("Select an active saved store before creating a product."); return; }
        if (HasAnyCatalogUnsavedChanges || CatalogSetup?.HasActiveDraft == true) { var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) BeginCreateProductDraft(); }); return; }
        BeginCreateProductDraft();
    }

    public void DiscardUnsavedChanges()
    {
        _draftGeneration++;
        CatalogSetup?.CancelActiveDrafts();
        ResetOfferingSecondaryDrafts();
        if (_isCreatingNewProduct) { _isCreatingNewProduct = false; _draftProductId = null; SelectedProduct = _products.FirstOrDefault(value => value.Id == SelectedProduct?.Id) ?? _products.FirstOrDefault(); }
        ApplyProductFields(SelectedProduct);
        if (_isCreatingNewOffering) { _isCreatingNewOffering = false; _draftOfferingId = null; }
        ApplyOfferingFields(SelectedProduct?.Offerings.FirstOrDefault(value => value.Id == SelectedOffering?.Id) ?? SelectedProduct?.Offerings.FirstOrDefault());
        RaiseEverything();
    }

    public async Task SaveSelectedProductAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null) { _reportError("Product and fulfillment setup is not available."); return; }
        if (!CanManageScope || _scope.StoreId is not { } storeId) { _reportError("Select a store before saving a product."); return; }
        var scope = _scope;
        var version = _scopeVersion;
        var draftGeneration = _draftGeneration;
        var isCreatingNewProduct = _isCreatingNewProduct;
        var draftProductId = _draftProductId;
        var selectedProductId = SelectedProduct?.Id;
        var draft = CurrentProductDraft();
        ProductSupplierSetupResult result;
        if (isCreatingNewProduct)
        {
            result = await _productsService.CreateProductAsync(new CreateProductRequest(storeId, ProductName, EmptyToNull(ProductDescription), EmptyToNull(ExternalProductId)), cancellationToken).ConfigureAwait(true);
        }
        else
        {
            var selectedProduct = SelectedProduct;
            if (selectedProduct is null) { _reportError("Select a product before saving."); return; }
            result = await _productsService.UpdateProductAsync(new UpdateProductRequest(selectedProduct.Id, ProductName, EmptyToNull(ProductDescription), EmptyToNull(ExternalProductId)), cancellationToken).ConfigureAwait(true);
        }
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || isCreatingNewProduct != _isCreatingNewProduct || draftProductId != _draftProductId || selectedProductId != SelectedProduct?.Id || draft != CurrentProductDraft())
        {
            if (result.Succeeded) _workspaceChanged();
            return;
        }
        if (isCreatingNewProduct && result.Succeeded) { _isCreatingNewProduct = false; _draftProductId = null; }
        ApplyProductResult(result);
    }

    public void RequestArchiveSelectedProduct()
    {
        if (!CanArchiveSelectedProduct) { _reportError("Select an active Blueprint before archiving it."); return; }
        _pendingArchiveProduct = SelectedProduct; ProductArchiveWarningVisible = true; Raise(nameof(ProductArchiveWarningMessage));
    }

    public async Task ConfirmArchiveSelectedProductAsync(CancellationToken cancellationToken = default)
    {
        if (_catalogService is null || _pendingArchiveProduct is null || !CanManageScope || _scope.StoreId is not { } storeId) { _reportError("Select a Blueprint before archiving it."); return; }
        var scope = _scope; var version = _scopeVersion; var product = _pendingArchiveProduct;
        try
        {
            var result = await _catalogService.ArchiveBlueprintWithDependentsAsync(new ArchiveBlueprintWithDependentsRequest(storeId, product.Id), cancellationToken).ConfigureAwait(true);
            if (!IsCurrent(scope, version)) return;
            _reportError(result.Error);
            if (result.Succeeded) { ClearProductArchiveWarning(); await LoadAsync(cancellationToken).ConfigureAwait(true); if (SelectedProduct is null) NavigationLevel = CatalogEditorLevel.Overview; }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { if (IsCurrent(scope, version)) _reportError($"The Blueprint could not be archived. {exception.Message}"); }
    }

    public void RequestDeleteSelectedProduct()
    {
        if (_isCreatingNewProduct) { _reportError("Save the new product before deleting it."); return; }
        if (SelectedProduct is null) { _reportError("Select a product before deleting."); return; }
        if (!SelectedProduct.IsArchived) { _reportError("Archive the Blueprint before permanently deleting it."); return; }
        _pendingDeleteProduct = SelectedProduct; ProductDeleteWarningVisible = true; Raise(nameof(ProductDeleteWarningMessage));
    }

    public async Task ConfirmDeleteProductAsync(CancellationToken cancellationToken = default)
    {
        if (_catalogService is null || _scope.StoreId is not { } storeId || _pendingDeleteProduct is null || !CanManageScope) { _reportError("Catalog permanent deletion is not available."); return; }
        var scope = _scope; var version = _scopeVersion; var product = _pendingDeleteProduct;
        var result = await _catalogService.DeleteBlueprintPermanentlyAsync(new DeleteBlueprintPermanentlyRequest(storeId, product.Id, Confirm: true), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version)) return;
        _reportError(result.Error);
        if (result.Succeeded) { ClearProductDeleteWarning(); await LoadAsync(cancellationToken).ConfigureAwait(true); if (SelectedProduct is null) NavigationLevel = CatalogEditorLevel.Overview; }
    }

    public void SelectOfferingForEditing(FulfillmentOfferingSummary offering)
    {
        ArgumentNullException.ThrowIfNull(offering);
        if (!CanManageScope || SelectedProduct?.Offerings.All(value => value.Id != offering.Id) != false) return;
        if (_isCreatingNewOffering && offering.Id == _draftOfferingId) return;
        if (SelectedOffering?.Id != offering.Id && HasAnyCatalogUnsavedChanges)
        {
            var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) PerformSelectOffering(offering); }); return;
        }
        PerformSelectOffering(offering);
    }

    public void StartCreateOffering()
    {
        if (_productsService is null || !CanManageScope || SelectedProduct is null) { _reportError("Select an active product before creating an offering."); return; }
        if (HasAnyCatalogUnsavedChanges || CatalogSetup?.HasActiveDraft == true) { var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) BeginCreateOfferingDraft(); }); return; }
        BeginCreateOfferingDraft();
    }

    public void CancelNewOffering()
    {
        if (!_isCreatingNewOffering) return;
        _isCreatingNewOffering = false; _draftOfferingId = null;
        PerformSelectOffering(SelectedProduct?.Offerings.FirstOrDefault());
        NavigationLevel = CatalogEditorLevel.ProductDetail;
        RaiseEverything();
    }

    public async Task SaveSelectedOfferingAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null) { _reportError("Product and fulfillment setup is not available."); return; }
        if (!CanManageScope || SelectedProduct is null) { _reportError("Select a product before saving an offering."); return; }
        var scope = _scope;
        var version = _scopeVersion;
        var draftGeneration = _draftGeneration;
        var productId = SelectedProduct.Id;
        var selectedOfferingId = SelectedOffering?.Id;
        var isCreatingNewOffering = _isCreatingNewOffering;
        var draftOfferingId = _draftOfferingId;
        var draft = CurrentOfferingDraft();
        var kind = OfferingKindIndex == 0 ? FulfillmentKind.FixedProvider : FulfillmentKind.PrintifyChoiceNetwork;
        ProductSupplierSetupResult result;
        if (isCreatingNewOffering)
        {
            result = await _productsService.CreateOfferingAsync(new FusionCanvas.Application.Products.CreateOfferingRequest(productId, OfferingName, kind, kind == FulfillmentKind.FixedProvider ? EmptyToNull(OfferingProviderName) : null, EmptyToNull(OfferingDescription), EmptyToNull(OfferingExternalOfferingId)), cancellationToken).ConfigureAwait(true);
        }
        else
        {
            var selectedOffering = SelectedOffering;
            if (selectedOffering is null) { _reportError("Select an offering before saving."); return; }
            result = await _productsService.UpdateOfferingAsync(new UpdateOfferingRequest(selectedOffering.Id, OfferingName, kind, kind == FulfillmentKind.FixedProvider ? EmptyToNull(OfferingProviderName) : null, EmptyToNull(OfferingDescription), EmptyToNull(OfferingExternalOfferingId)), cancellationToken).ConfigureAwait(true);
        }
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || productId != SelectedProduct?.Id || selectedOfferingId != SelectedOffering?.Id || isCreatingNewOffering != _isCreatingNewOffering || draftOfferingId != _draftOfferingId || draft != CurrentOfferingDraft())
        {
            if (result.Succeeded) _workspaceChanged();
            return;
        }
        if (isCreatingNewOffering && result.Succeeded) { _isCreatingNewOffering = false; _draftOfferingId = null; }
        ApplyProductResult(result);
        if (result.Succeeded && CatalogSetup is not null && _scope.StoreId is { } storeId)
        {
            var appliedScope = _scope;
            var appliedVersion = _scopeVersion;
            var appliedGeneration = _draftGeneration;
            var appliedProductId = SelectedProduct?.Id;
            var appliedOfferingId = SelectedOffering?.Id;
            await CatalogSetup.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
            if (IsCurrent(appliedScope, appliedVersion) && appliedGeneration == _draftGeneration && appliedProductId == SelectedProduct?.Id && appliedOfferingId == SelectedOffering?.Id)
                CatalogSetup.SelectOffering(appliedOfferingId);
        }
    }

    public void RequestDeleteSelectedOffering()
    {
        if (_isCreatingNewOffering) { _reportError("Save the new offering before deleting it."); return; }
        if (SelectedOffering is null) { _reportError("Select an offering before deleting."); return; }
        _pendingDeleteOffering = SelectedOffering; OfferingDeleteWarningVisible = true; Raise(nameof(OfferingDeleteWarningMessage));
    }

    public async Task ConfirmDeleteOfferingAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null || _pendingDeleteOffering is null || !CanManageScope) { _reportError("Select an offering before deleting."); return; }
        var scope = _scope; var version = _scopeVersion; var draftGeneration = _draftGeneration;
        var productId = SelectedProduct?.Id; var selectedOfferingId = SelectedOffering?.Id;
        var result = await _productsService.DeleteOfferingAsync(new DeleteOfferingRequest(_pendingDeleteOffering.Id, Confirm: true), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || productId != SelectedProduct?.Id || selectedOfferingId != SelectedOffering?.Id) { if (result.Succeeded) _workspaceChanged(); return; }
        _reportError(result.Error); ApplyProductState(result.State);
        if (result.Succeeded && SelectedOffering is null) NavigationLevel = CatalogEditorLevel.ProductDetail;
        ClearOfferingDeleteWarning();
    }

    public void StartAddVariant()
    {
        if (SelectedOffering is null) { _reportError("Select an offering before adding a variant."); return; }
        _draftGeneration++;
        IsAddingVariant = true; VariantColor = string.Empty; VariantSize = string.Empty;
    }

    public void StartAddDesignArea()
    {
        if (SelectedOffering is null) { _reportError("Select an offering before adding a printable area."); return; }
        _draftGeneration++;
        IsAddingDesignArea = true; AreaName = string.Empty; AreaPosition = string.Empty; AreaDecorationMethod = string.Empty; AreaWidth = string.Empty; AreaHeight = string.Empty;
        RefreshApplicableVariants();
    }

    public async Task AddVariantAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null || SelectedOffering is null || !CanManageScope) { _reportError("Select an offering before adding a variant."); return; }
        var scope = _scope; var version = _scopeVersion; var draftGeneration = _draftGeneration; var offeringId = SelectedOffering.Id; var draft = CurrentOfferingDraft(); var options = new List<VariantOptionDraft>();
        if (!string.IsNullOrWhiteSpace(VariantColor)) options.Add(new VariantOptionDraft("Color", VariantColor.Trim()));
        if (!string.IsNullOrWhiteSpace(VariantSize)) options.Add(new VariantOptionDraft("Size", VariantSize.Trim()));
        if (options.Count == 0) { _reportError("Enter at least a color or size for the variant."); return; }
        var result = await _productsService.CreateVariantAsync(new CreateVariantRequest(offeringId, options), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || offeringId != SelectedOffering?.Id || draft != CurrentOfferingDraft()) { if (result.Succeeded) _workspaceChanged(); return; }
        ApplyProductResult(result);
        if (result.Succeeded) { IsAddingVariant = false; VariantColor = string.Empty; VariantSize = string.Empty; }
    }

    public async Task RemoveVariantAsync(ProductVariantSummary variant, CancellationToken cancellationToken = default)
    {
        if (_productsService is null || !CanManageScope || SelectedOffering is null) return;
        var scope = _scope; var version = _scopeVersion; var draftGeneration = _draftGeneration; var offeringId = SelectedOffering.Id;
        var result = await _productsService.DeleteVariantAsync(new DeleteVariantRequest(variant.Id, Confirm: true), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || offeringId != SelectedOffering?.Id) { if (result.Succeeded) _workspaceChanged(); return; }
        ApplyProductResult(result);
    }

    public async Task AddDesignAreaAsync(CancellationToken cancellationToken = default)
    {
        if (_productsService is null || SelectedOffering is null || !CanManageScope) { _reportError("Select an offering before adding a printable area."); return; }
        if (!int.TryParse(AreaWidth, out var width) || width <= 0 || !int.TryParse(AreaHeight, out var height) || height <= 0) { _reportError("Design area width and height must be positive whole numbers."); return; }
        var scope = _scope; var version = _scopeVersion; var draftGeneration = _draftGeneration; var offeringId = SelectedOffering.Id; var draft = CurrentOfferingDraft();
        var applicableVariantIds = ApplicableVariants.Where(item => item.IsSelected).Select(item => item.Id).ToArray();
        var result = await _productsService.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, AreaName, string.IsNullOrWhiteSpace(AreaPosition) ? "front" : AreaPosition.Trim(), string.IsNullOrWhiteSpace(AreaDecorationMethod) ? "DTG" : AreaDecorationMethod.Trim(), width, height, applicableVariantIds), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || offeringId != SelectedOffering?.Id || draft != CurrentOfferingDraft() || !applicableVariantIds.SequenceEqual(ApplicableVariants.Where(item => item.IsSelected).Select(item => item.Id))) { if (result.Succeeded) _workspaceChanged(); return; }
        ApplyProductResult(result);
        if (result.Succeeded) { IsAddingDesignArea = false; AreaName = AreaPosition = AreaDecorationMethod = AreaWidth = AreaHeight = string.Empty; }
    }

    public async Task RemoveDesignAreaAsync(DesignAreaSummary area, CancellationToken cancellationToken = default)
    {
        if (_productsService is null || !CanManageScope || SelectedOffering is null) return;
        var scope = _scope; var version = _scopeVersion; var draftGeneration = _draftGeneration; var offeringId = SelectedOffering.Id;
        var result = await _productsService.DeleteDesignAreaAsync(new DeleteDesignAreaRequest(area.Id, Confirm: true), cancellationToken).ConfigureAwait(true);
        if (!IsCurrent(scope, version) || draftGeneration != _draftGeneration || offeringId != SelectedOffering?.Id) { if (result.Succeeded) _workspaceChanged(); return; }
        ApplyProductResult(result);
    }

    internal void ApplyProductResult(ProductSupplierSetupResult result)
    {
        _reportError(result.Error); ApplyProductState(result.State);
        if (result.Succeeded && result.Product is not null) { PerformSelectProduct(result.Product); _workspaceChanged(); }
        else if (result.Succeeded && result.Offering is not null) { PerformSelectOffering(result.Offering); _workspaceChanged(); }
        RaiseEverything();
    }

    public void ClearProductDeleteWarning() { _pendingDeleteProduct = null; ProductDeleteWarningVisible = false; Raise(nameof(ProductDeleteWarningMessage)); }
    public void ClearProductArchiveWarning() { _pendingArchiveProduct = null; ProductArchiveWarningVisible = false; Raise(nameof(ProductArchiveWarningMessage)); }
    public void ClearOfferingDeleteWarning() { _pendingDeleteOffering = null; OfferingDeleteWarningVisible = false; Raise(nameof(OfferingDeleteWarningMessage)); }

    public async Task RefreshBlueprintOfferingCardsAsync(CancellationToken cancellationToken = default)
    {
        var scope = _scope; var version = _scopeVersion; var cardVersion = ++_cardLoadVersion; var product = SelectedProduct;
        IReadOnlyList<BlueprintOfferingCardViewModel> cards;
        if (!CanManageScope || scope.StoreId is not { } storeId || product is null || _isCreatingNewProduct) cards = [];
        else if (_offeringService is not null)
        {
            try { cards = (await _offeringService.LoadForBlueprintAsync(storeId, product.Id, ShowArchivedOfferings, cancellationToken).ConfigureAwait(true)).Select(BlueprintOfferingCardViewModel.From).ToArray(); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch (Exception exception) { if (IsCurrent(scope, version) && SelectedProduct?.Id == product.Id && cardVersion == _cardLoadVersion) _reportError(exception.Message); return; }
        }
        else cards = product.Offerings.Select(offering => new BlueprintOfferingCardViewModel(offering.Id, offering.Name, offering.ProviderName ?? "Provider not configured", offering.Kind != FulfillmentKind.FixedProvider, "Setup incomplete", offering.Variants.Count, offering.DesignAreas.Count, 0)).ToArray();
        if (!IsCurrent(scope, version) || SelectedProduct?.Id != product?.Id || cardVersion != _cardLoadVersion) return;
        BlueprintOfferingCards.Clear(); foreach (var card in cards) BlueprintOfferingCards.Add(card); Raise(nameof(HasBlueprintOfferingCards));
    }

    public void ClearOfferingSelection() => ClearOfferingSelectionCore();

    public void SelectArchivedOfferingForCatalog(Guid offeringId)
    {
        CatalogSetup?.SelectOffering(offeringId);
        NavigationLevel = CatalogEditorLevel.OfferingDetail;
    }

    private void ClearOfferingSelectionCore()
    {
        _draftGeneration++;
        _isCreatingNewOffering = false; _draftOfferingId = null; SelectedOffering = null; CatalogSetup?.SelectOffering(null); ClearOfferingFields(); RaiseEverything();
    }

    public void NavigateCatalog(CatalogEditorLevel level)
    {
        if (level is CatalogEditorLevel.VariantManagement or CatalogEditorLevel.DesignAreaManagement or CatalogEditorLevel.MockupTemplateManagement)
        {
            if (SelectedOffering is null || _isCreatingNewOffering) { _reportError("Save the Blueprint Offering before managing its catalog setup."); return; }
            if (HasUnsavedOfferingChanges || CatalogSetup?.HasActiveDraft == true) { var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) NavigateCatalog(level); }); return; }
            CatalogSetup?.SelectOffering(SelectedOffering.Id);
        }
        else if (HasAnyCatalogUnsavedChanges || CatalogSetup?.HasActiveDraft == true)
        {
            var scope = _scope; DiscardRequested?.Invoke(() => { if (scope == _scope) NavigateCatalog(level); }); return;
        }
        NavigationLevel = level;
        if (level == CatalogEditorLevel.OfferingDetail) _runOperation(RefreshBlueprintOfferingCardsAsync);
    }

    internal void ApplyProductState(ProductSupplierSetupState state)
    {
        _products = state.Products; _archivedProducts = state.Archived;
        var visible = _showArchivedProducts ? _products.Concat(_archivedProducts) : _products;
        SelectedProduct = _isCreatingNewProduct ? DraftProduct() : visible.FirstOrDefault(value => value.Id == SelectedProduct?.Id) ?? visible.FirstOrDefault();
        if (!_isCreatingNewProduct) ApplyProductFields(SelectedProduct);
        Raise(nameof(Products), nameof(ArchivedProducts), nameof(EditorProducts), nameof(HasProducts));
        if (SelectedProduct is not null) ApplySelectedOfferingAfterProductChange(); else ClearOfferingSelection();
    }


    private void PerformSelectProduct(StoreProductSummary product)
    {
        _draftGeneration++;
        if (SelectedProduct?.Id != product.Id) ResetOfferingSecondaryDrafts();
        _isCreatingNewProduct = false; _draftProductId = null; SelectedProduct = product; ApplyProductFields(product); ApplySelectedOfferingAfterProductChange(); ClearProductDeleteWarning(); NavigationLevel = CatalogEditorLevel.ProductDetail; RaiseEverything();
    }

    private void PerformSelectOffering(FulfillmentOfferingSummary? offering)
    {
        _draftGeneration++;
        if (SelectedOffering?.Id != offering?.Id) ResetOfferingSecondaryDrafts();
        _isCreatingNewOffering = false; _draftOfferingId = null; SelectedOffering = offering;
        CatalogSetup?.SelectOffering(offering?.Id);
        ApplyOfferingFields(offering); ClearOfferingDeleteWarning();
        if (offering is not null) NavigationLevel = CatalogEditorLevel.OfferingDetail;
        RaiseEverything();
    }

    private void ApplySelectedOfferingAfterProductChange()
    {
        var offerings = SelectedProduct?.Offerings ?? [];
        var selected = offerings.FirstOrDefault(value => value.Id == SelectedOffering?.Id) ?? offerings.FirstOrDefault();
        SelectedOffering = selected; CatalogSetup?.SelectOffering(selected?.Id); ApplyOfferingFields(selected);
        _runOperation(RefreshBlueprintOfferingCardsAsync);
    }

    private void BeginCreateProductDraft()
    {
        _draftGeneration++;
        _isCreatingNewProduct = true; _draftProductId = Guid.NewGuid(); ClearProductFields(); SelectedProduct = DraftProduct(); CaptureOriginalProductDraft();
        ClearProductDeleteWarning(); IsBlueprintBasicsExpanded = true; NavigationLevel = CatalogEditorLevel.ProductDetail; RaiseEverything(); ProductNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }
    private void BeginCreateOfferingDraft()
    {
        _draftGeneration++;
        _isCreatingNewOffering = true; _draftOfferingId = Guid.NewGuid(); ClearOfferingFields(); SelectedOffering = DraftOffering(); CaptureOriginalOfferingDraft();
        ClearOfferingDeleteWarning(); NavigationLevel = CatalogEditorLevel.OfferingDetail; RaiseEverything(); OfferingNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }
    private void ApplyProductFields(StoreProductSummary? product) { ProductName = product?.Name ?? string.Empty; ProductDescription = product?.Description ?? string.Empty; ExternalProductId = product?.ExternalProductId ?? string.Empty; CaptureOriginalProductDraft(); }
    private void ApplyOfferingFields(FulfillmentOfferingSummary? offering) { OfferingName = offering?.Name ?? string.Empty; OfferingDescription = offering?.Description ?? string.Empty; OfferingExternalOfferingId = offering?.ExternalOfferingId ?? string.Empty; OfferingKindIndex = offering?.Kind == FulfillmentKind.PrintifyChoiceNetwork ? 1 : 0; OfferingProviderName = offering?.ProviderName ?? string.Empty; CaptureOriginalOfferingDraft(); }
    private void ClearProductFields() { ProductName = string.Empty; ProductDescription = string.Empty; ExternalProductId = string.Empty; }
    private void ClearOfferingFields()
    {
        OfferingName = string.Empty; OfferingDescription = string.Empty; OfferingExternalOfferingId = string.Empty; OfferingKindIndex = 0; OfferingProviderName = string.Empty;
        ResetOfferingSecondaryDrafts();
        CaptureOriginalOfferingDraft();
    }
    private void ResetOfferingSecondaryDrafts()
    {
        IsAddingVariant = false; IsAddingDesignArea = false;
        VariantColor = string.Empty; VariantSize = string.Empty;
        AreaName = string.Empty; AreaPosition = string.Empty; AreaDecorationMethod = string.Empty; AreaWidth = string.Empty; AreaHeight = string.Empty;
        ApplicableVariants.Clear();
    }
    private void RefreshApplicableVariants() { ApplicableVariants.Clear(); if (SelectedOffering is null) return; foreach (var variant in SelectedOffering.Variants) ApplicableVariants.Add(new ApplicableVariantViewModel(variant)); }
    private StoreProductSummary? DraftProduct() => !_isCreatingNewProduct || _draftProductId is not { } id || _scope.StoreId is not { } storeId ? null : new StoreProductSummary(id, storeId, string.IsNullOrWhiteSpace(ProductName) ? "New product" : ProductName.Trim(), EmptyToNull(ProductDescription), EmptyToNull(ExternalProductId), []);
    private FulfillmentOfferingSummary? DraftOffering() { if (!_isCreatingNewOffering || _draftOfferingId is not { } id || SelectedProduct is null) return null; var kind = OfferingKindIndex == 0 ? FulfillmentKind.FixedProvider : FulfillmentKind.PrintifyChoiceNetwork; return new FulfillmentOfferingSummary(id, SelectedProduct.Id, string.IsNullOrWhiteSpace(OfferingName) ? "New offering" : OfferingName.Trim(), EmptyToNull(OfferingDescription), kind, kind == FulfillmentKind.FixedProvider ? EmptyToNull(OfferingProviderName) : null, EmptyToNull(OfferingExternalOfferingId), [], []); }
    private ProductDraft CurrentProductDraft() => new(ProductName, ProductDescription, ExternalProductId);
    private OfferingDraft CurrentOfferingDraft() => new(OfferingName, OfferingDescription, OfferingExternalOfferingId, OfferingKindIndex, OfferingProviderName, AreaName, AreaPosition, AreaDecorationMethod, AreaWidth, AreaHeight, VariantColor, VariantSize);
    private bool IsCurrent(StoreManagementScope scope, long version) => _scope == scope && _scopeVersion == version;
    private void OnCatalogSetupPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CatalogSetupViewModel.HasActiveDraft))
            Raise(nameof(HasAnyCatalogUnsavedChanges));
    }

    private void RaiseDraft() { Raise(nameof(HasUnsavedProductChanges), nameof(HasAnyCatalogUnsavedChanges), nameof(CanSaveSelectedProduct), nameof(EditorProducts)); }
    private void RaiseOfferingDraft() { Raise(nameof(HasUnsavedOfferingChanges), nameof(HasAnyCatalogUnsavedChanges), nameof(CanSaveSelectedOffering)); }
    private void RaiseEverything() { Raise(nameof(Products), nameof(ArchivedProducts), nameof(EditorProducts), nameof(HasProducts), nameof(IsCreatingNewProduct), nameof(IsCreatingNewOffering), nameof(HasSelectedProduct), nameof(HasSelectedOffering), nameof(CanSaveSelectedProduct), nameof(CanSaveSelectedOffering), nameof(CanDeleteSelectedProduct), nameof(CanArchiveSelectedProduct), nameof(CanDeleteSelectedOffering), nameof(HasAnyCatalogUnsavedChanges), nameof(HasUnsavedProductChanges), nameof(HasUnsavedOfferingChanges), nameof(SelectedProduct), nameof(SelectedOffering), nameof(HasBlueprintOfferingCards)); }
    private void Raise(params string[] names) { foreach (var name in names) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); return true; }
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
