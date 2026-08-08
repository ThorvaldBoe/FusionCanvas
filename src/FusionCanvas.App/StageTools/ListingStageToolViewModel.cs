using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Listings;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Listings;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.StageTools;

public sealed class ListingStageToolViewModel : INotifyPropertyChanged
{
    private readonly IListingPreparationService? _service;
    private Guid _itemId;
    private string _statusSummary = string.Empty;
    private string _readOnlyReason = string.Empty;
    private string _price = string.Empty;
    private string _currency = "USD";
    private string _externalId = string.Empty;
    private string _channel = "Online Store";
    private int _strategyIndex;
    private int _readinessIndex;
    private ListingPublicationState _publicationState;
    private bool _isReadOnly;
    private bool _isBusy;
    private bool _isPrintifyLocked;
    private string? _errorMessage;
    private string _providerSummary = "No external marketplace is connected.";

    public ListingStageToolViewModel(IListingPreparationService? service = null)
    {
        _service = service;
        SaveCommand = new RelayCommand(_ => Run(SaveAsync()), () => CanSave);
        BindShopifyCommand = new RelayCommand(_ => Run(BindShopifyAsync()), () => CanBindShopify);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveCommand { get; }
    public ICommand BindShopifyCommand { get; }

    public string StatusSummary
    {
        get => _statusSummary;
        private set => SetField(ref _statusSummary, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set
        {
            if (SetField(ref _isReadOnly, value))
            {
                RaiseCommandState();
            }
        }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        private set => SetField(ref _readOnlyReason, value);
    }

    public string Price
    {
        get => _price;
        set
        {
            if (SetField(ref _price, value)) RaiseCommandState();
        }
    }

    public string Currency
    {
        get => _currency;
        set
        {
            if (SetField(ref _currency, value)) RaiseCommandState();
        }
    }

    public string ExternalId
    {
        get => _externalId;
        set
        {
            if (SetField(ref _externalId, value)) RaiseCommandState();
        }
    }

    public string Channel
    {
        get => _channel;
        set
        {
            if (SetField(ref _channel, value)) RaiseCommandState();
        }
    }

    public int StrategyIndex
    {
        get => _strategyIndex;
        set
        {
            if (SetField(ref _strategyIndex, Math.Clamp(value, 0, 2)))
            {
                OnPropertyChanged(nameof(StrategyLabel));
                OnPropertyChanged(nameof(IsShopifyStrategy));
                OnPropertyChanged(nameof(RequiresShopifyBinding));
                RaiseCommandState();
            }
        }
    }

    public int ReadinessIndex
    {
        get => _readinessIndex;
        set
        {
            if (SetField(ref _readinessIndex, Math.Clamp(value, 0, 1)))
            {
                OnPropertyChanged(nameof(ReadinessLabel));
                RaiseCommandState();
            }
        }
    }

    public string StrategyLabel => StrategyIndex switch
    {
        1 => "Shopify + manual fulfillment",
        2 => "Shopify + Printify fulfillment",
        _ => "Manual fulfillment"
    };

    public string ReadinessLabel => ReadinessIndex == 1 ? "Ready for manual marketplace work" : "Draft preparation";

    public bool IsShopifyStrategy => StrategyIndex > 0;

    public bool RequiresShopifyBinding => IsShopifyStrategy && string.IsNullOrWhiteSpace(ExternalId);

    public bool CanSave => !IsReadOnly && !IsBusy && !IsPrintifyLocked && _service is not null;

    public bool CanBindShopify => StrategyIndex == 1 && !IsReadOnly && !IsBusy && !string.IsNullOrWhiteSpace(ExternalId) && !string.IsNullOrWhiteSpace(Channel) && _service is not null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanBindShopify));
                RaiseCommandState();
            }
        }
    }

    public bool IsPrintifyLocked
    {
        get => _isPrintifyLocked;
        private set
        {
            if (SetField(ref _isPrintifyLocked, value))
            {
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(PrintifyLockGuidance));
            }
        }
    }

    public string PrintifyLockGuidance => IsPrintifyLocked
        ? "Printify preparation is locked after publication. Unlocking requires a future connector confirmation."
        : string.Empty;

    public string ProviderSummary
    {
        get => _providerSummary;
        private set => SetField(ref _providerSummary, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void Load(ItemStatus status, bool canEdit)
    {
        StatusSummary = $"This item is currently {ItemStatuses.GetDisplayName(status)}. Listing preparation keeps local marketplace data ready for manual work.";
        IsReadOnly = !canEdit || status is ItemStatus.Published or ItemStatus.Rejected;
        ReadOnlyReason = IsReadOnly ? "Listing preparation is read-only while the item is protected or inactive." : string.Empty;
    }

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        _itemId = itemId;
        ErrorMessage = null;
        if (_service is null)
        {
            Load(ItemStatus.Draft, canEdit);
            return;
        }

        IsBusy = true;
        try
        {
            var state = await _service.LoadAsync(itemId, cancellationToken).ConfigureAwait(true);
            if (state is null)
            {
                ErrorMessage = "The selected Item could not be loaded.";
                Load(ItemStatus.Draft, false);
                return;
            }

            Load(state.Item.Status, canEdit && state.CanEdit);
            if (!state.CanEdit)
            {
                ReadOnlyReason = state.ReadOnlyReason;
            }

            StrategyIndex = (int)state.Profile.Strategy;
            ReadinessIndex = (int)state.Profile.Readiness;
            _publicationState = state.Profile.Publication;
            Price = state.Profile.Price?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            Currency = state.Profile.Currency ?? "USD";
            IsPrintifyLocked = state.PrintifyLocked;
            var shopify = state.Providers.FirstOrDefault(provider => string.Equals(provider.Provider, "Shopify", StringComparison.OrdinalIgnoreCase));
            ExternalId = shopify?.ExternalId ?? string.Empty;
            Channel = shopify?.Channel ?? "Online Store";
            ProviderSummary = shopify is null
                ? "No external marketplace is connected. Manual preparation remains available."
                : $"Shopify item ID: {shopify.ExternalId} ({shopify.Channel})";
            StatusSummary = $"{state.Item.Name}: {ReadinessLabel}. Strategy: {StrategyLabel}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ItemStageSavePayload ToStagePayload() =>
        new(WorkflowStage.Listing, Idea: null, ConceptIdea: null, Phrase: null, GraphicDirection: null, Sll: null);

    private async Task SaveAsync()
    {
        if (!CanSave || _service is null) return;
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(Price) && !decimal.TryParse(Price, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                ErrorMessage = "Price must be a valid number.";
                return;
            }

            var parsedPrice = string.IsNullOrWhiteSpace(Price)
                ? (decimal?)null
                : decimal.Parse(Price, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);

            var result = await _service.UpdateAsync(new(
                _itemId,
                (ListingFulfillmentStrategy)StrategyIndex,
                parsedPrice,
                Currency,
                (ListingReadinessState)ReadinessIndex,
                _publicationState), TestCancellation()).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded && result.State is not null)
            {
                StatusSummary = $"{result.State.Item.Name}: {ReadinessLabel}. Strategy: {StrategyLabel}.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BindShopifyAsync()
    {
        if (!CanBindShopify || _service is null) return;
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _service.BindShopifyAsync(new(_itemId, ExternalId, Channel, FromPrintifyPublication: false), TestCancellation()).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, TestCancellation()).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static CancellationToken TestCancellation() => CancellationToken.None;

    private void RaiseCommandState()
    {
        (SaveCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (BindShopifyCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private void Run(Task task) => _ = task;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
