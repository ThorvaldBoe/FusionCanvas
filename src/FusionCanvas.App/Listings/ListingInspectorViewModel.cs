using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Listings;

public sealed class ListingInspectorLifecycleEventArgs(ListingManagementResult result, bool deleted) : EventArgs
{
    public ListingManagementResult Result { get; } = result;
    public bool Deleted { get; } = deleted;
}

public sealed class ListingInspectorViewModel : INotifyPropertyChanged
{
    private readonly IListingInspectorService _service;
    private readonly IListingManagementService _listingManagement;
    private ListingInspectorState? _state;
    private Guid? _loadedListingId;
    private WorkflowStage _currentStage = WorkflowStage.Idea;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _idea = string.Empty;
    private string _audience = string.Empty;
    private string _phrase = string.Empty;
    private string _graphicDirection = string.Empty;
    private string _notes = string.Empty;
    private string _tagInput = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _archiveConfirmationVisible;
    private bool _deleteConfirmationVisible;

    private string _originalTitle = string.Empty;
    private string _originalDescription = string.Empty;
    private string _originalIdea = string.Empty;
    private string _originalAudience = string.Empty;
    private string _originalPhrase = string.Empty;
    private string _originalGraphicDirection = string.Empty;
    private string _originalNotes = string.Empty;
    private IReadOnlyList<string> _originalTagNames = [];

    public ListingInspectorViewModel(IListingInspectorService service, IListingManagementService listingManagement)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _listingManagement = listingManagement ?? throw new ArgumentNullException(nameof(listingManagement));
        AddTagCommand = new RelayCommand(_ => AddTag());
        RemoveTagCommand = new RelayCommand(parameter =>
        {
            if (parameter is string name)
            {
                RemoveTag(name);
            }
        });
        RequestArchiveCommand = new RelayCommand(_ => ArchiveConfirmationVisible = true, () => CanArchive);
        ConfirmArchiveCommand = new RelayCommand(_ => Run(ConfirmArchiveAsync()));
        CancelArchiveCommand = new RelayCommand(_ => ArchiveConfirmationVisible = false);
        RestoreCommand = new RelayCommand(_ => Run(RestoreAsync()), () => CanRestore);
        RequestDeleteCommand = new RelayCommand(_ => DeleteConfirmationVisible = true, () => CanDelete);
        ConfirmDeleteCommand = new RelayCommand(_ => Run(ConfirmDeleteAsync()));
        CancelDeleteCommand = new RelayCommand(_ => DeleteConfirmationVisible = false);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? Saved;

    public event EventHandler<ListingInspectorLifecycleEventArgs>? LifecycleChanged;

    public ObservableCollection<string> TagDraft { get; } = [];

    public ListingInspectorState? State
    {
        get => _state;
        private set
        {
            if (SetField(ref _state, value))
            {
                OnPropertyChanged(nameof(HasState));
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(InactiveNotice));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(StageLabel));
                OnPropertyChanged(nameof(DisplayPath));
                OnPropertyChanged(nameof(Tags));
                OnPropertyChanged(nameof(Assets));
                OnPropertyChanged(nameof(AvailableTagNames));
                OnPropertyChanged(nameof(HasTags));
                OnPropertyChanged(nameof(HasAssets));
                OnPropertyChanged(nameof(HasCreativeFields));
                OnPropertyChanged(nameof(CanEdit));
                RaiseActionProperties();
            }
        }
    }

    public Guid? LoadedListingId
    {
        get => _loadedListingId;
        private set => SetField(ref _loadedListingId, value);
    }

    public bool HasState => _state is not null;

    public bool IsReadOnly => _state is { IsReadOnly: true };

    public bool CanEdit => _state is not null && !IsReadOnly && !IsBusy;

    public string InactiveNotice => IsReadOnly
        ? "This listing is archived or inactive. Restore it to edit its details."
        : string.Empty;

    public string StatusLabel => _state?.Status.ToString() ?? "No status";

    public string StageLabel => WorkflowStages.GetDisplayName(_currentStage);

    public string DisplayPath => _state?.DisplayPath ?? string.Empty;

    public IReadOnlyList<ListingInspectorTagEntry> Tags => _state?.Tags ?? [];

    public IReadOnlyList<ListingInspectorAssetEntry> Assets => _state?.Assets ?? [];

    public IReadOnlyList<string> AvailableTagNames => _state?.AvailableTagNames ?? [];

    public bool HasCreativeFields => _state?.Creative.HasAny == true;

    public bool HasTags => Tags.Count > 0;

    public bool HasAssets => Assets.Count > 0;

    public string Title
    {
        get => _title;
        set
        {
            if (SetField(ref _title, value))
            {
                RaiseDirty();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetField(ref _description, value))
            {
                RaiseDirty();
            }
        }
    }

    public string Idea
    {
        get => _idea;
        set
        {
            if (SetField(ref _idea, value))
            {
                RaiseDirty();
            }
        }
    }

    public string Audience
    {
        get => _audience;
        set
        {
            if (SetField(ref _audience, value))
            {
                RaiseDirty();
            }
        }
    }

    public string Phrase
    {
        get => _phrase;
        set
        {
            if (SetField(ref _phrase, value))
            {
                RaiseDirty();
            }
        }
    }

    public string GraphicDirection
    {
        get => _graphicDirection;
        set
        {
            if (SetField(ref _graphicDirection, value))
            {
                RaiseDirty();
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            if (SetField(ref _notes, value))
            {
                RaiseDirty();
            }
        }
    }

    public string TagInput
    {
        get => _tagInput;
        set => SetField(ref _tagInput, value);
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                RaiseDirty();
                RaiseActionProperties();
            }
        }
    }

    public bool ArchiveConfirmationVisible
    {
        get => _archiveConfirmationVisible;
        private set => SetField(ref _archiveConfirmationVisible, value);
    }

    public bool DeleteConfirmationVisible
    {
        get => _deleteConfirmationVisible;
        private set => SetField(ref _deleteConfirmationVisible, value);
    }

    public bool HasUnsavedChanges =>
        Title != _originalTitle
        || Description != _originalDescription
        || Idea != _originalIdea
        || Audience != _originalAudience
        || Phrase != _originalPhrase
        || GraphicDirection != _originalGraphicDirection
        || Notes != _originalNotes
        || !TagDraft.SequenceEqual(_originalTagNames);

    public bool CanArchive => _state is { IsArchived: false, IsEffectivelyActive: true } && !IsBusy;

    public bool CanRestore => _state is { IsArchived: true } && !IsBusy;

    public bool CanDelete => _state is not null && !IsBusy;

    public bool EmphasizesIdea => _currentStage == WorkflowStage.Idea;
    public bool EmphasizesConcept => _currentStage == WorkflowStage.Concept;
    public bool EmphasizesDesign => _currentStage == WorkflowStage.Design;
    public bool EmphasizesListing => _currentStage == WorkflowStage.Listing;

    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand RequestArchiveCommand { get; }
    public ICommand ConfirmArchiveCommand { get; }
    public ICommand CancelArchiveCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RequestDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }

    public async Task LoadAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            Clear();
            return;
        }

        ApplyState(state);
        LoadedListingId = listingId;
    }

    public void ApplyStage(WorkflowStage stage)
    {
        if (_currentStage == stage)
        {
            return;
        }

        _currentStage = stage;
        RaiseStageProperties();
    }

    public void Clear()
    {
        State = null;
        LoadedListingId = null;
        _currentStage = WorkflowStage.Idea;
        Title = string.Empty;
        Description = string.Empty;
        Idea = string.Empty;
        Audience = string.Empty;
        Phrase = string.Empty;
        GraphicDirection = string.Empty;
        Notes = string.Empty;
        TagDraft.Clear();
        TagInput = string.Empty;
        ErrorMessage = null;
        ArchiveConfirmationVisible = false;
        DeleteConfirmationVisible = false;
        ResetBaselines();
        RaiseStageProperties();
    }

    public async Task CommitEditsAsync(CancellationToken cancellationToken = default)
    {
        if (_state is not { } state || IsBusy || IsReadOnly || !HasUnsavedChanges)
        {
            return;
        }

        string? titleError = null;
        var normalizedTitle = Title.Trim();
        if (normalizedTitle.Length == 0 || normalizedTitle.Contains('\n') || normalizedTitle.Contains('\r'))
        {
            Title = _originalTitle;
            titleError = "The working title must be a non-empty single line. It was reverted to its last saved value.";
            if (!HasUnsavedChanges)
            {
                ErrorMessage = titleError;
                return;
            }
        }

        IsBusy = true;
        var result = await _service.SaveAsync(new ListingInspectorSaveRequest(
            state.Id,
            Title,
            Description,
            Idea,
            Audience,
            Phrase,
            GraphicDirection,
            Notes,
            [.. TagDraft]), cancellationToken).ConfigureAwait(false);
        IsBusy = false;

        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyState(result.State!);
        ErrorMessage = titleError;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyState(ListingInspectorState state)
    {
        State = state;
        _currentStage = state.Stage;
        Title = state.Title;
        Description = state.Description ?? string.Empty;
        Idea = state.Creative.Idea ?? string.Empty;
        Audience = state.Creative.Audience ?? string.Empty;
        Phrase = state.Creative.Phrase ?? string.Empty;
        GraphicDirection = state.Creative.GraphicDirection ?? string.Empty;
        Notes = state.Notes ?? string.Empty;
        TagDraft.Clear();
        foreach (var tag in state.Tags)
        {
            TagDraft.Add(tag.Name);
        }
        TagInput = string.Empty;
        ErrorMessage = null;
        ArchiveConfirmationVisible = false;
        DeleteConfirmationVisible = false;
        ResetBaselines();
        RaiseStageProperties();
    }

    private void ResetBaselines()
    {
        _originalTitle = Title;
        _originalDescription = Description;
        _originalIdea = Idea;
        _originalAudience = Audience;
        _originalPhrase = Phrase;
        _originalGraphicDirection = GraphicDirection;
        _originalNotes = Notes;
        _originalTagNames = [.. TagDraft];
        RaiseDirty();
    }

    private void AddTag()
    {
        if (!CanEdit)
        {
            return;
        }

        var name = TagInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\n') || name.Contains('\r'))
        {
            ErrorMessage = "Tag names must be a non-empty single line.";
            return;
        }

        if (!TagDraft.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            TagDraft.Add(name);
        }

        TagInput = string.Empty;
        ErrorMessage = null;
        Run(CommitEditsAsync());
    }

    private void RemoveTag(string name)
    {
        if (!CanEdit)
        {
            return;
        }

        if (TagDraft.Remove(name))
        {
            Run(CommitEditsAsync());
        }
    }

    private async Task ConfirmArchiveAsync()
    {
        if (_state is not { } state || IsBusy)
        {
            return;
        }

        ArchiveConfirmationVisible = false;
        IsBusy = true;
        var result = await _listingManagement.ArchiveListingAsync(state.Id).ConfigureAwait(false);
        IsBusy = false;
        ApplyLifecycleResult(result, deleted: false);
    }

    private async Task RestoreAsync()
    {
        if (_state is not { } state || IsBusy)
        {
            return;
        }

        IsBusy = true;
        var result = await _listingManagement.RestoreListingAsync(new ListingManagementRestoreRequest(state.Id)).ConfigureAwait(false);
        IsBusy = false;
        ApplyLifecycleResult(result, deleted: false);
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_state is not { } state || IsBusy)
        {
            return;
        }

        DeleteConfirmationVisible = false;
        IsBusy = true;
        var result = await _listingManagement.DeleteListingAsync(
            new ListingManagementDeleteRequest(state.Id, ConfirmPermanentDeletion: true)).ConfigureAwait(false);
        IsBusy = false;
        ApplyLifecycleResult(result, deleted: result.Succeeded);
    }

    private void ApplyLifecycleResult(ListingManagementResult result, bool deleted)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ErrorMessage = null;
        LifecycleChanged?.Invoke(this, new ListingInspectorLifecycleEventArgs(result, deleted));
    }

    private void RaiseStageProperties()
    {
        OnPropertyChanged(nameof(StageLabel));
        OnPropertyChanged(nameof(EmphasizesIdea));
        OnPropertyChanged(nameof(EmphasizesConcept));
        OnPropertyChanged(nameof(EmphasizesDesign));
        OnPropertyChanged(nameof(EmphasizesListing));
    }

    private void RaiseActionProperties()
    {
        OnPropertyChanged(nameof(CanArchive));
        OnPropertyChanged(nameof(CanRestore));
        OnPropertyChanged(nameof(CanDelete));
    }

    private void RaiseDirty()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    private static void Run(Task task) => _ = task;
}
