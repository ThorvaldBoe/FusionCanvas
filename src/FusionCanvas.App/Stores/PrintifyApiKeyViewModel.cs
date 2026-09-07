using System.ComponentModel;
using Avalonia.Threading;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyApiKeyViewModel(
    IStorePrintifyConfigurationService service,
    StoreCredentialScope scope,
    string storeName) : INotifyPropertyChanged
{
    private string _draft = string.Empty;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CloseRequested;
    public string Title => $"Printify API key — {storeName}";
    public string Draft
    {
        get => _draft;
        set { if (IsBusy) return; _draft = value; Notify(); }
    }
    public bool IsBusy { get; private set; }
    public bool CanEdit => !IsBusy;
    public bool CanSave => !IsBusy && PrintifyToken.IsValid(Draft);
    public bool ShowDiscard { get; private set; }
    public bool Saved { get; private set; }
    public bool CanClose { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public AsyncRelayCommand SaveCommand => new(SaveAsync, () => CanSave);
    public RelayCommand CancelCommand => new(_ => RequestClose());
    public RelayCommand DiscardCommand => new(_ => { if (!IsBusy) Close(); });
    public RelayCommand KeepEditingCommand => new(_ => { ShowDiscard = false; Notify(); });

    public bool RequestClose(bool requestWindowClose = true)
    {
        if (IsBusy) return false;
        if (CanClose) return true;
        if (Draft.Length != 0) { ShowDiscard = true; Notify(); return false; }
        Close(requestWindowClose);
        return true;
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;
        var key = Draft;
        IsBusy = true;
        Error = string.Empty;
        Notify();
        PrintifyConfigurationResult result;
        try { result = await service.SaveAsync(scope, key).ConfigureAwait(false); }
        catch (Exception) { result = PrintifyConfigurationResult.Unavailable; }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = false;
            if (result.Succeeded) { Saved = true; Close(); }
            else { Error = result.Message; Notify(); }
        });
    }

    private void Close(bool requestWindowClose = true)
    {
        _draft = string.Empty;
        ShowDiscard = false;
        CanClose = true;
        Notify();
        if (requestWindowClose) CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
