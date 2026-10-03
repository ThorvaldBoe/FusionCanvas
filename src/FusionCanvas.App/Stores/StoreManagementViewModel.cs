using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Products;
using FusionCanvas.Application.Catalog.Compatibility;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Stores;





public sealed class StoreManagementViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private enum PendingEditorAction
    {
        None,
        SelectStore,
        StartNewStore,
        CloseEditor,
        SelectNiche,
        StartNewNiche,
        SelectBasicInfoTab,
        SelectNichesTab,
        SelectTagsTab,
        SelectProductsTab,
        SelectArea,
        SelectVariant
    }



    private sealed class TrackedOperation
    {
        public TrackedOperation(CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
        }

        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationTokenSource Cancellation { get; }
    }

    private readonly IStoreManagementService _service;
    private readonly INicheManagementService? _nicheService;
    private readonly IWorkspaceRepository? _workspaceRepository;
    private readonly INichePopulationService? _nichePopulationService;
    private readonly StoreNicheConfigurationViewModel _storeConfiguration;
    private readonly TagEditorViewModel _tagEditor;
    private readonly ProductCatalogEditorViewModel _productCatalogEditor;
    private static readonly TimeSpan OperationShutdownTimeout = TimeSpan.FromSeconds(2);
    private readonly object _operationGate = new();
    private readonly HashSet<TrackedOperation> _inFlightOperations = [];
    private readonly List<Task> _retiredPrintifyOperations = [];
    private readonly CancellationTokenSource _shutdownCts = new();
    private Task? _disposeTask;
    private bool _isDisposed;
    private bool _isBusy;
    private bool _isSelectorExpanded;
    private bool _isStoreEditorOpen;
    private bool _firstStorePromptDismissed;
    private bool _deleteWarningVisible;
    private bool _nicheDeleteWarningVisible;
    private bool _discardChangesPromptVisible;
    private bool _isCreatingNewStore;
    private bool _isCreatingNewNiche;
    private Guid? _draftStoreId;
    private Guid? _draftNicheId;
    private StoreSummary? _pendingDeleteStore;
    private NicheSummary? _pendingDeleteNiche;
    private StoreSummary? _pendingEditorStore;
    private NicheSummary? _pendingEditorNiche;
    private Action? _pendingDiscardContinuation;
    private PendingEditorAction _pendingEditorAction;


    private StoreManagementEditorTab _selectedEditorTab;
    private CatalogEditorLevel _catalogEditorLevel;
    private bool _isBasicsSectionExpanded = true;
    private bool _isVariantsSectionExpanded = true;
    private bool _isDesignAreasSectionExpanded = true;
    private bool _isAdvancedSectionExpanded;
    private StoreSummary? _selectedStore;
    private NicheSummary? _selectedNiche;
    private bool _printifyShopSelectionChanged;
    private int? _printifyShopId;
    private string? _printifyShopTitle;

    private string? _errorMessage;

    private Task? _productSaveTask;




    public StoreManagementViewModel(IStoreManagementService service, INicheManagementService? nicheService = null, ITagManagementService? tagService = null, ILegacyCatalogCompatibilityService? legacyCatalogCompatibility = null, ICatalogSetupService? catalogService = null, IMockupTemplateSetupService? mockupService = null, IOfferingManagementService? offeringManagementService = null, IProviderCatalogCandidateSource? providerCatalog = null, IMockupTemplateSourceImageService? sourceImages = null, FusionCanvas.App.Assets.IAssetFilePicker? filePicker = null, IWorkspaceRepository? workspaceRepository = null, INichePopulationService? nichePopulationService = null, IRasterImageMetadataReader? rasterImageMetadataReader = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _nicheService = nicheService;
        _workspaceRepository = workspaceRepository;
        _nichePopulationService = nichePopulationService;
        _storeConfiguration = new StoreNicheConfigurationViewModel(
            _service,
            _nicheService,
            _nichePopulationService,
            ApplyState,
            ApplyResult,
            ApplyNicheState,
            ApplyNicheResult,
            message => ErrorMessage = message,
            () => WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty));
        _productCatalogEditor = new ProductCatalogEditorViewModel(
            legacyCatalogCompatibility,
            catalogService,
            offeringManagementService,
            operation => Run(operation),
            message => ErrorMessage = message,
            () => WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty));
        _tagEditor = new TagEditorViewModel(
            tagService,
            (operation) => Run(operation),
            message => ErrorMessage = message,
            () => WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty));
        _storeConfiguration.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(StoreConfigurationViewModel.NewStoreName)
                or nameof(StoreConfigurationViewModel.Description)
                or nameof(StoreConfigurationViewModel.Notes)
                or nameof(StoreConfigurationViewModel.TargetMarket)
                or nameof(StoreConfigurationViewModel.BrandDirection)
                or nameof(StoreConfigurationViewModel.PlanningContext)
                or nameof(StoreConfigurationViewModel.Url)
                or nameof(StoreNicheConfigurationViewModel.SelectedFulfillmentStrategy))
            {
                OnPropertyChanged(args.PropertyName);
                RaiseEditorStateProperties();
            }
            if (args.PropertyName is nameof(StoreConfigurationViewModel.NewStoreName))
                OnPropertyChanged(nameof(EditorActiveStores));
            if (args.PropertyName is nameof(StoreConfigurationViewModel.NicheName)
                or nameof(StoreConfigurationViewModel.NicheDescription)
                or nameof(StoreConfigurationViewModel.NicheAudience)
                or nameof(StoreConfigurationViewModel.NicheHumorStyle)
                or nameof(StoreConfigurationViewModel.NicheVisualStyleGuidance)
                or nameof(StoreConfigurationViewModel.NicheConstraints)
                or nameof(StoreConfigurationViewModel.NicheRisks)
                or nameof(StoreConfigurationViewModel.NicheResearchNotes)
                or nameof(StoreConfigurationViewModel.NicheNotes))
            {
                OnPropertyChanged(args.PropertyName);
                RaiseNicheEditorStateProperties();
            }
            if (args.PropertyName is nameof(StoreConfigurationViewModel.NicheName))
                OnPropertyChanged(nameof(EditorActiveNiches));
            if (args.PropertyName is nameof(StoreNicheConfigurationViewModel.HasUnsavedStoreChanges))
                OnPropertyChanged(nameof(HasUnsavedChanges));
            if (args.PropertyName is nameof(StoreNicheConfigurationViewModel.HasUnsavedNicheChanges))
                OnPropertyChanged(nameof(HasUnsavedNicheChanges));
            if (args.PropertyName is nameof(StoreNicheConfigurationViewModel.CanPopulateNiche)
                or nameof(StoreNicheConfigurationViewModel.IsNichePopulationBusy)
                or nameof(StoreNicheConfigurationViewModel.NichePopulationStatusMessage)
                or nameof(StoreNicheConfigurationViewModel.HasNichePopulationStatus))
            {
                OnPropertyChanged(nameof(CanPopulateNiche));
                OnPropertyChanged(nameof(IsNichePopulationBusy));
                OnPropertyChanged(nameof(NichePopulationButtonText));
                OnPropertyChanged(nameof(NichePopulationStatusMessage));
                OnPropertyChanged(nameof(HasNichePopulationStatus));
            }
        };
        _tagEditor.PropertyChanged += (_, args) =>
        {
            var propertyName = args.PropertyName switch
            {
                nameof(TagEditorViewModel.Scope) => null,
                nameof(TagEditorViewModel.DeleteWarningVisible) => nameof(TagDeleteWarningVisible),
                nameof(TagEditorViewModel.DeleteWarningMessage) => nameof(TagDeleteWarningMessage),
                nameof(TagEditorViewModel.HasUnsavedChanges) => null,
                _ => args.PropertyName
            };
            if (propertyName is not null)
                OnPropertyChanged(propertyName);
            if (args.PropertyName is nameof(TagEditorViewModel.TagName)
                or nameof(TagEditorViewModel.TagColor)
                or nameof(TagEditorViewModel.TagDescription)
                or nameof(TagEditorViewModel.HasUnsavedChanges)
                or nameof(TagEditorViewModel.CanSaveSelectedTag)
                or nameof(TagEditorViewModel.CanArchiveSelectedTag)
                or nameof(TagEditorViewModel.CanDeleteSelectedTag))
                RaiseTagEditorActionProperties();
        };
        _tagEditor.DiscardRequested += continuation => RequestDiscardBefore(PendingEditorAction.None, discardContinuation: continuation);
        _productCatalogEditor.DiscardRequested += continuation => RequestDiscardBefore(PendingEditorAction.None, discardContinuation: continuation);
        _productCatalogEditor.ProductNameFocusRequested += (_, args) => ProductNameFocusRequested?.Invoke(this, args);
        _productCatalogEditor.OfferingNameFocusRequested += (_, args) => OfferingNameFocusRequested?.Invoke(this, args);
        _productCatalogEditor.NavigationRequested += level => CatalogEditorLevel = level;
        _productCatalogEditor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProductCatalogEditorViewModel.Scope)) return;
            OnPropertyChanged(args.PropertyName);
            if (args.PropertyName is nameof(ProductCatalogEditorViewModel.ProductName)
                or nameof(ProductCatalogEditorViewModel.ProductDescription)
                or nameof(ProductCatalogEditorViewModel.ExternalProductId))
                RaiseProductEditorStateProperties();
            else
                RaiseOfferingEditorStateProperties();
            if (args.PropertyName == nameof(ProductCatalogEditorViewModel.ProductName))
                OnPropertyChanged(nameof(EditorProducts));
            if (args.PropertyName == nameof(ProductCatalogEditorViewModel.OfferingKindIndex))
                OnPropertyChanged(nameof(IsChoiceNetworkOffering));
        };
        _productCatalogEditor.CatalogSetup = catalogService is not null && mockupService is not null ? new CatalogSetupViewModel(catalogService, mockupService, offeringManagementService, providerCatalog, sourceImages, filePicker, rasterImageMetadataReader) : null;
        if (CatalogSetup is not null)
            CatalogSetup.CatalogChanged += OnCatalogChanged;
        ToggleStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = !IsSelectorExpanded);
        ExpandStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = true);
        CollapseStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = false);
        OpenStoreEditorCommand = new RelayCommand(_ => OpenBasicInfoTab());
        OpenNichesTabCommand = new RelayCommand(_ =>
        {
            OpenNichesTab();
        });
        OpenTagsTabCommand = new RelayCommand(_ => OpenTagsTab());
        SelectBasicInfoTabCommand = new RelayCommand(_ => SelectBasicInfoTab());
        SelectNichesTabCommand = new RelayCommand(_ => SelectNichesTab());
        SelectTagsTabCommand = new RelayCommand(_ => SelectTagsTab());
        StartCreateStoreCommand = new RelayCommand(_ => StartCreateStore());
        StartCreateNicheCommand = new RelayCommand(_ => StartCreateNiche());
        PopulateNicheCommand = new RelayCommand(_ => Run(PopulateNicheAsync));
        CloseStoreEditorCommand = new RelayCommand(_ => TryCloseStoreEditor());
        AcceptFirstStorePromptCommand = new RelayCommand(_ =>
        {
            _firstStorePromptDismissed = true;
            OpenStoreEditor();
            if (!HasActiveStores)
            {
                BeginCreateStoreDraft();
            }

            RaisePromptProperties();
        });
        DeclineFirstStorePromptCommand = new RelayCommand(_ =>
        {
            _firstStorePromptDismissed = true;
            RaisePromptProperties();
        });
        CreateStoreCommand = new RelayCommand(_ => Run(CreateStoreAsync));
        SelectStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                Run(cancellationToken => SelectStoreAsync(store, cancellationToken));
            }
            else if (parameter is StoreSelectorEntry entry)
            {
                Run(cancellationToken => SelectStoreAsync(entry.Store, cancellationToken));
            }
        });
        EditStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                SelectStoreForEditing(store);
            }
        });
        SaveSelectedStoreCommand = new RelayCommand(_ => Run(SaveSelectedStoreAsync));
        ArchiveSelectedStoreCommand = new RelayCommand(_ => Run(ArchiveSelectedStoreAsync));
        RestoreStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                Run(cancellationToken => RestoreStoreAsync(store, cancellationToken));
            }
        });
        RequestDeleteSelectedStoreCommand = new RelayCommand(_ => RequestDeleteSelectedStore());
        ConfirmDeleteStoreCommand = new RelayCommand(_ => Run(ConfirmDeleteStoreAsync));
        CancelDeleteStoreCommand = new RelayCommand(_ => ClearDeleteWarning());
        SelectNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                Run(cancellationToken => SelectNicheAsync(niche, cancellationToken));
            }
        });
        EditNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                SelectNicheForEditing(niche);
            }
        });
        SaveSelectedNicheCommand = new RelayCommand(_ => Run(SaveSelectedNicheAsync));
        ArchiveSelectedNicheCommand = new RelayCommand(_ => Run(ArchiveSelectedNicheAsync));
        RestoreNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                Run(cancellationToken => RestoreNicheAsync(niche, cancellationToken));
            }
        });
        RequestDeleteSelectedNicheCommand = new RelayCommand(_ => RequestDeleteSelectedNiche());
        ConfirmDeleteNicheCommand = new RelayCommand(_ => Run(ConfirmDeleteNicheAsync));
        CancelDeleteNicheCommand = new RelayCommand(_ => ClearNicheDeleteWarning());
        ConfirmDiscardChangesCommand = new RelayCommand(_ => ConfirmDiscardChanges());
        KeepEditingCommand = new RelayCommand(_ => ClearDiscardChangesPrompt());
        EditTagCommand = _tagEditor.EditTagCommand;
        SaveSelectedTagCommand = _tagEditor.SaveSelectedTagCommand;
        ArchiveSelectedTagCommand = _tagEditor.ArchiveSelectedTagCommand;
        RestoreTagCommand = _tagEditor.RestoreTagCommand;
        RequestDeleteSelectedTagCommand = _tagEditor.RequestDeleteSelectedTagCommand;
        ConfirmDeleteTagCommand = _tagEditor.ConfirmDeleteTagCommand;
        CancelDeleteTagCommand = _tagEditor.CancelDeleteTagCommand;
        StartCreateTagCommand = _tagEditor.StartCreateTagCommand;
        OpenProductsTabCommand = new RelayCommand(_ => OpenProductsTab());
        SelectProductsTabCommand = new RelayCommand(_ => SelectProductsTab());
        StartCreateProductCommand = new RelayCommand(_ => StartCreateProduct());
        SelectProductCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreProductSummary product)
            {
                SelectProductForEditing(product);
            }
        });
        SaveSelectedProductCommand = new RelayCommand(_ => StartSaveSelectedProduct());
        RequestDeleteSelectedProductCommand = new RelayCommand(_ => RequestDeleteSelectedProduct());
        ConfirmDeleteProductCommand = new RelayCommand(_ => Run(ConfirmDeleteProductAsync));
        CancelDeleteProductCommand = new RelayCommand(_ => ClearProductDeleteWarning());
        RequestArchiveSelectedProductCommand = new RelayCommand(_ => RequestArchiveSelectedProduct());
        ConfirmArchiveSelectedProductCommand = new RelayCommand(_ => Run(ConfirmArchiveSelectedProductAsync));
        CancelArchiveSelectedProductCommand = new RelayCommand(_ => ClearProductArchiveWarning());
        StartCreateOfferingCommand = new RelayCommand(_ => StartCreateOffering());
        CancelNewOfferingCommand = new RelayCommand(_ => CancelNewOffering());
        BackToProductsCommand = new RelayCommand(_ => Run(BackToProductsAsync));
        BackToProductCommand = new RelayCommand(_ => BackToProduct());
        BackToOfferingOverviewCommand = new RelayCommand(_ => NavigateOfferingCatalog(CatalogEditorLevel.OfferingDetail));
        OpenVariantManagementCommand = new RelayCommand(_ => OpenOfferingManagement(CatalogEditorLevel.VariantManagement));
        OpenDesignAreaManagementCommand = new RelayCommand(_ => OpenOfferingManagement(CatalogEditorLevel.DesignAreaManagement));
        OpenMockupTemplateManagementCommand = new RelayCommand(_ => OpenOfferingManagement(CatalogEditorLevel.MockupTemplateManagement));
        OpenNextOfferingReadinessStepCommand = new RelayCommand(_ => OpenNextOfferingReadinessStep());
        OpenProductDetailCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreProductSummary product)
            {
                SelectProductForEditing(product);
            }
        });
        OpenOfferingDetailCommand = new RelayCommand(parameter =>
        {
            if (parameter is FulfillmentOfferingSummary offering)
            {
                SelectOfferingForEditing(offering);
            }
        });
        StartAddVariantCommand = new RelayCommand(_ => StartAddVariant());
        CancelAddVariantCommand = new RelayCommand(_ => IsAddingVariant = false);
        StartAddDesignAreaCommand = new RelayCommand(_ => StartAddDesignArea());
        CancelAddDesignAreaCommand = new RelayCommand(_ => IsAddingDesignArea = false);
        SelectOfferingCommand = new RelayCommand(parameter =>
        {
            if (parameter is BlueprintOfferingCardViewModel card && card.Status == "Archived")
            {
                _productCatalogEditor.SelectArchivedOfferingForCatalog(card.Id);
            }
            else if (parameter is BlueprintOfferingCardViewModel activeCard && SelectedProduct?.Offerings.FirstOrDefault(value => value.Id == activeCard.Id) is { } cardOffering)
            {
                SelectOfferingForEditing(cardOffering);
            }
            else if (parameter is FulfillmentOfferingSummary offering)
            {
                SelectOfferingForEditing(offering);
            }
        });
        SaveSelectedOfferingCommand = new RelayCommand(_ => Run(SaveSelectedOfferingAsync));
        RequestDeleteSelectedOfferingCommand = new RelayCommand(_ => RequestDeleteSelectedOffering());
        ConfirmDeleteOfferingCommand = new RelayCommand(_ => Run(ConfirmDeleteOfferingAsync));
        CancelDeleteOfferingCommand = new RelayCommand(_ => ClearOfferingDeleteWarning());
        AddVariantCommand = new RelayCommand(_ => Run(AddVariantAsync));
        RemoveSelectedVariantCommand = new RelayCommand(parameter =>
        {
            if (parameter is ProductVariantSummary variant)
            {
                Run(cancellationToken => RemoveVariantAsync(variant, cancellationToken));
            }
        });
        AddDesignAreaCommand = new RelayCommand(_ => Run(AddDesignAreaAsync));
        RemoveSelectedDesignAreaCommand = new RelayCommand(parameter =>
        {
            if (parameter is DesignAreaSummary area)
            {
                Run(cancellationToken => RemoveDesignAreaAsync(area, cancellationToken));
            }
        });
    }

    private void OnCatalogChanged(object? sender, EventArgs e)
    {
        if (CatalogEditorLevel == CatalogEditorLevel.ProductDetail)
            Run(RefreshBlueprintOfferingCardsAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<StoreSummary?>? ActiveStoreChanged;

    public event EventHandler? WorkspaceStructureChanged;

    public event EventHandler? StoreNameFocusRequested;

    public event EventHandler? ProductNameFocusRequested;
    public event EventHandler? OfferingNameFocusRequested;

    public IReadOnlyList<StoreSummary> ActiveStores { get; private set; } = [];

    public IReadOnlyList<StoreSummary> ArchivedStores { get; private set; } = [];

    public IReadOnlyList<StoreSelectorEntry> SelectorStores { get; private set; } = [];

    public IReadOnlyList<NicheSummary> ActiveNiches { get; private set; } = [];

    public IReadOnlyList<NicheSummary> ArchivedNiches { get; private set; } = [];

    public IReadOnlyList<TagSummary> ActiveTags => _tagEditor.ActiveTags;
    public IReadOnlyList<TagSummary> ArchivedTags => _tagEditor.ArchivedTags;
    public IReadOnlyList<TagSummary> EditorActiveTags => _tagEditor.EditorActiveTags;
    public TagSummary? SelectedTag => _tagEditor.SelectedTag;
    public bool NeedsFirstTag => _tagEditor.NeedsFirstTag;
    public bool HasActiveTags => _tagEditor.HasActiveTags;
    public bool HasArchivedTags => _tagEditor.HasArchivedTags;
    public bool HasSelectedTag => _tagEditor.HasSelectedTag;
    public bool CanRestoreSelectedTag => _tagEditor.CanRestoreSelectedTag;
    public bool HasUnsavedTagChanges => _tagEditor.HasUnsavedChanges;
    public bool CanSaveSelectedTag => _tagEditor.CanSaveSelectedTag;
    public bool CanArchiveSelectedTag => _tagEditor.CanArchiveSelectedTag;
    public bool CanDeleteSelectedTag => _tagEditor.CanDeleteSelectedTag;
    public bool CanConfirmDeleteTag => _tagEditor.CanConfirmDeleteTag;

    public IReadOnlyList<NicheSummary> EditorActiveNiches =>
        _isCreatingNewNiche && DraftNiche() is { } draft
            ? ActiveNiches.Concat([draft]).ToArray()
            : ActiveNiches;

    public IReadOnlyList<StoreSummary> EditorActiveStores =>
        _isCreatingNewStore && DraftStore() is { } draft
            ? ActiveStores.Concat([draft]).ToArray()
            : ActiveStores;

    public StoreSummary? SelectedStore
    {
        get => _selectedStore;
        private set
        {
            SetField(ref _selectedStore, value);
            var scope = value is null
                ? new StoreManagementScope(_service.ActiveWorkspaceId, null)
                : new StoreManagementScope(value.WorkspaceId, value.Id, value.IsArchived, _isCreatingNewStore);
            _tagEditor.SetScope(scope);
            _storeConfiguration.SetScope(scope);
            _storeConfiguration.SetCanonicalSelection(value, SelectedNiche);
            _productCatalogEditor.Scope = scope;
        }
    }

    public StoreConfigurationViewModel StoreConfiguration => _storeConfiguration;

    public StoreNicheConfigurationViewModel StoreNicheConfiguration => _storeConfiguration;

    public TagEditorViewModel TagEditor => _tagEditor;

    public ProductCatalogEditorViewModel ProductCatalogEditor => _productCatalogEditor;

    public CatalogSetupViewModel? CatalogSetup => _productCatalogEditor.CatalogSetup;

    public StorePrintifyCredentialsViewModel? PrintifyCredentials { get; private set; }
    public FusionCanvas.Application.Stores.Printify.IPrintifyCatalogImportService? PrintifyCatalogImport { get; private set; }
    public PrintifyCatalogImportViewModel? PrintifyCatalogImportSession { get; private set; }

    public void ConfigurePrintify(FusionCanvas.Application.Stores.Printify.IStorePrintifyCredentialStore credentials,
        FusionCanvas.Application.Stores.Printify.IPrintifyCredentialVerifier verifier,
        FusionCanvas.Application.Stores.Printify.IPrintifyCatalogClient? catalogClient = null)
    {
        if (_isDisposed)
        {
            return;
        }

        if (PrintifyCredentials is { } oldCredentials)
        {
            TrackRetiredPrintifyOperation(oldCredentials.WaitForPendingOperationsAsync());
            oldCredentials.ShopSelectionChanged -= OnPrintifyShopSelectionChanged;
            oldCredentials.Dispose();
        }

        if (PrintifyCatalogImportSession is { } oldImportSession)
        {
            TrackRetiredPrintifyOperation(oldImportSession.WaitForPendingOperationsAsync());
            oldImportSession.Dispose();
        }
        PrintifyCredentials = new(new FusionCanvas.Application.Stores.Printify.StorePrintifyConfigurationService(_service, credentials, verifier));
        PrintifyCatalogImport = catalogClient is null
            ? null
            : new FusionCanvas.Application.Stores.Printify.PrintifyCatalogImportService(_service, credentials, catalogClient, _workspaceRepository);
        PrintifyCatalogImportSession = PrintifyCatalogImport is null
            ? null
            : new PrintifyCatalogImportViewModel(PrintifyCatalogImport, () =>
                SelectedStore is { WorkspaceId: var workspaceId, Id: var storeId }
                    ? new FusionCanvas.Application.Stores.Printify.StoreCredentialScope(workspaceId, storeId)
                    : null,
                RefreshAfterPrintifyImportAsync);
        PrintifyCredentials.ShopSelectionChanged += OnPrintifyShopSelectionChanged;
        OnPropertyChanged(nameof(PrintifyCredentials));
        OnPropertyChanged(nameof(PrintifyCatalogImport));
        OnPropertyChanged(nameof(PrintifyCatalogImportSession));
        RefreshPrintifyContext();
    }

    private async Task RefreshAfterPrintifyImportAsync(
        FusionCanvas.Application.Stores.Printify.StoreCredentialScope scope,
        CancellationToken cancellationToken)
    {
        if (SelectedStore is null || SelectedStore.Id != scope.StoreId || SelectedStore.WorkspaceId != scope.WorkspaceId)
            return;
        await LoadAsync(cancellationToken);
        await LoadProductsForSelectedStoreAsync(cancellationToken);
    }

    private bool _showStrategyWarning;
    private Guid? _confirmedStrategyStore;
    public bool ShowStrategyWarning
    {
        get => _showStrategyWarning;
        private set => SetField(ref _showStrategyWarning, value);
    }

    public ICommand ConfirmStrategyCommand => new RelayCommand(_ =>
    {
        if (!ShowStrategyWarning || SelectedStore is null) return;
        ShowStrategyWarning = false;
        _confirmedStrategyStore = SelectedStore.Id;
        Run(SaveSelectedStoreAsync);
    });

    public ICommand CancelStrategyCommand => new RelayCommand(_ =>
    {
        ShowStrategyWarning = false;
        if (SelectedStore is not null) SelectedFulfillmentStrategy = SelectedStore.FulfillmentStrategy;
    });

    private bool HasRetiredPrintifyOperations
    {
        get
        {
            lock (_operationGate)
            {
                return _retiredPrintifyOperations.Any(operation => !operation.IsCompleted);
            }
        }
    }

    private void TrackRetiredPrintifyOperation(Task operation)
    {
        if (operation.IsCompleted)
        {
            return;
        }

        lock (_operationGate)
        {
            _retiredPrintifyOperations.RemoveAll(retired => retired.IsCompleted);
            if (!operation.IsCompleted)
            {
                _retiredPrintifyOperations.Add(operation);
            }
        }
    }

    private void RefreshPrintifyContext() => PrintifyCredentials?.SetContext(
        SelectedStore, SelectedFulfillmentStrategy, _isCreatingNewStore, IsStoreEditorOpen);

    private void OnPrintifyShopSelectionChanged(object? sender, int? shopId)
    {
        _printifyShopId = shopId;
        _printifyShopTitle = (sender as StorePrintifyCredentialsViewModel)?.SelectedShop?.Title;
        _printifyShopSelectionChanged = true;
        _storeConfiguration.PrintifyShopId = _printifyShopId;
        _storeConfiguration.PrintifyShopTitle = _printifyShopTitle;
        _storeConfiguration.PrintifyShopSelectionChanged = true;
        RaiseEditorStateProperties();
    }

    public NicheSummary? SelectedNiche
    {
        get => _selectedNiche;
        private set
        {
            if (SetField(ref _selectedNiche, value))
                _storeConfiguration.SetCanonicalSelection(SelectedStore, value);
        }
    }

    public bool NeedsFirstStore { get; private set; }

    public bool HasActiveStores => ActiveStores.Count > 0;

    public bool HasArchivedStores => ArchivedStores.Count > 0;

    public bool HasActiveNiches => ActiveNiches.Count > 0;

    public bool HasArchivedNiches => ArchivedNiches.Count > 0;

    public bool NeedsFirstNiche { get; private set; }

    public bool HasSelectedStore => SelectedStore is not null;

    public bool HasSelectedNiche => SelectedNiche is not null;

    public bool CanRestoreSelectedStore => SelectedStore is { IsArchived: true };

    public bool CanRestoreSelectedNiche => SelectedNiche is { IsArchived: true };

    public bool HasUnsavedChanges => _storeConfiguration.HasUnsavedStoreChanges;

    public bool HasUnsavedNicheChanges => _storeConfiguration.HasUnsavedNicheChanges;

    public bool HasAnyUnsavedChanges => HasUnsavedChanges || HasUnsavedNicheChanges || HasUnsavedTagChanges || HasAnyCatalogUnsavedChanges;

    public bool CanSaveSelectedStore => _isCreatingNewStore || (SelectedStore is not null && HasUnsavedChanges);

    public bool CanEditStoreConfiguration => _isCreatingNewStore || SelectedStore is { IsArchived: false };

    public bool CanArchiveSelectedStore => SelectedStore is { IsArchived: false } && !_isCreatingNewStore;

    public bool CanDeleteSelectedStore => SelectedStore is not null && !_isCreatingNewStore;

    public bool CanSaveSelectedNiche => _nicheService is not null && (_isCreatingNewNiche || (SelectedNiche is not null && HasUnsavedNicheChanges));

    public bool CanPopulateNiche => _storeConfiguration.CanPopulateNiche;

    public bool IsNichePopulationBusy => _storeConfiguration.IsNichePopulationBusy;

    public string NichePopulationButtonText => IsNichePopulationBusy ? "Populating…" : "Populate";

    public string NichePopulationStatusMessage => _storeConfiguration.NichePopulationStatusMessage;

    public bool HasNichePopulationStatus => _storeConfiguration.HasNichePopulationStatus;

    public bool CanArchiveSelectedNiche => _nicheService is not null && SelectedNiche is { IsArchived: false } && !_isCreatingNewNiche;

    public bool CanDeleteSelectedNiche => _nicheService is not null && SelectedNiche is not null && !_isCreatingNewNiche;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy =>
        _isBusy
        || HasRetiredPrintifyOperations
        || PrintifyCredentials?.HasPendingOperations == true
        || PrintifyCatalogImportSession?.HasPendingOperations == true;

    public bool ShouldShowFirstStorePrompt => NeedsFirstStore && !_firstStorePromptDismissed && !IsStoreEditorOpen;

    public bool IsBasicInfoTabSelected => SelectedEditorTab == StoreManagementEditorTab.BasicInfo;

    public bool IsNichesTabSelected => SelectedEditorTab == StoreManagementEditorTab.Niches;

    public bool IsTagsTabSelected => SelectedEditorTab == StoreManagementEditorTab.Tags;

    public bool IsProductsTabSelected => SelectedEditorTab == StoreManagementEditorTab.Products;

    public CatalogEditorLevel CatalogEditorLevel
    {
        get => _catalogEditorLevel;
        private set
        {
            if (SetField(ref _catalogEditorLevel, value))
            {
                OnPropertyChanged(nameof(IsCatalogOverview));
                OnPropertyChanged(nameof(IsProductDetail));
                OnPropertyChanged(nameof(IsOfferingDetail));
                OnPropertyChanged(nameof(IsOfferingContext));
                OnPropertyChanged(nameof(IsVariantManagement));
                OnPropertyChanged(nameof(IsDesignAreaManagement));
                OnPropertyChanged(nameof(IsMockupTemplateManagement));
                OnPropertyChanged(nameof(CatalogBreadcrumb));
            }
            _productCatalogEditor.SynchronizeNavigationLevel(value);
        }
    }

    public bool IsCatalogOverview => CatalogEditorLevel == CatalogEditorLevel.Overview;

    public bool IsProductDetail => CatalogEditorLevel == CatalogEditorLevel.ProductDetail;

    public bool IsOfferingDetail => CatalogEditorLevel == CatalogEditorLevel.OfferingDetail;

    public bool IsOfferingContext => CatalogEditorLevel is CatalogEditorLevel.OfferingDetail
        or CatalogEditorLevel.VariantManagement
        or CatalogEditorLevel.DesignAreaManagement
        or CatalogEditorLevel.MockupTemplateManagement;

    public bool IsVariantManagement => CatalogEditorLevel == CatalogEditorLevel.VariantManagement;

    public bool IsDesignAreaManagement => CatalogEditorLevel == CatalogEditorLevel.DesignAreaManagement;

    public bool IsMockupTemplateManagement => CatalogEditorLevel == CatalogEditorLevel.MockupTemplateManagement;

    public string CatalogBreadcrumb => CatalogEditorLevel switch
    {
        CatalogEditorLevel.ProductDetail => $"Products  /  {SelectedProduct?.Name ?? "Product"}",
        CatalogEditorLevel.OfferingDetail => $"Products  /  {SelectedProduct?.Name ?? "Product"}  /  {SelectedOffering?.Name ?? "Offering"}",
        CatalogEditorLevel.VariantManagement => $"Products  /  {SelectedProduct?.Name ?? "Product"}  /  {SelectedOffering?.Name ?? "Offering"}  /  Variants",
        CatalogEditorLevel.DesignAreaManagement => $"Products  /  {SelectedProduct?.Name ?? "Product"}  /  {SelectedOffering?.Name ?? "Offering"}  /  Design Areas",
        CatalogEditorLevel.MockupTemplateManagement => $"Products  /  {SelectedProduct?.Name ?? "Product"}  /  {SelectedOffering?.Name ?? "Offering"}  /  Mockup Templates",
        _ => "Products"
    };

    public bool IsBasicsSectionExpanded
    {
        get => _isBasicsSectionExpanded;
        set => SetField(ref _isBasicsSectionExpanded, value);
    }

    public bool IsBlueprintBasicsExpanded
    {
        get => _productCatalogEditor.IsBlueprintBasicsExpanded;
        set => _productCatalogEditor.IsBlueprintBasicsExpanded = value;
    }

    public bool IsVariantsSectionExpanded
    {
        get => _isVariantsSectionExpanded;
        set => SetField(ref _isVariantsSectionExpanded, value);
    }

    public bool IsDesignAreasSectionExpanded
    {
        get => _isDesignAreasSectionExpanded;
        set => SetField(ref _isDesignAreasSectionExpanded, value);
    }

    public bool IsAdvancedSectionExpanded
    {
        get => _isAdvancedSectionExpanded;
        set => SetField(ref _isAdvancedSectionExpanded, value);
    }

    public bool IsAddingVariant
    {
        get => _productCatalogEditor.IsAddingVariant;
        private set => _productCatalogEditor.IsAddingVariant = value;
    }

    public bool IsAddingDesignArea
    {
        get => _productCatalogEditor.IsAddingDesignArea;
        private set => _productCatalogEditor.IsAddingDesignArea = value;
    }

    public StoreManagementEditorTab SelectedEditorTab
    {
        get => _selectedEditorTab;
        private set
        {
            if (SetField(ref _selectedEditorTab, value))
            {
                OnPropertyChanged(nameof(IsBasicInfoTabSelected));
                OnPropertyChanged(nameof(IsNichesTabSelected));
                OnPropertyChanged(nameof(IsTagsTabSelected));
                OnPropertyChanged(nameof(IsProductsTabSelected));
            }
        }
    }

    public string SelectorToggleGlyph => IsSelectorExpanded ? "▲" : "▼";

    public string SelectorToggleTooltip => IsSelectorExpanded ? "Collapse stores" : "Expand stores";

    public bool IsSelectorExpanded
    {
        get => _isSelectorExpanded;
        private set
        {
            if (SetField(ref _isSelectorExpanded, value))
            {
                OnPropertyChanged(nameof(IsSelectorCompact));
                OnPropertyChanged(nameof(SelectorToggleGlyph));
                OnPropertyChanged(nameof(SelectorToggleTooltip));
            }
        }
    }

    public bool IsSelectorCompact => !IsSelectorExpanded;

    public bool IsStoreEditorOpen
    {
        get => _isStoreEditorOpen;
        private set
        {
            if (SetField(ref _isStoreEditorOpen, value))
            {
                RaisePromptProperties();
                RefreshPrintifyContext();
            }
        }
    }

    public bool DeleteWarningVisible
    {
        get => _deleteWarningVisible;
        private set => SetField(ref _deleteWarningVisible, value);
    }

    public bool NicheDeleteWarningVisible
    {
        get => _nicheDeleteWarningVisible;
        private set => SetField(ref _nicheDeleteWarningVisible, value);
    }

    public bool TagDeleteWarningVisible => _tagEditor.DeleteWarningVisible;

    public bool DiscardChangesPromptVisible
    {
        get => _discardChangesPromptVisible;
        private set => SetField(ref _discardChangesPromptVisible, value);
    }

    public string DeleteWarningMessage => _pendingDeleteStore is null
        ? "Permanent deletion cannot be undone."
        : $"Delete '{_pendingDeleteStore.Name}' permanently? This cannot be undone.";

    public string NicheDeleteWarningMessage => _pendingDeleteNiche is null
        ? "Permanent deletion cannot be undone."
        : $"Delete niche '{_pendingDeleteNiche.Name}' permanently? This cannot be undone.";

    public string TagDeleteWarningMessage => _tagEditor.DeleteWarningMessage;

    public string DiscardChangesMessage =>
        HasUnsavedTagChanges && !HasUnsavedChanges && !HasUnsavedNicheChanges && !HasAnyCatalogUnsavedChanges
            ? "Discard changes? Unsaved tag edits will be lost."
            : HasAnyCatalogUnsavedChanges && !HasUnsavedChanges && !HasUnsavedNicheChanges && !HasUnsavedTagChanges
                ? "Discard changes? Unsaved product, offering, variant, or area edits will be lost."
                : HasUnsavedNicheChanges && !HasUnsavedChanges
                    ? "Discard changes? Unsaved niche edits will be lost."
                    : "Discard changes? Unsaved store, niche, tag, or catalog edits will be lost.";

    public string NewStoreName
    {
        get => _storeConfiguration.NewStoreName;
        set => _storeConfiguration.NewStoreName = value;
    }

    public IReadOnlyList<FulfillmentStrategy> AvailableFulfillmentStrategies => FulfillmentStrategyPolicy.AvailableStrategies;

    public IReadOnlyList<FulfillmentStrategy> FulfillmentStrategies => Enum.GetValues<FulfillmentStrategy>();

    public FulfillmentStrategy SelectedFulfillmentStrategy
    {
        get => _storeConfiguration.SelectedFulfillmentStrategy;
        set
        {
            _storeConfiguration.SelectedFulfillmentStrategy = value;
            RaiseEditorStateProperties();
        }
    }

    public string Description
    {
        get => _storeConfiguration.Description;
        set => _storeConfiguration.Description = value;
    }

    public string Notes
    {
        get => _storeConfiguration.Notes;
        set => _storeConfiguration.Notes = value;
    }

    public string TargetMarket
    {
        get => _storeConfiguration.TargetMarket;
        set => _storeConfiguration.TargetMarket = value;
    }

    public string BrandDirection
    {
        get => _storeConfiguration.BrandDirection;
        set => _storeConfiguration.BrandDirection = value;
    }

    public string PlanningContext
    {
        get => _storeConfiguration.PlanningContext;
        set => _storeConfiguration.PlanningContext = value;
    }

    public string Url
    {
        get => _storeConfiguration.Url;
        set => _storeConfiguration.Url = value;
    }

    public string NicheName
    {
        get => _storeConfiguration.NicheName;
        set => _storeConfiguration.NicheName = value;
    }

    public string NicheDescription
    {
        get => _storeConfiguration.NicheDescription;
        set => _storeConfiguration.NicheDescription = value;
    }

    public string NicheAudience
    {
        get => _storeConfiguration.NicheAudience;
        set => _storeConfiguration.NicheAudience = value;
    }

    public string NicheHumorStyle
    {
        get => _storeConfiguration.NicheHumorStyle;
        set => _storeConfiguration.NicheHumorStyle = value;
    }

    public string NicheVisualStyleGuidance
    {
        get => _storeConfiguration.NicheVisualStyleGuidance;
        set => _storeConfiguration.NicheVisualStyleGuidance = value;
    }

    public string NicheConstraints
    {
        get => _storeConfiguration.NicheConstraints;
        set => _storeConfiguration.NicheConstraints = value;
    }

    public string NicheRisks
    {
        get => _storeConfiguration.NicheRisks;
        set => _storeConfiguration.NicheRisks = value;
    }

    public string NicheResearchNotes
    {
        get => _storeConfiguration.NicheResearchNotes;
        set => _storeConfiguration.NicheResearchNotes = value;
    }

    public string NicheNotes
    {
        get => _storeConfiguration.NicheNotes;
        set => _storeConfiguration.NicheNotes = value;
    }

    public string TagName
    {
        get => _tagEditor.TagName;
        set => _tagEditor.TagName = value;
    }

    public string? TagColor
    {
        get => _tagEditor.TagColor;
        set => _tagEditor.TagColor = value;
    }

    public string TagDescription
    {
        get => _tagEditor.TagDescription;
        set => _tagEditor.TagDescription = value;
    }

    public IReadOnlyList<StoreProductSummary> Products => _productCatalogEditor.Products;
    public IReadOnlyList<StoreProductSummary> ArchivedProducts => _productCatalogEditor.ArchivedProducts;
    public ObservableCollection<BlueprintOfferingCardViewModel> BlueprintOfferingCards => _productCatalogEditor.BlueprintOfferingCards;
    public bool HasBlueprintOfferingCards => _productCatalogEditor.HasBlueprintOfferingCards;
    public bool ShowArchivedOfferings { get => _productCatalogEditor.ShowArchivedOfferings; set => _productCatalogEditor.ShowArchivedOfferings = value; }
    public bool HasProducts => _productCatalogEditor.HasProducts;
    public bool ShowArchivedProducts { get => _productCatalogEditor.ShowArchivedProducts; set => _productCatalogEditor.ShowArchivedProducts = value; }
    public IReadOnlyList<StoreProductSummary> EditorProducts => _productCatalogEditor.EditorProducts;
    public StoreProductSummary? SelectedProduct => _productCatalogEditor.SelectedProduct;
    public FulfillmentOfferingSummary? SelectedOffering => _productCatalogEditor.SelectedOffering;
    public IReadOnlyList<ProductVariantSummary> OfferingVariants => _productCatalogEditor.OfferingVariants;
    public bool HasOfferingVariants => _productCatalogEditor.HasOfferingVariants;
    public IReadOnlyList<DesignAreaSummary> OfferingDesignAreas => _productCatalogEditor.OfferingDesignAreas;
    public bool HasOfferingDesignAreas => _productCatalogEditor.HasOfferingDesignAreas;
    public bool HasSelectedProductOfferings => _productCatalogEditor.HasSelectedProductOfferings;
    public int SelectedProductOfferingCount => _productCatalogEditor.SelectedProductOfferingCount;
    public int SelectedOfferingVariantCount => _productCatalogEditor.SelectedOfferingVariantCount;
    public int SelectedOfferingDesignAreaCount => _productCatalogEditor.SelectedOfferingDesignAreaCount;
    public string SelectedProductSummary => _productCatalogEditor.SelectedProductSummary;
    public string SelectedOfferingSummary => _productCatalogEditor.SelectedOfferingSummary;
    public ObservableCollection<ApplicableVariantViewModel> ApplicableVariants => _productCatalogEditor.ApplicableVariants;
    public bool HasSelectedProduct => _productCatalogEditor.HasSelectedProduct;
    public bool HasSelectedOffering => _productCatalogEditor.HasSelectedOffering;
    public bool IsCreatingNewOffering => _productCatalogEditor.IsCreatingNewOffering;
    public bool HasSelectedVariant => _productCatalogEditor.HasSelectedVariant;
    public bool HasSelectedDesignArea => _productCatalogEditor.HasSelectedDesignArea;
    public bool HasAnyCatalogUnsavedChanges => _productCatalogEditor.HasAnyCatalogUnsavedChanges;
    public bool HasUnsavedProductChanges => _productCatalogEditor.HasUnsavedProductChanges;
    public bool HasUnsavedOfferingChanges => _productCatalogEditor.HasUnsavedOfferingChanges;
    public bool CanSaveSelectedProduct => _productCatalogEditor.CanSaveSelectedProduct;
    public bool CanSaveSelectedOffering => _productCatalogEditor.CanSaveSelectedOffering;
    public bool CanDeleteSelectedProduct => _productCatalogEditor.CanDeleteSelectedProduct;
    public bool CanArchiveSelectedProduct => _productCatalogEditor.CanArchiveSelectedProduct;
    public bool ProductArchiveWarningVisible => _productCatalogEditor.ProductArchiveWarningVisible;
    public string ProductArchiveWarningMessage => _productCatalogEditor.ProductArchiveWarningMessage;
    public bool CanDeleteSelectedOffering => _productCatalogEditor.CanDeleteSelectedOffering;
    public bool CanCreateCatalogItem => _productCatalogEditor.CanCreateCatalogItem;
    public bool ProductDeleteWarningVisible => _productCatalogEditor.ProductDeleteWarningVisible;
    public bool OfferingDeleteWarningVisible => _productCatalogEditor.OfferingDeleteWarningVisible;
    public string ProductDeleteWarningMessage => _productCatalogEditor.ProductDeleteWarningMessage;
    public string OfferingDeleteWarningMessage => _productCatalogEditor.OfferingDeleteWarningMessage;
    public string ProductName { get => _productCatalogEditor.ProductName; set => _productCatalogEditor.ProductName = value; }
    public string ProductDescription { get => _productCatalogEditor.ProductDescription; set => _productCatalogEditor.ProductDescription = value; }
    public string ExternalProductId { get => _productCatalogEditor.ExternalProductId; set => _productCatalogEditor.ExternalProductId = value; }
    public string OfferingName { get => _productCatalogEditor.OfferingName; set => _productCatalogEditor.OfferingName = value; }
    public string OfferingDescription { get => _productCatalogEditor.OfferingDescription; set => _productCatalogEditor.OfferingDescription = value; }
    public string OfferingExternalOfferingId { get => _productCatalogEditor.OfferingExternalOfferingId; set => _productCatalogEditor.OfferingExternalOfferingId = value; }
    public bool IsChoiceNetworkOffering => OfferingKindIndex == 1;
    public int OfferingKindIndex { get => _productCatalogEditor.OfferingKindIndex; set => _productCatalogEditor.OfferingKindIndex = value; }
    public string OfferingProviderName { get => _productCatalogEditor.OfferingProviderName; set => _productCatalogEditor.OfferingProviderName = value; }
    public string AreaName { get => _productCatalogEditor.AreaName; set => _productCatalogEditor.AreaName = value; }
    public string AreaPosition { get => _productCatalogEditor.AreaPosition; set => _productCatalogEditor.AreaPosition = value; }
    public string AreaDecorationMethod { get => _productCatalogEditor.AreaDecorationMethod; set => _productCatalogEditor.AreaDecorationMethod = value; }
    public string AreaWidth { get => _productCatalogEditor.AreaWidth; set => _productCatalogEditor.AreaWidth = value; }
    public string AreaHeight { get => _productCatalogEditor.AreaHeight; set => _productCatalogEditor.AreaHeight = value; }
    public string VariantColor { get => _productCatalogEditor.VariantColor; set => _productCatalogEditor.VariantColor = value; }
    public string VariantSize { get => _productCatalogEditor.VariantSize; set => _productCatalogEditor.VariantSize = value; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public ICommand ToggleStoreSelectorCommand { get; }

    public ICommand ExpandStoreSelectorCommand { get; }

    public ICommand CollapseStoreSelectorCommand { get; }

    public ICommand OpenStoreEditorCommand { get; }

    public ICommand OpenNichesTabCommand { get; }

    public ICommand OpenTagsTabCommand { get; }

    public ICommand SelectBasicInfoTabCommand { get; }

    public ICommand SelectNichesTabCommand { get; }

    public ICommand SelectTagsTabCommand { get; }

    public ICommand StartCreateStoreCommand { get; }

    public ICommand StartCreateNicheCommand { get; }

    public ICommand PopulateNicheCommand { get; }

    public ICommand CloseStoreEditorCommand { get; }

    public ICommand AcceptFirstStorePromptCommand { get; }

    public ICommand DeclineFirstStorePromptCommand { get; }

    public ICommand CreateStoreCommand { get; }

    public ICommand SelectStoreCommand { get; }

    public ICommand EditStoreCommand { get; }

    public ICommand SaveSelectedStoreCommand { get; }

    public ICommand ArchiveSelectedStoreCommand { get; }

    public ICommand RestoreStoreCommand { get; }

    public ICommand RequestDeleteSelectedStoreCommand { get; }

    public ICommand ConfirmDeleteStoreCommand { get; }

    public ICommand CancelDeleteStoreCommand { get; }

    public ICommand SelectNicheCommand { get; }

    public ICommand EditNicheCommand { get; }

    public ICommand SaveSelectedNicheCommand { get; }

    public ICommand ArchiveSelectedNicheCommand { get; }

    public ICommand RestoreNicheCommand { get; }

    public ICommand RequestDeleteSelectedNicheCommand { get; }

    public ICommand ConfirmDeleteNicheCommand { get; }

    public ICommand CancelDeleteNicheCommand { get; }

    public ICommand ConfirmDiscardChangesCommand { get; }

    public ICommand KeepEditingCommand { get; }

    public ICommand EditTagCommand { get; }

    public ICommand SaveSelectedTagCommand { get; }

    public ICommand ArchiveSelectedTagCommand { get; }

    public ICommand RestoreTagCommand { get; }

    public ICommand RequestDeleteSelectedTagCommand { get; }

    public ICommand ConfirmDeleteTagCommand { get; }

    public ICommand CancelDeleteTagCommand { get; }

    public ICommand StartCreateTagCommand { get; }

    public ICommand OpenProductsTabCommand { get; }

    public ICommand SelectProductsTabCommand { get; }

    public ICommand StartCreateProductCommand { get; }

    public ICommand SelectProductCommand { get; }

    public ICommand SaveSelectedProductCommand { get; }

    public ICommand RequestDeleteSelectedProductCommand { get; }

    public ICommand ConfirmDeleteProductCommand { get; }

    public ICommand CancelDeleteProductCommand { get; }

    public ICommand RequestArchiveSelectedProductCommand { get; }

    public ICommand ConfirmArchiveSelectedProductCommand { get; }

    public ICommand CancelArchiveSelectedProductCommand { get; }

    public ICommand StartCreateOfferingCommand { get; }
    public ICommand CancelNewOfferingCommand { get; }
    public ICommand BackToProductsCommand { get; }
    public ICommand BackToProductCommand { get; }
    public ICommand BackToOfferingOverviewCommand { get; }
    public ICommand OpenVariantManagementCommand { get; }
    public ICommand OpenDesignAreaManagementCommand { get; }
    public ICommand OpenMockupTemplateManagementCommand { get; }
    public ICommand OpenNextOfferingReadinessStepCommand { get; }
    public ICommand OpenProductDetailCommand { get; }
    public ICommand OpenOfferingDetailCommand { get; }
    public ICommand StartAddVariantCommand { get; }
    public ICommand CancelAddVariantCommand { get; }
    public ICommand StartAddDesignAreaCommand { get; }
    public ICommand CancelAddDesignAreaCommand { get; }

    public ICommand SelectOfferingCommand { get; }

    public ICommand SaveSelectedOfferingCommand { get; }

    public ICommand RequestDeleteSelectedOfferingCommand { get; }

    public ICommand ConfirmDeleteOfferingCommand { get; }

    public ICommand CancelDeleteOfferingCommand { get; }

    public ICommand AddVariantCommand { get; }

    public ICommand RemoveSelectedVariantCommand { get; }

    public ICommand AddDesignAreaCommand { get; }

    public ICommand RemoveSelectedDesignAreaCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.LoadStoresAsync(cancellationToken);
        await _storeConfiguration.LoadNichesAsync(cancellationToken);
        await LoadTagsForSelectedStoreAsync(cancellationToken);
        if (CatalogSetup is not null && SelectedStore is not null)
            await CatalogSetup.LoadForStoreAsync(SelectedStore.Id, cancellationToken);
    }

    public async Task SetActiveWorkspaceAsync(Guid? workspaceId, CancellationToken cancellationToken = default)
    {
        PrintifyCredentials?.SetContext(null, FulfillmentStrategy.Manual, false, false);
        ShowStrategyWarning = false;
        _service.SetActiveWorkspace(workspaceId);
        _nicheService?.SetActiveWorkspace(workspaceId);
        _tagEditor.SetScope(new StoreManagementScope(workspaceId, null));
        _isCreatingNewStore = false;
        _isCreatingNewNiche = false;
        _draftStoreId = null;
        _draftNicheId = null;
        _storeConfiguration.SetScope(new StoreManagementScope(workspaceId, null));
        _productCatalogEditor.Scope = new StoreManagementScope(workspaceId, null);
        await _storeConfiguration.LoadStoresAsync(cancellationToken);
        await _storeConfiguration.LoadNichesAsync(cancellationToken);
        await LoadTagsForSelectedStoreAsync(cancellationToken);
        if (CatalogSetup is not null && SelectedStore is not null)
            Run(token => CatalogSetup.LoadForStoreAsync(SelectedStore.Id, token), cancellationToken);
    }

    public async Task CreateStoreAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.CreateStoreAsync(cancellationToken);
        if (SelectedStore is not null)
        {
            _firstStorePromptDismissed = true;
            ClearDeleteWarning();
            RaisePromptProperties();
        }
    }

    public async Task SelectStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        await _storeConfiguration.SelectStoreAsync(store, cancellationToken);
    }

    public void SelectStoreForEditing(StoreSummary store)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (_isCreatingNewStore && store.Id == _draftStoreId)
        {
            SelectedStore = DraftStore();
            OnPropertyChanged(nameof(SelectedStore));
            return;
        }

        if (HasAnyUnsavedChanges && SelectedStore?.Id != store.Id)
        {
            RequestDiscardBefore(PendingEditorAction.SelectStore, store);
            return;
        }

        PerformSelectStoreForEditing(store);
    }

    private void PerformSelectStoreForEditing(StoreSummary store)
    {
        _isCreatingNewStore = false;
        _draftStoreId = null;
        ShowArchivedProducts = false;
        SelectedStore = store;
        ApplySelectedStoreFields(store);
        CaptureOriginalEditorState();
        ClearDeleteWarning();
        ClearDiscardChangesPrompt();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanEditStoreConfiguration));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        Run(LoadNichesForSelectedStoreAsync);
        Run(LoadTagsForSelectedStoreAsync);
        if (CatalogSetup is not null)
            Run(token => CatalogSetup.LoadForStoreAsync(store.Id, token));
    }

    public void StartCreateStore()
    {
        if (HasAnyUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.StartNewStore);
            return;
        }

        BeginCreateStoreDraft();
    }

    private void BeginCreateStoreDraft()
    {
        _isCreatingNewStore = true;
        _draftStoreId = Guid.NewGuid();
        ClearNicheSelection();
        SelectedStore = DraftStore();
        ErrorMessage = null;
        ClearDeleteWarning();
        ClearDiscardChangesPrompt();
        ClearEditorFields();
        _storeConfiguration.StartCreateStoreDraft(_draftStoreId);
        CaptureOriginalEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        StoreNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SelectNicheForEditing(NicheSummary niche)
    {
        ArgumentNullException.ThrowIfNull(niche);
        if (_isCreatingNewNiche && niche.Id == _draftNicheId)
        {
            SelectedNiche = DraftNiche();
            OnPropertyChanged(nameof(SelectedNiche));
            return;
        }

        if (HasUnsavedNicheChanges && SelectedNiche?.Id != niche.Id)
        {
            RequestDiscardBefore(PendingEditorAction.SelectNiche, niche: niche);
            return;
        }

        PerformSelectNicheForEditing(niche);
    }

    private void PerformSelectNicheForEditing(NicheSummary niche)
    {
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        SelectedNiche = niche;
        ApplySelectedNicheFields(niche);
        CaptureOriginalNicheEditorState();
        ClearNicheDeleteWarning();
        ClearDiscardChangesPrompt();
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
    }

    public void StartCreateNiche()
    {
        if (SelectedStore is null || SelectedStore.IsArchived || _isCreatingNewStore)
        {
            ErrorMessage = "Select an active saved store before creating a niche.";
            return;
        }

        if (HasUnsavedNicheChanges)
        {
            RequestDiscardBefore(PendingEditorAction.StartNewNiche);
            return;
        }

        BeginCreateNicheDraft();
    }

    private void BeginCreateNicheDraft()
    {
        _isCreatingNewNiche = true;
        _draftNicheId = Guid.NewGuid();
        SelectedNiche = DraftNiche();
        ErrorMessage = null;
        ClearNicheDeleteWarning();
        ClearDiscardChangesPrompt();
        ClearNicheEditorFields();
        _storeConfiguration.StartCreateNicheDraft(_draftNicheId);
        CaptureOriginalNicheEditorState();
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
    }

    public async Task RefreshNichePopulationAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.RefreshNichePopulationAvailabilityAsync(cancellationToken);
    }

    public async Task PopulateNicheAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.PopulateNicheAsync(cancellationToken);
    }





    public async Task SaveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (!_isCreatingNewStore && SelectedStore is not null && FulfillmentStrategyPolicy.RequiresPrintifyKey(SelectedStore.FulfillmentStrategy)
            && !FulfillmentStrategyPolicy.RequiresPrintifyKey(SelectedFulfillmentStrategy) && _confirmedStrategyStore != SelectedStore.Id)
        {
            ShowStrategyWarning = true;
            return;
        }
        _confirmedStrategyStore = null;
        await _storeConfiguration.SaveSelectedStoreAsync(cancellationToken);
    }

    public async Task SaveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.SaveSelectedNicheAsync(cancellationToken);
    }

    public async Task ArchiveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.ArchiveSelectedStoreAsync(cancellationToken);
    }

    public async Task ArchiveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.ArchiveSelectedNicheAsync(cancellationToken);
    }

    public async Task RestoreStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.RestoreStoreAsync(store, cancellationToken);
    }

    public async Task RestoreNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.RestoreNicheAsync(niche, cancellationToken);
    }

    public void RequestDeleteSelectedStore()
    {
        if (_isCreatingNewStore)
        {
            ErrorMessage = "Save the new store before deleting it.";
            return;
        }

        if (SelectedStore is null)
        {
            ErrorMessage = "Select a store before deleting.";
            return;
        }

        _pendingDeleteStore = SelectedStore;
        DeleteWarningVisible = true;
        OnPropertyChanged(nameof(DeleteWarningMessage));
    }

    public void RequestDeleteSelectedNiche()
    {
        if (_isCreatingNewNiche)
        {
            ErrorMessage = "Save the new niche before deleting it.";
            return;
        }

        if (SelectedNiche is null)
        {
            ErrorMessage = "Select a niche before deleting.";
            return;
        }

        _pendingDeleteNiche = SelectedNiche;
        NicheDeleteWarningVisible = true;
        OnPropertyChanged(nameof(NicheDeleteWarningMessage));
    }

    public async Task ConfirmDeleteStoreAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingDeleteStore is null)
        {
            ErrorMessage = "Select a store before deleting.";
            return;
        }
        if (await _storeConfiguration.DeleteStoreAsync(_pendingDeleteStore, cancellationToken))
        {
            SelectDefaultStoreForEditing();
        }
        ClearDeleteWarning();
    }

    public async Task ConfirmDeleteNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingDeleteNiche is null)
        {
            ErrorMessage = "Select a niche before deleting.";
            return;
        }
        if (await _storeConfiguration.DeleteNicheAsync(_pendingDeleteNiche, cancellationToken))
        {
            SelectDefaultNicheForEditing();
        }
        ClearNicheDeleteWarning();
    }

    public async Task SelectNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.SelectNicheAsync(niche, cancellationToken);
    }

    private void OpenStoreEditor()
    {
        IsStoreEditorOpen = true;
        if (SelectedStore is not null)
        {
            ApplySelectedStoreFields(SelectedStore);
            CaptureOriginalEditorState();
        }
    }

    private void OpenBasicInfoTab()
    {
        OpenStoreEditor();
        SelectBasicInfoTab();
    }

    private void OpenNichesTab()
    {
        OpenStoreEditor();
        SelectNichesTab();
    }

    private void SelectBasicInfoTab()
    {
        if (HasUnsavedNicheChanges || HasUnsavedTagChanges || HasAnyCatalogUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectBasicInfoTab);
            return;
        }

        SelectedEditorTab = StoreManagementEditorTab.BasicInfo;
    }

    private void SelectNichesTab()
    {
        if (HasUnsavedChanges || HasUnsavedTagChanges || HasAnyCatalogUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectNichesTab);
            return;
        }

        SelectedEditorTab = StoreManagementEditorTab.Niches;
        if (NeedsFirstNiche && SelectedStore is { IsArchived: false } && !_isCreatingNewStore)
        {
            BeginCreateNicheDraft();
        }

        Run(LoadNichesForSelectedStoreAsync);
    }

    public bool TryCloseStoreEditor()
    {
        if (!IsStoreEditorOpen)
        {
            return true;
        }

        if (HasAnyUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.CloseEditor);
            return false;
        }

        ClearDiscardChangesPrompt();
        IsStoreEditorOpen = false;
        return true;
    }

    private void ApplyResult(StoreManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyState(result.State);
        if (result.Store is not null && result.Succeeded)
        {
            PerformSelectStoreForEditing(result.Store);
            WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyNicheResult(NicheManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyNicheState(result.State);
        if (result.Niche is not null && result.Succeeded)
        {
            PerformSelectNicheForEditing(result.Niche);
            WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyState(StoreManagementState state)
    {
        ActiveStores = state.ActiveStores;
        ArchivedStores = state.ArchivedStores;
        SelectedStore = _isCreatingNewStore
            ? DraftStore()
            : state.ActiveStore
                ?? ActiveStores.FirstOrDefault(store => store.Id == SelectedStore?.Id)
                ?? ArchivedStores.FirstOrDefault(store => store.Id == SelectedStore?.Id)
                ?? ActiveStores.FirstOrDefault()
                ?? ArchivedStores.FirstOrDefault();
        NeedsFirstStore = state.NeedsFirstStore;
        SelectorStores = ActiveStores
            .Select(store => new StoreSelectorEntry(store, store.Id == state.ActiveStoreId))
            .ToArray();
        if (!_isCreatingNewStore)
        {
            ApplySelectedStoreFields(SelectedStore);
            CaptureOriginalEditorState();
        }

        OnPropertyChanged(nameof(ActiveStores));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(ArchivedStores));
        OnPropertyChanged(nameof(SelectorStores));
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(NeedsFirstStore));
        OnPropertyChanged(nameof(HasActiveStores));
        OnPropertyChanged(nameof(HasArchivedStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        RaiseEditorStateProperties();
        RaisePromptProperties();
        ActiveStoreChanged?.Invoke(this, state.ActiveStore);
    }

    private async Task LoadNichesForSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        await _storeConfiguration.LoadNichesAsync(cancellationToken);
    }

    private async Task LoadTagsForSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        await _tagEditor.LoadAsync(cancellationToken);
        if (IsTagsTabSelected)
            _tagEditor.OnTabSelected();
    }

    public void SelectTagForEditing(TagSummary tag) => _tagEditor.SelectTagForEditing(tag);

    public void StartCreateTag() => _tagEditor.StartCreateTag();


    public Task SaveSelectedTagAsync(CancellationToken cancellationToken = default) =>
        _tagEditor.SaveSelectedTagAsync(cancellationToken);

    public Task ArchiveSelectedTagAsync(CancellationToken cancellationToken = default) =>
        _tagEditor.ArchiveSelectedTagAsync(cancellationToken);

    public Task RestoreTagAsync(TagSummary tag, CancellationToken cancellationToken = default) =>
        _tagEditor.RestoreTagAsync(tag, cancellationToken);

    public void RequestDeleteSelectedTag() => _tagEditor.RequestDeleteSelectedTag();

    public Task ConfirmDeleteTagAsync(CancellationToken cancellationToken = default) =>
        _tagEditor.ConfirmDeleteTagAsync(cancellationToken);

    private void ClearTagDeleteWarning() => _tagEditor.ClearDeleteWarning();

    private void RaiseTagEditorActionProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedTagChanges));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedTag));
        OnPropertyChanged(nameof(CanArchiveSelectedTag));
        OnPropertyChanged(nameof(CanDeleteSelectedTag));
    }

    private void OpenTagsTab()
    {
        OpenStoreEditor();
        SelectTagsTab();
    }

    private void SelectTagsTab()
    {
        if (HasUnsavedChanges || HasUnsavedNicheChanges || HasAnyCatalogUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectTagsTab);
            return;
        }

        SelectedEditorTab = StoreManagementEditorTab.Tags;
        _tagEditor.OnTabSelected();

        Run(LoadTagsForSelectedStoreAsync);
    }

    private void OpenProductsTab()
    {
        OpenStoreEditor();
        SelectProductsTab();
    }

    private void SelectProductsTab()
    {
        if (HasUnsavedChanges || HasUnsavedNicheChanges || HasUnsavedTagChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectProductsTab);
            return;
        }
        SelectedEditorTab = StoreManagementEditorTab.Products;
        CatalogEditorLevel = CatalogEditorLevel.Overview;
        ShowArchivedProducts = false;
        IsAddingVariant = false;
        IsAddingDesignArea = false;
        Run(LoadProductsForSelectedStoreAsync);
    }

    private Task LoadProductsForSelectedStoreAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.LoadAsync(cancellationToken);
    public void SelectProductForEditing(StoreProductSummary product) => _productCatalogEditor.SelectProductForEditing(product);
    private Task RefreshBlueprintOfferingCardsAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.RefreshBlueprintOfferingCardsAsync(cancellationToken);
    public void SelectOfferingForEditing(FulfillmentOfferingSummary offering) => _productCatalogEditor.SelectOfferingForEditing(offering);

    private async Task BackToProductsAsync(CancellationToken cancellationToken = default)
    {
        if (_productSaveTask is { IsCompleted: false }) await _productSaveTask.WaitAsync(cancellationToken);
        _productCatalogEditor.NavigateCatalog(CatalogEditorLevel.Overview);
    }
    private void BackToProducts() => _productCatalogEditor.NavigateCatalog(CatalogEditorLevel.Overview);
    private void BackToProduct() => _productCatalogEditor.NavigateCatalog(CatalogEditorLevel.ProductDetail);
    private void OpenOfferingManagement(CatalogEditorLevel level) => _productCatalogEditor.NavigateCatalog(level);

    private void OpenNextOfferingReadinessStep()
    {
        var nextIssue = CatalogSetup?.OfferingReadinessIssues.FirstOrDefault();
        var level = nextIssue?.Kind switch
        {
            OfferingReadinessIssueKind.MissingVariants => CatalogEditorLevel.VariantManagement,
            OfferingReadinessIssueKind.MissingDesignAreas => CatalogEditorLevel.DesignAreaManagement,
            OfferingReadinessIssueKind.MissingMockupTemplates => CatalogEditorLevel.MockupTemplateManagement,
            OfferingReadinessIssueKind.IncompleteMockupTemplate => CatalogEditorLevel.MockupTemplateManagement,
            _ => (CatalogEditorLevel?)null
        };

        if (level is CatalogEditorLevel target)
        {
            OpenOfferingManagement(target);
        }
    }
    private void NavigateOfferingCatalog(CatalogEditorLevel level) => _productCatalogEditor.NavigateCatalog(level);
    private void StartAddVariant() => _productCatalogEditor.StartAddVariant();
    private void StartAddDesignArea() => _productCatalogEditor.StartAddDesignArea();
    public void StartCreateProduct() => _productCatalogEditor.StartCreateProduct();
    public Task SaveSelectedProductAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.SaveSelectedProductAsync(cancellationToken);

    private void StartSaveSelectedProduct()
    {
        if (_productSaveTask is { IsCompleted: false }) return;
        Run(cancellationToken =>
        {
            _productSaveTask = SaveSelectedProductAndReportFailureAsync(cancellationToken);
            return _productSaveTask;
        });
    }
    private async Task SaveSelectedProductAndReportFailureAsync(CancellationToken cancellationToken)
    {
        try { await SaveSelectedProductAsync(cancellationToken).ConfigureAwait(true); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { ErrorMessage = $"Blueprint could not be saved: {exception.Message}"; }
    }
    public void RequestDeleteSelectedProduct() => _productCatalogEditor.RequestDeleteSelectedProduct();
    public void RequestArchiveSelectedProduct() => _productCatalogEditor.RequestArchiveSelectedProduct();
    public Task ConfirmArchiveSelectedProductAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.ConfirmArchiveSelectedProductAsync(cancellationToken);
    private void ClearProductArchiveWarning() => _productCatalogEditor.ClearProductArchiveWarning();
    public Task ConfirmDeleteProductAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.ConfirmDeleteProductAsync(cancellationToken);
    private void ClearProductDeleteWarning() => _productCatalogEditor.ClearProductDeleteWarning();
    public void StartCreateOffering() => _productCatalogEditor.StartCreateOffering();
    private void CancelNewOffering() => _productCatalogEditor.CancelNewOffering();
    public Task SaveSelectedOfferingAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.SaveSelectedOfferingAsync(cancellationToken);
    public void RequestDeleteSelectedOffering() => _productCatalogEditor.RequestDeleteSelectedOffering();
    public Task ConfirmDeleteOfferingAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.ConfirmDeleteOfferingAsync(cancellationToken);
    private void ClearOfferingDeleteWarning() => _productCatalogEditor.ClearOfferingDeleteWarning();
    public Task AddVariantAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.AddVariantAsync(cancellationToken);
    public Task RemoveVariantAsync(ProductVariantSummary variant, CancellationToken cancellationToken = default) => _productCatalogEditor.RemoveVariantAsync(variant, cancellationToken);
    public Task AddDesignAreaAsync(CancellationToken cancellationToken = default) => _productCatalogEditor.AddDesignAreaAsync(cancellationToken);
    public Task RemoveDesignAreaAsync(DesignAreaSummary area, CancellationToken cancellationToken = default) => _productCatalogEditor.RemoveDesignAreaAsync(area, cancellationToken);
    private void RaiseProductEditorStateProperties() { OnPropertyChanged(nameof(HasUnsavedProductChanges)); OnPropertyChanged(nameof(HasAnyUnsavedChanges)); OnPropertyChanged(nameof(HasAnyCatalogUnsavedChanges)); OnPropertyChanged(nameof(CanSaveSelectedProduct)); OnPropertyChanged(nameof(EditorProducts)); }
    private void RaiseOfferingEditorStateProperties() { OnPropertyChanged(nameof(HasUnsavedOfferingChanges)); OnPropertyChanged(nameof(HasAnyUnsavedChanges)); OnPropertyChanged(nameof(HasAnyCatalogUnsavedChanges)); OnPropertyChanged(nameof(CanSaveSelectedOffering)); OnPropertyChanged(nameof(CanDeleteSelectedOffering)); OnPropertyChanged(nameof(IsChoiceNetworkOffering)); }

    private void ApplyNicheState(NicheManagementState state)
    {
        ActiveNiches = state.ActiveNiches;
        ArchivedNiches = state.ArchivedNiches;
        SelectedNiche = _isCreatingNewNiche
            ? DraftNiche()
            : state.ActiveNiche
                ?? ActiveNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id)
                ?? ArchivedNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id)
                ?? ActiveNiches.FirstOrDefault()
                ?? ArchivedNiches.FirstOrDefault();
        NeedsFirstNiche = state.NeedsFirstNiche;
        if (!_isCreatingNewNiche)
        {
            ApplySelectedNicheFields(SelectedNiche);
            CaptureOriginalNicheEditorState();
        }

        OnPropertyChanged(nameof(ActiveNiches));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(ArchivedNiches));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(NeedsFirstNiche));
        OnPropertyChanged(nameof(HasActiveNiches));
        OnPropertyChanged(nameof(HasArchivedNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
        RaiseNicheEditorStateProperties();
    }

    private void SelectDefaultStoreForEditing()
    {
        var defaultStore = ActiveStores.FirstOrDefault() ?? ArchivedStores.FirstOrDefault();
        if (defaultStore is not null)
        {
            PerformSelectStoreForEditing(defaultStore);
            return;
        }

        SelectedStore = null;
        ClearEditorFields();
        CaptureOriginalEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
    }

    private void SelectDefaultNicheForEditing()
    {
        var defaultNiche = ActiveNiches.FirstOrDefault() ?? ArchivedNiches.FirstOrDefault();
        if (defaultNiche is not null)
        {
            PerformSelectNicheForEditing(defaultNiche);
            return;
        }

        ClearNicheSelection();
    }

    private void ClearNicheSelection()
    {
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        ActiveNiches = [];
        ArchivedNiches = [];
        SelectedNiche = null;
        ClearNicheEditorFields();
        CaptureOriginalNicheEditorState();
        OnPropertyChanged(nameof(ActiveNiches));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(ArchivedNiches));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(HasActiveNiches));
        OnPropertyChanged(nameof(HasArchivedNiches));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
    }

    private void ApplySelectedStoreFields(StoreSummary? store)
    {
        if (store is null)
            _storeConfiguration.DiscardStoreDraft();
        else
            _storeConfiguration.SelectStoreForEditing(store);
        _printifyShopId = store?.Context.PrintifyShopId;
        _printifyShopTitle = store?.Context.PrintifyShopTitle;
        _printifyShopSelectionChanged = false;
    }

    private void ApplySelectedNicheFields(NicheSummary? niche)
    {
        if (niche is null)
            _storeConfiguration.DiscardNicheDraft();
        else
            _storeConfiguration.SelectNicheForEditing(niche);
    }

    private void ClearEditorFields() => _storeConfiguration.DiscardStoreDraft();

    private void ClearNicheEditorFields() => _storeConfiguration.DiscardNicheDraft();

    private StoreSummary? DraftStore()
    {
        if (!_isCreatingNewStore || _draftStoreId is not { } id)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var name = string.IsNullOrWhiteSpace(NewStoreName) ? "New store" : NewStoreName.Trim();
        return new StoreSummary(id, SelectedStore?.WorkspaceId ?? WorkspaceDefaults.DefaultWorkspaceId, name, CurrentContext(), false, now, now, SelectedFulfillmentStrategy);
    }

    private NicheSummary? DraftNiche()
    {
        if (!_isCreatingNewNiche || _draftNicheId is not { } id || SelectedStore is null)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var name = string.IsNullOrWhiteSpace(NicheName) ? "New niche" : NicheName.Trim();
        return new NicheSummary(id, SelectedStore.Id, name, CurrentNicheContext(), IsArchived: false, now, now);
    }

    private void RequestDiscardBefore(PendingEditorAction action, StoreSummary? store = null, NicheSummary? niche = null, Action? discardContinuation = null)
    {
        _pendingEditorAction = action;
        _pendingEditorStore = store;
        _pendingEditorNiche = niche;
        _pendingDiscardContinuation = discardContinuation;
        ClearDeleteWarning();
        ClearNicheDeleteWarning();
        ClearTagDeleteWarning();
        ClearProductDeleteWarning();
        ClearOfferingDeleteWarning();
        DiscardChangesPromptVisible = true;
        OnPropertyChanged(nameof(DiscardChangesMessage));
    }

    private void ConfirmDiscardChanges()
    {
        var action = _pendingEditorAction;
        var store = _pendingEditorStore;
        var niche = _pendingEditorNiche;
        var discardContinuation = _pendingDiscardContinuation;
        DiscardCurrentEditorChanges();
        ClearDiscardChangesPrompt();

        switch (action)
        {
            case PendingEditorAction.SelectStore when store is not null:
                PerformSelectStoreForEditing(store);
                break;
            case PendingEditorAction.StartNewStore:
                BeginCreateStoreDraft();
                break;
            case PendingEditorAction.CloseEditor:
                IsStoreEditorOpen = false;
                break;
            case PendingEditorAction.SelectNiche when niche is not null:
                PerformSelectNicheForEditing(niche);
                break;
            case PendingEditorAction.StartNewNiche:
                BeginCreateNicheDraft();
                break;
            case PendingEditorAction.SelectBasicInfoTab:
                SelectedEditorTab = StoreManagementEditorTab.BasicInfo;
                break;
            case PendingEditorAction.SelectNichesTab:
                SelectedEditorTab = StoreManagementEditorTab.Niches;
                Run(LoadNichesForSelectedStoreAsync);
                break;
            case PendingEditorAction.SelectTagsTab:
                SelectedEditorTab = StoreManagementEditorTab.Tags;
                _tagEditor.OnTabSelected();
                Run(LoadTagsForSelectedStoreAsync);
                break;
            case PendingEditorAction.SelectProductsTab:
                SelectedEditorTab = StoreManagementEditorTab.Products;
                Run(LoadProductsForSelectedStoreAsync);
                break;
        }

        discardContinuation?.Invoke();
    }

    private void DiscardCurrentEditorChanges()
    {
        if (_isCreatingNewStore)
        {
            _isCreatingNewStore = false;
            _draftStoreId = null;
            SelectedStore = ActiveStores.FirstOrDefault(store => store.Id == SelectedStore?.Id) ?? ActiveStores.FirstOrDefault();
            ApplySelectedStoreFields(SelectedStore);
        }
        else
        {
            ApplySelectedStoreFields(SelectedStore);
        }

        CaptureOriginalEditorState();
        if (_isCreatingNewNiche)
        {
            _isCreatingNewNiche = false;
            _draftNicheId = null;
            SelectedNiche = ActiveNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id) ?? ActiveNiches.FirstOrDefault();
        }

        ApplySelectedNicheFields(SelectedNiche);
        CaptureOriginalNicheEditorState();
        _tagEditor.DiscardUnsavedChanges();
        _productCatalogEditor.DiscardUnsavedChanges();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(SelectedProduct));
        OnPropertyChanged(nameof(EditorProducts));
        OnPropertyChanged(nameof(SelectedOffering));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        RaiseNicheEditorActionProperties();
        RaiseTagEditorActionProperties();
        RaiseProductEditorStateProperties();
        RaiseOfferingEditorStateProperties();
    }

    private void ClearDiscardChangesPrompt()
    {
        _pendingEditorAction = PendingEditorAction.None;
        _pendingEditorStore = null;
        _pendingEditorNiche = null;
        _pendingDiscardContinuation = null;
        DiscardChangesPromptVisible = false;
    }

    private void ClearDeleteWarning()
    {
        _pendingDeleteStore = null;
        DeleteWarningVisible = false;
        OnPropertyChanged(nameof(DeleteWarningMessage));
    }

    private void ClearNicheDeleteWarning()
    {
        _pendingDeleteNiche = null;
        NicheDeleteWarningVisible = false;
        OnPropertyChanged(nameof(NicheDeleteWarningMessage));
    }

    private StoreContext CurrentContext() =>
        new(
            EmptyToNull(Description),
            EmptyToNull(Notes),
            EmptyToNull(TargetMarket),
            EmptyToNull(BrandDirection),
            EmptyToNull(PlanningContext),
            EmptyToNull(Url),
            PrintifyCredentials is null ? _printifyShopId : PrintifyCredentials.SelectedShopId,
            PrintifyCredentials?.SelectedShop?.Title ?? _printifyShopTitle);

    private NicheContext CurrentNicheContext() =>
        new(
            EmptyToNull(NicheDescription),
            EmptyToNull(NicheAudience),
            EmptyToNull(NicheHumorStyle),
            EmptyToNull(NicheVisualStyleGuidance),
            EmptyToNull(NicheConstraints),
            EmptyToNull(NicheRisks),
            EmptyToNull(NicheResearchNotes),
            EmptyToNull(NicheNotes));

    private void CaptureOriginalEditorState()
    {
        _storeConfiguration.PrintifyShopId = _printifyShopId;
        _storeConfiguration.PrintifyShopTitle = _printifyShopTitle;
        _storeConfiguration.PrintifyShopSelectionChanged = _printifyShopSelectionChanged;
        _storeConfiguration.CaptureStoreDraft();
        RaiseEditorStateProperties();
    }

    private void CaptureOriginalNicheEditorState()
    {
        _storeConfiguration.CaptureNicheDraft();
        RaiseNicheEditorStateProperties();
    }

    private void RaiseEditorStateProperties() =>
        RaiseEditorActionProperties();

    private void RaiseEditorActionProperties()
    {
        RefreshPrintifyContext();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedStore));
        OnPropertyChanged(nameof(CanArchiveSelectedStore));
        OnPropertyChanged(nameof(CanDeleteSelectedStore));
    }

    private void RaiseNicheEditorStateProperties() =>
        RaiseNicheEditorActionProperties();

    private void RaiseNicheEditorActionProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedNicheChanges));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedNiche));
        OnPropertyChanged(nameof(CanArchiveSelectedNiche));
        OnPropertyChanged(nameof(CanDeleteSelectedNiche));
        RaiseNichePopulationProperties();
    }

    private void RaiseNichePopulationProperties()
    {
        OnPropertyChanged(nameof(CanPopulateNiche));
        OnPropertyChanged(nameof(IsNichePopulationBusy));
        OnPropertyChanged(nameof(NichePopulationButtonText));
        OnPropertyChanged(nameof(NichePopulationStatusMessage));
        OnPropertyChanged(nameof(HasNichePopulationStatus));
    }


    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public ValueTask DisposeAsync()
    {
        Task disposalTask;
        TaskCompletionSource cancellationCompleted;
        lock (_operationGate)
        {
            if (_disposeTask is not null)
            {
                return new ValueTask(_disposeTask);
            }

            _isDisposed = true;
            cancellationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var childOperations = new List<Task>(_retiredPrintifyOperations);
            _retiredPrintifyOperations.Clear();
            if (PrintifyCredentials is { } credentialsForShutdown)
            {
                childOperations.Add(credentialsForShutdown.WaitForPendingOperationsAsync());
            }

            if (PrintifyCatalogImportSession is { } importSession)
            {
                childOperations.Add(importSession.WaitForPendingOperationsAsync());
            }

            var operations = _inFlightOperations
                .Select(operation => operation.Completion.Task)
                .Concat(childOperations)
                .Distinct()
                .ToArray();
            _disposeTask = CompleteDisposalAsync(operations, cancellationCompleted.Task);
            disposalTask = _disposeTask;
        }

        Exception? shutdownException = null;
        if (PrintifyCredentials is { } credentials)
        {
            credentials.ShopSelectionChanged -= OnPrintifyShopSelectionChanged;
            try
            {
                credentials.Dispose();
            }
            catch (Exception exception)
            {
                shutdownException = exception;
            }
        }

        try
        {
            PrintifyCatalogImportSession?.Dispose();
        }
        catch (Exception exception)
        {
            shutdownException = shutdownException is null
                ? exception
                : new AggregateException(shutdownException, exception);
        }

        try
        {
            _shutdownCts.Cancel();
        }
        catch (Exception exception)
        {
            shutdownException = shutdownException is null
                ? exception
                : new AggregateException(shutdownException, exception);
        }

        if (shutdownException is null)
        {
            cancellationCompleted.TrySetResult();
        }
        else
        {
            cancellationCompleted.TrySetException(shutdownException);
        }

        return new ValueTask(disposalTask);
    }

    private async Task CompleteDisposalAsync(IReadOnlyCollection<Task> operations, Task cancellationCompleted)
    {
        Exception? failure = null;
        try
        {
            try
            {
                await cancellationCompleted.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (operations.Count > 0)
            {
                try
                {
                    await Task.WhenAll(operations).WaitAsync(OperationShutdownTimeout).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                }
                catch (Exception exception) when (failure is not null)
                {
                    failure = new AggregateException(failure, exception);
                }
            }
        }
        finally
        {
            _shutdownCts.Dispose();
        }

        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private void Run(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        TrackedOperation trackedOperation;
        lock (_operationGate)
        {
            if (_isDisposed)
            {
                return;
            }

            trackedOperation = new TrackedOperation(CancellationTokenSource.CreateLinkedTokenSource(
                _shutdownCts.Token,
                cancellationToken));
            _inFlightOperations.Add(trackedOperation);
            _isBusy = true;
        }

        try
        {
            OnPropertyChanged(nameof(IsBusy));
        }
        finally
        {
            _ = RunAndObserveAsync(trackedOperation, operation);
        }
    }

    private async Task RunAndObserveAsync(
        TrackedOperation trackedOperation,
        Func<CancellationToken, Task> operation)
    {
        try
        {
            await operation(trackedOperation.Cancellation.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (trackedOperation.Cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            trackedOperation.Cancellation.Dispose();
            lock (_operationGate)
            {
                _inFlightOperations.Remove(trackedOperation);
                _isBusy = _inFlightOperations.Count > 0;
            }

            try
            {
                OnPropertyChanged(nameof(IsBusy));
            }
            finally
            {
                trackedOperation.Completion.TrySetResult();
            }
        }
    }

    private void RaisePromptProperties()
    {
        OnPropertyChanged(nameof(ShouldShowFirstStorePrompt));
        OnPropertyChanged(nameof(IsSelectorCompact));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
