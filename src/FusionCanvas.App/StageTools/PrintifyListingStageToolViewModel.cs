using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.StageTools;

public sealed class PrintifyListingStageToolViewModel : INotifyPropertyChanged
{
    private readonly IListingLifecycleServiceFactory? _serviceFactory;
    private readonly IListingProjectionSource? _projectionSource;
    private readonly Func<WorkspaceSnapshot> _snapshot;
    private readonly Func<Guid, Store?> _storeResolver;
    private readonly IStoreContextMapper _storeContextMapper;
    private readonly PrintifyListingImportService? _variantSetupImport;
    private ListingLifecycleService? _service;
    private ListingConnectionRequest? _request;
    private ListingProductProjection? _projection;
    private ListingLifecycleConflict? _conflict;
    private Guid _itemId;
    private bool _canEdit;
    private bool _isBusy;
    private bool _isUncertainCreateRetryConfirmationVisible;
    private bool _isLinkConfirmationVisible;
    private string _status = "Not loaded";
    private string _connectionStatus = "Not checked";
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _shippingProfile = "Default";
    private string _priceAmount = "30.00";
    private string? _error;
    private string? _feedback;
    private ListingMapping? _mapping;
    private ListingProductLinkCandidate? _linkCandidate;
    private string _candidateProductId = string.Empty;
    private string? _shopTitle;
    private bool _publicationOperationsAvailable;
    private string? _publicationAvailabilityMessage;
    private bool _isPublishAvailabilityConfirmationVisible;
    private bool _publishAvailabilityAcknowledged;
    private readonly IListingColorSelectionService? _listingColors;
    private bool _isMoreOptionsVisible;
    private string _listingColorSearchText = string.Empty;
    private string _previewColorName = string.Empty;
    private string _previewSwatchHex = "#808080";
    private Bitmap? _previewArtworkBitmap;

    public PrintifyListingStageToolViewModel(
        IListingLifecycleServiceFactory? serviceFactory,
        IListingProjectionSource? projectionSource,
        Func<WorkspaceSnapshot> snapshot,
        Func<Guid, Store?> storeResolver,
        IStoreContextMapper storeContextMapper,
        PrintifyListingImportService? variantSetupImport = null,
        IListingColorSelectionService? listingColors = null)
    {
        _serviceFactory = serviceFactory;
        _projectionSource = projectionSource;
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _storeResolver = storeResolver ?? throw new ArgumentNullException(nameof(storeResolver));
        _storeContextMapper = storeContextMapper ?? throw new ArgumentNullException(nameof(storeContextMapper));
        _variantSetupImport = variantSetupImport;
        _listingColors = listingColors;
        CreateOrUpdateCommand = new RelayCommand(_ => _ = CreateOrUpdateAsync(), () => CanCreateOrUpdate);
        ReconcileUncertainCreateCommand = new RelayCommand(_ => _ = ReconcileUncertainCreateAsync(), () => CanReconcileUncertainCreate);
        ConfirmUncertainCreateRetryCommand = new RelayCommand(_ => _ = ConfirmUncertainCreateRetryAsync(), () => CanConfirmUncertainCreateRetry);
        CancelUncertainCreateRetryCommand = new RelayCommand(_ => CancelUncertainCreateRetry(), () => IsUncertainCreateRetryConfirmationVisible);
        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), () => CanRefresh);
        PublishCommand = new RelayCommand(_ => ShowPublishAvailabilityConfirmation(), () => CanPublish && !IsPublishAvailabilityConfirmationVisible);
        ConfirmPublishAvailabilityCommand = new RelayCommand(_ => _ = ConfirmPublishAvailabilityAsync(), () => CanConfirmPublishAvailability);
        CancelPublishAvailabilityCommand = new RelayCommand(_ => CancelPublishAvailabilityConfirmation(), () => IsPublishAvailabilityConfirmationVisible);
        UnpublishCommand = new RelayCommand(_ => _ = UnpublishAsync(), () => CanUnpublish);
        ArchiveCommand = new RelayCommand(_ => _ = ArchiveAsync(), () => CanArchive);
        DeleteCommand = new RelayCommand(_ => ShowDeleteConfirmation(), () => CanDelete);
        ConfirmDeleteCommand = new RelayCommand(_ => _ = DeleteAsync(), () => CanConfirmDelete);
        CancelDeleteCommand = new RelayCommand(_ => HideDeleteConfirmation(), () => IsDeleteConfirmationVisible);
        VerifyProductLinkCommand = new RelayCommand(_ => _ = VerifyProductLinkAsync(), () => CanVerifyProductLink);
        ConfirmProductLinkCommand = new RelayCommand(_ => _ = ConfirmProductLinkAsync(), () => CanConfirmProductLink);
        CancelProductLinkCommand = new RelayCommand(_ => CancelProductLink(), () => IsLinkConfirmationVisible);
        AcceptRemoteCommand = new RelayCommand(_ => _ = ReconcileAsync(ListingConflictResolution.AcceptRemote), () => CanReconcile);
        KeepLocalCommand = new RelayCommand(_ => _ = ReconcileAsync(ListingConflictResolution.KeepLocal), () => CanReconcile);
        DownloadVariantSetupCommand = new RelayCommand(_ => _ = DownloadVariantSetupAsync(), () => CanDownloadVariantSetup);
        ShowMoreOptionsCommand = new RelayCommand(_ => OpenMoreOptions(), () => ListingOtherColors.Count > 0);
        ApplyMoreOptionsCommand = new RelayCommand(_ => _ = ApplyMoreOptionsAsync(), () => IsMoreOptionsVisible && !IsBusy);
        CancelMoreOptionsCommand = new RelayCommand(_ => CancelMoreOptions(), () => IsMoreOptionsVisible);
        PreviewListingColorCommand = new RelayCommand(parameter => _ = PreviewListingColorAsync(parameter as ListingColorChoiceViewModel));
        ToggleFavoriteListingColorCommand = new RelayCommand(parameter => _ = ToggleFavoriteListingColorAsync(parameter as ListingColorChoiceViewModel), () => !IsBusy && _canEdit);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CreateOrUpdateCommand { get; }
    public ICommand ReconcileUncertainCreateCommand { get; }
    public ICommand ConfirmUncertainCreateRetryCommand { get; }
    public ICommand CancelUncertainCreateRetryCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand ConfirmPublishAvailabilityCommand { get; }
    public ICommand CancelPublishAvailabilityCommand { get; }
    public ICommand UnpublishCommand { get; }
    public ICommand ArchiveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand VerifyProductLinkCommand { get; }
    public ICommand ConfirmProductLinkCommand { get; }
    public ICommand CancelProductLinkCommand { get; }
    public ICommand AcceptRemoteCommand { get; }
    public ICommand KeepLocalCommand { get; }
    public ICommand DownloadVariantSetupCommand { get; }
    public ICommand ShowMoreOptionsCommand { get; }
    public ICommand ApplyMoreOptionsCommand { get; }
    public ICommand CancelMoreOptionsCommand { get; }
    public ICommand PreviewListingColorCommand { get; }
    public ICommand ToggleFavoriteListingColorCommand { get; }
    public ObservableCollection<ListingColorChoiceViewModel> ListingFavoriteColors { get; } = [];
    public ObservableCollection<ListingColorChoiceViewModel> ListingOtherColors { get; } = [];
    public ObservableCollection<UnresolvedListingColorViewModel> UnresolvedListingColors { get; } = [];
    public string Status { get => _status; private set => SetField(ref _status, value); }
    public string ConnectionStatus { get => _connectionStatus; private set => SetField(ref _connectionStatus, value); }
    public string Title { get => _title; private set { if (SetField(ref _title, value)) NotifyActions(); } }
    public string Description { get => _description; private set => SetField(ref _description, value); }
    public string ShippingProfile { get => _shippingProfile; set { if (SetField(ref _shippingProfile, value)) NotifyActions(); } }
    public string OutOfStockPolicy => "Controlled by Printify";
    public string PriceAmount { get => _priceAmount; set { if (SetField(ref _priceAmount, value)) NotifyActions(); } }
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) NotifyActions(); } }
    public string? ErrorMessage { get => _error; private set { if (SetField(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public string? FeedbackMessage { get => _feedback; private set { if (SetField(ref _feedback, value)) OnPropertyChanged(nameof(HasFeedback)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);
    public bool HasConflict => _conflict is not null;
    public bool IsDeleteConfirmationVisible { get; private set; }
    public bool IsUncertainCreateRetryConfirmationVisible { get => _isUncertainCreateRetryConfirmationVisible; private set { if (SetField(ref _isUncertainCreateRetryConfirmationVisible, value)) { OnPropertyChanged(nameof(IsUncertainCreateRetryConfirmationVisible)); NotifyActions(); } } }
    public bool HasUnresolvedOperation => _mapping is { } mapping
        && (mapping.SynchronizationState is ListingSynchronizationState.Pending or ListingSynchronizationState.Uncertain
            || mapping.Operation?.State is ListingOperationState.IntentRecorded or ListingOperationState.NeedsReconciliation);
    public bool HasUnresolvedCreate => HasUnresolvedOperation && _mapping?.Identity is null;
    public string UncertainCreateRetryConfirmationText => "Printify did not return a product ID. Check the selected shop directly before retrying. Confirm only if the product is absent; retrying while it exists could create a duplicate.";
    public bool IsLinkConfirmationVisible { get => _isLinkConfirmationVisible; private set { if (SetField(ref _isLinkConfirmationVisible, value)) OnPropertyChanged(nameof(IsLinkConfirmationVisible)); } }
    public string CurrentProductId => _mapping?.Identity?.ProductId ?? string.Empty;
    public string CurrentProductIdDisplay => string.IsNullOrWhiteSpace(CurrentProductId) ? "Not linked" : CurrentProductId;
    public string CandidateProductId { get => _candidateProductId; set { if (SetField(ref _candidateProductId, value)) NotifyActions(); } }
    public string LinkConfirmationText => _linkCandidate is null
        ? string.Empty
        : $"Link {Title} to Printify product {_linkCandidate.ProductId}{(string.IsNullOrWhiteSpace(_linkCandidate.Title) ? string.Empty : $" — {_linkCandidate.Title}")}{(string.IsNullOrWhiteSpace(_shopTitle) ? string.Empty : $" in {_shopTitle}")}? The remote product will not be changed.";
    public string DeleteConfirmationText => _mapping?.Identity is { } identity
        ? $"Delete the remote Printify draft {identity.ProductId}? This cannot be undone. Local mapping history will be retained."
        : "Delete the remote Printify draft? This cannot be undone.";
    public string ConflictSummary => _conflict is null
        ? string.Empty
        : string.Join(Environment.NewLine, _conflict.Comparison.Changes.Select(change => $"{change.Field}: {change.Remote ?? "(empty)"}"));
    public string VariantSummary => _projection is null ? "Not available" : $"{_projection.Variants.Count} selected variants";
    public string ArtworkSummary => _projection is null ? "Not available" : $"{_projection.Artwork.Count} design area{(_projection.Artwork.Count == 1 ? string.Empty : "s")}";
    public string ProductSummary => _projection is null ? "Not available" : $"Blueprint {(_projection.ExternalBlueprintId?.ToString(CultureInfo.InvariantCulture) ?? "unresolved")}, provider {(_projection.ExternalProviderId?.ToString(CultureInfo.InvariantCulture) ?? "unresolved")}";
    public string? PublicationAvailabilityMessage => _mapping is { Identity: not null, PublicationState: ListingPublicationState.Unpublished or ListingPublicationState.Published }
        ? _publicationAvailabilityMessage
        : null;
    public bool HasPublicationAvailabilityMessage => !string.IsNullOrWhiteSpace(PublicationAvailabilityMessage);
    public bool IsMoreOptionsVisible { get => _isMoreOptionsVisible; private set { if (SetField(ref _isMoreOptionsVisible, value)) NotifyActions(); } }
    public string ListingColorSearchText
    {
        get => _listingColorSearchText;
        set { if (SetField(ref _listingColorSearchText, value ?? string.Empty)) OnPropertyChanged(nameof(FilteredListingOtherColors)); }
    }
    public IReadOnlyList<ListingColorChoiceViewModel> FilteredListingOtherColors => ListingOtherColors
        .Where(value => string.IsNullOrWhiteSpace(ListingColorSearchText)
            || value.Name.Contains(ListingColorSearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
    public string PreviewColorName { get => _previewColorName; private set => SetField(ref _previewColorName, value); }
    public string PreviewSwatchHex { get => _previewSwatchHex; private set => SetField(ref _previewSwatchHex, value); }
    public Bitmap? PreviewArtworkBitmap
    {
        get => _previewArtworkBitmap;
        private set
        {
            if (ReferenceEquals(_previewArtworkBitmap, value)) return;
            _previewArtworkBitmap?.Dispose();
            _previewArtworkBitmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPreviewArtwork));
        }
    }
    public bool HasPreviewArtwork => PreviewArtworkBitmap is not null;
    public bool HasListingColors => ListingFavoriteColors.Count > 0 || ListingOtherColors.Count > 0 || UnresolvedListingColors.Count > 0;
    public bool CanShowMoreOptions => ListingOtherColors.Count > 0;
    public string PreviewColorSummary => string.IsNullOrWhiteSpace(PreviewColorName) ? "Choose a Color to preview its flat swatch." : $"Viewing {PreviewColorName}";
    public string PreviewSelectionNote => string.IsNullOrWhiteSpace(PreviewColorName)
        ? "Preview changes do not add a Color."
        : $"Previewing {PreviewColorName}. Preview changes do not add a Color.";
    public bool IsPublishAvailabilityConfirmationVisible
    {
        get => _isPublishAvailabilityConfirmationVisible;
        private set { if (SetField(ref _isPublishAvailabilityConfirmationVisible, value)) NotifyActions(); }
    }
    public bool PublishAvailabilityAcknowledged
    {
        get => _publishAvailabilityAcknowledged;
        set { if (SetField(ref _publishAvailabilityAcknowledged, value)) { OnPropertyChanged(nameof(CanConfirmPublishAvailability)); NotifyActions(); } }
    }
    public string PublishAvailabilityReminder => "Check the product's availability setting in Printify and make sure it remains visible in your store when out of stock. FusionCanvas cannot read or change this setting.";
    public bool CanConfirmPublishAvailability => IsPublishAvailabilityConfirmationVisible && PublishAvailabilityAcknowledged && CanPublish;
    public bool CanCreateOrUpdate => !IsBusy && !HasUnresolvedOperation && _canEdit && _service is not null && _projection is not null && _request is not null && string.IsNullOrWhiteSpace(ErrorMessage) && !HasConflict;
    public bool CanReconcileUncertainCreate => !IsBusy && _canEdit && HasUnresolvedCreate && _service is not null && _projection is not null && _request is not null;
    public bool CanConfirmUncertainCreateRetry => !IsBusy && IsUncertainCreateRetryConfirmationVisible && _service is not null && _projection is not null && _request is not null;
    public bool CanRefresh => !IsBusy && _service is not null && _request is not null && _mapping?.Identity is not null;
    public bool CanPublish => !IsBusy && _publicationOperationsAvailable && _request?.RequirePublication == true && _mapping?.Identity is not null && _mapping.PublicationState == ListingPublicationState.Unpublished;
    public bool CanUnpublish => !IsBusy && _publicationOperationsAvailable && _request?.RequirePublication == true && _mapping?.Identity is not null && _mapping.PublicationState == ListingPublicationState.Published;
    public bool CanArchive => !IsBusy && _mapping is not null && _service is not null;
    public bool CanDelete => !IsBusy && !IsDeleteConfirmationVisible && _mapping?.Identity is not null && _service is not null;
    public bool CanConfirmDelete => !IsBusy && IsDeleteConfirmationVisible && _mapping?.Identity is not null && _service is not null;
    public bool CanEditCandidateProductId => !IsBusy && _canEdit && _service is not null && _request is not null;
    public bool CanVerifyProductLink => !IsBusy && _canEdit && _service is not null && _request is not null && !string.IsNullOrWhiteSpace(CandidateProductId);
    public bool CanConfirmProductLink => !IsBusy && _canEdit && IsLinkConfirmationVisible && _linkCandidate is not null && _service is not null && _request is not null;
    public bool CanReconcile => !IsBusy && HasConflict && _service is not null && _projection is not null && _request is not null;
    public bool CanDownloadVariantSetup => !IsBusy && _variantSetupImport is not null && _mapping?.Identity is not null;

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        CancelPublishAvailabilityConfirmation();
        PreviewArtworkBitmap = null;
        PreviewColorName = string.Empty;
        PreviewSwatchHex = "#808080";
        _itemId = itemId;
        _canEdit = canEdit;
        _projection = null;
        _conflict = null;
        _mapping = null;
        _publicationOperationsAvailable = false;
        _publicationAvailabilityMessage = null;
        ListingFavoriteColors.Clear();
        ListingOtherColors.Clear();
        UnresolvedListingColors.Clear();
        IsMoreOptionsVisible = false;
        IsUncertainCreateRetryConfirmationVisible = false;
        _linkCandidate = null;
        CandidateProductId = string.Empty;
        IsLinkConfirmationVisible = false;
        OnPropertyChanged(nameof(CurrentProductId));
        OnPropertyChanged(nameof(CurrentProductIdDisplay));
        IsDeleteConfirmationVisible = false;
        OnPropertyChanged(nameof(IsDeleteConfirmationVisible));
        ErrorMessage = null;
        FeedbackMessage = null;
        Status = canEdit ? "Checking Printify…" : "Read-only";
        ConnectionStatus = "Checking…";
        var initialSnapshot = _snapshot();
        var selectedItem = initialSnapshot.Items.SingleOrDefault(value => value.Id == itemId);
        var store = _storeResolver(selectedItem?.StoreId ?? Guid.Empty);
        var savedMapping = initialSnapshot.ExternalListingMappings.SingleOrDefault(value => value.ItemId == itemId && value.StoreId == selectedItem?.StoreId);
        if (savedMapping is not null)
        {
            _mapping = ToApplicationMapping(savedMapping);
            OnPropertyChanged(nameof(HasUnresolvedOperation));
            OnPropertyChanged(nameof(HasUnresolvedCreate));
            NotifyActions();
        }
        if (store is null || store.IsArchived)
        {
            SetBlocked("The selected Store is unavailable.");
            return;
        }
        if (store.FulfillmentStrategy is not (FulfillmentStrategy.Printify or FulfillmentStrategy.ShopifyPrintify))
        {
            SetBlocked("Select Printify or Shopify + Printify as the Store fulfillment strategy first.");
            return;
        }
        var context = _storeContextMapper.Read(store);
        _shopTitle = context.PrintifyShopTitle;
        if (context.PrintifyShopId is not int shopId)
        {
            SetBlocked("Select and save a Printify shop in Store settings first.");
            return;
        }
        if (_serviceFactory is null || _projectionSource is null)
        {
            SetBlocked("Printify listing is unavailable in this runtime.");
            return;
        }

        _service = _serviceFactory.Create(store);
        _request = new ListingConnectionRequest(
            store.Id,
            shopId.ToString(CultureInfo.InvariantCulture),
            RequirePublication: true,
            RequireShopifyChannel: store.FulfillmentStrategy == FulfillmentStrategy.ShopifyPrintify);
        var readiness = await _service.CheckConnectionAsync(_request, cancellationToken).ConfigureAwait(true);
        ConnectionStatus = readiness.ProductOperationsAvailable ? "Connected" : "Unavailable";
        _publicationOperationsAvailable = readiness.PublicationOperationsAvailable;
        _publicationAvailabilityMessage = readiness.PublicationOperationsAvailable
            ? null
            : readiness.Issues.FirstOrDefault()?.Message ?? "The selected Printify shop cannot publish to its sales channel.";
        OnPropertyChanged(nameof(PublicationAvailabilityMessage));
        OnPropertyChanged(nameof(HasPublicationAvailabilityMessage));
        if (!readiness.ProductOperationsAvailable)
        {
            SetBlocked(readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");
            return;
        }

        await LoadListingColorsAsync(itemId, cancellationToken).ConfigureAwait(true);
        var projectionResult = await _projectionSource.BuildAsync(_snapshot(), itemId, new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, ParsePrice()), ShippingProfile, null, cancellationToken).ConfigureAwait(true);
        if (!projectionResult.IsValid)
        {
            SetBlocked(string.Join(" ", projectionResult.Issues.Select(value => value.Message)), connectionUnavailable: false);
            return;
        }
        _projection = projectionResult.Projection;
        OnPropertyChanged(nameof(VariantSummary));
        OnPropertyChanged(nameof(ArtworkSummary));
        OnPropertyChanged(nameof(ProductSummary));
        Title = _projection!.Title;
        Description = _projection.Description ?? string.Empty;
        var mapping = _snapshot().ExternalListingMappings.SingleOrDefault(value => value.StoreId == store.Id && value.ItemId == itemId);
        if (mapping is not null)
            _mapping = ToApplicationMapping(mapping);
        OnPropertyChanged(nameof(CurrentProductId));
        OnPropertyChanged(nameof(CurrentProductIdDisplay));
        Status = _mapping?.SynchronizationState.ToString() ?? "Ready to create";
        if (HasUnresolvedOperation)
            ErrorMessage = _mapping?.Identity is null
                ? "A previous Printify create is unresolved. Check the shop before retrying."
                : "A previous Printify operation is unresolved. Refresh the listing before creating or updating.";
        NotifyActions();
    }

    private async Task CreateOrUpdateAsync()
    {
        if (!CanCreateOrUpdate || _projection is null || _request is null || _service is null) return;
        await RunAsync(async cancellationToken =>
        {
            var result = await _service.CreateOrUpdateAsync(_request, CurrentProjection(), cancellationToken).ConfigureAwait(true);
            ApplyResult(result);
        }).ConfigureAwait(true);
    }

    private Task ReconcileUncertainCreateAsync() => RunAsync(async cancellationToken =>
    {
        if (_service is null || _request is null || _projection is null) return;
        IsUncertainCreateRetryConfirmationVisible = false;
        var result = await _service.ReconcileUncertainCreateAsync(_request, _itemId, CurrentProjection(), cancellationToken).ConfigureAwait(true);
        ApplyResult(result);
        if (result.Kind == ListingLifecycleResultKind.NoRemoteMatch)
            IsUncertainCreateRetryConfirmationVisible = true;
    });

    private Task ConfirmUncertainCreateRetryAsync() => RunAsync(async cancellationToken =>
    {
        if (_service is null || _request is null || _projection is null) return;
        IsUncertainCreateRetryConfirmationVisible = false;
        var result = await _service.RetryUncertainCreateAsync(_request, _itemId, CurrentProjection(), cancellationToken).ConfigureAwait(true);
        ApplyResult(result);
        if (result.Kind == ListingLifecycleResultKind.NoRemoteMatch)
            IsUncertainCreateRetryConfirmationVisible = true;
    });

    private ListingProductProjection CurrentProjection() => _projection! with
    {
        Title = Title,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
        ShippingProfile = ShippingProfile,
        OutOfStockPolicy = null,
        Pricing = new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, ParsePrice())
    };

    private void CancelUncertainCreateRetry() => IsUncertainCreateRetryConfirmationVisible = false;

    private Task RefreshAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.RefreshAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task PublishAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.PublishAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));

    private async Task LoadListingColorsAsync(Guid itemId, CancellationToken cancellationToken)
    {
        if (_listingColors is null) return;
        var state = await _listingColors.LoadAsync(itemId, cancellationToken).ConfigureAwait(true);
        ListingFavoriteColors.Clear();
        ListingOtherColors.Clear();
        foreach (var choice in state.Favorites)
        {
            var viewModel = new ListingColorChoiceViewModel(choice, state.ArtworkVariants);
            viewModel.ArtworkVariantChanged += (_, variant) => _ = SaveArtworkVariantAsync(viewModel, variant);
            ListingFavoriteColors.Add(viewModel);
        }
        foreach (var choice in state.OtherColors)
        {
            var viewModel = new ListingColorChoiceViewModel(choice, state.ArtworkVariants);
            viewModel.ArtworkVariantChanged += (_, variant) => _ = SaveArtworkVariantAsync(viewModel, variant);
            ListingOtherColors.Add(viewModel);
        }
        var candidates = ListingFavoriteColors.Concat(ListingOtherColors).Where(value => value.CanSelect).ToArray();
        foreach (var choice in state.UnresolvedColors)
        {
            var viewModel = new UnresolvedListingColorViewModel(choice, candidates);
            viewModel.CandidateSelected += ResolveUnresolvedListingColor;
            UnresolvedListingColors.Add(viewModel);
        }
        if (string.IsNullOrWhiteSpace(PreviewColorName))
        {
            var first = ListingFavoriteColors.FirstOrDefault() ?? ListingOtherColors.FirstOrDefault();
            if (first is not null) await PreviewListingColorAsync(first).ConfigureAwait(true);
        }
        OnPropertyChanged(nameof(HasListingColors));
        OnPropertyChanged(nameof(CanShowMoreOptions));
        OnPropertyChanged(nameof(FilteredListingOtherColors));
        NotifyActions();
    }

    private void OpenMoreOptions()
    {
        foreach (var color in ListingOtherColors) color.IsDraftSelected = color.IsSelected;
        IsMoreOptionsVisible = true;
        ListingColorSearchText = string.Empty;
    }

    private void CancelMoreOptions()
    {
        foreach (var color in ListingOtherColors) color.IsDraftSelected = color.IsSelected;
        IsMoreOptionsVisible = false;
    }

    private async Task ApplyMoreOptionsAsync()
    {
        if (_listingColors is null || !IsMoreOptionsVisible || IsBusy) return;
        var selected = ListingFavoriteColors.Where(value => value.IsSelected).Select(value => value.OptionValueId)
            .Concat(ListingOtherColors.Where(value => value.IsDraftSelected).Select(value => value.OptionValueId))
            .Distinct().ToArray();
        IsBusy = true;
        try
        {
            await _listingColors.SetSelectedColorsAsync(_itemId, selected).ConfigureAwait(true);
            IsMoreOptionsVisible = false;
            await LoadListingColorsAsync(_itemId, CancellationToken.None).ConfigureAwait(true);
            await RebuildListingProjectionAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = exception.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task ToggleFavoriteListingColorAsync(ListingColorChoiceViewModel? color)
    {
        if (color is null || _listingColors is null || IsBusy) return;
        var selected = ListingFavoriteColors.Where(value => value.OptionValueId != color.OptionValueId && value.IsSelected)
            .Concat(ListingOtherColors.Where(value => value.IsSelected))
            .Select(value => value.OptionValueId).ToList();
        if (!color.IsSelected) selected.Add(color.OptionValueId);
        IsBusy = true;
        try
        {
            await _listingColors.SetSelectedColorsAsync(_itemId, selected).ConfigureAwait(true);
            await LoadListingColorsAsync(_itemId, CancellationToken.None).ConfigureAwait(true);
            await RebuildListingProjectionAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task SaveArtworkVariantAsync(ListingColorChoiceViewModel color, ListingArtworkVariantChoice? variant)
    {
        if (_listingColors is null || variant is null || !color.IsSelected) return;
        IsBusy = true;
        try
        {
            await _listingColors.SetArtworkVariantAsync(_itemId, color.OptionValueId, variant.Id).ConfigureAwait(true);
            await RebuildListingProjectionAsync().ConfigureAwait(true);
            if (ReferenceEquals(color, ListingFavoriteColors.FirstOrDefault(value => value.OptionValueId == color.OptionValueId))
                || ReferenceEquals(color, ListingOtherColors.FirstOrDefault(value => value.OptionValueId == color.OptionValueId)))
                await LoadListingColorsAsync(_itemId, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async void ResolveUnresolvedListingColor(UnresolvedListingColorViewModel unresolved, ListingColorChoiceViewModel candidate)
    {
        if (_listingColors is null || IsBusy) return;
        IsBusy = true;
        try
        {
            await _listingColors.ResolveUnresolvedColorAsync(_itemId, unresolved.SortOrder, candidate.OptionValueId).ConfigureAwait(true);
            await LoadListingColorsAsync(_itemId, CancellationToken.None).ConfigureAwait(true);
            await RebuildListingProjectionAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task PreviewListingColorAsync(ListingColorChoiceViewModel? color)
    {
        if (color is null) return;
        PreviewColorName = color.Name;
        PreviewSwatchHex = color.SwatchHex;
        if (_listingColors is not null)
        {
            try
            {
                await using var stream = await _listingColors.OpenArtworkPreviewAsync(_itemId, color.OptionValueId).ConfigureAwait(true);
                PreviewArtworkBitmap = stream is null ? null : new Bitmap(stream);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                PreviewArtworkBitmap = null;
                ErrorMessage = $"Artwork preview could not be loaded. {exception.Message}";
            }
        }
        OnPropertyChanged(nameof(PreviewSwatchBrush));
        OnPropertyChanged(nameof(PreviewColorSummary));
        OnPropertyChanged(nameof(PreviewSelectionNote));
    }

    public Avalonia.Media.IBrush PreviewSwatchBrush
    {
        get
        {
            try { return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(PreviewSwatchHex)); }
            catch (FormatException) { return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#808080")); }
        }
    }

    private async Task RebuildListingProjectionAsync()
    {
        if (_projectionSource is null) return;
        var result = await _projectionSource.BuildAsync(_snapshot(), _itemId,
            new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, ParsePrice()), ShippingProfile, null,
            CancellationToken.None).ConfigureAwait(true);
        if (!result.IsValid)
        {
            _projection = null;
            ErrorMessage = string.Join(" ", result.Issues.Select(value => value.Message));
            OnPropertyChanged(nameof(VariantSummary));
            OnPropertyChanged(nameof(ArtworkSummary));
            OnPropertyChanged(nameof(ProductSummary));
            NotifyActions();
            return;
        }

        _projection = result.Projection;
        ErrorMessage = null;
        Title = _projection!.Title;
        Description = _projection.Description ?? string.Empty;
        OnPropertyChanged(nameof(VariantSummary));
        OnPropertyChanged(nameof(ArtworkSummary));
        OnPropertyChanged(nameof(ProductSummary));
        NotifyActions();
    }

    private void ShowPublishAvailabilityConfirmation()
    {
        if (!CanPublish) return;
        PublishAvailabilityAcknowledged = false;
        IsPublishAvailabilityConfirmationVisible = true;
    }

    private async Task ConfirmPublishAvailabilityAsync()
    {
        if (!CanConfirmPublishAvailability) return;
        IsPublishAvailabilityConfirmationVisible = false;
        await PublishAsync().ConfigureAwait(true);
        PublishAvailabilityAcknowledged = false;
    }

    private void CancelPublishAvailabilityConfirmation()
    {
        IsPublishAvailabilityConfirmationVisible = false;
        PublishAvailabilityAcknowledged = false;
    }
    private Task UnpublishAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.UnpublishAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task ArchiveAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.ArchiveLocallyAsync(_request!.StoreId, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task DeleteAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.DeleteRemoteAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task ReconcileAsync(ListingConflictResolution resolution) => RunAsync(async cancellationToken => ApplyResult(await _service!.ReconcileAsync(_request!, _projection!, resolution, cancellationToken).ConfigureAwait(true)));

    private Task VerifyProductLinkAsync() => RunAsync(async cancellationToken =>
    {
        IsLinkConfirmationVisible = false;
        _linkCandidate = null;
        OnPropertyChanged(nameof(LinkConfirmationText));
        var result = await _service!.VerifyProductLinkAsync(_request!, _itemId, CandidateProductId, cancellationToken).ConfigureAwait(true);
        if (!result.Succeeded || result.Candidate is null)
        {
            ErrorMessage = result.Message ?? "Printify could not verify this product.";
            return;
        }

        _linkCandidate = result.Candidate;
        IsLinkConfirmationVisible = true;
        OnPropertyChanged(nameof(LinkConfirmationText));
        NotifyActions();
    });

    private Task ConfirmProductLinkAsync() => RunAsync(async cancellationToken =>
    {
        if (_linkCandidate is null) return;
        ApplyResult(await _service!.LinkExistingProductAsync(_request!, _itemId, _linkCandidate, cancellationToken).ConfigureAwait(true));
        CandidateProductId = string.Empty;
        _linkCandidate = null;
        IsLinkConfirmationVisible = false;
        OnPropertyChanged(nameof(CurrentProductId));
        OnPropertyChanged(nameof(CurrentProductIdDisplay));
        OnPropertyChanged(nameof(LinkConfirmationText));
        NotifyActions();
    });

    private void CancelProductLink()
    {
        CandidateProductId = string.Empty;
        _linkCandidate = null;
        IsLinkConfirmationVisible = false;
        ErrorMessage = null;
        OnPropertyChanged(nameof(LinkConfirmationText));
        NotifyActions();
    }

    private Task DownloadVariantSetupAsync() => RunAsync(async cancellationToken =>
    {
        var storeId = _snapshot().Items.SingleOrDefault(value => value.Id == _itemId)?.StoreId ?? Guid.Empty;
        var store = _storeResolver(storeId);
        if (store is null) throw new InvalidOperationException("The selected Store is unavailable.");
        var result = await _variantSetupImport!.DownloadVariantSetupAsync(new StoreCredentialScope(store.WorkspaceId, store.Id), _itemId, cancellationToken).ConfigureAwait(true);
        if (!result.Succeeded) ErrorMessage = result.Message;
        else FeedbackMessage = result.Message;
        OnPropertyChanged(nameof(CanDownloadVariantSetup));
        VariantSetupCompleted?.Invoke(this, EventArgs.Empty);
    });

    public event EventHandler? VariantSetupCompleted;

    private async Task RunAsync(Func<CancellationToken, Task> operation)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        FeedbackMessage = null;
        try { await operation(CancellationToken.None).ConfigureAwait(true); }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private void ApplyResult(ListingLifecycleResult result)
    {
        IsDeleteConfirmationVisible = false;
        _mapping = result.Mapping;
        _conflict = result.Conflict;
        Status = result.Kind.ToString();
        ErrorMessage = result.Kind is ListingLifecycleResultKind.Failed or ListingLifecycleResultKind.Unavailable or ListingLifecycleResultKind.Uncertain or ListingLifecycleResultKind.NoRemoteMatch or ListingLifecycleResultKind.NotAllowed ? result.Message : null;
        FeedbackMessage = result.Kind is ListingLifecycleResultKind.Succeeded ? result.Message : null;
        OnPropertyChanged(nameof(HasConflict));
        OnPropertyChanged(nameof(HasUnresolvedOperation));
        OnPropertyChanged(nameof(HasUnresolvedCreate));
        OnPropertyChanged(nameof(ConflictSummary));
        OnPropertyChanged(nameof(DeleteConfirmationText));
        OnPropertyChanged(nameof(CurrentProductId));
        OnPropertyChanged(nameof(CurrentProductIdDisplay));
        NotifyActions();
    }

    private void ShowDeleteConfirmation()
    {
        if (!CanDelete) return;
        IsDeleteConfirmationVisible = true;
        OnPropertyChanged(nameof(IsDeleteConfirmationVisible));
        OnPropertyChanged(nameof(DeleteConfirmationText));
        NotifyActions();
    }

    private void HideDeleteConfirmation()
    {
        if (!IsDeleteConfirmationVisible) return;
        IsDeleteConfirmationVisible = false;
        OnPropertyChanged(nameof(IsDeleteConfirmationVisible));
        NotifyActions();
    }

    private void SetBlocked(string message, bool connectionUnavailable = true)
    {
        Status = "Unavailable";
        ErrorMessage = message;
        if (connectionUnavailable)
            ConnectionStatus = "Unavailable";
        NotifyActions();
    }

    private decimal ParsePrice() => decimal.TryParse(PriceAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) && price > 0m ? price : 30m;

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(CanCreateOrUpdate));
        OnPropertyChanged(nameof(CanReconcileUncertainCreate));
        OnPropertyChanged(nameof(CanConfirmUncertainCreateRetry));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(CanConfirmPublishAvailability));
        OnPropertyChanged(nameof(CanUnpublish));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanConfirmDelete));
        OnPropertyChanged(nameof(CanEditCandidateProductId));
        OnPropertyChanged(nameof(CanVerifyProductLink));
        OnPropertyChanged(nameof(CanConfirmProductLink));
        OnPropertyChanged(nameof(CanReconcile));
        OnPropertyChanged(nameof(CanDownloadVariantSetup));
        OnPropertyChanged(nameof(PublicationAvailabilityMessage));
        OnPropertyChanged(nameof(HasPublicationAvailabilityMessage));
        foreach (var command in new[] { CreateOrUpdateCommand, ReconcileUncertainCreateCommand, ConfirmUncertainCreateRetryCommand, CancelUncertainCreateRetryCommand, RefreshCommand, PublishCommand, ConfirmPublishAvailabilityCommand, CancelPublishAvailabilityCommand, UnpublishCommand, ArchiveCommand, DeleteCommand, ConfirmDeleteCommand, CancelDeleteCommand, VerifyProductLinkCommand, ConfirmProductLinkCommand, CancelProductLinkCommand, AcceptRemoteCommand, KeepLocalCommand, ShowMoreOptionsCommand, ApplyMoreOptionsCommand, CancelMoreOptionsCommand, PreviewListingColorCommand, ToggleFavoriteListingColorCommand })
            (command as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private static ListingMapping ToApplicationMapping(Domain.Products.ExternalListingMapping mapping) =>
        new(mapping.StoreId, mapping.ItemId, mapping.ProductId is null ? null : new ListingExternalIdentity(mapping.ShopId, mapping.ProductId, mapping.ExternalPublicationId, mapping.ExternalHandle), (ListingSynchronizationState)mapping.SynchronizationState, (ListingPublicationState)mapping.PublicationState, null);

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

