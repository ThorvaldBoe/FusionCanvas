using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Stores;

public sealed class StoreManagementViewModel : INotifyPropertyChanged
{
    private readonly IStoreManagementService _service;
    private string _newStoreName = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _targetMarket = string.Empty;
    private string _brandDirection = string.Empty;
    private string _planningContext = string.Empty;
    private string? _errorMessage;

    public StoreManagementViewModel(IStoreManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        CreateStoreCommand = new RelayCommand(_ => Run(CreateStoreAsync()));
        SelectStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                Run(SelectStoreAsync(store));
            }
        });
        SaveSelectedStoreCommand = new RelayCommand(_ => Run(SaveSelectedStoreAsync()));
        ArchiveSelectedStoreCommand = new RelayCommand(_ => Run(ArchiveSelectedStoreAsync()));
        RestoreStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                Run(RestoreStoreAsync(store));
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<StoreSummary?>? ActiveStoreChanged;

    public IReadOnlyList<StoreSummary> ActiveStores { get; private set; } = [];

    public IReadOnlyList<StoreSummary> ArchivedStores { get; private set; } = [];

    public StoreSummary? SelectedStore { get; private set; }

    public bool NeedsFirstStore { get; private set; }

    public bool HasActiveStores => ActiveStores.Count > 0;

    public bool HasArchivedStores => ArchivedStores.Count > 0;

    public bool HasSelectedStore => SelectedStore is not null;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string NewStoreName
    {
        get => _newStoreName;
        set => SetField(ref _newStoreName, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public string TargetMarket
    {
        get => _targetMarket;
        set => SetField(ref _targetMarket, value);
    }

    public string BrandDirection
    {
        get => _brandDirection;
        set => SetField(ref _brandDirection, value);
    }

    public string PlanningContext
    {
        get => _planningContext;
        set => SetField(ref _planningContext, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public ICommand CreateStoreCommand { get; }

    public ICommand SelectStoreCommand { get; }

    public ICommand SaveSelectedStoreCommand { get; }

    public ICommand ArchiveSelectedStoreCommand { get; }

    public ICommand RestoreStoreCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(cancellationToken).ConfigureAwait(false);
        ApplyState(state);
    }

    public async Task CreateStoreAsync(CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateStoreAsync(
            new StoreManagementCreateRequest(NewStoreName, CurrentContext()),
            cancellationToken).ConfigureAwait(false);
        ApplyResult(result);

    }

    public async Task SelectStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var result = await _service.SelectStoreAsync(store.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    public async Task SaveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedStore is null)
        {
            ErrorMessage = "Select a store before saving.";
            return;
        }

        var result = await _service.UpdateStoreAsync(
            new StoreManagementUpdateRequest(SelectedStore.Id, NewStoreName, CurrentContext()),
            cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    public async Task ArchiveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedStore is null)
        {
            ErrorMessage = "Select a store before archiving.";
            return;
        }

        var result = await _service.ArchiveStoreAsync(SelectedStore.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    public async Task RestoreStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var result = await _service.RestoreStoreAsync(store.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    private void ApplyResult(StoreManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyState(result.State);
    }

    private void ApplyState(StoreManagementState state)
    {
        ActiveStores = state.ActiveStores;
        ArchivedStores = state.ArchivedStores;
        SelectedStore = state.ActiveStore;
        NeedsFirstStore = state.NeedsFirstStore;
        ApplySelectedStoreFields(SelectedStore);

        OnPropertyChanged(nameof(ActiveStores));
        OnPropertyChanged(nameof(ArchivedStores));
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(NeedsFirstStore));
        OnPropertyChanged(nameof(HasActiveStores));
        OnPropertyChanged(nameof(HasArchivedStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        ActiveStoreChanged?.Invoke(this, SelectedStore);
    }

    private void ApplySelectedStoreFields(StoreSummary? store)
    {
        NewStoreName = store?.Name ?? NewStoreName;
        Description = store?.Context.Description ?? string.Empty;
        Notes = store?.Context.Notes ?? string.Empty;
        TargetMarket = store?.Context.TargetMarket ?? string.Empty;
        BrandDirection = store?.Context.BrandDirection ?? string.Empty;
        PlanningContext = store?.Context.PlanningContext ?? string.Empty;
    }

    private StoreContext CurrentContext() =>
        new(
            EmptyToNull(Description),
            EmptyToNull(Notes),
            EmptyToNull(TargetMarket),
            EmptyToNull(BrandDirection),
            EmptyToNull(PlanningContext));

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Run(Task task) => _ = task;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
