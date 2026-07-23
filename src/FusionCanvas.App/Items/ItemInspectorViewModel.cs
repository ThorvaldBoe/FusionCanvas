using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Items;

public sealed class ItemInspectorLifecycleEventArgs(ItemManagementResult result, bool deleted) : EventArgs
{
    public ItemManagementResult Result { get; } = result;
    public bool Deleted { get; } = deleted;
}

public sealed class ItemInspectorViewModel : INotifyPropertyChanged
{
    private readonly IItemInspectorService _service;
    private readonly IItemManagementService _itemManagement;
    private readonly ITagManagementService? _tagManagement;
    private ItemInspectorState? _state;
    private Guid? _loadedItemId;
    private WorkflowStage _currentStage = WorkflowStage.Idea;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _idea = string.Empty;
    private string _conceptIdea = string.Empty;
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
    private string _originalConceptIdea = string.Empty;
    private string _originalPhrase = string.Empty;
    private string _originalGraphicDirection = string.Empty;
    private string _originalNotes = string.Empty;
    private IReadOnlyList<string> _originalTagNames = [];

    public ItemInspectorViewModel(
        IItemInspectorService service,
        IItemManagementService itemManagement,
        ITagManagementService? tagManagement = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _itemManagement = itemManagement ?? throw new ArgumentNullException(nameof(itemManagement));
        _tagManagement = tagManagement;
        SaveCommand = new RelayCommand(_ => Run(CommitEditsAsync()));
        DiscardCommand = new RelayCommand(_ => DiscardChanges());
        AddTagCommand = new RelayCommand(_ => Run(AddTagAsync()));
        RemoveTagCommand = new RelayCommand(parameter =>
        {
            if (parameter is string name)
            {
                Run(RemoveTagAsync(name));
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

    public event EventHandler? TagsChanged;

    public event EventHandler<ItemInspectorLifecycleEventArgs>? LifecycleChanged;

    public ObservableCollection<string> TagDraft { get; } = [];

    public ItemInspectorState? State
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
                OnPropertyChanged(nameof(CanEditShared));
                OnPropertyChanged(nameof(CanEditStage));
                OnPropertyChanged(nameof(StageReadOnlyReason));
                RaiseActionProperties();
            }
        }
    }

    public Guid? LoadedItemId
    {
        get => _loadedItemId;
        private set => SetField(ref _loadedItemId, value);
    }

    public bool HasState => _state is not null;

    public bool IsReadOnly => _state is { IsReadOnly: true };

    public bool CanEdit => CanEditShared;

    public bool CanEditShared => _state is { IsEffectivelyActive: true } && !IsBusy;

    public bool CanEditStage =>
        _state is { IsEffectivelyActive: true } state
        && !IsBusy
        && state.Stage == _currentStage
        && state.Status is not (ItemStatus.Published or ItemStatus.Rejected);

    public string StageReadOnlyReason
    {
        get
        {
            if (_state is not { } state)
            {
                return string.Empty;
            }

            if (!state.IsEffectivelyActive)
            {
                return "Restore the item before editing its content.";
            }

            return ItemWorkflowPolicy.CanEditStage(
                new Item(state.Id, Guid.Empty, null, null, state.Title, state.Description, state.Status, state.Stage,
                    state.IsArchived, state.UpdatedAt, state.UpdatedAt, "{}"),
                _currentStage).Reason;
        }
    }

    public string InactiveNotice => IsReadOnly
        ? "This item is archived or inactive. Restore it to edit its details."
        : string.Empty;

    public string StatusLabel => _state?.Status.ToString() ?? "No status";

    public string StageLabel => WorkflowStages.GetDisplayName(_currentStage);

    public string DisplayPath => _state?.DisplayPath ?? string.Empty;

    public IReadOnlyList<ItemInspectorTagEntry> Tags => _state?.Tags ?? [];

    public IReadOnlyList<ItemInspectorAssetEntry> Assets => _state?.Assets ?? [];

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

    public string ConceptIdea
    {
        get => _conceptIdea;
        set
        {
            if (SetField(ref _conceptIdea, value))
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
                OnPropertyChanged(nameof(CanEditShared));
                OnPropertyChanged(nameof(CanEditStage));
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
        || Notes != _originalNotes
        || HasCurrentStageDraftChanges;

    private bool HasCurrentStageDraftChanges =>
        _state?.Stage == _currentStage
        && _currentStage switch
        {
            WorkflowStage.Idea => Idea != _originalIdea,
            WorkflowStage.Concept => ConceptIdea != _originalConceptIdea
                || Phrase != _originalPhrase
                || GraphicDirection != _originalGraphicDirection,
            WorkflowStage.Design or WorkflowStage.Listing => false,
            _ => false
        };

    public bool CanArchive => _state is { IsArchived: false, IsEffectivelyActive: true } && !IsBusy;

    public bool CanRestore => _state is { IsArchived: true } && !IsBusy;

    public bool CanDelete => _state is not null && !IsBusy;

    public bool EmphasizesIdea => _currentStage == WorkflowStage.Idea;
    public bool EmphasizesConcept => _currentStage == WorkflowStage.Concept;
    public bool EmphasizesDesign => _currentStage == WorkflowStage.Design;
    public bool EmphasizesListing => _currentStage == WorkflowStage.Listing;

    public ICommand SaveCommand { get; }
    public ICommand DiscardCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand RequestArchiveCommand { get; }
    public ICommand ConfirmArchiveCommand { get; }
    public ICommand CancelArchiveCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RequestDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }

    public async Task LoadAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(itemId, cancellationToken).ConfigureAwait(true);
        if (state is null)
        {
            Clear();
            return;
        }

        ApplyState(state);
        LoadedItemId = itemId;
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
        LoadedItemId = null;
        _currentStage = WorkflowStage.Idea;
        Title = string.Empty;
        Description = string.Empty;
        Idea = string.Empty;
        ConceptIdea = string.Empty;
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
        if (_state is not { } state || IsBusy || !CanEditShared || !HasUnsavedChanges)
        {
            return;
        }

        var normalizedTitle = Title.Trim();
        if (normalizedTitle.Contains('\n') || normalizedTitle.Contains('\r'))
        {
            ErrorMessage = "The working title must be a single line.";
            return;
        }

        IsBusy = true;
        var result = await _service.SaveStageAsync(new ItemStageAwareSaveRequest(
            state.Id,
            state.Stage,
            Title,
            Notes,
            CreateStagePayload(state.Stage),
            [.. TagDraft]), cancellationToken).ConfigureAwait(true);
        IsBusy = false;

        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyState(result.State!);
        ErrorMessage = null;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    public void DiscardChanges()
    {
        Title = _originalTitle;
        Description = _originalDescription;
        Idea = _originalIdea;
        ConceptIdea = _originalConceptIdea;
        Phrase = _originalPhrase;
        GraphicDirection = _originalGraphicDirection;
        Notes = _originalNotes;
        ErrorMessage = null;
        RaiseDirty();
    }

    private void ApplyState(ItemInspectorState state)
    {
        State = state;
        _currentStage = state.Stage;
        Title = state.Title;
        Description = state.Description ?? string.Empty;
        Idea = state.Creative.Idea ?? string.Empty;
        Audience = state.Creative.Audience ?? string.Empty;
        ConceptIdea = state.Creative.ConceptIdea ?? string.Empty;
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
        _originalConceptIdea = ConceptIdea;
        _originalPhrase = Phrase;
        _originalGraphicDirection = GraphicDirection;
        _originalNotes = Notes;
        _originalTagNames = [.. TagDraft];
        RaiseDirty();
    }

    private async Task AddTagAsync()
    {
        if (!CanEditShared || _state is not { } state)
        {
            return;
        }

        var name = TagInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\n') || name.Contains('\r'))
        {
            ErrorMessage = "Tag names must be a non-empty single line.";
            return;
        }

        if (_tagManagement is null)
        {
            if (!TagDraft.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                TagDraft.Add(name);
            }
            TagInput = string.Empty;
            await CommitLegacyTagChangeAsync().ConfigureAwait(true);
            return;
        }

        IsBusy = true;
        var result = await _tagManagement.ApplyOrCreateTagAsync(
            new ApplyOrCreateTagRequest(state.Id, name)).ConfigureAwait(true);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await RefreshTagsPreservingDraftAsync(state.Id).ConfigureAwait(true);
        TagInput = string.Empty;
        ErrorMessage = null;
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RemoveTagAsync(string name)
    {
        if (!CanEditShared || _state is not { } state)
        {
            return;
        }

        if (_tagManagement is null)
        {
            if (TagDraft.Remove(name))
            {
                await CommitLegacyTagChangeAsync().ConfigureAwait(true);
            }
            return;
        }

        var tag = state.Tags.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        if (tag is null)
        {
            return;
        }

        IsBusy = true;
        var result = await _tagManagement.RemoveTagAsync(new RemoveTagRequest(state.Id, tag.Id)).ConfigureAwait(true);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        await RefreshTagsPreservingDraftAsync(state.Id).ConfigureAwait(true);
        ErrorMessage = null;
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ConfirmArchiveAsync()
    {
        if (_state is not { } state || IsBusy)
        {
            return;
        }

        ArchiveConfirmationVisible = false;
        IsBusy = true;
        var result = await _itemManagement.ArchiveItemAsync(state.Id).ConfigureAwait(true);
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
        var result = await _itemManagement.RestoreItemAsync(new ItemManagementRestoreRequest(state.Id)).ConfigureAwait(true);
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
        var result = await _itemManagement.DeleteItemAsync(
            new ItemManagementDeleteRequest(state.Id, ConfirmPermanentDeletion: true)).ConfigureAwait(true);
        IsBusy = false;
        ApplyLifecycleResult(result, deleted: result.Succeeded);
    }

    private void ApplyLifecycleResult(ItemManagementResult result, bool deleted)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ErrorMessage = null;
        LifecycleChanged?.Invoke(this, new ItemInspectorLifecycleEventArgs(result, deleted));
    }

    private void RaiseStageProperties()
    {
        OnPropertyChanged(nameof(StageLabel));
        OnPropertyChanged(nameof(EmphasizesIdea));
        OnPropertyChanged(nameof(EmphasizesConcept));
        OnPropertyChanged(nameof(EmphasizesDesign));
        OnPropertyChanged(nameof(EmphasizesListing));
        OnPropertyChanged(nameof(CanEditStage));
        OnPropertyChanged(nameof(StageReadOnlyReason));
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

    private ItemStageSavePayload CreateStagePayload(WorkflowStage stage) =>
        stage switch
        {
            WorkflowStage.Idea => new(stage, Idea, null, null, null),
            WorkflowStage.Concept => new(stage, null, ConceptIdea, Phrase, GraphicDirection),
            WorkflowStage.Design or WorkflowStage.Listing => new(stage, null, null, null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage.")
        };

    private async Task RefreshTagsPreservingDraftAsync(Guid itemId)
    {
        var state = await _service.LoadAsync(itemId).ConfigureAwait(true);
        if (state is null)
        {
            return;
        }

        State = state;
        TagDraft.Clear();
        foreach (var tag in state.Tags)
        {
            TagDraft.Add(tag.Name);
        }
        _originalTagNames = [.. TagDraft];
        OnPropertyChanged(nameof(HasTags));
    }

    private async Task CommitLegacyTagChangeAsync()
    {
        if (_state is not { } state)
        {
            return;
        }

        var result = await _service.SaveStageAsync(new ItemStageAwareSaveRequest(
            state.Id,
            state.Stage,
            _originalTitle,
            _originalNotes,
            CreateStagePayload(state.Stage),
            [.. TagDraft])).ConfigureAwait(true);
        if (result.Succeeded)
        {
            _originalTagNames = [.. TagDraft];
            State = result.State;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }
}
