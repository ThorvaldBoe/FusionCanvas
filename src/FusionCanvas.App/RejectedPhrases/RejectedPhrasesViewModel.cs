using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.RejectedPhrases;

namespace FusionCanvas.App.RejectedPhrases;

public sealed class RejectedPhrasesViewModel : INotifyPropertyChanged
{
    private readonly IRejectedPhraseManagementService _service;
    private RejectedPhraseManagementState _confirmedState;
    private RejectedPhraseScope _scope;
    private RejectedPhraseSummary? _selectedRejection;
    private string _searchText = string.Empty;
    private string _phrase = string.Empty;
    private string _reason = string.Empty;
    private bool _isNewDraft;
    private bool _isLoaded;
    private bool _isBusy;
    private bool _deleteConfirmationVisible;
    private bool _unsavedPromptVisible;
    private string? _errorMessage;
    private Func<Task>? _pendingTransition;
    private int _searchGeneration;
    private Task _activeOperation = Task.CompletedTask;

    public RejectedPhrasesViewModel(IRejectedPhraseManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _scope = RejectedPhraseScope.WholeWorkspaceView;
        _confirmedState = RejectedPhraseManagementState.Empty(_scope);

        SelectRejectionCommand = new RelayCommand(parameter =>
        {
            if (parameter is RejectedPhraseSummary summary)
            {
                RequestTransition(() =>
                {
                    SelectConfirmed(summary.Id);
                    return Task.CompletedTask;
                });
            }
        });
        SelectScopeCommand = new RelayCommand(parameter =>
        {
            if (parameter is ScopeOption option)
            {
                RequestTransition(() => ChangeScopeAsync(option.Scope));
            }
        });
        NewCommand = new RelayCommand(_ => RequestTransition(StartNewDraftAsync));
        SaveCommand = new RelayCommand(_ => Begin(SaveAsync()));
        RequestDeleteCommand = new RelayCommand(_ => RequestDelete());
        ConfirmDeleteCommand = new RelayCommand(_ => Begin(ConfirmDeleteAsync()));
        CancelDeleteCommand = new RelayCommand(_ => CancelDelete());
        SaveAndContinueCommand = new RelayCommand(_ => Begin(SaveAndContinueAsync()));
        DiscardAndContinueCommand = new RelayCommand(_ => Begin(DiscardAndContinueAsync()));
        CancelPendingCommand = new RelayCommand(_ => CancelPendingTransition());
        CloseCommand = new RelayCommand(_ => RequestTransition(RequestCloseAsync));
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CloseRequested;

    public event EventHandler? FocusPhraseRequested;

    public event EventHandler? FocusEditorRequested;

    public event EventHandler? StateMutated;

    public ObservableCollection<RejectedPhraseSummary> Rejections { get; } = [];

    public ObservableCollection<ScopeOption> ScopeOptions { get; } = [];

    public RejectedPhraseScope Scope
    {
        get => _scope;
        private set
        {
            if (SetField(ref _scope, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public RejectedPhraseSummary? SelectedRejection
    {
        get => _selectedRejection;
        private set
        {
            if (SetField(ref _selectedRejection, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetField(ref _searchText, value ?? string.Empty))
            {
                return;
            }

            var generation = Interlocked.Increment(ref _searchGeneration);
            Begin(RefreshSearchAsync(generation));
        }
    }

    public string Phrase
    {
        get => _phrase;
        set
        {
            if (SetField(ref _phrase, value ?? string.Empty))
            {
                RaiseDraftProperties();
            }
        }
    }

    public string Reason
    {
        get => _reason;
        set
        {
            if (SetField(ref _reason, value ?? string.Empty))
            {
                RaiseDraftProperties();
            }
        }
    }

    public bool IsNewDraft
    {
        get => _isNewDraft;
        private set
        {
            if (SetField(ref _isNewDraft, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetField(ref _isLoaded, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public bool DeleteConfirmationVisible
    {
        get => _deleteConfirmationVisible;
        private set
        {
            if (SetField(ref _deleteConfirmationVisible, value))
            {
                RaiseStateProperties();
            }
        }
    }

    public bool UnsavedPromptVisible
    {
        get => _unsavedPromptVisible;
        private set
        {
            if (SetField(ref _unsavedPromptVisible, value))
            {
                RaiseStateProperties();
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasRejections => _confirmedState.AllRejections.Count > 0;

    public bool HasVisibleRejections => Rejections.Count > 0;

    public bool IsEmpty => !IsBusy && !HasRejections;

    public bool HasNoResults => !IsBusy && HasRejections && !HasVisibleRejections;

    public bool HasScopeOptions => ScopeOptions.Count > 0;

    public bool HasSelection => SelectedRejection is not null || IsNewDraft;

    public bool IsDirty =>
        IsNewDraft
            ? !string.IsNullOrWhiteSpace(Phrase) || !string.IsNullOrWhiteSpace(Reason)
            : SelectedRejection is not null &&
              (Phrase != SelectedRejection.Text || (Reason ?? string.Empty) != (SelectedRejection.Reason ?? string.Empty));

    public bool CanSave => HasSelection && IsDirty && !string.IsNullOrWhiteSpace(Phrase) && !IsBusy && !UnsavedPromptVisible && !DeleteConfirmationVisible;

    public bool CanDelete => SelectedRejection is not null && !IsNewDraft && !IsBusy && !UnsavedPromptVisible;

    public bool CanMutate => !IsBusy && !UnsavedPromptVisible && !DeleteConfirmationVisible;

    public string DeletePromptMessage =>
        SelectedRejection is null
            ? string.Empty
            : $"Permanently delete \"{SelectedRejection.Text}\"?";

    public string UnsavedPromptMessage =>
        IsNewDraft
            ? "Save this new rejected phrase before continuing?"
            : "Save changes to this rejected phrase before continuing?";

    public ICommand SelectRejectionCommand { get; }

    public ICommand SelectScopeCommand { get; }

    public ICommand NewCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand RequestDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand SaveAndContinueCommand { get; }

    public ICommand DiscardAndContinueCommand { get; }

    public ICommand CancelPendingCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand ClearSearchCommand { get; }

    public async Task OpenAsync(
        RejectedPhraseScope scope,
        IEnumerable<ScopeOption> scopeOptions,
        CancellationToken cancellationToken = default)
    {
        Scope = scope;
        ReplaceScopeOptions(scopeOptions);
        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _service.InitializeAsync(scope, SearchText, cancellationToken).ConfigureAwait(false);
            ApplyResult(result, selectAffected: false);
            if (result.Succeeded && SelectedRejection is null && !IsNewDraft)
            {
                SelectFirstVisibleOrClear();
            }

            IsLoaded = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task WhenIdleAsync() => _activeOperation;

    public void RequestClose() => RequestTransition(RequestCloseAsync);

    private async Task RefreshSearchAsync(int generation)
    {
        var result = await _service.LoadAsync(Scope, SearchText).ConfigureAwait(false);
        if (generation != Volatile.Read(ref _searchGeneration))
        {
            return;
        }

        ApplyResult(result, selectAffected: false, preserveDraft: true);
    }

    private async Task ChangeScopeAsync(RejectedPhraseScope scope)
    {
        Scope = scope;
        var result = await _service.LoadAsync(scope, SearchText).ConfigureAwait(false);
        ApplyResult(result, selectAffected: false);
        if (result.Succeeded)
        {
            SelectFirstVisibleOrClear();
        }
    }

    private void RequestSelection(Guid id)
    {
        if (SelectedRejection?.Id == id && !IsNewDraft)
        {
            return;
        }

        RequestTransition(() =>
        {
            SelectConfirmed(id);
            return Task.CompletedTask;
        });
    }

    private void RequestTransition(Func<Task> transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if (IsBusy)
        {
            return;
        }

        if (IsNewDraft && !IsDirty)
        {
            ClearDraft();
            Begin(transition());
            return;
        }

        if (IsDirty)
        {
            _pendingTransition = transition;
            DeleteConfirmationVisible = false;
            UnsavedPromptVisible = true;
            return;
        }

        Begin(transition());
    }

    private Task StartNewDraftAsync()
    {
        SelectedRejection = null;
        IsNewDraft = true;
        Phrase = string.Empty;
        Reason = string.Empty;
        DeleteConfirmationVisible = false;
        ClearMessages();
        FocusPhraseRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (!CanSave)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var result = IsNewDraft
                ? await _service.CreateAsync(
                    new RejectedPhraseCreateRequest(Phrase, Reason, Scope, SearchText)).ConfigureAwait(false)
                : await _service.UpdateAsync(
                    new RejectedPhraseUpdateRequest(
                        SelectedRejection!.Id,
                        Phrase,
                        Reason,
                        Scope,
                        SearchText)).ConfigureAwait(false);

            ApplyResult(result, selectAffected: result.Succeeded);
            if (result.Succeeded)
            {
                IsNewDraft = false;
                FocusEditorRequested?.Invoke(this, EventArgs.Empty);
                StateMutated?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RequestDelete()
    {
        if (!CanDelete)
        {
            return;
        }

        ClearMessages();
        DeleteConfirmationVisible = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (SelectedRejection is null || IsBusy)
        {
            return;
        }

        var visibleIndex = Rejections
            .Select((item, index) => (item, index))
            .FirstOrDefault(entry => entry.item.Id == SelectedRejection.Id)
            .index;
        var id = SelectedRejection.Id;
        IsBusy = true;
        DeleteConfirmationVisible = false;
        ClearMessages();
        try
        {
            var result = await _service.DeleteAsync(id, Scope, SearchText).ConfigureAwait(false);
            ApplyResult(result, selectAffected: false);
            if (result.Succeeded)
            {
                var replacement = Rejections.Count == 0
                    ? null
                    : Rejections[Math.Min(visibleIndex, Rejections.Count - 1)];
                if (replacement is null)
                {
                    ClearDraft();
                }
                else
                {
                    SelectConfirmed(replacement.Id);
                }

                StateMutated?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsBusy = false;
            FocusEditorRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CancelDelete()
    {
        DeleteConfirmationVisible = false;
        FocusEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAndContinueAsync()
    {
        var transition = _pendingTransition;
        UnsavedPromptVisible = false;
        await SaveAsync().ConfigureAwait(false);
        if (!HasError && !IsDirty && transition is not null)
        {
            _pendingTransition = null;
            await transition().ConfigureAwait(false);
        }
        else
        {
            _pendingTransition = transition;
        }
    }

    private async Task DiscardAndContinueAsync()
    {
        var transition = _pendingTransition;
        _pendingTransition = null;
        UnsavedPromptVisible = false;
        RestoreConfirmedDraft();
        if (transition is not null)
        {
            await transition().ConfigureAwait(false);
        }
    }

    private void CancelPendingTransition()
    {
        _pendingTransition = null;
        UnsavedPromptVisible = false;
        FocusEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private Task RequestCloseAsync()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void ApplyResult(
        RejectedPhraseManagementResult result,
        bool selectAffected,
        bool preserveDraft = false)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        var selectedId = SelectedRejection?.Id;
        _confirmedState = result.State;
        Scope = result.State.Scope;
        ReplaceVisible(result.State.VisibleRejections);
        ErrorMessage = null;

        if (selectAffected && result.AffectedSummary is not null)
        {
            SelectConfirmed(result.AffectedSummary.Id);
        }
        else if (!preserveDraft && selectedId is Guid id)
        {
            var confirmed = result.State.AllRejections.FirstOrDefault(item => item.Id == id);
            if (confirmed is null)
            {
                ClearDraft();
            }
            else
            {
                SelectConfirmed(confirmed.Id);
            }
        }

        RaiseStateProperties();
    }

    private void ReplaceVisible(IReadOnlyList<RejectedPhraseSummary> visible)
    {
        Rejections.Clear();
        foreach (var rejection in visible)
        {
            Rejections.Add(rejection);
        }

        OnPropertyChanged(nameof(HasVisibleRejections));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasScopeOptions));
    }

    private void ReplaceScopeOptions(IEnumerable<ScopeOption> options)
    {
        ScopeOptions.Clear();
        foreach (var option in options)
        {
            ScopeOptions.Add(option);
        }
    }

    private void SelectConfirmed(Guid id)
    {
        var confirmed = _confirmedState.AllRejections.FirstOrDefault(item => item.Id == id);
        if (confirmed is null)
        {
            return;
        }

        SelectedRejection = confirmed;
        IsNewDraft = false;
        Phrase = confirmed.Text;
        Reason = confirmed.Reason ?? string.Empty;
        DeleteConfirmationVisible = false;
        UnsavedPromptVisible = false;
        ClearMessages();
    }

    private void SelectFirstVisibleOrClear()
    {
        if (Rejections.Count > 0)
        {
            SelectConfirmed(Rejections[0].Id);
        }
        else
        {
            ClearDraft();
        }
    }

    private void RestoreConfirmedDraft()
    {
        if (SelectedRejection is null)
        {
            ClearDraft();
            return;
        }

        SelectConfirmed(SelectedRejection.Id);
    }

    private void ClearDraft()
    {
        SelectedRejection = null;
        IsNewDraft = false;
        Phrase = string.Empty;
        Reason = string.Empty;
        DeleteConfirmationVisible = false;
        UnsavedPromptVisible = false;
        RaiseStateProperties();
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
    }

    private void Begin(Task task)
    {
        _activeOperation = ObserveAsync(task);
    }

    private async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsBusy = false;
        }
    }

    private void RaiseDraftProperties()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(UnsavedPromptMessage));
    }

    private void RaiseStateProperties()
    {
        OnPropertyChanged(nameof(HasRejections));
        OnPropertyChanged(nameof(HasVisibleRejections));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanMutate));
        OnPropertyChanged(nameof(DeletePromptMessage));
        OnPropertyChanged(nameof(UnsavedPromptMessage));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
