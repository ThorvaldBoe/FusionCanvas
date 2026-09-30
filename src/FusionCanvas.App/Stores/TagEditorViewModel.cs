using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Tags;

namespace FusionCanvas.App.Stores;

/// <summary>Owns tag management state and workflows for one explicit store scope.</summary>
public sealed class TagEditorViewModel : INotifyPropertyChanged
{
    private sealed record Draft(string Name, string? Color, string? Description);
    private readonly ITagManagementService? _service;
    private readonly Action<Func<CancellationToken, Task>> _runOperation;
    private readonly Action<string?> _reportError;
    private readonly Action _workspaceChanged;
    private StoreManagementScope _scope = StoreManagementScope.Empty;
    private Draft _originalDraft = new(string.Empty, null, null);
    private string _tagName = string.Empty;
    private string? _tagColor;
    private string _tagDescription = string.Empty;
    private IReadOnlyList<TagSummary> _activeTags = [];
    private IReadOnlyList<TagSummary> _archivedTags = [];
    private TagSummary? _selectedTag;
    private TagSummary? _pendingDeleteTag;
    private bool _needsFirstTag;
    private bool _isCreatingDraft;
    private bool _deleteWarningVisible;
    private int? _pendingDeleteItemCount;
    private bool _isRefreshingDeleteItemCount;
    private Guid? _draftTagId;
    private long _loadVersion;
    private long _draftGeneration;
    private long _deleteCountVersion;

    public TagEditorViewModel(
        ITagManagementService? service,
        Action<Func<CancellationToken, Task>>? runOperation = null,
        Action<string?>? reportError = null,
        Action? workspaceChanged = null)
    {
        _service = service;
        _runOperation = runOperation ?? (operation => _ = operation(CancellationToken.None));
        _reportError = reportError ?? (_ => { });
        _workspaceChanged = workspaceChanged ?? (() => { });
        EditTagCommand = new RelayCommand(parameter => { if (parameter is TagSummary tag) SelectTagForEditing(tag); });
        StartCreateTagCommand = new RelayCommand(_ => StartCreateTag());
        SaveSelectedTagCommand = new RelayCommand(_ => _runOperation(SaveSelectedTagAsync));
        ArchiveSelectedTagCommand = new RelayCommand(_ => _runOperation(ArchiveSelectedTagAsync));
        RestoreTagCommand = new RelayCommand(parameter => { if (parameter is TagSummary tag) _runOperation(token => RestoreTagAsync(tag, token)); });
        RequestDeleteSelectedTagCommand = new RelayCommand(_ => RequestDeleteSelectedTag());
        ConfirmDeleteTagCommand = new RelayCommand(_ => _runOperation(ConfirmDeleteTagAsync));
        CancelDeleteTagCommand = new RelayCommand(_ => ClearDeleteWarning());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Requests coordinator-owned confirmation before discarding this editor's draft.</summary>
    public event Action<Action>? DiscardRequested;

    public StoreManagementScope Scope => _scope;
    public string TagName { get => _tagName; set { if (SetField(ref _tagName, value)) RaiseDraftProperties(); } }
    public string? TagColor { get => _tagColor; set { if (SetField(ref _tagColor, value)) RaiseDraftProperties(); } }
    public string TagDescription { get => _tagDescription; set { if (SetField(ref _tagDescription, value)) RaiseDraftProperties(); } }
    public IReadOnlyList<TagSummary> ActiveTags => _activeTags;
    public IReadOnlyList<TagSummary> ArchivedTags => _archivedTags;
    public IReadOnlyList<TagSummary> EditorActiveTags => _isCreatingDraft && DraftTag() is { } draft ? _activeTags.Concat([draft]).ToArray() : _activeTags;
    public TagSummary? SelectedTag => _selectedTag;
    public bool NeedsFirstTag => _needsFirstTag;
    public bool IsCreatingDraft => _isCreatingDraft;
    public bool HasActiveTags => _activeTags.Count > 0;
    public bool HasArchivedTags => _archivedTags.Count > 0;
    public bool HasSelectedTag => _selectedTag is not null;
    public bool CanRestoreSelectedTag => _selectedTag is { IsArchived: true };
    public bool HasUnsavedChanges => CurrentDraft() != _originalDraft;
    public bool CanSaveSelectedTag => _service is not null && (_isCreatingDraft || (_selectedTag is not null && HasUnsavedChanges));
    public bool CanArchiveSelectedTag => _service is not null && _selectedTag is { IsArchived: false } && !_isCreatingDraft && CanManageScope;
    public bool CanDeleteSelectedTag => _service is not null && _selectedTag is not null && !_isCreatingDraft && CanManageScope;
    public bool DeleteWarningVisible => _deleteWarningVisible;
    public bool CanConfirmDeleteTag => _deleteWarningVisible && _pendingDeleteTag is not null && !_isRefreshingDeleteItemCount;
    public string DeleteWarningMessage => _pendingDeleteTag is null
        ? "Permanent deletion cannot be undone."
        : _pendingDeleteItemCount is not { } itemCount
            ? _isRefreshingDeleteItemCount
                ? $"Checking how many items use tag '{_pendingDeleteTag.Name}'."
                : $"The number of items that use tag '{_pendingDeleteTag.Name}' is unknown. This cannot be undone."
            : itemCount == 0
                ? $"Delete tag '{_pendingDeleteTag.Name}' permanently? It is applied to 0 items. This cannot be undone."
                : $"Delete tag '{_pendingDeleteTag.Name}' permanently? It will be removed from {itemCount} item{(itemCount == 1 ? string.Empty : "s")}. This cannot be undone.";

    public ICommand EditTagCommand { get; }
    public ICommand StartCreateTagCommand { get; }
    public ICommand SaveSelectedTagCommand { get; }
    public ICommand ArchiveSelectedTagCommand { get; }
    public ICommand RestoreTagCommand { get; }
    public ICommand RequestDeleteSelectedTagCommand { get; }
    public ICommand ConfirmDeleteTagCommand { get; }
    public ICommand CancelDeleteTagCommand { get; }

    public void SetScope(StoreManagementScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (_scope == scope) return;
        _scope = scope;
        _loadVersion++;
        _draftGeneration++;
        _deleteCountVersion++;
        SetServiceActiveStoreForCurrentScope();
        _isCreatingDraft = false;
        _draftTagId = null;
        ClearDeleteWarning();
        _activeTags = [];
        _archivedTags = [];
        _selectedTag = null;
        _needsFirstTag = false;
        ClearEditorFields();
        CaptureOriginalDraft();
        RaiseAllProperties();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Scope)));
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_service is null) return;
        var scope = _scope;
        var version = ++_loadVersion;
        var state = await _service.LoadAsync(CanManageScope ? scope.StoreId : null, cancellationToken);
        if (version != _loadVersion || scope != _scope)
        {
            SetServiceActiveStoreForCurrentScope();
            return;
        }
        ApplyState(state);
    }

    public void SelectTagForEditing(TagSummary tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (!CanManageScope || tag.StoreId != _scope.StoreId) return;
        if (_isCreatingDraft && tag.Id == _draftTagId) return;
        if (HasUnsavedChanges && _selectedTag?.Id != tag.Id)
        {
            var requestedScope = _scope;
            DiscardRequested?.Invoke(() =>
            {
                if (requestedScope == _scope && tag.StoreId == _scope.StoreId)
                    PerformSelectTag(tag);
            });
            return;
        }
        PerformSelectTag(tag);
    }

    public void StartCreateTag()
    {
        if (!CanManageScope)
        {
            _reportError("Select an active saved store before creating a tag.");
            return;
        }
        if (HasUnsavedChanges)
        {
            var requestedScope = _scope;
            DiscardRequested?.Invoke(() =>
            {
                if (requestedScope == _scope && CanManageScope)
                    BeginCreateDraft();
            });
            return;
        }
        BeginCreateDraft();
    }

    public void OnTabSelected()
    {
        if (_needsFirstTag && CanManageScope && !_isCreatingDraft)
            BeginCreateDraft();
    }

    public void DiscardUnsavedChanges()
    {
        _draftGeneration++;
        if (_isCreatingDraft)
        {
            _isCreatingDraft = false;
            _draftTagId = null;
            _selectedTag = _activeTags.FirstOrDefault(tag => tag.Id == _selectedTag?.Id) ?? _activeTags.FirstOrDefault();
        }
        ApplySelectedFields(_selectedTag);
        CaptureOriginalDraft();
        RaiseAllProperties();
    }

    public async Task SaveSelectedTagAsync(CancellationToken cancellationToken = default)
    {
        if (_service is null) { _reportError("Tag management is not available."); return; }
        if (!CanManageScope || _scope.StoreId is not { } storeId) { _reportError("Select a store before saving a tag."); return; }
        var scope = _scope;
        var draftGeneration = _draftGeneration;
        var isCreatingDraft = _isCreatingDraft;
        var draftTagId = _draftTagId;
        var selectedTagId = _selectedTag?.Id;
        var draft = CurrentDraft();
        TagManagementResult result;
        if (isCreatingDraft)
        {
            result = await _service.CreateTagAsync(new TagManagementCreateRequest(storeId, TagName, EmptyToNull(TagDescription), TagColor), cancellationToken);
        }
        else
        {
            var selectedTag = _selectedTag;
            if (selectedTag is null) { _reportError("Select a tag before saving."); return; }
            result = await _service.UpdateTagAsync(new TagManagementUpdateRequest(selectedTag.Id, TagName, EmptyToNull(TagDescription), TagColor), cancellationToken);
        }
        if (scope != _scope) { SetServiceActiveStoreForCurrentScope(); if (result.Succeeded) _workspaceChanged(); return; }
        if (draftGeneration != _draftGeneration || isCreatingDraft != _isCreatingDraft || draftTagId != _draftTagId || selectedTagId != _selectedTag?.Id || draft != CurrentDraft()) { if (result.Succeeded) _workspaceChanged(); return; }
        if (isCreatingDraft && result.Succeeded) { _isCreatingDraft = false; _draftTagId = null; }
        ApplyResult(result);
    }

    public async Task ArchiveSelectedTagAsync(CancellationToken cancellationToken = default)
    {
        if (_service is null) { _reportError("Tag management is not available."); return; }
        if (_isCreatingDraft) { _reportError("Save the new tag before archiving it."); return; }
        var selectedTag = _selectedTag;
        if (!CanManageScope || selectedTag is null) { _reportError("Select an active tag before archiving."); return; }
        var scope = _scope;
        var result = await _service.ArchiveTagAsync(selectedTag.Id, cancellationToken);
        if (scope == _scope) ApplyResult(result);
        else SetServiceActiveStoreForCurrentScope();
    }

    public async Task RestoreTagAsync(TagSummary tag, CancellationToken cancellationToken = default)
    {
        if (_service is null) { _reportError("Tag management is not available."); return; }
        ArgumentNullException.ThrowIfNull(tag);
        if (!CanManageScope || tag.StoreId != _scope.StoreId) return;
        var scope = _scope;
        var result = await _service.RestoreTagAsync(tag.Id, cancellationToken);
        if (scope == _scope) ApplyResult(result);
        else SetServiceActiveStoreForCurrentScope();
    }

    public void RequestDeleteSelectedTag()
    {
        if (_service is null) { _reportError("Tag management is not available."); return; }
        if (_isCreatingDraft) { _reportError("Save the new tag before deleting it."); return; }
        if (!CanManageScope || _selectedTag is null) { _reportError("Select a tag before deleting."); return; }
        _pendingDeleteTag = _selectedTag;
        _pendingDeleteItemCount = null;
        _isRefreshingDeleteItemCount = true;
        _deleteWarningVisible = true;
        _deleteCountVersion++;
        RaiseDeleteWarningProperties();
        _runOperation(RefreshPendingDeleteItemCountAsync);
    }

    public async Task ConfirmDeleteTagAsync(CancellationToken cancellationToken = default)
    {
        if (_service is null) { _reportError("Tag management is not available."); return; }
        var pendingDeleteTag = _pendingDeleteTag;
        if (pendingDeleteTag is null || !CanManageScope) { _reportError("Select a tag before deleting."); return; }
        if (_isRefreshingDeleteItemCount) { _reportError("Wait for the tag usage count to finish loading."); return; }
        var scope = _scope;
        var result = await _service.DeleteTagAsync(new TagManagementDeleteRequest(pendingDeleteTag.Id, ConfirmPermanentDeletion: true), cancellationToken);
        if (scope != _scope)
        {
            SetServiceActiveStoreForCurrentScope();
            return;
        }
        _reportError(result.Error);
        ApplyState(result.State);
        if (result.Succeeded) SelectDefaultTagForEditing();
        ClearDeleteWarning();
        if (result.Succeeded) _workspaceChanged();
    }

    public async Task RefreshPendingDeleteItemCountAsync(CancellationToken cancellationToken = default)
    {
        var pendingDeleteTag = _pendingDeleteTag;
        if (_service is null || pendingDeleteTag is null) return;
        var scope = _scope;
        var tagId = pendingDeleteTag.Id;
        var version = _deleteCountVersion;
        try
        {
            var count = await _service.GetTagApplicationCountAsync(tagId, cancellationToken);
            if (scope != _scope || version != _deleteCountVersion || _pendingDeleteTag?.Id != tagId || !_deleteWarningVisible) return;
            _pendingDeleteItemCount = count;
        }
        finally
        {
            if (scope == _scope && version == _deleteCountVersion && _pendingDeleteTag?.Id == tagId && _deleteWarningVisible)
            {
                _isRefreshingDeleteItemCount = false;
                RaiseDeleteWarningProperties();
            }
        }
    }

    public void ClearDeleteWarning()
    {
        _pendingDeleteTag = null;
        _pendingDeleteItemCount = null;
        _isRefreshingDeleteItemCount = false;
        _deleteWarningVisible = false;
        _deleteCountVersion++;
        RaiseDeleteWarningProperties();
    }

    private bool CanManageScope => _scope.StoreId is not null && !_scope.IsStoreArchived && !_scope.IsCreatingNewStore;

    private void SetServiceActiveStoreForCurrentScope() =>
        _service?.SetActiveStore(CanManageScope ? _scope.StoreId : null);

    private void ApplyState(TagManagementState state)
    {
        _activeTags = state.ActiveTags;
        _archivedTags = state.ArchivedTags;
        _selectedTag = _isCreatingDraft ? DraftTag()
            : _activeTags.FirstOrDefault(tag => tag.Id == _selectedTag?.Id)
                ?? _archivedTags.FirstOrDefault(tag => tag.Id == _selectedTag?.Id)
                ?? _activeTags.FirstOrDefault() ?? _archivedTags.FirstOrDefault();
        _needsFirstTag = state.NeedsFirstTag;
        if (!_isCreatingDraft) { ApplySelectedFields(_selectedTag); CaptureOriginalDraft(); }
        RaiseAllProperties();
    }

    private void ApplyResult(TagManagementResult result)
    {
        _reportError(result.Error);
        ApplyState(result.State);
        if (result.Tag is not null && result.Succeeded)
        {
            PerformSelectTag(result.Tag);
            _workspaceChanged();
        }
    }

    private void PerformSelectTag(TagSummary tag)
    {
        _draftGeneration++;
        _isCreatingDraft = false;
        _draftTagId = null;
        _selectedTag = tag;
        ApplySelectedFields(tag);
        CaptureOriginalDraft();
        ClearDeleteWarning();
        RaiseAllProperties();
    }

    private void BeginCreateDraft()
    {
        _draftGeneration++;
        _isCreatingDraft = true;
        _draftTagId = Guid.NewGuid();
        _selectedTag = DraftTag();
        ClearDeleteWarning();
        ClearEditorFields();
        CaptureOriginalDraft();
        RaiseAllProperties();
        _reportError(null);
    }

    private void SelectDefaultTagForEditing()
    {
        var tag = _activeTags.FirstOrDefault() ?? _archivedTags.FirstOrDefault();
        if (tag is null) { _selectedTag = null; ClearEditorFields(); CaptureOriginalDraft(); RaiseAllProperties(); }
        else PerformSelectTag(tag);
    }

    private TagSummary? DraftTag()
    {
        if (!_isCreatingDraft || _draftTagId is not { } id || _scope.StoreId is not { } storeId) return null;
        var now = DateTimeOffset.Now;
        return new TagSummary(id, storeId, string.IsNullOrWhiteSpace(TagName) ? "New tag" : TagName.Trim(), EmptyToNull(TagDescription), TagColor, false, now, now);
    }

    private void ApplySelectedFields(TagSummary? tag)
    {
        if (tag is null) { ClearEditorFields(); return; }
        TagName = tag.Name;
        TagColor = tag.Color;
        TagDescription = tag.Description ?? string.Empty;
    }

    private void ClearEditorFields() { TagName = string.Empty; TagColor = null; TagDescription = string.Empty; }
    private Draft CurrentDraft() => new(TagName, string.IsNullOrWhiteSpace(TagColor) ? null : TagColor, EmptyToNull(TagDescription));
    private void CaptureOriginalDraft() { _originalDraft = CurrentDraft(); RaiseDraftProperties(); }

    private void RaiseDraftProperties()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnsavedChanges)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSaveSelectedTag)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditorActiveTags)));
    }

    private void RaiseAllProperties()
    {
        foreach (var property in new[] { nameof(ActiveTags), nameof(ArchivedTags), nameof(EditorActiveTags), nameof(SelectedTag), nameof(NeedsFirstTag), nameof(HasActiveTags), nameof(HasArchivedTags), nameof(HasSelectedTag), nameof(CanRestoreSelectedTag), nameof(IsCreatingDraft), nameof(CanSaveSelectedTag), nameof(CanArchiveSelectedTag), nameof(CanDeleteSelectedTag) })
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        RaiseDraftProperties();
    }

    private void RaiseDeleteWarningProperties()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeleteWarningVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanConfirmDeleteTag)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeleteWarningMessage)));
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
