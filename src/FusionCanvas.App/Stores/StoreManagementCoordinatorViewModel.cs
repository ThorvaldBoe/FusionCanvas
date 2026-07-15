using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Stores;

public sealed record StoreSelectorEntry(StoreSummary Store, bool IsSelected)
{
    public Guid Id => Store.Id;

    public string Name => Store.Name;
}

public sealed class StoreManagementViewModel : INotifyPropertyChanged
{
    private enum PendingEditorAction
    {
        None,
        SelectStore,
        StartNewStore,
        CloseEditor
    }

    private sealed record EditorState(
        string Name,
        string Description,
        string Notes,
        string TargetMarket,
        string BrandDirection,
        string PlanningContext);

    private readonly IStoreManagementService _service;
    private bool _isSelectorExpanded;
    private bool _isStoreEditorOpen;
    private bool _firstStorePromptDismissed;
    private bool _deleteWarningVisible;
    private bool _discardChangesPromptVisible;
    private bool _isCreatingNewStore;
    private Guid? _draftStoreId;
    private StoreSummary? _pendingDeleteStore;
    private StoreSummary? _pendingEditorStore;
    private PendingEditorAction _pendingEditorAction;
    private EditorState _originalEditorState = EmptyEditorState();
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
        ToggleStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = !IsSelectorExpanded);
        ExpandStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = true);
        CollapseStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = false);
        OpenStoreEditorCommand = new RelayCommand(_ => OpenStoreEditor());
        StartCreateStoreCommand = new RelayCommand(_ => StartCreateStore());
        CloseStoreEditorCommand = new RelayCommand(_ => TryCloseStoreEditor());
        AcceptFirstStorePromptCommand = new RelayCommand(_ =>
        {
            _firstStorePromptDismissed = true;
            OpenStoreEditor();
            if (!HasActiveStores)
            {
                BeginCreateStoreDraft();
            }

            RaisePromptProperties();
        });
        DeclineFirstStorePromptCommand = new RelayCommand(_ =>
        {
            _firstStorePromptDismissed = true;
            RaisePromptProperties();
        });
        CreateStoreCommand = new RelayCommand(_ => Run(CreateStoreAsync()));
        SelectStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                Run(SelectStoreAsync(store));
            }
            else if (parameter is StoreSelectorEntry entry)
            {
                Run(SelectStoreAsync(entry.Store));
            }
        });
        EditStoreCommand = new RelayCommand(parameter =>
        {
            if (parameter is StoreSummary store)
            {
                SelectStoreForEditing(store);
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
        RequestDeleteSelectedStoreCommand = new RelayCommand(_ => RequestDeleteSelectedStore());
        ConfirmDeleteStoreCommand = new RelayCommand(_ => Run(ConfirmDeleteStoreAsync()));
        CancelDeleteStoreCommand = new RelayCommand(_ => ClearDeleteWarning());
        ConfirmDiscardChangesCommand = new RelayCommand(_ => ConfirmDiscardChanges());
        KeepEditingCommand = new RelayCommand(_ => ClearDiscardChangesPrompt());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<StoreSummary?>? ActiveStoreChanged;

    public event EventHandler? StoreNameFocusRequested;

    public IReadOnlyList<StoreSummary> ActiveStores { get; private set; } = [];

    public IReadOnlyList<StoreSummary> ArchivedStores { get; private set; } = [];

    public IReadOnlyList<StoreSelectorEntry> SelectorStores { get; private set; } = [];

    public IReadOnlyList<StoreSummary> EditorActiveStores =>
        _isCreatingNewStore && DraftStore() is { } draft
            ? ActiveStores.Concat([draft]).ToArray()
            : ActiveStores;

    public StoreSummary? SelectedStore { get; private set; }

    public bool NeedsFirstStore { get; private set; }

    public bool HasActiveStores => ActiveStores.Count > 0;

    public bool HasArchivedStores => ArchivedStores.Count > 0;

    public bool HasSelectedStore => SelectedStore is not null;

    public bool CanRestoreSelectedStore => SelectedStore is { IsArchived: true };

    public bool HasUnsavedChanges => CurrentEditorState() != _originalEditorState;

    public bool CanSaveSelectedStore => _isCreatingNewStore || (SelectedStore is not null && HasUnsavedChanges);

    public bool CanArchiveSelectedStore => SelectedStore is { IsArchived: false } && !_isCreatingNewStore;

    public bool CanDeleteSelectedStore => SelectedStore is not null && !_isCreatingNewStore;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool ShouldShowFirstStorePrompt => NeedsFirstStore && !_firstStorePromptDismissed && !IsStoreEditorOpen;

    public string SelectorToggleGlyph => IsSelectorExpanded ? "▲" : "▼";

    public string SelectorToggleTooltip => IsSelectorExpanded ? "Collapse stores" : "Expand stores";

    public bool IsSelectorExpanded
    {
        get => _isSelectorExpanded;
        private set
        {
            if (SetField(ref _isSelectorExpanded, value))
            {
                OnPropertyChanged(nameof(IsSelectorCompact));
                OnPropertyChanged(nameof(SelectorToggleGlyph));
                OnPropertyChanged(nameof(SelectorToggleTooltip));
            }
        }
    }

    public bool IsSelectorCompact => !IsSelectorExpanded;

    public bool IsStoreEditorOpen
    {
        get => _isStoreEditorOpen;
        private set
        {
            if (SetField(ref _isStoreEditorOpen, value))
            {
                RaisePromptProperties();
            }
        }
    }

    public bool DeleteWarningVisible
    {
        get => _deleteWarningVisible;
        private set => SetField(ref _deleteWarningVisible, value);
    }

    public bool DiscardChangesPromptVisible
    {
        get => _discardChangesPromptVisible;
        private set => SetField(ref _discardChangesPromptVisible, value);
    }

    public string DeleteWarningMessage => _pendingDeleteStore is null
        ? "Permanent deletion cannot be undone."
        : $"Delete '{_pendingDeleteStore.Name}' permanently? This cannot be undone.";

    public string DiscardChangesMessage => "Discard changes? Unsaved store edits will be lost.";

    public string NewStoreName
    {
        get => _newStoreName;
        set
        {
            if (SetField(ref _newStoreName, value))
            {
                RaiseEditorStateProperties();
                OnPropertyChanged(nameof(EditorActiveStores));
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
                RaiseEditorStateProperties();
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
                RaiseEditorStateProperties();
            }
        }
    }

    public string TargetMarket
    {
        get => _targetMarket;
        set
        {
            if (SetField(ref _targetMarket, value))
            {
                RaiseEditorStateProperties();
            }
        }
    }

    public string BrandDirection
    {
        get => _brandDirection;
        set
        {
            if (SetField(ref _brandDirection, value))
            {
                RaiseEditorStateProperties();
            }
        }
    }

    public string PlanningContext
    {
        get => _planningContext;
        set
        {
            if (SetField(ref _planningContext, value))
            {
                RaiseEditorStateProperties();
            }
        }
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

    public ICommand ToggleStoreSelectorCommand { get; }

    public ICommand ExpandStoreSelectorCommand { get; }

    public ICommand CollapseStoreSelectorCommand { get; }

    public ICommand OpenStoreEditorCommand { get; }

    public ICommand StartCreateStoreCommand { get; }

    public ICommand CloseStoreEditorCommand { get; }

    public ICommand AcceptFirstStorePromptCommand { get; }

    public ICommand DeclineFirstStorePromptCommand { get; }

    public ICommand CreateStoreCommand { get; }

    public ICommand SelectStoreCommand { get; }

    public ICommand EditStoreCommand { get; }

    public ICommand SaveSelectedStoreCommand { get; }

    public ICommand ArchiveSelectedStoreCommand { get; }

    public ICommand RestoreStoreCommand { get; }

    public ICommand RequestDeleteSelectedStoreCommand { get; }

    public ICommand ConfirmDeleteStoreCommand { get; }

    public ICommand CancelDeleteStoreCommand { get; }

    public ICommand ConfirmDiscardChangesCommand { get; }

    public ICommand KeepEditingCommand { get; }

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
        if (result.Succeeded)
        {
            _firstStorePromptDismissed = true;
            ClearDeleteWarning();
            RaisePromptProperties();
        }
    }

    public async Task SelectStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var result = await _service.SelectStoreAsync(store.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    public void SelectStoreForEditing(StoreSummary store)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (_isCreatingNewStore && store.Id == _draftStoreId)
        {
            SelectedStore = DraftStore();
            OnPropertyChanged(nameof(SelectedStore));
            return;
        }

        if (HasUnsavedChanges && SelectedStore?.Id != store.Id)
        {
            RequestDiscardBefore(PendingEditorAction.SelectStore, store);
            return;
        }

        PerformSelectStoreForEditing(store);
    }

    private void PerformSelectStoreForEditing(StoreSummary store)
    {
        _isCreatingNewStore = false;
        _draftStoreId = null;
        SelectedStore = store;
        ApplySelectedStoreFields(store);
        CaptureOriginalEditorState();
        ClearDeleteWarning();
        ClearDiscardChangesPrompt();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
    }

    public void StartCreateStore()
    {
        if (HasUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.StartNewStore);
            return;
        }

        BeginCreateStoreDraft();
    }

    private void BeginCreateStoreDraft()
    {
        _isCreatingNewStore = true;
        _draftStoreId = Guid.NewGuid();
        SelectedStore = DraftStore();
        ErrorMessage = null;
        ClearDeleteWarning();
        ClearDiscardChangesPrompt();
        ClearEditorFields();
        CaptureOriginalEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        StoreNameFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (_isCreatingNewStore)
        {
            var createResult = await _service.CreateStoreAsync(
                new StoreManagementCreateRequest(NewStoreName, CurrentContext()),
                cancellationToken).ConfigureAwait(false);

            if (createResult.Succeeded)
            {
                _isCreatingNewStore = false;
                _draftStoreId = null;
                _firstStorePromptDismissed = true;
            }

            ApplyResult(createResult);
            if (createResult.Succeeded)
            {
                ClearDeleteWarning();
                ClearDiscardChangesPrompt();
                RaisePromptProperties();
            }

            return;
        }

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
        if (_isCreatingNewStore)
        {
            ErrorMessage = "Save the new store before archiving it.";
            return;
        }

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

    public void RequestDeleteSelectedStore()
    {
        if (_isCreatingNewStore)
        {
            ErrorMessage = "Save the new store before deleting it.";
            return;
        }

        if (SelectedStore is null)
        {
            ErrorMessage = "Select a store before deleting.";
            return;
        }

        _pendingDeleteStore = SelectedStore;
        DeleteWarningVisible = true;
        OnPropertyChanged(nameof(DeleteWarningMessage));
    }

    public async Task ConfirmDeleteStoreAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingDeleteStore is null)
        {
            ErrorMessage = "Select a store before deleting.";
            return;
        }

        var result = await _service.DeleteStoreAsync(
            new StoreManagementDeleteRequest(_pendingDeleteStore.Id, ConfirmPermanentDeletion: true),
            cancellationToken).ConfigureAwait(false);
        ErrorMessage = result.Error;
        ApplyState(result.State);
        if (result.Succeeded)
        {
            SelectDefaultStoreForEditing();
        }

        ClearDeleteWarning();
    }

    private void OpenStoreEditor()
    {
        IsStoreEditorOpen = true;
        if (SelectedStore is not null)
        {
            ApplySelectedStoreFields(SelectedStore);
            CaptureOriginalEditorState();
        }
    }

    public bool TryCloseStoreEditor()
    {
        if (!IsStoreEditorOpen)
        {
            return true;
        }

        if (HasUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.CloseEditor);
            return false;
        }

        ClearDiscardChangesPrompt();
        IsStoreEditorOpen = false;
        return true;
    }

    private void ApplyResult(StoreManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyState(result.State);
        if (result.Store is not null && result.Succeeded)
        {
            PerformSelectStoreForEditing(result.Store);
        }
    }

    private void ApplyState(StoreManagementState state)
    {
        ActiveStores = state.ActiveStores;
        ArchivedStores = state.ArchivedStores;
        SelectedStore = _isCreatingNewStore
            ? DraftStore()
            : state.ActiveStore
                ?? ActiveStores.FirstOrDefault(store => store.Id == SelectedStore?.Id)
                ?? ArchivedStores.FirstOrDefault(store => store.Id == SelectedStore?.Id)
                ?? ActiveStores.FirstOrDefault()
                ?? ArchivedStores.FirstOrDefault();
        NeedsFirstStore = state.NeedsFirstStore;
        SelectorStores = ActiveStores
            .Select(store => new StoreSelectorEntry(store, store.Id == state.ActiveStoreId))
            .ToArray();
        if (!_isCreatingNewStore)
        {
            ApplySelectedStoreFields(SelectedStore);
            CaptureOriginalEditorState();
        }

        OnPropertyChanged(nameof(ActiveStores));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(ArchivedStores));
        OnPropertyChanged(nameof(SelectorStores));
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(NeedsFirstStore));
        OnPropertyChanged(nameof(HasActiveStores));
        OnPropertyChanged(nameof(HasArchivedStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        RaiseEditorStateProperties();
        RaisePromptProperties();
        ActiveStoreChanged?.Invoke(this, state.ActiveStore);
    }

    private void SelectDefaultStoreForEditing()
    {
        var defaultStore = ActiveStores.FirstOrDefault() ?? ArchivedStores.FirstOrDefault();
        if (defaultStore is not null)
        {
            PerformSelectStoreForEditing(defaultStore);
            return;
        }

        SelectedStore = null;
        ClearEditorFields();
        CaptureOriginalEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
    }

    private void ApplySelectedStoreFields(StoreSummary? store)
    {
        if (store is null)
        {
            return;
        }

        NewStoreName = store.Name;
        Description = store.Context.Description ?? string.Empty;
        Notes = store.Context.Notes ?? string.Empty;
        TargetMarket = store.Context.TargetMarket ?? string.Empty;
        BrandDirection = store.Context.BrandDirection ?? string.Empty;
        PlanningContext = store.Context.PlanningContext ?? string.Empty;
    }

    private void ClearEditorFields()
    {
        NewStoreName = string.Empty;
        Description = string.Empty;
        Notes = string.Empty;
        TargetMarket = string.Empty;
        BrandDirection = string.Empty;
        PlanningContext = string.Empty;
    }

    private StoreSummary? DraftStore()
    {
        if (!_isCreatingNewStore || _draftStoreId is not { } id)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var name = string.IsNullOrWhiteSpace(NewStoreName) ? "New store" : NewStoreName.Trim();
        return new StoreSummary(id, name, CurrentContext(), IsArchived: false, now, now);
    }

    private void RequestDiscardBefore(PendingEditorAction action, StoreSummary? store = null)
    {
        _pendingEditorAction = action;
        _pendingEditorStore = store;
        ClearDeleteWarning();
        DiscardChangesPromptVisible = true;
        OnPropertyChanged(nameof(DiscardChangesMessage));
    }

    private void ConfirmDiscardChanges()
    {
        var action = _pendingEditorAction;
        var store = _pendingEditorStore;
        DiscardCurrentEditorChanges();
        ClearDiscardChangesPrompt();

        switch (action)
        {
            case PendingEditorAction.SelectStore when store is not null:
                PerformSelectStoreForEditing(store);
                break;
            case PendingEditorAction.StartNewStore:
                BeginCreateStoreDraft();
                break;
            case PendingEditorAction.CloseEditor:
                IsStoreEditorOpen = false;
                break;
        }
    }

    private void DiscardCurrentEditorChanges()
    {
        if (_isCreatingNewStore)
        {
            _isCreatingNewStore = false;
            _draftStoreId = null;
            SelectedStore = ActiveStores.FirstOrDefault(store => store.Id == SelectedStore?.Id) ?? ActiveStores.FirstOrDefault();
            ApplySelectedStoreFields(SelectedStore);
        }
        else
        {
            ApplySelectedStoreFields(SelectedStore);
        }

        CaptureOriginalEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
    }

    private void ClearDiscardChangesPrompt()
    {
        _pendingEditorAction = PendingEditorAction.None;
        _pendingEditorStore = null;
        DiscardChangesPromptVisible = false;
    }

    private void ClearDeleteWarning()
    {
        _pendingDeleteStore = null;
        DeleteWarningVisible = false;
        OnPropertyChanged(nameof(DeleteWarningMessage));
    }

    private StoreContext CurrentContext() =>
        new(
            EmptyToNull(Description),
            EmptyToNull(Notes),
            EmptyToNull(TargetMarket),
            EmptyToNull(BrandDirection),
            EmptyToNull(PlanningContext));

    private EditorState CurrentEditorState() =>
        new(NewStoreName, Description, Notes, TargetMarket, BrandDirection, PlanningContext);

    private void CaptureOriginalEditorState()
    {
        _originalEditorState = CurrentEditorState();
        RaiseEditorStateProperties();
    }

    private void RaiseEditorStateProperties() =>
        RaiseEditorActionProperties();

    private void RaiseEditorActionProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedStore));
        OnPropertyChanged(nameof(CanArchiveSelectedStore));
        OnPropertyChanged(nameof(CanDeleteSelectedStore));
    }

    private static EditorState EmptyEditorState() => new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Run(Task task) => _ = task;

    private void RaisePromptProperties()
    {
        OnPropertyChanged(nameof(ShouldShowFirstStorePrompt));
        OnPropertyChanged(nameof(IsSelectorCompact));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
