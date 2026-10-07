using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores;
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
    private ListingLifecycleService? _service;
    private ListingConnectionRequest? _request;
    private ListingProductProjection? _projection;
    private ListingLifecycleConflict? _conflict;
    private Guid _itemId;
    private bool _canEdit;
    private bool _isBusy;
    private string _status = "Not loaded";
    private string _connectionStatus = "Not checked";
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _shippingProfile = "Default";
    private string _priceAmount = "30.00";
    private string? _error;
    private string? _feedback;
    private ListingMapping? _mapping;

    public PrintifyListingStageToolViewModel(
        IListingLifecycleServiceFactory? serviceFactory,
        IListingProjectionSource? projectionSource,
        Func<WorkspaceSnapshot> snapshot,
        Func<Guid, Store?> storeResolver,
        IStoreContextMapper storeContextMapper)
    {
        _serviceFactory = serviceFactory;
        _projectionSource = projectionSource;
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _storeResolver = storeResolver ?? throw new ArgumentNullException(nameof(storeResolver));
        _storeContextMapper = storeContextMapper ?? throw new ArgumentNullException(nameof(storeContextMapper));
        CreateOrUpdateCommand = new RelayCommand(_ => _ = CreateOrUpdateAsync(), () => CanCreateOrUpdate);
        RefreshCommand = new RelayCommand(_ => _ = RefreshAsync(), () => CanRefresh);
        PublishCommand = new RelayCommand(_ => _ = PublishAsync(), () => CanPublish);
        UnpublishCommand = new RelayCommand(_ => _ = UnpublishAsync(), () => CanUnpublish);
        ArchiveCommand = new RelayCommand(_ => _ = ArchiveAsync(), () => CanArchive);
        DeleteCommand = new RelayCommand(_ => ShowDeleteConfirmation(), () => CanDelete);
        ConfirmDeleteCommand = new RelayCommand(_ => _ = DeleteAsync(), () => CanConfirmDelete);
        CancelDeleteCommand = new RelayCommand(_ => HideDeleteConfirmation(), () => IsDeleteConfirmationVisible);
        AcceptRemoteCommand = new RelayCommand(_ => _ = ReconcileAsync(ListingConflictResolution.AcceptRemote), () => CanReconcile);
        KeepLocalCommand = new RelayCommand(_ => _ = ReconcileAsync(ListingConflictResolution.KeepLocal), () => CanReconcile);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CreateOrUpdateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand UnpublishCommand { get; }
    public ICommand ArchiveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand AcceptRemoteCommand { get; }
    public ICommand KeepLocalCommand { get; }
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
    public string DeleteConfirmationText => _mapping?.Identity is { } identity
        ? $"Delete the remote Printify draft {identity.ProductId}? This cannot be undone. Local mapping history will be retained."
        : "Delete the remote Printify draft? This cannot be undone.";
    public string ConflictSummary => _conflict is null
        ? string.Empty
        : string.Join(Environment.NewLine, _conflict.Comparison.Changes.Select(change => $"{change.Field}: {change.Remote ?? "(empty)"}"));
    public string VariantSummary => _projection is null ? "Not available" : $"{_projection.Variants.Count} selected variants";
    public string ArtworkSummary => _projection is null ? "Not available" : $"{_projection.Artwork.Count} design area{(_projection.Artwork.Count == 1 ? string.Empty : "s")}";
    public string ProductSummary => _projection is null ? "Not available" : $"Blueprint {(_projection.ExternalBlueprintId?.ToString(CultureInfo.InvariantCulture) ?? "unresolved")}, provider {(_projection.ExternalProviderId?.ToString(CultureInfo.InvariantCulture) ?? "unresolved")}";
    public bool CanCreateOrUpdate => !IsBusy && _canEdit && _service is not null && _projection is not null && _request is not null && string.IsNullOrWhiteSpace(ErrorMessage) && !HasConflict;
    public bool CanRefresh => !IsBusy && _service is not null && _request is not null && _mapping?.Identity is not null;
    public bool CanPublish => !IsBusy && _request?.RequirePublication == true && _mapping?.Identity is not null && _mapping.PublicationState == ListingPublicationState.Unpublished;
    public bool CanUnpublish => !IsBusy && _request?.RequirePublication == true && _mapping?.Identity is not null && _mapping.PublicationState == ListingPublicationState.Published;
    public bool CanArchive => !IsBusy && _mapping is not null && _service is not null;
    public bool CanDelete => !IsBusy && !IsDeleteConfirmationVisible && _mapping?.Identity is not null && _service is not null;
    public bool CanConfirmDelete => !IsBusy && IsDeleteConfirmationVisible && _mapping?.Identity is not null && _service is not null;
    public bool CanReconcile => !IsBusy && HasConflict && _service is not null && _projection is not null && _request is not null;

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        _itemId = itemId;
        _canEdit = canEdit;
        _projection = null;
        _conflict = null;
        _mapping = null;
        IsDeleteConfirmationVisible = false;
        OnPropertyChanged(nameof(IsDeleteConfirmationVisible));
        ErrorMessage = null;
        FeedbackMessage = null;
        Status = canEdit ? "Checking Printify…" : "Read-only";
        ConnectionStatus = "Checking…";
        var store = _storeResolver(_snapshot().Items.SingleOrDefault(value => value.Id == itemId)?.StoreId ?? Guid.Empty);
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
        _request = new ListingConnectionRequest(store.Id, shopId.ToString(CultureInfo.InvariantCulture), store.FulfillmentStrategy == FulfillmentStrategy.ShopifyPrintify);
        var readiness = await _service.CheckConnectionAsync(_request, cancellationToken).ConfigureAwait(true);
        ConnectionStatus = readiness.ProductOperationsAvailable ? "Connected" : "Unavailable";
        if (!readiness.ProductOperationsAvailable)
        {
            SetBlocked(readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");
            return;
        }

        var projectionResult = await _projectionSource.BuildAsync(_snapshot(), itemId, new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, ParsePrice()), ShippingProfile, null, cancellationToken).ConfigureAwait(true);
        if (!projectionResult.IsValid)
        {
            SetBlocked(string.Join(" ", projectionResult.Issues.Select(value => value.Message)));
            return;
        }
        _projection = projectionResult.Projection;
        Title = _projection!.Title;
        Description = _projection.Description ?? string.Empty;
        var mapping = _snapshot().ExternalListingMappings.SingleOrDefault(value => value.StoreId == store.Id && value.ItemId == itemId);
        if (mapping is not null)
            _mapping = ToApplicationMapping(mapping);
        Status = _mapping?.SynchronizationState.ToString() ?? "Ready to create";
        NotifyActions();
    }

    private async Task CreateOrUpdateAsync()
    {
        if (!CanCreateOrUpdate || _projection is null || _request is null || _service is null) return;
        await RunAsync(async cancellationToken =>
        {
            var result = await _service.CreateOrUpdateAsync(_request, _projection with
            {
                Title = Title,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                ShippingProfile = ShippingProfile,
                OutOfStockPolicy = null,
                Pricing = new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, ParsePrice())
            }, cancellationToken).ConfigureAwait(true);
            ApplyResult(result);
        }).ConfigureAwait(true);
    }

    private Task RefreshAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.RefreshAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task PublishAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.PublishAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task UnpublishAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.UnpublishAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task ArchiveAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.ArchiveLocallyAsync(_request!.StoreId, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task DeleteAsync() => RunAsync(async cancellationToken => ApplyResult(await _service!.DeleteRemoteAsync(_request!, _itemId, cancellationToken).ConfigureAwait(true)));
    private Task ReconcileAsync(ListingConflictResolution resolution) => RunAsync(async cancellationToken => ApplyResult(await _service!.ReconcileAsync(_request!, _projection!, resolution, cancellationToken).ConfigureAwait(true)));

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
        ErrorMessage = result.Kind is ListingLifecycleResultKind.Failed or ListingLifecycleResultKind.Unavailable ? result.Message : null;
        FeedbackMessage = result.Kind is ListingLifecycleResultKind.Succeeded ? result.Message : null;
        OnPropertyChanged(nameof(HasConflict));
        OnPropertyChanged(nameof(ConflictSummary));
        OnPropertyChanged(nameof(DeleteConfirmationText));
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

    private void SetBlocked(string message)
    {
        Status = "Unavailable";
        ErrorMessage = message;
        ConnectionStatus = "Unavailable";
        NotifyActions();
    }

    private decimal ParsePrice() => decimal.TryParse(PriceAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) && price > 0m ? price : 30m;

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(CanCreateOrUpdate));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(CanUnpublish));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanConfirmDelete));
        OnPropertyChanged(nameof(CanReconcile));
        foreach (var command in new[] { CreateOrUpdateCommand, RefreshCommand, PublishCommand, UnpublishCommand, ArchiveCommand, DeleteCommand, ConfirmDeleteCommand, CancelDeleteCommand, AcceptRemoteCommand, KeepLocalCommand })
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
