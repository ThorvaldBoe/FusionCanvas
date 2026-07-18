using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Listings;

public sealed class ListingManagementViewModel : INotifyPropertyChanged
{
    private readonly IListingManagementService _service;
    private ListingSummary? _selected;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _originalDescription = string.Empty;
    private string _originalNotes = string.Empty;
    private bool _isOpen;
    private bool _isBusy;
    private bool _discardPromptVisible;
    private bool _deleteConfirmationVisible;
    private string? _errorMessage;
    private ListingSummary? _pendingSelection;

    public ListingManagementViewModel(IListingManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        SaveCommand = new RelayCommand(_ => Run(SaveAsync()));
        SelectListingCommand = new RelayCommand(parameter =>
        {
            if (parameter is ListingSummary listing) RequestSelection(listing);
        });
        ArchiveCommand = new RelayCommand(_ => Run(ArchiveAsync()));
        RestoreCommand = new RelayCommand(_ => Run(RestoreAsync()));
        RequestDeleteCommand = new RelayCommand(_ => DeleteConfirmationVisible = true);
        ConfirmDeleteCommand = new RelayCommand(_ => Run(DeleteAsync()));
        CancelDeleteCommand = new RelayCommand(_ => DeleteConfirmationVisible = false);
        CloseCommand = new RelayCommand(_ => RequestClose());
        SaveAndCloseCommand = new RelayCommand(_ => Run(SaveAndCloseAsync()));
        DiscardAndCloseCommand = new RelayCommand(_ => DiscardAndContinue());
        KeepEditingCommand = new RelayCommand(_ => { _pendingSelection = null; DiscardPromptVisible = false; });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<ListingSummary?>? WorkspaceStructureChanged;

    public IReadOnlyList<ListingSummary> ActiveListings { get; private set; } = [];
    public IReadOnlyList<ListingSummary> ArchivedListings { get; private set; } = [];
    public IReadOnlyList<ListingDestination> Destinations { get; private set; } = [];
    public ListingSummary? SelectedListing { get => _selected; private set { if (SetField(ref _selected, value)) RaiseState(); } }
    public bool IsOpen { get => _isOpen; private set => SetField(ref _isOpen, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) RaiseState(); } }
    public bool DiscardPromptVisible { get => _discardPromptVisible; private set => SetField(ref _discardPromptVisible, value); }
    public bool DeleteConfirmationVisible { get => _deleteConfirmationVisible; private set => SetField(ref _deleteConfirmationVisible, value); }
    public string Description { get => _description; set { if (SetField(ref _description, value)) RaiseState(); } }
    public string Notes { get => _notes; set { if (SetField(ref _notes, value)) RaiseState(); } }
    public string? ErrorMessage { get => _errorMessage; private set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasUnsavedChanges => Description != _originalDescription || Notes != _originalNotes;
    public bool CanSave => SelectedListing is not null && HasUnsavedChanges && !IsBusy;
    public bool CanArchive => SelectedListing is { IsEffectivelyActive: true } && !IsBusy;
    public bool CanRestore => SelectedListing is { IsArchived: true } && !IsBusy;
    public bool CanDelete => SelectedListing is not null && !IsBusy;

    public ICommand SaveCommand { get; }
    public ICommand SelectListingCommand { get; }
    public ICommand ArchiveCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RequestDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SaveAndCloseCommand { get; }
    public ICommand DiscardAndCloseCommand { get; }
    public ICommand KeepEditingCommand { get; }

    public void SetActiveWorkspace(Guid? id) => _service.SetActiveWorkspace(id);

    public async Task OpenForEditAsync(Guid storeId, Guid listingId, CancellationToken cancellationToken = default)
    {
        IsOpen = true;
        await LoadAsync(storeId, cancellationToken).ConfigureAwait(false);
        Select(ActiveListings.Concat(ArchivedListings).SingleOrDefault(item => item.Id == listingId));
    }

    private async Task LoadAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var state = await _service.LoadAsync(storeId, cancellationToken).ConfigureAwait(false);
        ActiveListings = state.ActiveListings;
        ArchivedListings = state.ArchivedListings;
        Destinations = state.ValidDestinations;
        OnPropertyChanged(nameof(ActiveListings));
        OnPropertyChanged(nameof(ArchivedListings));
        OnPropertyChanged(nameof(Destinations));
    }

    private void Select(ListingSummary? listing)
    {
        SelectedListing = listing;
        Description = listing?.Context.Description ?? string.Empty;
        Notes = listing?.Context.Notes ?? string.Empty;
        _originalDescription = Description;
        _originalNotes = Notes;
        ErrorMessage = null;
        RaiseState();
    }

    private void RequestSelection(ListingSummary listing)
    {
        if (SelectedListing?.Id == listing.Id) return;
        if (HasUnsavedChanges)
        {
            _pendingSelection = listing;
            DiscardPromptVisible = true;
            return;
        }

        Select(listing);
    }

    private async Task SaveAsync()
    {
        if (!CanSave || SelectedListing is not { } listing) return;
        IsBusy = true;
        var result = await _service.UpdateListingAsync(new ListingManagementUpdateRequest(
            listing.Id,
            listing.Name,
            listing.Context with { Description = Description, Notes = Notes })).ConfigureAwait(false);
        IsBusy = false;
        Apply(result);
    }

    private async Task ArchiveAsync()
    {
        if (!CanArchive || SelectedListing is not { } listing) return;
        IsBusy = true;
        var result = await _service.ArchiveListingAsync(listing.Id).ConfigureAwait(false);
        IsBusy = false;
        Apply(result);
    }

    private async Task RestoreAsync()
    {
        if (!CanRestore || SelectedListing is not { } listing) return;
        IsBusy = true;
        var result = await _service.RestoreListingAsync(new ListingManagementRestoreRequest(listing.Id)).ConfigureAwait(false);
        IsBusy = false;
        Apply(result);
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete || SelectedListing is not { } listing) return;
        DeleteConfirmationVisible = false;
        IsBusy = true;
        var result = await _service.DeleteListingAsync(new ListingManagementDeleteRequest(listing.Id, true)).ConfigureAwait(false);
        IsBusy = false;
        Apply(result, deleted: result.Succeeded);
    }

    private void Apply(ListingManagementResult result, bool deleted = false)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ActiveListings = result.State.ActiveListings;
        ArchivedListings = result.State.ArchivedListings;
        Destinations = result.State.ValidDestinations;
        OnPropertyChanged(nameof(ActiveListings));
        OnPropertyChanged(nameof(ArchivedListings));
        OnPropertyChanged(nameof(Destinations));
        var changed = ActiveListings.Concat(ArchivedListings).SingleOrDefault(item => item.Id == result.Listing?.Id) ?? result.Listing;
        var replacement = deleted || changed is { IsEffectivelyActive: false }
            ? ActiveListings.FirstOrDefault() ?? changed
            : changed;
        Select(deleted ? replacement : changed);
        WorkspaceStructureChanged?.Invoke(this, replacement);
    }

    private void RequestClose()
    {
        if (HasUnsavedChanges) DiscardPromptVisible = true;
        else CloseImmediately();
    }

    private async Task SaveAndCloseAsync()
    {
        await SaveAsync().ConfigureAwait(false);
        if (HasError) return;
        if (_pendingSelection is { } pending)
        {
            _pendingSelection = null;
            DiscardPromptVisible = false;
            Select(pending);
        }
        else
        {
            CloseImmediately();
        }
    }

    private void DiscardAndContinue()
    {
        if (_pendingSelection is { } pending)
        {
            _pendingSelection = null;
            DiscardPromptVisible = false;
            Select(pending);
        }
        else
        {
            CloseImmediately();
        }
    }

    private void CloseImmediately()
    {
        DiscardPromptVisible = false;
        _pendingSelection = null;
        DeleteConfirmationVisible = false;
        IsOpen = false;
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanRestore));
        OnPropertyChanged(nameof(CanDelete));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private static void Run(Task task) => _ = task;
}
