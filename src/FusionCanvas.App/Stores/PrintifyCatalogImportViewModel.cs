using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportViewModel : INotifyPropertyChanged
{
    private readonly IPrintifyCatalogImportService _service;
    private readonly Func<StoreCredentialScope?> _scope;
    private readonly Func<StoreCredentialScope, CancellationToken, Task>? _onImported;
    private CancellationTokenSource? _operationCancellation;
    private bool _isOpen;
    private bool _isBusy;
    private string? _errorMessage;

    public PrintifyCatalogImportViewModel(IPrintifyCatalogImportService service, Func<StoreCredentialScope?> scope, Func<StoreCredentialScope, CancellationToken, Task>? onImported = null)
    {
        _service = service;
        _scope = scope;
        _onImported = onImported;
        OpenCommand = new RelayCommand(_ => Open());
        LoadCommand = new AsyncRelayCommand(StartLoadAsync);
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => CanConfirm);
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    public ObservableCollection<PrintifyCatalogImportItemViewModel> Blueprints { get; } = [];
    public ICommand OpenCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public bool IsOpen { get => _isOpen; private set => SetField(ref _isOpen, value); }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetField(ref _errorMessage, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public int SelectedCount => Blueprints.Count(item => item.IsSelected);
    public bool HasBlueprints => Blueprints.Count > 0;
    public bool CanStart => !IsBusy;
    public bool CanConfirm => IsOpen && !IsBusy && SelectedCount > 0;
    public event EventHandler? SelectionFocusRequested;
    public event EventHandler? ImportFocusRequested;

    private Task StartLoadAsync()
    {
        if (IsBusy) return Task.CompletedTask;
        CancelOperation();
        _operationCancellation = new CancellationTokenSource();
        return LoadAsync(_operationCancellation.Token);
    }

    public void Open()
    {
        if (IsBusy) return;
        CancelOperation();
        IsOpen = true;
        ErrorMessage = null;
        _operationCancellation = new CancellationTokenSource();
        _ = LoadAsync(_operationCancellation.Token);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (IsBusy) return;
        var scope = _scope();
        if (scope is null) { ErrorMessage = "Save and select an active Printify Store first."; return; }
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.LoadBlueprintsAsync(scope, cancellationToken).ConfigureAwait(true);
            if (!IsCurrentScope(scope)) return;
            if (!result.Succeeded) { ErrorMessage = result.Message; return; }
            Blueprints.Clear();
            foreach (var blueprint in result.Blueprints ?? [])
            {
                var item = new PrintifyCatalogImportItemViewModel(blueprint);
                item.PropertyChanged += OnItemPropertyChanged;
                Blueprints.Add(item);
            }
            OnPropertyChanged(nameof(HasBlueprints));
            SelectionFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) { ErrorMessage = "Printify catalog could not be loaded. Try again."; }
        finally { IsBusy = false; NotifyCommands(); }
    }

    private async Task ConfirmAsync()
    {
        var scope = _scope();
        if (scope is null || !CanConfirm) return;
        CancelOperation();
        _operationCancellation = new CancellationTokenSource();
        var cancellationToken = _operationCancellation.Token;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.LoadSelectedAsync(scope, Blueprints.Where(item => item.IsSelected).Select(item => item.Id).ToArray(), cancellationToken).ConfigureAwait(true);
            if (!IsCurrentScope(scope)) return;
            if (!result.Succeeded) { ErrorMessage = result.Message; return; }
            if (_onImported is not null)
                await _onImported(scope, cancellationToken).ConfigureAwait(true);
            IsOpen = false;
            ImportFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception) { ErrorMessage = "The selected Printify catalog could not be prepared for import."; }
        finally { IsBusy = false; NotifyCommands(); }
    }

    private void Cancel()
    {
        if (IsBusy) return;
        CancelOperation();
        IsOpen = false;
        ImportFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool IsCurrentScope(StoreCredentialScope expected) => IsOpen && _scope() == expected;

    private void CancelOperation()
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(PrintifyCatalogImportItemViewModel.IsSelected)) NotifyCommands();
    }

    private void NotifyCommands()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanStart));
        (ConfirmCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
        NotifyCommands();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public event PropertyChangedEventHandler? PropertyChanged;
}
