using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPrintifyCatalogImportService _service;
    private readonly Func<StoreCredentialScope?> _scope;
    private readonly Func<StoreCredentialScope, CancellationToken, Task>? _onImported;
    private CancellationTokenSource? _operationCancellation;
    private readonly object _operationGate = new();
    private readonly HashSet<Task> _activeOperations = [];
    private long _operationVersion;
    private bool _isDisposed;
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
        ConfirmCommand = new AsyncRelayCommand(() => PendingOperation = TrackOperation(ConfirmAsync()), () => CanConfirm);
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    public ObservableCollection<PrintifyCatalogImportItemViewModel> Blueprints { get; } = [];
    public ICommand OpenCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public bool IsOpen { get => _isOpen; private set => SetField(ref _isOpen, value); }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public Task PendingOperation { get; private set; } = Task.CompletedTask;
    public bool HasPendingOperations
    {
        get
        {
            lock (_operationGate)
            {
                return _activeOperations.Any(operation => !operation.IsCompleted);
            }
        }
    }

    public Task WaitForPendingOperationsAsync()
    {
        lock (_operationGate)
        {
            return Task.WhenAll(_activeOperations.ToArray());
        }
    }
    public string? ErrorMessage { get => _errorMessage; private set => SetField(ref _errorMessage, value); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public int SelectedCount => Blueprints.Count(item => item.IsSelected);
    public bool HasBlueprints => Blueprints.Count > 0;
    public bool CanStart => !_isDisposed && !IsBusy;
    public bool CanConfirm => !_isDisposed && IsOpen && !IsBusy && SelectedCount > 0;
    public event EventHandler? SelectionFocusRequested;
    public event EventHandler? ImportFocusRequested;

    private Task StartLoadAsync()
    {
        if (_isDisposed || IsBusy) return Task.CompletedTask;
        InvalidateOperation();
        ClearBlueprints();
        var source = _operationCancellation = new CancellationTokenSource();
        return PendingOperation = TrackOperation(LoadAsync(_operationVersion, source));
    }

    public void Open()
    {
        if (_isDisposed || IsBusy) return;
        InvalidateOperation();
        ClearBlueprints();
        IsOpen = true;
        ErrorMessage = null;
        var source = _operationCancellation = new CancellationTokenSource();
        PendingOperation = TrackOperation(LoadAsync(_operationVersion, source));
    }

    private async Task LoadAsync(long operationVersion, CancellationTokenSource source)
    {
        var cancellationToken = source.Token;
        if (IsBusy)
        {
            DisposeCompletedOperation(operationVersion, source);
            return;
        }

        StoreCredentialScope? scope = null;
        try
        {
            scope = _scope();
            if (scope is not { } currentScope)
            {
                ErrorMessage = "Save and select an active Printify Store first.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            var result = await _service.LoadBlueprintsAsync(currentScope, cancellationToken).ConfigureAwait(true);
            if (!CanPublish(operationVersion, currentScope)) return;
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
            if (IsCurrentVersion(operationVersion)
                && (scope is null || CanPublish(operationVersion, scope)))
            {
                ErrorMessage = "Printify catalog could not be loaded. Try again.";
            }
        }
        finally
        {
            DisposeCompletedOperation(operationVersion, source);
            if (IsCurrentVersion(operationVersion))
            {
                IsBusy = false;
                NotifyCommands();
            }
        }
    }

    private async Task ConfirmAsync()
    {
        if (_isDisposed) return;
        var scope = _scope();
        if (scope is null || !CanConfirm) return;
        InvalidateOperation();
        var source = _operationCancellation = new CancellationTokenSource();
        var operationVersion = _operationVersion;
        var cancellationToken = source.Token;
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
            DisposeCompletedOperation(operationVersion, source);
            if (IsCurrentVersion(operationVersion))
            {
                IsBusy = false;
                NotifyCommands();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        try
        {
            InvalidateOperation();
        }
        finally
        {
            IsBusy = false;
            IsOpen = false;
            ClearBlueprints();
            NotifyCommands();
        }
    }

    private void Cancel()
    {
        if (_isDisposed) return;
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
        !_isDisposed && IsOpen && IsCurrentOperation(operationVersion, expected);

    private void DisposeCompletedOperation(long operationVersion, CancellationTokenSource source)
    {
        if (IsCurrentVersion(operationVersion) && ReferenceEquals(_operationCancellation, source))
        {
            _operationCancellation = null;
        }

        source.Dispose();
    }

    private void InvalidateOperation()
    {
        var source = _operationCancellation;
        _operationCancellation = null;
        _operationVersion++;
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        finally
        {
            if (!IsBusy)
            {
                source.Dispose();
            }
        }
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
        if (!_isDisposed && args.PropertyName == nameof(PrintifyCatalogImportItemViewModel.IsSelected)) NotifyCommands();
    }

    private void NotifyCommands()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanStart));
        (ConfirmCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private Task TrackOperation(Task operation)
    {
        lock (_operationGate)
        {
            _activeOperations.Add(operation);
            PendingOperation = operation;
        }

        _ = RemoveCompletedOperationAsync(operation);
        return operation;
    }

    private async Task RemoveCompletedOperationAsync(Task operation)
    {
        try
        {
            await operation.ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            lock (_operationGate)
            {
                _activeOperations.Remove(operation);
            }
        }
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
