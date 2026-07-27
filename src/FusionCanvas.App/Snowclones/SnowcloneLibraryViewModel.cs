using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.App.Snowclones;

public sealed class SnowcloneLibraryViewModel : INotifyPropertyChanged
{
    private readonly ISnowcloneLibraryService _service;
    private ISnowcloneCsvFilePicker _filePicker;
    private SnowcloneLibraryState _confirmedState = SnowcloneLibraryState.Empty;
    private SnowcloneSummary? _selectedSnowclone;
    private string _searchText = string.Empty;
    private string _phrase = string.Empty;
    private string _guidance = string.Empty;
    private bool _isNewDraft;
    private bool _isLoaded;
    private bool _isBusy;
    private bool _deleteConfirmationVisible;
    private bool _unsavedPromptVisible;
    private string? _errorMessage;
    private string? _summaryMessage;
    private Func<Task>? _pendingTransition;
    private int _searchGeneration;
    private Task _activeOperation = Task.CompletedTask;

    public SnowcloneLibraryViewModel(
        ISnowcloneLibraryService service,
        ISnowcloneCsvFilePicker? filePicker = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _filePicker = filePicker ?? new NullSnowcloneCsvFilePicker();

        SelectSnowcloneCommand = new RelayCommand(parameter =>
        {
            if (parameter is SnowcloneSummary summary)
            {
                RequestSelection(summary.Id);
            }
        });
        NewCommand = new RelayCommand(_ => RequestTransition(StartNewDraftAsync));
        SaveCommand = new RelayCommand(_ => Begin(SaveAsync()));
        RequestDeleteCommand = new RelayCommand(_ => RequestDelete());
        ConfirmDeleteCommand = new RelayCommand(_ => Begin(ConfirmDeleteAsync()));
        CancelDeleteCommand = new RelayCommand(_ => CancelDelete());
        ImportCommand = new RelayCommand(_ => RequestTransition(ImportAsync));
        ExportCommand = new RelayCommand(_ => Begin(ExportAsync()));
        ImportBundledCommand = new RelayCommand(_ => RequestTransition(ImportBundledAsync));
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

    public ObservableCollection<SnowcloneSummary> Snowclones { get; } = [];

    public SnowcloneSummary? SelectedSnowclone
    {
        get => _selectedSnowclone;
        private set
        {
            if (SetField(ref _selectedSnowclone, value))
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

    public string Guidance
    {
        get => _guidance;
        set
        {
            if (SetField(ref _guidance, value ?? string.Empty))
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

    public string? SummaryMessage
    {
        get => _summaryMessage;
        private set
        {
            if (SetField(ref _summaryMessage, value))
            {
                OnPropertyChanged(nameof(HasSummary));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSummary => !string.IsNullOrWhiteSpace(SummaryMessage);

    public bool HasSnowclones => _confirmedState.AllSnowclones.Count > 0;

    public bool HasVisibleSnowclones => Snowclones.Count > 0;

    public bool IsEmpty => !IsBusy && !HasSnowclones;

    public bool HasNoResults => !IsBusy && HasSnowclones && !HasVisibleSnowclones;

    public bool HasSelection => SelectedSnowclone is not null || IsNewDraft;

    public bool IsDirty =>
        IsNewDraft
            ? !string.IsNullOrWhiteSpace(Phrase) || !string.IsNullOrWhiteSpace(Guidance)
            : SelectedSnowclone is not null &&
              (Phrase != SelectedSnowclone.Phrase || Guidance != SelectedSnowclone.Guidance);

    public bool CanSave => HasSelection && IsDirty && !IsBusy && !UnsavedPromptVisible && !DeleteConfirmationVisible;

    public bool CanDelete => SelectedSnowclone is not null && !IsNewDraft && !IsBusy && !UnsavedPromptVisible;

    public bool CanMutate => !IsBusy && !UnsavedPromptVisible && !DeleteConfirmationVisible;

    public string DeletePromptMessage =>
        SelectedSnowclone is null
            ? string.Empty
            : $"Permanently delete \"{SelectedSnowclone.Phrase}\"?";

    public string UnsavedPromptMessage =>
        IsNewDraft
            ? "Save this new snowclone before continuing?"
            : "Save changes to this snowclone before continuing?";

    public ISnowcloneCsvFilePicker FilePicker
    {
        get => _filePicker;
        set => _filePicker = value ?? new NullSnowcloneCsvFilePicker();
    }

    public ICommand SelectSnowcloneCommand { get; }

    public ICommand NewCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand RequestDeleteCommand { get; }

    public ICommand ConfirmDeleteCommand { get; }

    public ICommand CancelDeleteCommand { get; }

    public ICommand ImportCommand { get; }

    public ICommand ExportCommand { get; }

    public ICommand ImportBundledCommand { get; }

    public ICommand SaveAndContinueCommand { get; }

    public ICommand DiscardAndContinueCommand { get; }

    public ICommand CancelPendingCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand ClearSearchCommand { get; }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _service.InitializeAsync(SearchText, cancellationToken).ConfigureAwait(false);
            ApplyResult(result, selectAffected: false);
            if (result.Succeeded && SelectedSnowclone is null && !IsNewDraft)
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
        var result = await _service.LoadAsync(SearchText).ConfigureAwait(false);
        if (generation != Volatile.Read(ref _searchGeneration))
        {
            return;
        }

        ApplyResult(result, selectAffected: false, preserveDraft: true);
    }

    private void RequestSelection(Guid id)
    {
        if (SelectedSnowclone?.Id == id && !IsNewDraft)
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
        SelectedSnowclone = null;
        IsNewDraft = true;
        Phrase = string.Empty;
        Guidance = string.Empty;
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
                    new SnowcloneCreateRequest(Phrase, Guidance, SearchText)).ConfigureAwait(false)
                : await _service.UpdateAsync(
                    new SnowcloneUpdateRequest(
                        SelectedSnowclone!.Id,
                        Phrase,
                        Guidance,
                        SearchText)).ConfigureAwait(false);

            ApplyResult(result, selectAffected: result.Succeeded);
            if (result.Succeeded)
            {
                IsNewDraft = false;
                FocusEditorRequested?.Invoke(this, EventArgs.Empty);
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
        if (SelectedSnowclone is null || IsBusy)
        {
            return;
        }

        var visibleIndex = Snowclones
            .Select((item, index) => (item, index))
            .FirstOrDefault(entry => entry.item.Id == SelectedSnowclone.Id)
            .index;
        var id = SelectedSnowclone.Id;
        IsBusy = true;
        DeleteConfirmationVisible = false;
        ClearMessages();
        try
        {
            var result = await _service.DeleteAsync(id, SearchText).ConfigureAwait(false);
            ApplyResult(result, selectAffected: false);
            if (result.Succeeded)
            {
                var replacement = Snowclones.Count == 0
                    ? null
                    : Snowclones[Math.Min(visibleIndex, Snowclones.Count - 1)];
                if (replacement is null)
                {
                    ClearDraft();
                }
                else
                {
                    SelectConfirmed(replacement.Id);
                }
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

    private async Task ImportAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var stream = await FilePicker.OpenImportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        await using (stream)
        {
            IsBusy = true;
            ClearMessages();
            try
            {
                var result = await _service.ImportAsync(stream, SearchText).ConfigureAwait(false);
                ApplyResult(result, selectAffected: false, preserveDraft: true);
                if (result.Succeeded)
                {
                    SummaryMessage = ImportSummary(result, "CSV");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task ImportBundledAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearMessages();
        try
        {
            var result = await _service.ImportBundledAsync(SearchText).ConfigureAwait(false);
            ApplyResult(result, selectAffected: false, preserveDraft: true);
            if (result.Succeeded)
            {
                SummaryMessage = ImportSummary(result, "bundled library");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var stream = await FilePicker.OpenExportAsync().ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        await using (stream)
        {
            IsBusy = true;
            ClearMessages();
            try
            {
                var result = await _service.ExportAsync(stream, SearchText).ConfigureAwait(false);
                ApplyResult(result, selectAffected: false, preserveDraft: true);
                if (result.Succeeded)
                {
                    SummaryMessage = $"Exported {_confirmedState.AllSnowclones.Count} snowclone(s).";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
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
        SnowcloneLibraryResult result,
        bool selectAffected,
        bool preserveDraft = false)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        var selectedId = SelectedSnowclone?.Id;
        _confirmedState = result.State;
        ReplaceVisible(result.State.VisibleSnowclones);
        ErrorMessage = null;

        if (selectAffected && result.AffectedSnowclone is not null)
        {
            SelectConfirmed(result.AffectedSnowclone.Id);
        }
        else if (!preserveDraft && selectedId is Guid id)
        {
            var confirmed = result.State.AllSnowclones.FirstOrDefault(item => item.Id == id);
            if (confirmed is null)
            {
                ClearDraft();
            }
            else
            {
                SelectedSnowclone = confirmed;
                Phrase = confirmed.Phrase;
                Guidance = confirmed.Guidance;
                IsNewDraft = false;
            }
        }

        RaiseStateProperties();
    }

    private void ReplaceVisible(IReadOnlyList<SnowcloneSummary> visible)
    {
        Snowclones.Clear();
        foreach (var snowclone in visible)
        {
            Snowclones.Add(snowclone);
        }

        OnPropertyChanged(nameof(HasVisibleSnowclones));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
    }

    private void SelectConfirmed(Guid id)
    {
        var confirmed = _confirmedState.AllSnowclones.FirstOrDefault(item => item.Id == id);
        if (confirmed is null)
        {
            return;
        }

        SelectedSnowclone = confirmed;
        IsNewDraft = false;
        Phrase = confirmed.Phrase;
        Guidance = confirmed.Guidance;
        DeleteConfirmationVisible = false;
        UnsavedPromptVisible = false;
        ClearMessages();
    }

    private void SelectFirstVisibleOrClear()
    {
        if (Snowclones.Count > 0)
        {
            SelectConfirmed(Snowclones[0].Id);
        }
        else
        {
            ClearDraft();
        }
    }

    private void RestoreConfirmedDraft()
    {
        if (SelectedSnowclone is null)
        {
            ClearDraft();
            return;
        }

        SelectConfirmed(SelectedSnowclone.Id);
    }

    private void ClearDraft()
    {
        SelectedSnowclone = null;
        IsNewDraft = false;
        Phrase = string.Empty;
        Guidance = string.Empty;
        DeleteConfirmationVisible = false;
        UnsavedPromptVisible = false;
        RaiseStateProperties();
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SummaryMessage = null;
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

    private static string ImportSummary(SnowcloneLibraryResult result, string source) =>
        $"Imported {result.AddedCount} snowclone(s) from {source}; skipped {result.SkippedCount} duplicate(s).";

    private void RaiseDraftProperties()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(UnsavedPromptMessage));
    }

    private void RaiseStateProperties()
    {
        OnPropertyChanged(nameof(HasSnowclones));
        OnPropertyChanged(nameof(HasVisibleSnowclones));
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
