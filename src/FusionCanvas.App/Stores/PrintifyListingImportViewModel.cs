using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Commands;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyListingImportViewModel : INotifyPropertyChanged
{
    private readonly PrintifyListingImportService _service;
    private readonly StoreCredentialScope _scope;
    private readonly Guid _nicheId;
    private bool _showLinked;
    private bool _isBusy;
    private string? _message;
    private string? _error;
    private CancellationTokenSource? _operationCancellation;

    public PrintifyListingImportViewModel(PrintifyListingImportService service, StoreCredentialScope scope, Guid nicheId)
    {
        _service = service; _scope = scope; _nicheId = nicheId;
        LoadCommand = new AsyncRelayCommand(() => LoadAsync(), () => !IsBusy);
        CheckDuplicatesCommand = new AsyncRelayCommand(CheckDuplicatesAsync, () => !IsBusy && SelectedRows.Length > 0);
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !IsBusy && SelectedRows.Length > 0);
        ToggleLinkedCommand = new RelayCommand(_ => { ShowLinked = !ShowLinked; });
        CancelCommand = new RelayCommand(_ => _operationCancellation?.Cancel());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<PrintifyListingImportRowViewModel> Products { get; } = [];
    public ICommand LoadCommand { get; }
    public ICommand CheckDuplicatesCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ToggleLinkedCommand { get; }
    public ICommand CancelCommand { get; }
    public bool ShowLinked { get => _showLinked; set { _showLinked = value; Changed(); Changed(nameof(VisibleProducts)); } }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            Changed();
            Changed(nameof(CanImport));
            ((AsyncRelayCommand)LoadCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)CheckDuplicatesCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ImportCommand).NotifyCanExecuteChanged();
        }
    }
    public bool CanImport => !IsBusy && SelectedRows.Length > 0;
    public string? Message { get => _message; private set { _message = value; Changed(); } }
    public string? Error { get => _error; private set { _error = value; Changed(); Changed(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public IEnumerable<PrintifyListingImportRowViewModel> VisibleProducts => Products.Where(row => ShowLinked || !row.IsLinked);
    private PrintifyListingImportRowViewModel[] SelectedRows => Products.Where(row => row.IsSelected && !row.IsLinked).ToArray();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _operationCancellation = source;
        IsBusy = true; Error = null; Message = "Loading Printify products…";
        try
        {
            Products.Clear();
            foreach (var preview in await _service.LoadPreviewAsync(_scope, _nicheId, source.Token))
                AddProduct(preview);
            Message = Products.Count == 0 ? "This Printify shop has no products." : $"{Products.Count} products loaded.";
            Changed(nameof(VisibleProducts));
        }
        catch (OperationCanceledException) { Message = "Loading cancelled."; }
        catch (Exception exception) { Error = exception.Message; Message = null; }
        finally { IsBusy = false; _operationCancellation = null; }
    }

    public async Task CheckDuplicatesAsync()
    {
        using var source = new CancellationTokenSource(); _operationCancellation = source;
        IsBusy = true; Error = null;
        try
        {
            var matches = await _service.CheckDuplicatesAsync(_scope, SelectedRows.Select(row => row.ProductId).ToArray(), source.Token);
            foreach (var row in SelectedRows) row.Candidates = matches.GetValueOrDefault(row.ProductId) ?? [];
            Message = "Duplicate check complete. Choose an existing Item from a product row or import it as new.";
        }
        catch (OperationCanceledException) { Message = "Duplicate check cancelled."; }
        catch (Exception exception) { Error = exception.Message; }
        finally { IsBusy = false; _operationCancellation = null; }
    }

    public async Task ImportAsync()
    {
        using var source = new CancellationTokenSource(); _operationCancellation = source;
        IsBusy = true; Error = null;
        try
        {
            var selected = SelectedRows;
            Message = "Importing selected products…";
            var outcomes = await _service.ImportAsync(_scope, _nicheId, selected.Select(row => new PrintifyListingImportDecision(row.ProductId, row.ConnectToItemId)).ToArray(), source.Token);
            var succeeded = outcomes.Count(outcome => outcome.Succeeded);
            Message = source.IsCancellationRequested
                ? $"Import cancelled. Imported {succeeded} of {outcomes.Count} products; retry the remaining items later."
                : $"Imported {succeeded} of {outcomes.Count} selected products.";
            Error = string.Join(Environment.NewLine, outcomes.Where(outcome => !outcome.Succeeded).Select(outcome => $"{outcome.ProductId}: {outcome.Message}"));
            foreach (var outcome in outcomes)
            {
                var row = Products.FirstOrDefault(value => value.ProductId == outcome.ProductId);
                if (row is null) continue;
                row.ImportOutcome = outcome.Succeeded ? "Imported" : outcome.Message;
                if (outcome.Succeeded) row.IsSelected = false;
            }
        }
        catch (OperationCanceledException) { Message = "Import cancelled. Completed products remain imported; retry to continue."; }
        catch (Exception exception) { Error = exception.Message; }
        finally { IsBusy = false; _operationCancellation = null; }
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    private void OnProductChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrintifyListingImportRowViewModel.IsSelected))
        {
            Changed(nameof(CanImport));
            ((AsyncRelayCommand)CheckDuplicatesCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ImportCommand).NotifyCanExecuteChanged();
        }
    }

    public PrintifyListingImportRowViewModel AddProduct(PrintifyListingImportPreview preview)
    {
        var row = new PrintifyListingImportRowViewModel(preview);
        row.PropertyChanged += OnProductChanged;
        Products.Add(row);
        return row;
    }
}
