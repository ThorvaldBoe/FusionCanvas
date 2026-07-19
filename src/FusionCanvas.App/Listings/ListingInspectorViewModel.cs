using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Listings;

public sealed class ListingInspectorViewModel : INotifyPropertyChanged
{
    private readonly IListingInspectorService _service;
    private ListingInspectorState? _state;
    private Guid? _loadedListingId;
    private WorkflowStage _currentStage = WorkflowStage.Idea;

    private string _title = string.Empty;
    private string _idea = string.Empty;
    private string _phrase = string.Empty;
    private string _graphicDirection = string.Empty;
    private string _notes = string.Empty;
    private string _tagInput = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _discardPromptVisible;

    private string _originalTitle = string.Empty;
    private string _originalIdea = string.Empty;
    private string _originalPhrase = string.Empty;
    private string _originalGraphicDirection = string.Empty;
    private string _originalNotes = string.Empty;
    private IReadOnlyList<string> _originalTagNames = [];
    private Action? _pendingLeave;

    public ListingInspectorViewModel(IListingInspectorService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        SaveCommand = new RelayCommand(_ => Run(SaveAsync()));
        RevertCommand = new RelayCommand(_ => Revert(), () => HasUnsavedChanges && !IsBusy);
        AddTagCommand = new RelayCommand(_ => AddTag());
        RemoveTagCommand = new RelayCommand(parameter =>
        {
            if (parameter is string name)
            {
                TagDraft.Remove(name);
            }
        });
        SaveAndContinueCommand = new RelayCommand(_ => Run(SaveAndContinueAsync()), () => DiscardPromptVisible && !IsBusy);
        DiscardAndContinueCommand = new RelayCommand(_ => DiscardAndContinue(), () => DiscardPromptVisible);
        KeepEditingCommand = new RelayCommand(_ => KeepEditing(), () => DiscardPromptVisible);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? Saved;

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
        ? "This listing is archived or inactive. Restore it from the lifecycle surface to edit."
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
            }
        }
    }

    public bool DiscardPromptVisible
    {
        get => _discardPromptVisible;
        private set => SetField(ref _discardPromptVisible, value);
    }

    public bool HasUnsavedChanges =>
        Title != _originalTitle
        || Idea != _originalIdea
        || Phrase != _originalPhrase
        || GraphicDirection != _originalGraphicDirection
        || Notes != _originalNotes
        || !TagDraft.SequenceEqual(_originalTagNames);

    public bool CanSave => CanEdit && HasUnsavedChanges;

    public bool EmphasizesIdea => _currentStage == WorkflowStage.Idea;
    public bool EmphasizesConcept => _currentStage == WorkflowStage.Concept;
    public bool EmphasizesDesign => _currentStage == WorkflowStage.Design;
    public bool EmphasizesListing => _currentStage == WorkflowStage.Listing;

    public ICommand SaveCommand { get; }
    public ICommand RevertCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand SaveAndContinueCommand { get; }
    public ICommand DiscardAndContinueCommand { get; }
    public ICommand KeepEditingCommand { get; }

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
        OnPropertyChanged(nameof(StageLabel));
        OnPropertyChanged(nameof(EmphasizesIdea));
        OnPropertyChanged(nameof(EmphasizesConcept));
        OnPropertyChanged(nameof(EmphasizesDesign));
        OnPropertyChanged(nameof(EmphasizesListing));
    }

    public void Clear()
    {
        State = null;
        LoadedListingId = null;
        _currentStage = WorkflowStage.Idea;
        Title = string.Empty;
        Idea = string.Empty;
        Phrase = string.Empty;
        GraphicDirection = string.Empty;
        Notes = string.Empty;
        TagDraft.Clear();
        TagInput = string.Empty;
        ErrorMessage = null;
        _pendingLeave = null;
        DiscardPromptVisible = false;
        ResetBaselines();
        OnPropertyChanged(nameof(StageLabel));
        OnPropertyChanged(nameof(EmphasizesIdea));
        OnPropertyChanged(nameof(EmphasizesConcept));
        OnPropertyChanged(nameof(EmphasizesDesign));
        OnPropertyChanged(nameof(EmphasizesListing));
    }

    public void RequestLeave(Action proceed)
    {
        ArgumentNullException.ThrowIfNull(proceed);
        if (DiscardPromptVisible)
        {
            _pendingLeave = proceed;
            return;
        }

        if (!HasUnsavedChanges)
        {
            proceed();
            return;
        }

        _pendingLeave = proceed;
        DiscardPromptVisible = true;
    }

    private void ApplyState(ListingInspectorState state)
    {
        State = state;
        _currentStage = state.Stage;
        Title = state.Title;
        Idea = state.Creative.Idea ?? string.Empty;
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
        ResetBaselines();
        OnPropertyChanged(nameof(StageLabel));
        OnPropertyChanged(nameof(EmphasizesIdea));
        OnPropertyChanged(nameof(EmphasizesConcept));
        OnPropertyChanged(nameof(EmphasizesDesign));
        OnPropertyChanged(nameof(EmphasizesListing));
    }

    private void ResetBaselines()
    {
        _originalTitle = Title;
        _originalIdea = Idea;
        _originalPhrase = Phrase;
        _originalGraphicDirection = GraphicDirection;
        _originalNotes = Notes;
        _originalTagNames = [.. TagDraft];
        RaiseDirty();
    }

    private void Revert()
    {
        if (_state is null)
        {
            return;
        }

        Title = _state.Title;
        Idea = _state.Creative.Idea ?? string.Empty;
        Phrase = _state.Creative.Phrase ?? string.Empty;
        GraphicDirection = _state.Creative.GraphicDirection ?? string.Empty;
        Notes = _state.Notes ?? string.Empty;
        TagDraft.Clear();
        foreach (var tag in _state.Tags)
        {
            TagDraft.Add(tag.Name);
        }
        TagInput = string.Empty;
        ErrorMessage = null;
        ResetBaselines();
    }

    private void AddTag()
    {
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
        RaiseDirty();
    }

    private async Task SaveAsync()
    {
        if (_state is not { } state || !CanSave)
        {
            return;
        }

        IsBusy = true;
        var result = await _service.SaveAsync(new ListingInspectorSaveRequest(
            state.Id,
            Title,
            Idea,
            Phrase,
            GraphicDirection,
            Notes,
            [.. TagDraft])).ConfigureAwait(false);
        IsBusy = false;

        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyState(result.State!);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAndContinueAsync()
    {
        await SaveAsync().ConfigureAwait(false);
        if (HasError)
        {
            return;
        }

        ContinuePendingLeave();
    }

    private void DiscardAndContinue()
    {
        Revert();
        ContinuePendingLeave();
    }

    private void KeepEditing()
    {
        _pendingLeave = null;
        DiscardPromptVisible = false;
    }

    private void ContinuePendingLeave()
    {
        var pending = _pendingLeave;
        _pendingLeave = null;
        DiscardPromptVisible = false;
        pending?.Invoke();
    }

    private void RaiseDirty()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanSave));
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
