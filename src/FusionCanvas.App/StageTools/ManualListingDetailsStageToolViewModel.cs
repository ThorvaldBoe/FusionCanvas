using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.Application.Listings;
using FusionCanvas.Domain.Products;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.StageTools;

public sealed class ManualListingDetailsStageToolViewModel : INotifyPropertyChanged
{
    private readonly ManualListingDetailsService? _service;
    private string _title = string.Empty, _description = string.Empty, _currency = "USD", _shippingName = string.Empty,
        _customerShippingCharge = string.Empty, _sellerShippingCost = string.Empty, _deliveryEstimate = string.Empty;
    private string _status = "Not loaded";
    private string? _error;
    private string _historySummary = string.Empty;
    private bool _isBusy, _canEdit, _hasOffering;
    private bool _isLoading, _isDirty;
    private Guid _itemId;
    private readonly Dictionary<Guid, ManualListingDraft> _drafts = [];

    public ManualListingDetailsStageToolViewModel(ManualListingDetailsService? service)
    {
        _service = service;
        SaveCommand = new RelayCommand(_ => _ = SaveAsync(), () => CanSave);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public RelayCommand SaveCommand { get; }
    public ObservableCollection<VariantListingTermsViewModel> Variants { get; } = [];
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Description { get => _description; set => Set(ref _description, value); }
    public string Currency { get => _currency; set => Set(ref _currency, value); }
    public string ShippingName { get => _shippingName; set => Set(ref _shippingName, value); }
    public string CustomerShippingCharge { get => _customerShippingCharge; set => Set(ref _customerShippingCharge, value); }
    public string SellerShippingCost { get => _sellerShippingCost; set => Set(ref _sellerShippingCost, value); }
    public string DeliveryEstimate { get => _deliveryEstimate; set => Set(ref _deliveryEstimate, value); }
    public string Status { get => _status; private set { if (_status == value) return; _status = value; Changed(nameof(Status)); } }
    public string? Error { get => _error; private set { if (Set(ref _error, value)) Changed(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value, trackDirty: false); }
    public bool CanSave => !IsBusy && _canEdit && _service is not null;
    public bool CanEditFulfillmentTerms => _canEdit && _hasOffering;
    public string HistorySummary { get => _historySummary; private set => Set(ref _historySummary, value); }
    public string OfferingSummary { get; private set; } = "Select an Offering in Design to add fulfillment and shipping details.";
    public string VariantPricingGuidance => string.IsNullOrWhiteSpace(OfferingSummary)
        ? string.Empty
        : Variants.Count == 0 && !OfferingSummary.StartsWith("Select an Offering", StringComparison.Ordinal)
            ? "This Offering has no active Variants. Configure Variants in Store Editor before recording prices."
            : Variants.Count == 0 ? "Variant pricing is available after an Offering is selected in Design." : string.Empty;

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        if (_isDirty && _itemId != Guid.Empty) _drafts[_itemId] = CaptureDraft();
        _itemId = itemId; _canEdit = canEdit; Error = null;
        if (_service is null) { Status = "Local listing details are unavailable."; return; }
        try
        {
            var state = await _service.LoadAsync(itemId, cancellationToken).ConfigureAwait(true);
            if (state is null) { Status = "Item not found."; return; }
            _isLoading = true;
            Title = state.Title; Description = state.Description; Currency = state.CurrencyCode ?? "USD";
            ShippingName = state.Details?.ShippingOptionName ?? string.Empty;
            CustomerShippingCharge = Format(state.Details?.CustomerShippingCharge);
            SellerShippingCost = Format(state.Details?.ExpectedSellerShippingCost);
            DeliveryEstimate = state.Details?.DeliveryEstimate ?? string.Empty;
            Variants.Clear();
            foreach (var variant in state.Variants)
            {
                var terms = state.VariantTerms.SingleOrDefault(value => value.VariantId == variant.Id);
                var row = new VariantListingTermsViewModel(variant.Id, variant.Name, terms?.SellingPrice, terms?.ExpectedFulfillmentCost);
                row.PropertyChanged += (_, _) => { if (!_isLoading) _isDirty = true; Changed(nameof(GrossProfitSummary)); };
                Variants.Add(row);
            }
            if (_drafts.TryGetValue(itemId, out var draft)) ApplyDraft(draft);
            _isDirty = _drafts.ContainsKey(itemId);
            _isLoading = false;
            _canEdit = canEdit && state.CanEdit;
            _hasOffering = state.OfferingId is not null;
            OfferingSummary = state.OfferingName is null ? "Select an Offering in Design to add fulfillment and shipping details." : $"Fulfillment setup: {state.OfferingName}";
            Changed(nameof(OfferingSummary)); Changed(nameof(VariantPricingGuidance)); Changed(nameof(CanSave)); Changed(nameof(CanEditFulfillmentTerms));
            SaveCommand.NotifyCanExecuteChanged();
            HistorySummary = state.History.Count == 0 ? "No prior fulfillment setups." : string.Join(Environment.NewLine,
                state.History.Select(entry =>
                {
                    var archivedTerms = JsonSerializer.Deserialize<ItemVariantListingTerms[]>(entry.VariantTermsJson) ?? [];
                    var variantSummary = archivedTerms.Length == 0 ? "no Variant prices saved" : string.Join(", ", archivedTerms.Select(term =>
                        $"{term.VariantId.ToString()[..8]}: price {Format(term.SellingPrice) ?? "—"}, cost {Format(term.ExpectedFulfillmentCost) ?? "—"}"));
                    return $"{entry.OfferingName} · {entry.ArchivedAt.ToLocalTime():g}\nTitle: {entry.Title ?? "(empty)"}\nDescription: {entry.Description ?? "(empty)"}\nShipping: {entry.ShippingOptionName ?? "not recorded"}, customer charge {Format(entry.CustomerShippingCharge) ?? "—"}, seller cost {Format(entry.ExpectedSellerShippingCost) ?? "—"}, delivery {entry.DeliveryEstimate ?? "—"}\n{variantSummary}";
                }));
            Status = _isDirty ? "Unsaved draft retained on navigation" : state.CanEdit ? "Local details" : state.ReadOnlyReason ?? "Read-only";
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { _isLoading = false; Error = exception.Message; Status = "Could not load local details."; }
    }

    private async Task SaveAsync()
    {
        if (!CanSave) return;
        IsBusy = true; Error = null;
        try
        {
            var terms = Variants.Select(value => new ItemVariantListingTerms(_itemId, value.VariantId,
                ParseOptional(value.SellingPrice, "selling price"), ParseOptional(value.FulfillmentCost, "fulfillment cost"))).ToArray();
            await _service!.SaveAsync(_itemId, Title, Description, Currency, ShippingName,
                ParseOptional(CustomerShippingCharge, "customer shipping charge"), ParseOptional(SellerShippingCost, "seller shipping cost"),
                DeliveryEstimate, terms).ConfigureAwait(true);
            Status = "Saved locally";
            _isDirty = false;
            _drafts.Remove(_itemId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException) { Error = exception.Message; Status = "Save failed; your edits are still here."; }
        finally { IsBusy = false; Changed(nameof(CanSave)); }
    }

    private static decimal? ParseOptional(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed is < 0 or > 999999999.99m)
            throw new ArgumentException($"Enter a valid non-negative {label} using a period for decimals.");
        return parsed;
    }
    private static string Format(decimal? amount) => amount?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
    public string GrossProfitSummary => string.Join(" · ", Variants.Select(value =>
    {
        var price = ParseForDisplay(value.SellingPrice);
        var cost = ParseForDisplay(value.FulfillmentCost);
        var customerShipping = ParseForDisplay(CustomerShippingCharge);
        var sellerShipping = ParseForDisplay(SellerShippingCost);
        return price is not null && cost is not null && customerShipping is not null && sellerShipping is not null
            ? $"{value.Name}: {Currency.ToUpperInvariant()} {(price.Value + customerShipping.Value - cost.Value - sellerShipping.Value):0.##} gross profit"
            : $"{value.Name}: incomplete";
    }));
    private static decimal? ParseForDisplay(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0 ? parsed : null;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null, bool trackDirty = true)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; if (trackDirty && !_isLoading) _isDirty = true; Changed(name); Changed(nameof(CanSave)); SaveCommand.NotifyCanExecuteChanged(); if (name is nameof(CustomerShippingCharge) or nameof(SellerShippingCost) or nameof(Currency)) Changed(nameof(GrossProfitSummary)); return true; }
    private void Changed(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private ManualListingDraft CaptureDraft() => new(Title, Description, Currency, ShippingName, CustomerShippingCharge,
        SellerShippingCost, DeliveryEstimate, Variants.Select(value => new VariantDraft(value.VariantId, value.SellingPrice, value.FulfillmentCost)).ToArray());

    private void ApplyDraft(ManualListingDraft draft)
    {
        _isLoading = true;
        Title = draft.Title; Description = draft.Description; Currency = draft.Currency; ShippingName = draft.ShippingName;
        CustomerShippingCharge = draft.CustomerShippingCharge; SellerShippingCost = draft.SellerShippingCost; DeliveryEstimate = draft.DeliveryEstimate;
        foreach (var row in Variants)
            if (draft.Variants.SingleOrDefault(value => value.VariantId == row.VariantId) is { } saved)
            { row.SellingPrice = saved.SellingPrice; row.FulfillmentCost = saved.FulfillmentCost; }
        _isLoading = false;
    }

    private sealed record ManualListingDraft(string Title, string Description, string Currency, string ShippingName,
        string CustomerShippingCharge, string SellerShippingCost, string DeliveryEstimate, IReadOnlyList<VariantDraft> Variants);
    private sealed record VariantDraft(Guid VariantId, string SellingPrice, string FulfillmentCost);
}
