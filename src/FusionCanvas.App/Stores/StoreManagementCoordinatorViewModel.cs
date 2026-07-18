using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Stores;

public sealed record StoreSelectorEntry(StoreSummary Store, bool IsSelected)
{
    public Guid Id => Store.Id;

    public string Name => Store.Name;
}

public enum StoreManagementEditorTab
{
    BasicInfo,
    Niches
}

public sealed class StoreManagementViewModel : INotifyPropertyChanged
{
    private enum PendingEditorAction
    {
        None,
        SelectStore,
        StartNewStore,
        CloseEditor,
        SelectNiche,
        StartNewNiche,
        SelectBasicInfoTab,
        SelectNichesTab
    }

    private sealed record EditorState(
        string Name,
        string Description,
        string Notes,
        string TargetMarket,
        string BrandDirection,
        string PlanningContext);

    private sealed record NicheEditorState(
        string Name,
        string Description,
        string Audience,
        string HumorStyle,
        string VisualStyleGuidance,
        string Constraints,
        string Risks,
        string ResearchNotes,
        string Notes);

    private readonly IStoreManagementService _service;
    private readonly INicheManagementService? _nicheService;
    private bool _isSelectorExpanded;
    private bool _isStoreEditorOpen;
    private bool _firstStorePromptDismissed;
    private bool _deleteWarningVisible;
    private bool _nicheDeleteWarningVisible;
    private bool _discardChangesPromptVisible;
    private bool _isCreatingNewStore;
    private bool _isCreatingNewNiche;
    private Guid? _draftStoreId;
    private Guid? _draftNicheId;
    private StoreSummary? _pendingDeleteStore;
    private NicheSummary? _pendingDeleteNiche;
    private StoreSummary? _pendingEditorStore;
    private NicheSummary? _pendingEditorNiche;
    private PendingEditorAction _pendingEditorAction;
    private EditorState _originalEditorState = EmptyEditorState();
    private NicheEditorState _originalNicheEditorState = EmptyNicheEditorState();
    private StoreManagementEditorTab _selectedEditorTab;
    private string _newStoreName = string.Empty;
    private string _description = string.Empty;
    private string _notes = string.Empty;
    private string _targetMarket = string.Empty;
    private string _brandDirection = string.Empty;
    private string _planningContext = string.Empty;
    private string _nicheName = string.Empty;
    private string _nicheDescription = string.Empty;
    private string _nicheAudience = string.Empty;
    private string _nicheHumorStyle = string.Empty;
    private string _nicheVisualStyleGuidance = string.Empty;
    private string _nicheConstraints = string.Empty;
    private string _nicheRisks = string.Empty;
    private string _nicheResearchNotes = string.Empty;
    private string _nicheNotes = string.Empty;
    private string? _errorMessage;

    public StoreManagementViewModel(IStoreManagementService service, INicheManagementService? nicheService = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _nicheService = nicheService;
        ToggleStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = !IsSelectorExpanded);
        ExpandStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = true);
        CollapseStoreSelectorCommand = new RelayCommand(_ => IsSelectorExpanded = false);
        OpenStoreEditorCommand = new RelayCommand(_ => OpenBasicInfoTab());
        OpenNichesTabCommand = new RelayCommand(_ =>
        {
            OpenNichesTab();
        });
        SelectBasicInfoTabCommand = new RelayCommand(_ => SelectBasicInfoTab());
        SelectNichesTabCommand = new RelayCommand(_ => SelectNichesTab());
        StartCreateStoreCommand = new RelayCommand(_ => StartCreateStore());
        StartCreateNicheCommand = new RelayCommand(_ => StartCreateNiche());
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
        SelectNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                Run(SelectNicheAsync(niche));
            }
        });
        EditNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                SelectNicheForEditing(niche);
            }
        });
        SaveSelectedNicheCommand = new RelayCommand(_ => Run(SaveSelectedNicheAsync()));
        ArchiveSelectedNicheCommand = new RelayCommand(_ => Run(ArchiveSelectedNicheAsync()));
        RestoreNicheCommand = new RelayCommand(parameter =>
        {
            if (parameter is NicheSummary niche)
            {
                Run(RestoreNicheAsync(niche));
            }
        });
        RequestDeleteSelectedNicheCommand = new RelayCommand(_ => RequestDeleteSelectedNiche());
        ConfirmDeleteNicheCommand = new RelayCommand(_ => Run(ConfirmDeleteNicheAsync()));
        CancelDeleteNicheCommand = new RelayCommand(_ => ClearNicheDeleteWarning());
        ConfirmDiscardChangesCommand = new RelayCommand(_ => ConfirmDiscardChanges());
        KeepEditingCommand = new RelayCommand(_ => ClearDiscardChangesPrompt());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<StoreSummary?>? ActiveStoreChanged;

    public event EventHandler? WorkspaceStructureChanged;

    public event EventHandler? StoreNameFocusRequested;

    public IReadOnlyList<StoreSummary> ActiveStores { get; private set; } = [];

    public IReadOnlyList<StoreSummary> ArchivedStores { get; private set; } = [];

    public IReadOnlyList<StoreSelectorEntry> SelectorStores { get; private set; } = [];

    public IReadOnlyList<NicheSummary> ActiveNiches { get; private set; } = [];

    public IReadOnlyList<NicheSummary> ArchivedNiches { get; private set; } = [];

    public IReadOnlyList<NicheSummary> EditorActiveNiches =>
        _isCreatingNewNiche && DraftNiche() is { } draft
            ? ActiveNiches.Concat([draft]).ToArray()
            : ActiveNiches;

    public IReadOnlyList<StoreSummary> EditorActiveStores =>
        _isCreatingNewStore && DraftStore() is { } draft
            ? ActiveStores.Concat([draft]).ToArray()
            : ActiveStores;

    public StoreSummary? SelectedStore { get; private set; }

    public NicheSummary? SelectedNiche { get; private set; }

    public bool NeedsFirstStore { get; private set; }

    public bool HasActiveStores => ActiveStores.Count > 0;

    public bool HasArchivedStores => ArchivedStores.Count > 0;

    public bool HasActiveNiches => ActiveNiches.Count > 0;

    public bool HasArchivedNiches => ArchivedNiches.Count > 0;

    public bool NeedsFirstNiche { get; private set; }

    public bool HasSelectedStore => SelectedStore is not null;

    public bool HasSelectedNiche => SelectedNiche is not null;

    public bool CanRestoreSelectedStore => SelectedStore is { IsArchived: true };

    public bool CanRestoreSelectedNiche => SelectedNiche is { IsArchived: true };

    public bool HasUnsavedChanges => CurrentEditorState() != _originalEditorState;

    public bool HasUnsavedNicheChanges => CurrentNicheEditorState() != _originalNicheEditorState;

    public bool HasAnyUnsavedChanges => HasUnsavedChanges || HasUnsavedNicheChanges;

    public bool CanSaveSelectedStore => _isCreatingNewStore || (SelectedStore is not null && HasUnsavedChanges);

    public bool CanArchiveSelectedStore => SelectedStore is { IsArchived: false } && !_isCreatingNewStore;

    public bool CanDeleteSelectedStore => SelectedStore is not null && !_isCreatingNewStore;

    public bool CanSaveSelectedNiche => _nicheService is not null && (_isCreatingNewNiche || (SelectedNiche is not null && HasUnsavedNicheChanges));

    public bool CanArchiveSelectedNiche => _nicheService is not null && SelectedNiche is { IsArchived: false } && !_isCreatingNewNiche;

    public bool CanDeleteSelectedNiche => _nicheService is not null && SelectedNiche is not null && !_isCreatingNewNiche;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool ShouldShowFirstStorePrompt => NeedsFirstStore && !_firstStorePromptDismissed && !IsStoreEditorOpen;

    public bool IsBasicInfoTabSelected => SelectedEditorTab == StoreManagementEditorTab.BasicInfo;

    public bool IsNichesTabSelected => SelectedEditorTab == StoreManagementEditorTab.Niches;

    public StoreManagementEditorTab SelectedEditorTab
    {
        get => _selectedEditorTab;
        private set
        {
            if (SetField(ref _selectedEditorTab, value))
            {
                OnPropertyChanged(nameof(IsBasicInfoTabSelected));
                OnPropertyChanged(nameof(IsNichesTabSelected));
            }
        }
    }

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

    public bool NicheDeleteWarningVisible
    {
        get => _nicheDeleteWarningVisible;
        private set => SetField(ref _nicheDeleteWarningVisible, value);
    }

    public bool DiscardChangesPromptVisible
    {
        get => _discardChangesPromptVisible;
        private set => SetField(ref _discardChangesPromptVisible, value);
    }

    public string DeleteWarningMessage => _pendingDeleteStore is null
        ? "Permanent deletion cannot be undone."
        : $"Delete '{_pendingDeleteStore.Name}' permanently? This cannot be undone.";

    public string NicheDeleteWarningMessage => _pendingDeleteNiche is null
        ? "Permanent deletion cannot be undone."
        : $"Delete niche '{_pendingDeleteNiche.Name}' permanently? This cannot be undone.";

    public string DiscardChangesMessage => HasUnsavedNicheChanges && !HasUnsavedChanges
        ? "Discard changes? Unsaved niche edits will be lost."
        : "Discard changes? Unsaved store or niche edits will be lost.";

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

    public string NicheName
    {
        get => _nicheName;
        set
        {
            if (SetField(ref _nicheName, value))
            {
                RaiseNicheEditorStateProperties();
                OnPropertyChanged(nameof(EditorActiveNiches));
            }
        }
    }

    public string NicheDescription
    {
        get => _nicheDescription;
        set
        {
            if (SetField(ref _nicheDescription, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheAudience
    {
        get => _nicheAudience;
        set
        {
            if (SetField(ref _nicheAudience, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheHumorStyle
    {
        get => _nicheHumorStyle;
        set
        {
            if (SetField(ref _nicheHumorStyle, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheVisualStyleGuidance
    {
        get => _nicheVisualStyleGuidance;
        set
        {
            if (SetField(ref _nicheVisualStyleGuidance, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheConstraints
    {
        get => _nicheConstraints;
        set
        {
            if (SetField(ref _nicheConstraints, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheRisks
    {
        get => _nicheRisks;
        set
        {
            if (SetField(ref _nicheRisks, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheResearchNotes
    {
        get => _nicheResearchNotes;
        set
        {
            if (SetField(ref _nicheResearchNotes, value))
            {
                RaiseNicheEditorStateProperties();
            }
        }
    }

    public string NicheNotes
    {
        get => _nicheNotes;
        set
        {
            if (SetField(ref _nicheNotes, value))
            {
                RaiseNicheEditorStateProperties();
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

    public ICommand OpenNichesTabCommand { get; }

    public ICommand SelectBasicInfoTabCommand { get; }

    public ICommand SelectNichesTabCommand { get; }

    public ICommand StartCreateStoreCommand { get; }

    public ICommand StartCreateNicheCommand { get; }

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

    public ICommand SelectNicheCommand { get; }

    public ICommand EditNicheCommand { get; }

    public ICommand SaveSelectedNicheCommand { get; }

    public ICommand ArchiveSelectedNicheCommand { get; }

    public ICommand RestoreNicheCommand { get; }

    public ICommand RequestDeleteSelectedNicheCommand { get; }

    public ICommand ConfirmDeleteNicheCommand { get; }

    public ICommand CancelDeleteNicheCommand { get; }

    public ICommand ConfirmDiscardChangesCommand { get; }

    public ICommand KeepEditingCommand { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _service.LoadAsync(cancellationToken).ConfigureAwait(false);
        ApplyState(state);
        await LoadNichesForSelectedStoreAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetActiveWorkspaceAsync(Guid? workspaceId, CancellationToken cancellationToken = default)
    {
        _service.SetActiveWorkspace(workspaceId);
        _nicheService?.SetActiveWorkspace(workspaceId);
        _isCreatingNewStore = false;
        _isCreatingNewNiche = false;
        _draftStoreId = null;
        _draftNicheId = null;
        var state = await _service.LoadAsync(cancellationToken).ConfigureAwait(false);
        ApplyState(state);
        await LoadNichesForSelectedStoreAsync(cancellationToken).ConfigureAwait(false);
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

        if (HasAnyUnsavedChanges && SelectedStore?.Id != store.Id)
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
        Run(LoadNichesForSelectedStoreAsync());
    }

    public void StartCreateStore()
    {
        if (HasAnyUnsavedChanges)
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
        ClearNicheSelection();
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

    public void SelectNicheForEditing(NicheSummary niche)
    {
        ArgumentNullException.ThrowIfNull(niche);
        if (_isCreatingNewNiche && niche.Id == _draftNicheId)
        {
            SelectedNiche = DraftNiche();
            OnPropertyChanged(nameof(SelectedNiche));
            return;
        }

        if (HasUnsavedNicheChanges && SelectedNiche?.Id != niche.Id)
        {
            RequestDiscardBefore(PendingEditorAction.SelectNiche, niche: niche);
            return;
        }

        PerformSelectNicheForEditing(niche);
    }

    private void PerformSelectNicheForEditing(NicheSummary niche)
    {
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        SelectedNiche = niche;
        ApplySelectedNicheFields(niche);
        CaptureOriginalNicheEditorState();
        ClearNicheDeleteWarning();
        ClearDiscardChangesPrompt();
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
    }

    public void StartCreateNiche()
    {
        if (SelectedStore is null || SelectedStore.IsArchived || _isCreatingNewStore)
        {
            ErrorMessage = "Select an active saved store before creating a niche.";
            return;
        }

        if (HasUnsavedNicheChanges)
        {
            RequestDiscardBefore(PendingEditorAction.StartNewNiche);
            return;
        }

        BeginCreateNicheDraft();
    }

    private void BeginCreateNicheDraft()
    {
        _isCreatingNewNiche = true;
        _draftNicheId = Guid.NewGuid();
        SelectedNiche = DraftNiche();
        ErrorMessage = null;
        ClearNicheDeleteWarning();
        ClearDiscardChangesPrompt();
        ClearNicheEditorFields();
        CaptureOriginalNicheEditorState();
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
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

    public async Task SaveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            ErrorMessage = "Niche management is not available.";
            return;
        }

        if (SelectedStore is null)
        {
            ErrorMessage = "Select a store before saving a niche.";
            return;
        }

        if (_isCreatingNewNiche)
        {
            var createResult = await _nicheService.CreateNicheAsync(
                new NicheManagementCreateRequest(SelectedStore.Id, NicheName, CurrentNicheContext()),
                cancellationToken).ConfigureAwait(false);

            if (createResult.Succeeded)
            {
                _isCreatingNewNiche = false;
                _draftNicheId = null;
            }

            ApplyNicheResult(createResult);
            return;
        }

        if (SelectedNiche is null)
        {
            ErrorMessage = "Select a niche before saving.";
            return;
        }

        var result = await _nicheService.UpdateNicheAsync(
            new NicheManagementUpdateRequest(SelectedNiche.Id, NicheName, CurrentNicheContext()),
            cancellationToken).ConfigureAwait(false);
        ApplyNicheResult(result);
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

    public async Task ArchiveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            ErrorMessage = "Niche management is not available.";
            return;
        }

        if (_isCreatingNewNiche)
        {
            ErrorMessage = "Save the new niche before archiving it.";
            return;
        }

        if (SelectedNiche is null)
        {
            ErrorMessage = "Select a niche before archiving.";
            return;
        }

        var result = await _nicheService.ArchiveNicheAsync(SelectedNiche.Id, cancellationToken).ConfigureAwait(false);
        ApplyNicheResult(result);
    }

    public async Task RestoreStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var result = await _service.RestoreStoreAsync(store.Id, cancellationToken).ConfigureAwait(false);
        ApplyResult(result);
    }

    public async Task RestoreNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            ErrorMessage = "Niche management is not available.";
            return;
        }

        ArgumentNullException.ThrowIfNull(niche);
        var result = await _nicheService.RestoreNicheAsync(niche.Id, cancellationToken).ConfigureAwait(false);
        ApplyNicheResult(result);
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

    public void RequestDeleteSelectedNiche()
    {
        if (_isCreatingNewNiche)
        {
            ErrorMessage = "Save the new niche before deleting it.";
            return;
        }

        if (SelectedNiche is null)
        {
            ErrorMessage = "Select a niche before deleting.";
            return;
        }

        _pendingDeleteNiche = SelectedNiche;
        NicheDeleteWarningVisible = true;
        OnPropertyChanged(nameof(NicheDeleteWarningMessage));
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

    public async Task ConfirmDeleteNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            ErrorMessage = "Niche management is not available.";
            return;
        }

        if (_pendingDeleteNiche is null)
        {
            ErrorMessage = "Select a niche before deleting.";
            return;
        }

        var result = await _nicheService.DeleteNicheAsync(
            new NicheManagementDeleteRequest(_pendingDeleteNiche.Id, ConfirmPermanentDeletion: true),
            cancellationToken).ConfigureAwait(false);
        ErrorMessage = result.Error;
        ApplyNicheState(result.State);
        if (result.Succeeded)
        {
            SelectDefaultNicheForEditing();
        }

        ClearNicheDeleteWarning();
    }

    public async Task SelectNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            ErrorMessage = "Niche management is not available.";
            return;
        }

        ArgumentNullException.ThrowIfNull(niche);
        var result = await _nicheService.SelectNicheAsync(niche.Id, cancellationToken).ConfigureAwait(false);
        ApplyNicheResult(result);
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

    private void OpenBasicInfoTab()
    {
        OpenStoreEditor();
        SelectBasicInfoTab();
    }

    private void OpenNichesTab()
    {
        OpenStoreEditor();
        SelectNichesTab();
    }

    private void SelectBasicInfoTab()
    {
        if (HasUnsavedNicheChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectBasicInfoTab);
            return;
        }

        SelectedEditorTab = StoreManagementEditorTab.BasicInfo;
    }

    private void SelectNichesTab()
    {
        if (HasUnsavedChanges)
        {
            RequestDiscardBefore(PendingEditorAction.SelectNichesTab);
            return;
        }

        SelectedEditorTab = StoreManagementEditorTab.Niches;
        if (NeedsFirstNiche && SelectedStore is { IsArchived: false } && !_isCreatingNewStore)
        {
            BeginCreateNicheDraft();
        }

        Run(LoadNichesForSelectedStoreAsync());
    }

    public bool TryCloseStoreEditor()
    {
        if (!IsStoreEditorOpen)
        {
            return true;
        }

        if (HasAnyUnsavedChanges)
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
            WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyNicheResult(NicheManagementResult result)
    {
        ErrorMessage = result.Error;
        ApplyNicheState(result.State);
        if (result.Niche is not null && result.Succeeded)
        {
            PerformSelectNicheForEditing(result.Niche);
            WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
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

    private async Task LoadNichesForSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (_nicheService is null)
        {
            return;
        }

        var state = await _nicheService.LoadAsync(SelectedStore is { IsArchived: false } ? SelectedStore.Id : null, cancellationToken).ConfigureAwait(false);
        ApplyNicheState(state);
    }

    private void ApplyNicheState(NicheManagementState state)
    {
        ActiveNiches = state.ActiveNiches;
        ArchivedNiches = state.ArchivedNiches;
        SelectedNiche = _isCreatingNewNiche
            ? DraftNiche()
            : state.ActiveNiche
                ?? ActiveNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id)
                ?? ArchivedNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id)
                ?? ActiveNiches.FirstOrDefault()
                ?? ArchivedNiches.FirstOrDefault();
        NeedsFirstNiche = state.NeedsFirstNiche;
        if (!_isCreatingNewNiche)
        {
            ApplySelectedNicheFields(SelectedNiche);
            CaptureOriginalNicheEditorState();
        }

        OnPropertyChanged(nameof(ActiveNiches));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(ArchivedNiches));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(NeedsFirstNiche));
        OnPropertyChanged(nameof(HasActiveNiches));
        OnPropertyChanged(nameof(HasArchivedNiches));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
        RaiseNicheEditorStateProperties();
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

    private void SelectDefaultNicheForEditing()
    {
        var defaultNiche = ActiveNiches.FirstOrDefault() ?? ArchivedNiches.FirstOrDefault();
        if (defaultNiche is not null)
        {
            PerformSelectNicheForEditing(defaultNiche);
            return;
        }

        ClearNicheSelection();
    }

    private void ClearNicheSelection()
    {
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        ActiveNiches = [];
        ArchivedNiches = [];
        SelectedNiche = null;
        ClearNicheEditorFields();
        CaptureOriginalNicheEditorState();
        OnPropertyChanged(nameof(ActiveNiches));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(ArchivedNiches));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(HasSelectedNiche));
        OnPropertyChanged(nameof(HasActiveNiches));
        OnPropertyChanged(nameof(HasArchivedNiches));
        OnPropertyChanged(nameof(CanRestoreSelectedNiche));
        RaiseNicheEditorActionProperties();
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

    private void ApplySelectedNicheFields(NicheSummary? niche)
    {
        if (niche is null)
        {
            ClearNicheEditorFields();
            return;
        }

        NicheName = niche.Name;
        NicheDescription = niche.Context.Description ?? string.Empty;
        NicheAudience = niche.Context.Audience ?? string.Empty;
        NicheHumorStyle = niche.Context.HumorStyle ?? string.Empty;
        NicheVisualStyleGuidance = niche.Context.VisualStyleGuidance ?? string.Empty;
        NicheConstraints = niche.Context.Constraints ?? string.Empty;
        NicheRisks = niche.Context.Risks ?? string.Empty;
        NicheResearchNotes = niche.Context.ResearchNotes ?? string.Empty;
        NicheNotes = niche.Context.Notes ?? string.Empty;
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

    private void ClearNicheEditorFields()
    {
        NicheName = string.Empty;
        NicheDescription = string.Empty;
        NicheAudience = string.Empty;
        NicheHumorStyle = string.Empty;
        NicheVisualStyleGuidance = string.Empty;
        NicheConstraints = string.Empty;
        NicheRisks = string.Empty;
        NicheResearchNotes = string.Empty;
        NicheNotes = string.Empty;
    }

    private StoreSummary? DraftStore()
    {
        if (!_isCreatingNewStore || _draftStoreId is not { } id)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var name = string.IsNullOrWhiteSpace(NewStoreName) ? "New store" : NewStoreName.Trim();
        return new StoreSummary(id, SelectedStore?.WorkspaceId ?? WorkspaceDefaults.DefaultWorkspaceId, name, CurrentContext(), false, now, now);
    }

    private NicheSummary? DraftNiche()
    {
        if (!_isCreatingNewNiche || _draftNicheId is not { } id || SelectedStore is null)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var name = string.IsNullOrWhiteSpace(NicheName) ? "New niche" : NicheName.Trim();
        return new NicheSummary(id, SelectedStore.Id, name, CurrentNicheContext(), IsArchived: false, now, now);
    }

    private void RequestDiscardBefore(PendingEditorAction action, StoreSummary? store = null, NicheSummary? niche = null)
    {
        _pendingEditorAction = action;
        _pendingEditorStore = store;
        _pendingEditorNiche = niche;
        ClearDeleteWarning();
        ClearNicheDeleteWarning();
        DiscardChangesPromptVisible = true;
        OnPropertyChanged(nameof(DiscardChangesMessage));
    }

    private void ConfirmDiscardChanges()
    {
        var action = _pendingEditorAction;
        var store = _pendingEditorStore;
        var niche = _pendingEditorNiche;
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
            case PendingEditorAction.SelectNiche when niche is not null:
                PerformSelectNicheForEditing(niche);
                break;
            case PendingEditorAction.StartNewNiche:
                BeginCreateNicheDraft();
                break;
            case PendingEditorAction.SelectBasicInfoTab:
                SelectedEditorTab = StoreManagementEditorTab.BasicInfo;
                break;
            case PendingEditorAction.SelectNichesTab:
                SelectedEditorTab = StoreManagementEditorTab.Niches;
                Run(LoadNichesForSelectedStoreAsync());
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
        if (_isCreatingNewNiche)
        {
            _isCreatingNewNiche = false;
            _draftNicheId = null;
            SelectedNiche = ActiveNiches.FirstOrDefault(niche => niche.Id == SelectedNiche?.Id) ?? ActiveNiches.FirstOrDefault();
        }

        ApplySelectedNicheFields(SelectedNiche);
        CaptureOriginalNicheEditorState();
        OnPropertyChanged(nameof(SelectedStore));
        OnPropertyChanged(nameof(EditorActiveStores));
        OnPropertyChanged(nameof(SelectedNiche));
        OnPropertyChanged(nameof(EditorActiveNiches));
        OnPropertyChanged(nameof(HasSelectedStore));
        OnPropertyChanged(nameof(CanRestoreSelectedStore));
        RaiseEditorActionProperties();
        RaiseNicheEditorActionProperties();
    }

    private void ClearDiscardChangesPrompt()
    {
        _pendingEditorAction = PendingEditorAction.None;
        _pendingEditorStore = null;
        _pendingEditorNiche = null;
        DiscardChangesPromptVisible = false;
    }

    private void ClearDeleteWarning()
    {
        _pendingDeleteStore = null;
        DeleteWarningVisible = false;
        OnPropertyChanged(nameof(DeleteWarningMessage));
    }

    private void ClearNicheDeleteWarning()
    {
        _pendingDeleteNiche = null;
        NicheDeleteWarningVisible = false;
        OnPropertyChanged(nameof(NicheDeleteWarningMessage));
    }

    private StoreContext CurrentContext() =>
        new(
            EmptyToNull(Description),
            EmptyToNull(Notes),
            EmptyToNull(TargetMarket),
            EmptyToNull(BrandDirection),
            EmptyToNull(PlanningContext));

    private NicheContext CurrentNicheContext() =>
        new(
            EmptyToNull(NicheDescription),
            EmptyToNull(NicheAudience),
            EmptyToNull(NicheHumorStyle),
            EmptyToNull(NicheVisualStyleGuidance),
            EmptyToNull(NicheConstraints),
            EmptyToNull(NicheRisks),
            EmptyToNull(NicheResearchNotes),
            EmptyToNull(NicheNotes));

    private EditorState CurrentEditorState() =>
        new(NewStoreName, Description, Notes, TargetMarket, BrandDirection, PlanningContext);

    private NicheEditorState CurrentNicheEditorState() =>
        new(NicheName, NicheDescription, NicheAudience, NicheHumorStyle, NicheVisualStyleGuidance, NicheConstraints, NicheRisks, NicheResearchNotes, NicheNotes);

    private void CaptureOriginalEditorState()
    {
        _originalEditorState = CurrentEditorState();
        RaiseEditorStateProperties();
    }

    private void CaptureOriginalNicheEditorState()
    {
        _originalNicheEditorState = CurrentNicheEditorState();
        RaiseNicheEditorStateProperties();
    }

    private void RaiseEditorStateProperties() =>
        RaiseEditorActionProperties();

    private void RaiseEditorActionProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedStore));
        OnPropertyChanged(nameof(CanArchiveSelectedStore));
        OnPropertyChanged(nameof(CanDeleteSelectedStore));
    }

    private void RaiseNicheEditorStateProperties() =>
        RaiseNicheEditorActionProperties();

    private void RaiseNicheEditorActionProperties()
    {
        OnPropertyChanged(nameof(HasUnsavedNicheChanges));
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(CanSaveSelectedNiche));
        OnPropertyChanged(nameof(CanArchiveSelectedNiche));
        OnPropertyChanged(nameof(CanDeleteSelectedNiche));
    }

    private static EditorState EmptyEditorState() => new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

    private static NicheEditorState EmptyNicheEditorState() => new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

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
