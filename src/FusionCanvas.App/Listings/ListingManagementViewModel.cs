using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Listings;

public sealed class ListingManagementViewModel : INotifyPropertyChanged
{
    private readonly IListingManagementService _service;
    private readonly ITagManagementService? _tagService;
    private ListingSummary? _selected;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _originalDescription = string.Empty;
    private string _originalNotes = string.Empty;
    private bool _isOpen;
    private bool _isBusy;
    private bool _isBusyTags;
    private bool _discardPromptVisible;
    private bool _deleteConfirmationVisible;
    private string? _errorMessage;
    private string? _tagErrorMessage;
    private ListingSummary? _pendingSelection;
    private string _tagInput = string.Empty;
    private int _selectedSuggestionIndex = -1;
    private IReadOnlyList<TagSummary> _appliedTags = [];
    private IReadOnlyList<TagSummary> _suggestedTags = [];

    public ListingManagementViewModel(IListingManagementService service, ITagManagementService? tagService = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tagService = tagService;
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
        ApplyTagSuggestionCommand = new RelayCommand(parameter =>
        {
            if (parameter is TagSummary tag) Run(ApplyTagByIdAsync(tag.Id));
        });
        ApplyOrCreateTagCommand = new RelayCommand(_ => Run(ApplyOrCreateTagFromInputAsync()));
        RemoveTagCommand = new RelayCommand(parameter =>
        {
            if (parameter is TagSummary tag) Run(RemoveTagAsync(tag.Id));
        });
        ClearTagInputCommand = new RelayCommand(_ => ClearTagInput());
        SelectPreviousSuggestionCommand = new RelayCommand(_ => MoveSuggestion(-1));
        SelectNextSuggestionCommand = new RelayCommand(_ => MoveSuggestion(1));
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

    public bool IsBusyTags { get => _isBusyTags; private set => SetField(ref _isBusyTags, value); }
    public bool HasTagError => !string.IsNullOrWhiteSpace(TagErrorMessage);
    public string? TagErrorMessage { get => _tagErrorMessage; private set { if (SetField(ref _tagErrorMessage, value)) OnPropertyChanged(nameof(HasTagError)); } }
    public string TagInput
    {
        get => _tagInput;
        set
        {
            if (SetField(ref _tagInput, value))
            {
                RefreshSuggestions();
            }
        }
    }
    public int SelectedSuggestionIndex
    {
        get => _selectedSuggestionIndex;
        private set => SetField(ref _selectedSuggestionIndex, value);
    }
    public IReadOnlyList<TagSummary> AppliedTags { get => _appliedTags; private set => SetField(ref _appliedTags, value); }
    public IReadOnlyList<TagSummary> SuggestedTags { get => _suggestedTags; private set => SetField(ref _suggestedTags, value); }
    public bool CanEditTags => SelectedListing is { IsArchived: false, IsEffectivelyActive: true } && _tagService is not null && !IsBusyTags;
    public bool HasAppliedTags => AppliedTags.Count > 0;
    public bool HasSuggestions => SuggestedTags.Count > 0;

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
    public ICommand ApplyTagSuggestionCommand { get; }
    public ICommand ApplyOrCreateTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand ClearTagInputCommand { get; }
    public ICommand SelectPreviousSuggestionCommand { get; }
    public ICommand SelectNextSuggestionCommand { get; }

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
        TagErrorMessage = null;
        TagInput = string.Empty;
        RaiseState();
        Run(LoadAppliedTagsAsync());
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
        OnPropertyChanged(nameof(CanEditTags));
    }

    private async Task LoadAppliedTagsAsync()
    {
        if (_tagService is null || SelectedListing is not { } listing)
        {
            AppliedTags = [];
            SuggestedTags = [];
            return;
        }

        try
        {
            var tagIds = (await _tagService.GetListingTagsAsync(listing.Id, CancellationToken.None)).ToHashSet();
            var vocabulary = await _tagService.GetActiveTagVocabularyAsync(listing.StoreId, CancellationToken.None);
            var archived = vocabulary.Where(tag => tagIds.Contains(tag.Id)).ToArray();
            if (archived.Length < tagIds.Count)
            {
                var state = await _tagService.LoadAsync(listing.StoreId, CancellationToken.None);
                var missing = state.ArchivedTags.Where(tag => tagIds.Contains(tag.Id));
                archived = [.. archived, .. missing];
            }
            AppliedTags = archived.OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            RefreshSuggestions();
        }
        catch (Exception ex)
        {
            TagErrorMessage = ex.Message;
        }
    }

    private void RefreshSuggestions()
    {
        if (_tagService is null || SelectedListing is null)
        {
            SuggestedTags = [];
            SelectedSuggestionIndex = -1;
            return;
        }

        var input = (TagInput ?? string.Empty).Trim();
        SuggestedTags = AppliedTags.Count == 0 && string.IsNullOrEmpty(input)
            ? CurrentStoreVocabulary()
            : CurrentStoreVocabulary()
                .Where(tag => !AppliedTags.Any(applied => applied.Id == tag.Id))
                .Where(tag => string.IsNullOrEmpty(input) || tag.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToArray();
        SelectedSuggestionIndex = SuggestedTags.Count > 0 ? 0 : -1;
    }

    private IReadOnlyList<TagSummary> _cachedVocabulary = [];

    private IReadOnlyList<TagSummary> CurrentStoreVocabulary()
    {
        if (SelectedListing is { } listing && _tagService is not null)
        {
            try
            {
                _cachedVocabulary = _tagService.GetActiveTagVocabularyAsync(listing.StoreId, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                _cachedVocabulary = [];
            }
        }
        return _cachedVocabulary;
    }

    private async Task ApplyTagByIdAsync(Guid tagId)
    {
        if (!CanEditTags || SelectedListing is not { } listing || _tagService is null)
        {
            TagErrorMessage = "Restore the listing or its parent topic before editing tags.";
            return;
        }
        IsBusyTags = true;
        TagErrorMessage = null;
        try
        {
            var result = await _tagService.ApplyTagAsync(new ApplyTagRequest(listing.Id, tagId), CancellationToken.None);
            if (!result.Succeeded)
            {
                TagErrorMessage = result.Error;
            }
            await LoadAppliedTagsAsync();
        }
        catch (Exception ex)
        {
            TagErrorMessage = ex.Message;
        }
        finally
        {
            IsBusyTags = false;
        }
        TagInput = string.Empty;
    }

    private async Task ApplyOrCreateTagFromInputAsync()
    {
        if (!CanEditTags || SelectedListing is not { } listing || _tagService is null)
        {
            TagErrorMessage = "Restore the listing or its parent topic before editing tags.";
            return;
        }
        var name = (TagInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (SelectedSuggestionIndex is int index and >= 0 && index < SuggestedTags.Count)
        {
            await ApplyTagByIdAsync(SuggestedTags[index].Id);
            return;
        }

        IsBusyTags = true;
        TagErrorMessage = null;
        try
        {
            var result = await _tagService.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, name), CancellationToken.None);
            if (!result.Succeeded)
            {
                TagErrorMessage = result.Error;
            }
            await LoadAppliedTagsAsync();
        }
        catch (Exception ex)
        {
            TagErrorMessage = ex.Message;
        }
        finally
        {
            IsBusyTags = false;
        }
        TagInput = string.Empty;
    }

    private async Task RemoveTagAsync(Guid tagId)
    {
        if (!CanEditTags || SelectedListing is not { } listing || _tagService is null)
        {
            TagErrorMessage = "Restore the listing or its parent topic before editing tags.";
            return;
        }
        IsBusyTags = true;
        TagErrorMessage = null;
        try
        {
            var result = await _tagService.RemoveTagAsync(new RemoveTagRequest(listing.Id, tagId), CancellationToken.None);
            if (!result.Succeeded)
            {
                TagErrorMessage = result.Error;
            }
            await LoadAppliedTagsAsync();
        }
        catch (Exception ex)
        {
            TagErrorMessage = ex.Message;
        }
        finally
        {
            IsBusyTags = false;
        }
    }

    private void ClearTagInput()
    {
        TagInput = string.Empty;
        TagErrorMessage = null;
    }

    private void MoveSuggestion(int delta)
    {
        if (SuggestedTags.Count == 0) return;
        var next = Math.Clamp(SelectedSuggestionIndex + delta, 0, SuggestedTags.Count - 1);
        SelectedSuggestionIndex = next;
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
