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
    private long _operationVersion;
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
        InvalidateOperation();
        ClearBlueprints();
        _operationCancellation = new CancellationTokenSource();
        return LoadAsync(_operationVersion, _operationCancellation.Token);
    }

    public void Open()
    {
        if (IsBusy) return;
        InvalidateOperation();
        ClearBlueprints();
        IsOpen = true;
        ErrorMessage = null;
        _operationCancellation = new CancellationTokenSource();
        _ = LoadAsync(_operationVersion, _operationCancellation.Token);
    }

    private async Task LoadAsync(long operationVersion, CancellationToken cancellationToken)
    {
        if (IsBusy) return;
        var scope = _scope();
        if (scope is null) { ErrorMessage = "Save and select an active Printify Store first."; return; }
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.LoadBlueprintsAsync(scope, cancellationToken).ConfigureAwait(true);
            if (!CanPublish(operationVersion, scope)) return;
            if (!result.Succeeded) { ErrorMessage = result.Message; return; }
            var products = result.Products ?? result.Blueprints?.Select(blueprint =>
                new PrintifyShopProductSummary(blueprint.Id.ToString(), blueprint.Title, blueprint.Description, blueprint.Id, 1)) ?? [];
            foreach (var product in products)
            {
                var item = new PrintifyCatalogImportItemViewModel(product);
                item.PropertyChanged += OnItemPropertyChanged;
                Blueprints.Add(item);
            }
            OnPropertyChanged(nameof(HasBlueprints));
            SelectionFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception)
        {
            if (CanPublish(operationVersion, scope))
            {
                ErrorMessage = "Printify catalog could not be loaded. Try again.";
            }
        }
        finally
        {
            if (IsCurrentVersion(operationVersion))
            {
                IsBusy = false;
                NotifyCommands();
            }
        }
    }

    private async Task ConfirmAsync()
    {
        var scope = _scope();
        if (scope is null || !CanConfirm) return;
        InvalidateOperation();
        _operationCancellation = new CancellationTokenSource();
        var operationVersion = _operationVersion;
        var cancellationToken = _operationCancellation.Token;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.LoadSelectedAsync(scope, Blueprints.Where(item => item.IsSelected).Select(item => item.Id).ToArray(), cancellationToken).ConfigureAwait(true);
            if (!CanPublish(operationVersion, scope)) return;
            if (!result.Succeeded) { ErrorMessage = result.Message; return; }
            if (_onImported is not null && CanPublish(operationVersion, scope))
                await _onImported(scope, cancellationToken).ConfigureAwait(true);
            if (!CanPublish(operationVersion, scope)) return;
            IsOpen = false;
            ImportFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception)
        {
            if (CanPublish(operationVersion, scope))
            {
                ErrorMessage = "The selected Printify catalog could not be prepared for import.";
            }
        }
        finally
        {
            if (IsCurrentVersion(operationVersion))
            {
                IsBusy = false;
                NotifyCommands();
            }
        }
    }

    private void Cancel()
    {
        InvalidateOperation();
        IsBusy = false;
        IsOpen = false;
        ErrorMessage = null;
        NotifyCommands();
        ImportFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool IsCurrentOperation(long operationVersion, StoreCredentialScope expected) =>
        IsCurrentVersion(operationVersion) && _scope() == expected;

    private bool IsCurrentVersion(long operationVersion) => operationVersion == _operationVersion;

    private bool CanPublish(long operationVersion, StoreCredentialScope expected) =>
        IsOpen && IsCurrentOperation(operationVersion, expected);

    private void InvalidateOperation()
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
        _operationVersion++;
    }

    private void ClearBlueprints()
    {
        foreach (var blueprint in Blueprints)
        {
            blueprint.PropertyChanged -= OnItemPropertyChanged;
        }

        Blueprints.Clear();
        OnPropertyChanged(nameof(HasBlueprints));
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
