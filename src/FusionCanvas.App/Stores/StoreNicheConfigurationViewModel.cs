using System.ComponentModel;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Stores;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Niches;

namespace FusionCanvas.App.Stores;

/// <summary>Owns store/niche draft state and the persistence/population workflows for an explicit store scope.</summary>
public sealed class StoreNicheConfigurationViewModel : StoreConfigurationViewModel
{
    private sealed record StoreDraft(string Name, string Description, string Notes, string TargetMarket,
        string BrandDirection, string PlanningContext, string Url, FulfillmentStrategy Strategy, int? PrintifyShopId, string? PrintifyShopTitle, bool PrintifyShopSelectionChanged);
    private sealed record NicheDraft(string Name, string Description, string Audience, string HumorStyle,
        string VisualStyleGuidance, string Constraints, string Risks, string ResearchNotes, string Notes);

    private readonly IStoreManagementService _stores;
    private readonly INicheManagementService? _niches;
    private readonly INichePopulationService? _population;
    private readonly Action<StoreManagementState> _applyStoreState;
    private readonly Action<StoreManagementResult> _applyStoreResult;
    private readonly Action<NicheManagementState> _applyNicheState;
    private readonly Action<NicheManagementResult> _applyNicheResult;
    private readonly Action<string?> _reportError;
    private readonly Action _workspaceChanged;
    private StoreSummary? _selectedStore;
    private NicheSummary? _selectedNiche;
    private Guid? _draftStoreId;
    private Guid? _draftNicheId;
    private StoreDraft? _originalStoreDraft;
    private NicheDraft? _originalNicheDraft;
    private bool _isCreatingNewStore;
    private bool _isCreatingNewNiche;
    private bool _isPopulationBusy;
    private string? _populationMessage;
    private AiAvailabilityResult _populationAvailability = new(AiAvailabilityKind.MissingModel,
        "Configure General AI settings before populating niche fields.");
    private long _scopeVersion;
    private long _storeDraftGeneration;
    private long _nicheDraftGeneration;
    private long _populationVersion;
    private FulfillmentStrategy _fulfillmentStrategy = FulfillmentStrategy.Manual;
    private int? _printifyShopId;
    private string? _printifyShopTitle;

    public StoreNicheConfigurationViewModel(
        IStoreManagementService stores,
        INicheManagementService? niches,
        INichePopulationService? population,
        Action<StoreManagementState> applyStoreState,
        Action<StoreManagementResult> applyStoreResult,
        Action<NicheManagementState> applyNicheState,
        Action<NicheManagementResult>? applyNicheResult = null,
        Action<string?>? reportError = null,
        Action? workspaceChanged = null)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        _niches = niches;
        _population = population;
        _applyStoreState = applyStoreState ?? throw new ArgumentNullException(nameof(applyStoreState));
        _applyStoreResult = applyStoreResult ?? throw new ArgumentNullException(nameof(applyStoreResult));
        _applyNicheState = applyNicheState ?? throw new ArgumentNullException(nameof(applyNicheState));
        _applyNicheResult = applyNicheResult ?? (_ => { });
        _reportError = reportError ?? (_ => { });
        _workspaceChanged = workspaceChanged ?? (() => { });
        PropertyChanged += (_, args) =>
        {
            var storeDraftChanged = args.PropertyName is nameof(NewStoreName) or nameof(Description) or nameof(Notes)
                or nameof(TargetMarket) or nameof(BrandDirection) or nameof(PlanningContext) or nameof(Url)
                or nameof(SelectedFulfillmentStrategy) or nameof(PrintifyShopId) or nameof(PrintifyShopTitle)
                or nameof(PrintifyShopSelectionChanged);
            var nicheDraftChanged = args.PropertyName is nameof(NicheName) or nameof(NicheDescription) or nameof(NicheAudience)
                or nameof(NicheHumorStyle) or nameof(NicheVisualStyleGuidance) or nameof(NicheConstraints)
                or nameof(NicheRisks) or nameof(NicheResearchNotes) or nameof(NicheNotes);
            if (storeDraftChanged) _storeDraftGeneration++;
            if (nicheDraftChanged) _nicheDraftGeneration++;
            if (storeDraftChanged || nicheDraftChanged) RaiseDirtyState();
        };
    }

    public StoreSummary? SelectedStore => _selectedStore;
    public NicheSummary? SelectedNiche => _selectedNiche;
    public bool IsCreatingNewStore => _isCreatingNewStore;
    public bool IsCreatingNewNiche => _isCreatingNewNiche;
    public FulfillmentStrategy SelectedFulfillmentStrategy
    {
        get => _fulfillmentStrategy;
        set { if (_fulfillmentStrategy != value) { _fulfillmentStrategy = value; RaiseProperty(); RaiseDirtyState(); } }
    }
    public int? PrintifyShopId { get => _printifyShopId; set { if (_printifyShopId != value) { _printifyShopId = value; RaiseProperty(); RaiseDirtyState(); } } }
    public string? PrintifyShopTitle { get => _printifyShopTitle; set { if (_printifyShopTitle != value) { _printifyShopTitle = value; RaiseProperty(); RaiseDirtyState(); } } }
    private bool _printifyShopSelectionChanged;
    public bool PrintifyShopSelectionChanged
    {
        get => _printifyShopSelectionChanged;
        set { if (_printifyShopSelectionChanged != value) { _printifyShopSelectionChanged = value; RaiseProperty(); RaiseDirtyState(); } }
    }
    public bool IsNichePopulationBusy => _isPopulationBusy;
    public string NichePopulationStatusMessage => _isPopulationBusy ? "Generating suggestions…" :
        _populationAvailability.IsReady ? _populationMessage ?? string.Empty : _populationAvailability.Message;
    public bool HasNichePopulationStatus => !string.IsNullOrWhiteSpace(NichePopulationStatusMessage);
    public bool CanPopulateNiche => _population is not null && !_isPopulationBusy && _populationAvailability.IsReady &&
        Scope.StoreId is not null && !Scope.IsStoreArchived && !Scope.IsCreatingNewStore &&
        _selectedNiche is { IsArchived: false } && !string.IsNullOrWhiteSpace(NicheName);
    public bool HasUnsavedStoreChanges => CurrentStoreDraft() != _originalStoreDraft;
    public bool HasUnsavedNicheChanges => CurrentNicheDraft() != _originalNicheDraft;

    public void SetScope(StoreManagementScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (Scope == scope) return;
        var preserveStoreDraft = _isCreatingNewStore && scope.IsCreatingNewStore && Scope.WorkspaceId == scope.WorkspaceId;
        base.Scope = scope;
        _scopeVersion++;
        _storeDraftGeneration++;
        _nicheDraftGeneration++;
        _populationVersion++;
        _selectedStore = null;
        _selectedNiche = null;
        _isCreatingNewStore = scope.IsCreatingNewStore;
        _isCreatingNewNiche = false;
        if (!preserveStoreDraft)
        {
            _draftStoreId = null;
            _draftNicheId = null;
            ClearStoreDraft();
            ClearNicheDraft();
            CaptureStoreDraft();
            CaptureNicheDraft();
        }
        _stores.SetActiveWorkspace(scope.WorkspaceId);
        _niches?.SetActiveWorkspace(scope.WorkspaceId);
        SetServiceStore();
        RaiseAll();
    }

    public void SetCanonicalSelection(StoreSummary? store, NicheSummary? niche = null)
    {
        if (_selectedStore?.Id != store?.Id) _storeDraftGeneration++;
        if (_selectedNiche?.Id != niche?.Id) _nicheDraftGeneration++;
        if (store is not null && Scope.StoreId != store.Id)
            SetScope(new StoreManagementScope(store.WorkspaceId, store.Id, store.IsArchived, _isCreatingNewStore));
        _selectedStore = store;
        _selectedNiche = niche;
        RaiseAll();
    }

    public void StartCreateStoreDraft(Guid? draftId = null)
    {
        _storeDraftGeneration++;
        _nicheDraftGeneration++;
        _isCreatingNewStore = true;
        _draftStoreId = draftId ?? Guid.NewGuid();
        _draftNicheId = null;
        _isCreatingNewNiche = false;
        _selectedNiche = null;
        base.Scope = Scope with { StoreId = null, IsStoreArchived = false, IsCreatingNewStore = true };
        ClearStoreDraft();
        CaptureStoreDraft();
        SetServiceStore();
        RaiseAll();
    }

    public void SelectStoreForEditing(StoreSummary store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _storeDraftGeneration++;
        _nicheDraftGeneration++;
        _isCreatingNewStore = false;
        _draftStoreId = null;
        _selectedStore = store;
        base.Scope = new StoreManagementScope(store.WorkspaceId, store.Id, store.IsArchived, false);
        PrintifyShopSelectionChanged = false;
        ApplyStoreFields(store);
        CaptureStoreDraft();
        SetServiceStore();
        RaiseAll();
    }

    public void StartCreateNicheDraft(Guid? draftId = null)
    {
        if (Scope.StoreId is null || Scope.IsStoreArchived || Scope.IsCreatingNewStore)
        {
            _reportError("Select an active saved store before creating a niche.");
            return;
        }
        _nicheDraftGeneration++;
        _isCreatingNewNiche = true;
        _draftNicheId = draftId ?? Guid.NewGuid();
        var now = DateTimeOffset.Now;
        _selectedNiche = new NicheSummary(_draftNicheId.Value, Scope.StoreId!.Value, "New niche", new NicheContext(), false, now, now);
        _populationMessage = null;
        ClearNicheDraft();
        CaptureNicheDraft();
        RaiseAll();
    }

    public void SelectNicheForEditing(NicheSummary niche)
    {
        ArgumentNullException.ThrowIfNull(niche);
        if (Scope.StoreId != niche.StoreId) return;
        _nicheDraftGeneration++;
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        _selectedNiche = niche;
        _populationMessage = null;
        ApplyNicheFields(niche);
        CaptureNicheDraft();
        RaiseAll();
    }

    public void DiscardStoreDraft()
    {
        _storeDraftGeneration++;
        _nicheDraftGeneration++;
        _isCreatingNewStore = false;
        _draftStoreId = null;
        PrintifyShopSelectionChanged = false;
        ApplyStoreFields(_selectedStore);
        CaptureStoreDraft();
        base.Scope = new StoreManagementScope(_selectedStore?.WorkspaceId ?? Scope.WorkspaceId, _selectedStore?.Id, _selectedStore?.IsArchived ?? false);
        SetServiceStore();
        RaiseAll();
    }

    public void DiscardNicheDraft()
    {
        _nicheDraftGeneration++;
        _isCreatingNewNiche = false;
        _draftNicheId = null;
        ApplyNicheFields(_selectedNiche);
        CaptureNicheDraft();
        RaiseAll();
    }
    public void CaptureStoreDraft() { _originalStoreDraft = CurrentStoreDraft(); RaiseDirtyState(); }
    public void CaptureNicheDraft() { _originalNicheDraft = CurrentNicheDraft(); RaiseDirtyState(); }

    public async Task LoadStoresAsync(CancellationToken cancellationToken = default)
    {
        var version = _scopeVersion;
        var scope = Scope;
        var state = await _stores.LoadAsync(cancellationToken);
        if (version == _scopeVersion && scope == Scope) _applyStoreState(state);
    }

    public async Task LoadNichesAsync(CancellationToken cancellationToken = default)
    {
        if (_niches is null) return;
        var version = _scopeVersion;
        var scope = Scope;
        var state = await _niches.LoadAsync(CanManageScope ? scope.StoreId : null, cancellationToken);
        if (version == _scopeVersion && scope == Scope) _applyNicheState(state);
    }

    public async Task SelectStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var scope = Scope;
        var result = await _stores.SelectStoreAsync(store.Id, cancellationToken);
        if (scope == Scope || scope.StoreId == store.Id) _applyStoreResult(result);
    }

    public async Task SelectNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        if (_niches is null) { _reportError("Niche management is not available."); return; }
        ArgumentNullException.ThrowIfNull(niche);
        var scope = Scope;
        var result = await _niches.SelectNicheAsync(niche.Id, cancellationToken);
        if (scope == Scope && niche.StoreId == Scope.StoreId) _applyNicheResult(result);
    }

    public Task CreateStoreAsync(CancellationToken cancellationToken = default)
    {
        _isCreatingNewStore = true;
        return SaveSelectedStoreAsync(cancellationToken);
    }

    public async Task SaveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        var scope = Scope;
        var scopeVersion = _scopeVersion;
        var draftGeneration = _storeDraftGeneration;
        var isCreatingNewStore = _isCreatingNewStore;
        var draftStoreId = _draftStoreId;
        var selectedStoreId = _selectedStore?.Id;
        var draft = CurrentStoreDraft();
        StoreManagementResult result;
        if (isCreatingNewStore)
        {
            result = await _stores.CreateStoreAsync(new StoreManagementCreateRequest(NewStoreName, CurrentStoreContext(), SelectedFulfillmentStrategy), cancellationToken);
        }
        else
        {
            var selectedStore = _selectedStore;
            if (selectedStore is null) { _reportError("Select a store before saving."); return; }
            result = await _stores.UpdateStoreAsync(new StoreManagementUpdateRequest(selectedStore.Id, NewStoreName, CurrentStoreContext(), SelectedFulfillmentStrategy), cancellationToken);
        }
        if (scope != Scope || scopeVersion != _scopeVersion) { SetServiceStore(); if (result.Succeeded) _workspaceChanged(); return; }
        if (draftGeneration != _storeDraftGeneration || isCreatingNewStore != _isCreatingNewStore || draftStoreId != _draftStoreId || selectedStoreId != _selectedStore?.Id || draft != CurrentStoreDraft()) { if (result.Succeeded) _workspaceChanged(); return; }
        if (isCreatingNewStore && result.Succeeded)
        {
            _isCreatingNewStore = false;
            _draftStoreId = null;
            base.Scope = Scope with { IsCreatingNewStore = false, StoreId = result.Store?.Id, IsStoreArchived = result.Store?.IsArchived ?? false };
        }
        _applyStoreResult(result);
    }

    public async Task SaveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_niches is null) { _reportError("Niche management is not available."); return; }
        if (Scope.StoreId is not { } storeId || Scope.IsStoreArchived || Scope.IsCreatingNewStore) { _reportError("Select an active saved store before saving a niche."); return; }
        var scope = Scope;
        var scopeVersion = _scopeVersion;
        var draftGeneration = _nicheDraftGeneration;
        var isCreatingNewNiche = _isCreatingNewNiche;
        var draftNicheId = _draftNicheId;
        var selectedNicheId = _selectedNiche?.Id;
        var draft = CurrentNicheDraft();
        NicheManagementResult result;
        if (isCreatingNewNiche)
        {
            result = await _niches.CreateNicheAsync(new NicheManagementCreateRequest(storeId, NicheName, CurrentNicheContext()), cancellationToken);
        }
        else
        {
            if (_selectedNiche is null) { _reportError("Select a niche before saving."); return; }
            result = await _niches.UpdateNicheAsync(new NicheManagementUpdateRequest(_selectedNiche.Id, NicheName, CurrentNicheContext()), cancellationToken);
        }
        if (scope != Scope || scopeVersion != _scopeVersion) { if (result.Succeeded) _workspaceChanged(); return; }
        if (draftGeneration != _nicheDraftGeneration || isCreatingNewNiche != _isCreatingNewNiche || draftNicheId != _draftNicheId || selectedNicheId != _selectedNiche?.Id || draft != CurrentNicheDraft()) { if (result.Succeeded) _workspaceChanged(); return; }
        if (isCreatingNewNiche && result.Succeeded) { _isCreatingNewNiche = false; _draftNicheId = null; }
        _applyNicheResult(result);
    }

    public async Task ArchiveSelectedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (_isCreatingNewStore) { _reportError("Save the new store before archiving it."); return; }
        if (_selectedStore is null) { _reportError("Select a store before archiving."); return; }
        var scope = Scope;
        var result = await _stores.ArchiveStoreAsync(_selectedStore.Id, cancellationToken);
        if (scope == Scope) _applyStoreResult(result);
    }

    public async Task ArchiveSelectedNicheAsync(CancellationToken cancellationToken = default)
    {
        if (_niches is null) { _reportError("Niche management is not available."); return; }
        if (_isCreatingNewNiche) { _reportError("Save the new niche before archiving it."); return; }
        if (_selectedNiche is null) { _reportError("Select a niche before archiving."); return; }
        var scope = Scope;
        var result = await _niches.ArchiveNicheAsync(_selectedNiche.Id, cancellationToken);
        if (scope == Scope) _applyNicheResult(result);
    }

    public async Task RestoreStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var scope = Scope;
        var result = await _stores.RestoreStoreAsync(store.Id, cancellationToken);
        if (scope == Scope || scope.StoreId == store.Id) _applyStoreResult(result);
    }

    public async Task RestoreNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        if (_niches is null) { _reportError("Niche management is not available."); return; }
        ArgumentNullException.ThrowIfNull(niche);
        var scope = Scope;
        var result = await _niches.RestoreNicheAsync(niche.Id, cancellationToken);
        if (scope == Scope && niche.StoreId == Scope.StoreId) _applyNicheResult(result);
    }

    public async Task<bool> DeleteStoreAsync(StoreSummary store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var scope = Scope;
        var result = await _stores.DeleteStoreAsync(new StoreManagementDeleteRequest(store.Id, true), cancellationToken);
        var isCurrentScope = scope == Scope;
        if (isCurrentScope) _applyStoreResult(result);
        return isCurrentScope && result.Succeeded;
    }

    public async Task<bool> DeleteNicheAsync(NicheSummary niche, CancellationToken cancellationToken = default)
    {
        if (_niches is null) { _reportError("Niche management is not available."); return false; }
        ArgumentNullException.ThrowIfNull(niche);
        var scope = Scope;
        var result = await _niches.DeleteNicheAsync(new NicheManagementDeleteRequest(niche.Id, true), cancellationToken);
        var isCurrentScope = scope == Scope && niche.StoreId == Scope.StoreId;
        if (isCurrentScope) _applyNicheResult(result);
        return isCurrentScope && result.Succeeded;
    }

    public async Task RefreshNichePopulationAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (_population is null) return;
        try { _populationAvailability = await _population.GetAvailabilityAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { _populationAvailability = new(AiAvailabilityKind.InvalidConfiguration, "AI settings could not be checked. Open AI settings and try again."); }
        RaisePopulation();
    }

    public async Task PopulateNicheAsync(CancellationToken cancellationToken = default)
    {
        if (!CanPopulateNiche || _population is null || _selectedNiche is null) return;
        var fields = BlankPopulationFields();
        if (fields.Count == 0) { _populationMessage = "All eligible niche fields already contain values."; RaisePopulation(); return; }
        var version = ++_populationVersion;
        var scope = Scope;
        var nicheId = _selectedNiche.Id;
        var isDraft = _isCreatingNewNiche;
        _isPopulationBusy = true; _populationMessage = null; RaisePopulation();
        try
        {
            var result = await _population.PopulateAsync(new NichePopulationRequest(NicheName, fields), cancellationToken);
            if (version != _populationVersion || scope != Scope || nicheId != _selectedNiche?.Id || isDraft != _isCreatingNewNiche) return;
            if (!result.Succeeded) { _populationMessage = result.Message ?? "No usable niche suggestions were returned. Try again."; return; }
            var applied = result.Suggestions.Count(pair => ApplySuggestion(pair.Key, pair.Value));
            _populationMessage = applied > 0 ? "Suggestions were added to the draft. Review them before saving." : "No new suggestions were applied because the fields were edited while the request was running.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { _populationMessage = "Population was canceled. Your existing niche values were preserved."; }
        catch { _populationMessage = "AI could not populate the niche fields. Check AI settings or try again."; }
        finally { if (version == _populationVersion) { _isPopulationBusy = false; RaisePopulation(); } }
    }

    private bool CanManageScope => Scope.StoreId is not null && !Scope.IsStoreArchived && !Scope.IsCreatingNewStore;
    private void SetServiceStore() => _niches?.SetActiveWorkspace(Scope.WorkspaceId);
    private StoreContext CurrentStoreContext() => new(EmptyToNull(Description), EmptyToNull(Notes), EmptyToNull(TargetMarket), EmptyToNull(BrandDirection), EmptyToNull(PlanningContext), EmptyToNull(Url), PrintifyShopId, PrintifyShopTitle);
    private NicheContext CurrentNicheContext() => new(EmptyToNull(NicheDescription), EmptyToNull(NicheAudience), EmptyToNull(NicheHumorStyle), EmptyToNull(NicheVisualStyleGuidance), EmptyToNull(NicheConstraints), EmptyToNull(NicheRisks), EmptyToNull(NicheResearchNotes), EmptyToNull(NicheNotes));
    private StoreDraft CurrentStoreDraft() => new(NewStoreName, Description, Notes, TargetMarket, BrandDirection, PlanningContext, Url, SelectedFulfillmentStrategy, PrintifyShopId, PrintifyShopTitle, PrintifyShopSelectionChanged);
    private NicheDraft CurrentNicheDraft() => new(NicheName, NicheDescription, NicheAudience, NicheHumorStyle, NicheVisualStyleGuidance, NicheConstraints, NicheRisks, NicheResearchNotes, NicheNotes);
    private void ApplyStoreFields(StoreSummary? store) { NewStoreName = store?.Name ?? string.Empty; Description = store?.Context.Description ?? string.Empty; Notes = store?.Context.Notes ?? string.Empty; TargetMarket = store?.Context.TargetMarket ?? string.Empty; BrandDirection = store?.Context.BrandDirection ?? string.Empty; PlanningContext = store?.Context.PlanningContext ?? string.Empty; Url = store?.Context.Url ?? string.Empty; SelectedFulfillmentStrategy = store?.FulfillmentStrategy ?? FulfillmentStrategy.Manual; PrintifyShopId = store?.Context.PrintifyShopId; PrintifyShopTitle = store?.Context.PrintifyShopTitle; }
    private void ApplyNicheFields(NicheSummary? niche) { NicheName = niche?.Name ?? string.Empty; NicheDescription = niche?.Context.Description ?? string.Empty; NicheAudience = niche?.Context.Audience ?? string.Empty; NicheHumorStyle = niche?.Context.HumorStyle ?? string.Empty; NicheVisualStyleGuidance = niche?.Context.VisualStyleGuidance ?? string.Empty; NicheConstraints = niche?.Context.Constraints ?? string.Empty; NicheRisks = niche?.Context.Risks ?? string.Empty; NicheResearchNotes = niche?.Context.ResearchNotes ?? string.Empty; NicheNotes = niche?.Context.Notes ?? string.Empty; }
    private void ClearStoreDraft() { NewStoreName = Description = Notes = TargetMarket = BrandDirection = PlanningContext = Url = string.Empty; SelectedFulfillmentStrategy = FulfillmentStrategy.Manual; PrintifyShopId = null; PrintifyShopTitle = null; }
    private void ClearNicheDraft() { NicheName = NicheDescription = NicheAudience = NicheHumorStyle = NicheVisualStyleGuidance = NicheConstraints = NicheRisks = NicheResearchNotes = NicheNotes = string.Empty; }
    private IReadOnlyList<NichePopulationField> BlankPopulationFields() { var f = new List<NichePopulationField>(); if (string.IsNullOrWhiteSpace(NicheDescription)) f.Add(NichePopulationField.Description); if (string.IsNullOrWhiteSpace(NicheAudience)) f.Add(NichePopulationField.Audience); if (string.IsNullOrWhiteSpace(NicheHumorStyle)) f.Add(NichePopulationField.HumorStyle); if (string.IsNullOrWhiteSpace(NicheVisualStyleGuidance)) f.Add(NichePopulationField.VisualStyleGuidance); if (string.IsNullOrWhiteSpace(NicheConstraints)) f.Add(NichePopulationField.Constraints); if (string.IsNullOrWhiteSpace(NicheNotes)) f.Add(NichePopulationField.Notes); return f; }
    private bool ApplySuggestion(NichePopulationField field, string value) { if (string.IsNullOrWhiteSpace(value)) return false; switch (field) { case NichePopulationField.Description when string.IsNullOrWhiteSpace(NicheDescription): NicheDescription = value; return true; case NichePopulationField.Audience when string.IsNullOrWhiteSpace(NicheAudience): NicheAudience = value; return true; case NichePopulationField.HumorStyle when string.IsNullOrWhiteSpace(NicheHumorStyle): NicheHumorStyle = value; return true; case NichePopulationField.VisualStyleGuidance when string.IsNullOrWhiteSpace(NicheVisualStyleGuidance): NicheVisualStyleGuidance = value; return true; case NichePopulationField.Constraints when string.IsNullOrWhiteSpace(NicheConstraints): NicheConstraints = value; return true; case NichePopulationField.Notes when string.IsNullOrWhiteSpace(NicheNotes): NicheNotes = value; return true; default: return false; } }
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void RaiseProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        RaisePropertyChanged(propertyName);
    private void RaiseDirtyState() { RaiseProperty(nameof(HasUnsavedStoreChanges)); RaiseProperty(nameof(HasUnsavedNicheChanges)); }
    private void RaisePopulation() { RaiseProperty(nameof(IsNichePopulationBusy)); RaiseProperty(nameof(NichePopulationStatusMessage)); RaiseProperty(nameof(HasNichePopulationStatus)); RaiseProperty(nameof(CanPopulateNiche)); }
    private void RaiseAll() { RaiseProperty(nameof(SelectedStore)); RaiseProperty(nameof(SelectedNiche)); RaiseProperty(nameof(IsCreatingNewStore)); RaiseProperty(nameof(IsCreatingNewNiche)); RaiseDirtyState(); RaisePopulation(); }
}
