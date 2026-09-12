using System.ComponentModel;
using Avalonia.Threading;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Stores;

public sealed class StorePrintifyCredentialsViewModel(IStorePrintifyConfigurationService service) : INotifyPropertyChanged
{
    private StoreSummary? _store;
    private StoreCredentialScope? _scope;
    private CancellationTokenSource? _pending;
    private long _generation;
    private bool _persistedPrintify;
    private int? _selectedShopId;
    private PrintifyShopOption? _selectedShop;
    private IReadOnlyList<PrintifyShopOption> _shops = [];
    private PrintifyConfigurationKind? _kind;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? EditRequested;
    public bool IsVisible { get; private set; }
    public bool IsBusy { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string Verification { get; private set; } = string.Empty;
    public IReadOnlyList<PrintifyShopOption> Shops => _shops;
    public int? SelectedShopId { get => _selectedShopId; set { if (_selectedShopId == value) return; _selectedShopId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedShopId))); ShopSelectionChanged?.Invoke(this, value); } }
    public PrintifyShopOption? SelectedShop
    {
        get => _selectedShop;
        set
        {
            if (Equals(_selectedShop, value)) return;
            _selectedShop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedShop)));
            SelectedShopId = value?.Id;
        }
    }
    public event EventHandler<int?>? ShopSelectionChanged;
    public bool HasKey => _kind == PrintifyConfigurationKind.Available;
    public bool IsMissing => _kind == PrintifyConfigurationKind.Missing;
    public bool HasError => _kind is PrintifyConfigurationKind.Unavailable or PrintifyConfigurationKind.InvalidContext;
    public bool CanManage => IsVisible && _scope is not null && !IsBusy && (HasKey || IsMissing);
    public bool CanVerify => CanManage && HasKey && _persistedPrintify;
    public bool ShowSaveGuidance => HasKey && !_persistedPrintify;
    public string ManageLabel => HasKey ? "Manage" : "Add";
    public Task PendingOperation { get; private set; } = Task.CompletedTask;
    public RelayCommand ManageCommand => new(_ => { if (CanManage) EditRequested?.Invoke(this, EventArgs.Empty); }, () => CanManage);
    public AsyncRelayCommand VerifyCommand => new(VerifyAsync, () => CanVerify);
    public AsyncRelayCommand RetryCommand => new(RefreshAsync, () => !IsBusy && _scope is not null);

    public void SetContext(StoreSummary? store, FulfillmentStrategy selectedStrategy, bool isDraft, bool editorOpen)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetContext(store, selectedStrategy, isDraft, editorOpen));
            return;
        }
        var visible = editorOpen && FulfillmentStrategyPolicy.RequiresPrintifyKey(selectedStrategy);
        var scope = visible && !isDraft && store is { IsArchived: false }
            ? new StoreCredentialScope(store.WorkspaceId, store.Id) : null;
        var persisted = store is not null && FulfillmentStrategyPolicy.RequiresPrintifyKey(store.FulfillmentStrategy);
        if (Equals(_store, store) && Equals(_scope, scope) && IsVisible == visible && _persistedPrintify == persisted) return;
        CancelPending();
        _store = store;
        _scope = scope;
        _persistedPrintify = persisted;
        _selectedShopId = store?.Context.PrintifyShopId;
        _shops = _selectedShopId is { } selectedShopId
            ? [new PrintifyShopOption(selectedShopId, $"Saved shop {selectedShopId}")]
            : [];
        _selectedShop = _shops.FirstOrDefault();
        IsVisible = visible;
        _kind = null;
        Verification = string.Empty;
        Status = store?.IsArchived == true ? "Archived Store configuration is read-only." : "Save the Store before adding its Printify key.";
        Notify();
        if (_scope is not null) PendingOperation = RefreshAsync();
    }

    public PrintifyApiKeyViewModel? CreateEditor() =>
        CanManage && _scope is not null ? new(service, _scope, _store!.Name) : null;

    public async Task RefreshAsync()
    {
        if (_scope is null) return;
        CancelPending();
        var generation = _generation;
        var scope = _scope;
        _pending = new();
        var cancellation = _pending.Token;
        IsBusy = true;
        _kind = null;
        Status = "Reading Printify credential status…";
        Verification = string.Empty;
        Notify();
        try
        {
            var result = await service.ReadStatusAsync(scope, cancellation).ConfigureAwait(false);
            await PublishAsync(generation, () => { _kind = result.Kind; Status = result.Message; });
        }
        catch (OperationCanceledException) { }
        catch (Exception) { await PublishAsync(generation, () => { _kind = PrintifyConfigurationKind.Unavailable; Status = PrintifyConfigurationResult.Unavailable.Message; }); }
        finally { await PublishAsync(generation, () => IsBusy = false); }
    }

    public async Task VerifyAsync()
    {
        if (!CanVerify || _scope is null) return;
        var generation = _generation;
        var scope = _scope;
        _pending?.Dispose();
        _pending = new();
        var cancellation = _pending.Token;
        IsBusy = true;
        Verification = "Verifying Printify key…";
        Notify();
        try
        {
            var result = await service.VerifyAsync(scope, cancellation).ConfigureAwait(false);
            await PublishAsync(generation, () =>
            {
                Verification = result.Message;
                _shops = result.Shops ?? [];
                _selectedShop = _selectedShopId is { } selectedShopId
                    ? _shops.FirstOrDefault(shop => shop.Id == selectedShopId)
                    : null;
                if (_selectedShopId is not null && _selectedShop is null)
                {
                    _selectedShopId = null;
                    Verification = "Printify key verified. Select a shop for this Store.";
                    ShopSelectionChanged?.Invoke(this, null);
                }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception) { await PublishAsync(generation, () => Verification = "Printify verification could not complete. Try again."); }
        finally { await PublishAsync(generation, () => IsBusy = false); }
    }

    public void CancelPending()
    {
        _generation++;
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
        IsBusy = false;
    }

    private async Task PublishAsync(long generation, Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (generation != _generation) return;
            action();
            Notify();
        });
    }

    private void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
