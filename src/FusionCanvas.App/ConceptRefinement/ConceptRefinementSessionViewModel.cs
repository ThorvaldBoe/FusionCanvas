using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Items;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.App.ConceptRefinement;

public sealed class ConceptRefinementSessionViewModel : INotifyPropertyChanged
{
    private readonly IConceptRefinementService _service;
    private readonly IConceptRefinementAccessStatus _accessStatus;
    private readonly ItemInspectorViewModel _inspector;
    private bool _isBusy;
    private string? _errorMessage;
    private int _currentIndex = -1;
    private CancellationTokenSource? _sessionCts;
    private Guid? _sessionItemId;
    private int _operationSequence;
    private bool _isApplying;
    private bool _isRollingBack;
    private ConceptRefinementTriangle _baseline = new("", "", "");
    private sealed record CapturedOperation(int Sequence, Guid ItemId);

    public ConceptRefinementSessionViewModel(
        IConceptRefinementService service,
        IConceptRefinementAccessStatus accessStatus,
        ItemInspectorViewModel inspector)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _accessStatus = accessStatus ?? throw new ArgumentNullException(nameof(accessStatus));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

        History = [];

        // Commands
        InitializeCommand = new RelayCommand(_ => Run(ExecuteInitializeAsync()),
            () => CanInitialize);
        FineTuneConceptIdeaCommand = new RelayCommand(_ => Run(ExecuteFineTuneAsync(ConceptRefinementCorner.ConceptIdea)),
            () => CanFineTuneConceptIdea);
        FineTunePhraseCommand = new RelayCommand(_ => Run(ExecuteFineTuneAsync(ConceptRefinementCorner.Phrase)),
            () => CanFineTunePhrase);
        FineTuneGraphicDirectionCommand = new RelayCommand(_ => Run(ExecuteFineTuneAsync(ConceptRefinementCorner.GraphicDirection)),
            () => CanFineTuneGraphicDirection);
        ChangeConceptIdeaCommand = new RelayCommand(_ => Run(ExecuteChangeAsync(ConceptRefinementCorner.ConceptIdea)),
            () => CanChangeConceptIdea);
        ChangePhraseCommand = new RelayCommand(_ => Run(ExecuteChangeAsync(ConceptRefinementCorner.Phrase)),
            () => CanChangePhrase);
        ChangeGraphicDirectionCommand = new RelayCommand(_ => Run(ExecuteChangeAsync(ConceptRefinementCorner.GraphicDirection)),
            () => CanChangeGraphicDirection);
        SelectHistoryEntryCommand = new RelayCommand(parameter =>
        {
            if (parameter is ConceptRefinementHistoryEntry entry)
            {
                Run(ExecuteRollbackAsync(entry));
            }
        }, () => !IsBusy);

        // Subscribe to inspector events for manual-commit tracking
        _inspector.PropertyChanged += OnInspectorPropertyChanged;
        _inspector.Saved += OnInspectorSaved;

        // Subscribe to access status changes
        _accessStatus.AvailabilityChanged += OnAccessAvailabilityChanged;

        // Recompute score on creation
        RecomputeScore();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ConceptRefinementHistoryEntry> History { get; }

    // --- Selected history entry for rollback via ListBox ---

    private ConceptRefinementHistoryEntry? _selectedHistoryEntry;
    public ConceptRefinementHistoryEntry? SelectedHistoryEntry
    {
        get => _selectedHistoryEntry;
        set
        {
            if (SetField(ref _selectedHistoryEntry, value) && value is not null)
            {
                Run(ExecuteRollbackAsync(value));
            }
        }
    }

    // --- Score ---

    private int _score;
    public int Score
    {
        get => _score;
        private set => SetField(ref _score, value);
    }

    // --- Availability ---

    public ConceptRefinementAccessAvailability AccessStatus => _accessStatus.GetAvailability();

    public bool IsAvailable => AccessStatus.IsAvailable;

    public string? UnavailableReason => AccessStatus.UnavailableReason;

    // --- Busy ---

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    // --- Error ---

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

    // --- Initialize disabled reason (VR-002) ---

    public string? InitializeDisabledReason
    {
        get
        {
            if (IsAvailable && CanRefine && _inspector.CanEditStage)
            {
                if (!HasNonWhitespace(_inspector.Idea))
                {
                    return "A base idea is required before initializing.";
                }

                if (HasAnyNonWhitespaceCorner())
                {
                    return "All concept fields must be empty to initialize from the base idea.";
                }

                return null;
            }

            if (!_inspector.CanEditStage)
            {
                return _inspector.StageReadOnlyReason;
            }

            if (!IsAvailable)
            {
                return UnavailableReason;
            }

            if (IsBusy)
            {
                return "A refinement operation is in progress.";
            }

            return null;
        }
    }

    // --- History visibility (VR-007a) ---

    public bool HasHistory => History.Count > 0;

    // --- Current entry index (VR-007b) ---

    public int? CurrentEntryIndex => _currentIndex >= 0 ? _currentIndex : null;

    // --- Can initialize ---

    public bool CanInitialize =>
        !IsBusy
        && IsAvailable
        && HasNonWhitespace(_inspector.Idea)
        && !HasAnyNonWhitespaceCorner()
        && _inspector.CanEditStage;

    // --- Can FineTune (per corner) ---

    public bool CanFineTuneConceptIdea =>
        CanRefine && HasNonWhitespace(_inspector.ConceptIdea);

    public bool CanFineTunePhrase =>
        CanRefine && HasNonWhitespace(_inspector.Phrase);

    public bool CanFineTuneGraphicDirection =>
        CanRefine && HasNonWhitespace(_inspector.GraphicDirection);

    // --- Can Change (per corner) ---

    public bool CanChangeConceptIdea => CanRefine;
    public bool CanChangePhrase => CanRefine;
    public bool CanChangeGraphicDirection => CanRefine;

    private bool CanRefine =>
        !IsBusy
        && IsAvailable
        && _inspector.CanEditStage;

    // --- Commands ---

    public ICommand InitializeCommand { get; }
    public ICommand FineTuneConceptIdeaCommand { get; }
    public ICommand FineTunePhraseCommand { get; }
    public ICommand FineTuneGraphicDirectionCommand { get; }
    public ICommand ChangeConceptIdeaCommand { get; }
    public ICommand ChangePhraseCommand { get; }
    public ICommand ChangeGraphicDirectionCommand { get; }
    public ICommand SelectHistoryEntryCommand { get; }

    // --- Session lifecycle ---

    /// <summary>
    /// Call when the inspector loads a new item (item switch or Clear()).
    /// Cancels any in-flight operation and clears history.
    /// </summary>
    public void ResetSession()
    {
        CancelInFlight();
        History.Clear();
        _currentIndex = -1;
        ErrorMessage = null;
        _sessionItemId = _inspector.LoadedItemId;
        _baseline = new ConceptRefinementTriangle(
            _inspector.ConceptIdea,
            _inspector.Phrase,
            _inspector.GraphicDirection);
        if (_sessionItemId is not null)
        {
            _sessionCts = new CancellationTokenSource();
        }

        RecomputeScore();
        NotifyHistoryChanged();
        RaiseCommandStates();
    }

    /// <summary>
    /// Call to refresh availability (e.g., after AI settings save).
    /// </summary>
    public async Task RefreshAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        await _accessStatus.RefreshAsync(cancellationToken).ConfigureAwait(true);
        RaiseCommandStates();
    }

    // --- Command execution ---

    private async Task ExecuteInitializeAsync()
    {
        if (!CanInitialize)
        {
            return;
        }

        await GuardApplyAsync(async ct =>
        {
            var result = await _service.InitializeAsync(
                EnsureSessionItemId(),
                _inspector.Idea,
                ct).ConfigureAwait(true);
            return result;
        }, "Initialized from base idea", null);
    }

    private async Task ExecuteFineTuneAsync(ConceptRefinementCorner corner)
    {
        if (!CanRefine)
        {
            return;
        }

        var cornerName = corner switch
        {
            ConceptRefinementCorner.ConceptIdea => "Concept idea",
            ConceptRefinementCorner.Phrase => "Phrase",
            ConceptRefinementCorner.GraphicDirection => "Graphic direction",
            _ => "Corner"
        };

        await GuardApplyAsync(async ct =>
        {
            var current = CaptureTriangle();
            var result = await _service.RefineAsync(
                EnsureSessionItemId(),
                ConceptRefinementActionKind.FineTune,
                corner,
                current,
                _inspector.Idea,
                ct).ConfigureAwait(true);
            return result;
        }, $"Fine-tuned {cornerName}", corner);
    }

    private async Task ExecuteChangeAsync(ConceptRefinementCorner corner)
    {
        if (!CanRefine)
        {
            return;
        }

        var cornerName = corner switch
        {
            ConceptRefinementCorner.ConceptIdea => "Concept idea",
            ConceptRefinementCorner.Phrase => "Phrase",
            ConceptRefinementCorner.GraphicDirection => "Graphic direction",
            _ => "Corner"
        };

        await GuardApplyAsync(async ct =>
        {
            var current = CaptureTriangle();
            var result = await _service.RefineAsync(
                EnsureSessionItemId(),
                ConceptRefinementActionKind.Change,
                corner,
                current,
                _inspector.Idea,
                ct).ConfigureAwait(true);
            return result;
        }, $"Changed {cornerName}", corner);
    }

    private async Task ExecuteRollbackAsync(ConceptRefinementHistoryEntry entry)
    {
        if (IsBusy || !_inspector.CanEditStage)
        {
            return;
        }

        var targetIndex = History.IndexOf(entry);
        if (targetIndex < 0)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        _isRollingBack = true;

        try
        {
            _inspector.ConceptIdea = entry.ConceptIdea;
            _inspector.Phrase = entry.Phrase;
            _inspector.GraphicDirection = entry.GraphicDirection;
            _currentIndex = targetIndex;
            await _inspector.CommitEditsAsync().ConfigureAwait(true);
            RecomputeScore();
            OnPropertyChanged(nameof(CurrentEntryIndex));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _isRollingBack = false;
            IsBusy = false;
        }
    }

    // --- Apply guard (D5/D6) ---

    private async Task GuardApplyAsync(
        Func<CancellationToken, Task<ConceptRefinementResult>> operation,
        string actionLabel,
        ConceptRefinementCorner? singleCorner)
    {
        IsBusy = true;
        ErrorMessage = null;

        var sequence = Interlocked.Increment(ref _operationSequence);
        var captured = new CapturedOperation(sequence, EnsureSessionItemId());

        try
        {
            var ct = _sessionCts?.Token ?? CancellationToken.None;
            var result = await operation(ct).ConfigureAwait(true);

            ct.ThrowIfCancellationRequested();

            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "The refinement operation failed.";
                return;
            }

            // Identity check: ensure we're still on the same item and the operation is still valid
            if (_sessionItemId != captured.ItemId || _operationSequence != captured.Sequence)
            {
                // Late result, discard
                return;
            }

            // Apply values to drafts
            if (singleCorner is { } corner)
            {
                ApplySingleCornerValue(corner, result);
            }
            else
            {
                // Initialize - set all three
                if (result.ConceptIdea is not null)
                {
                    _inspector.ConceptIdea = result.ConceptIdea;
                }

                if (result.Phrase is not null)
                {
                    _inspector.Phrase = result.Phrase;
                }

                if (result.GraphicDirection is not null)
                {
                    _inspector.GraphicDirection = result.GraphicDirection;
                }
            }

            // Append history entry
            var entry = new ConceptRefinementHistoryEntry(
                actionLabel,
                _inspector.ConceptIdea,
                _inspector.Phrase,
                _inspector.GraphicDirection,
                DateTimeOffset.UtcNow);

            // Truncate entries after current index before appending
            TruncateAfterCurrent();

            History.Add(entry);
            _currentIndex = History.Count - 1;
            NotifyHistoryChanged();

            // Commit through the inspector's automatic-save path
            _isApplying = true;
            try
            {
                await _inspector.CommitEditsAsync().ConfigureAwait(true);
                RecomputeScore();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Failed commit: keep draft + entry but surface inline error
                ErrorMessage = ex.Message;
            }
            finally
            {
                _isApplying = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Drafts, history unchanged; no error shown for cancellation
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Helpers ---

    private void NotifyHistoryChanged()
    {
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(CurrentEntryIndex));
    }

    private ConceptRefinementTriangle CaptureTriangle() =>
        new(_inspector.ConceptIdea, _inspector.Phrase, _inspector.GraphicDirection);

    private void ApplySingleCornerValue(ConceptRefinementCorner corner, ConceptRefinementResult result)
    {
        switch (corner)
        {
            case ConceptRefinementCorner.ConceptIdea when result.ConceptIdea is { } v:
                _inspector.ConceptIdea = v;
                break;
            case ConceptRefinementCorner.Phrase when result.Phrase is { } v:
                _inspector.Phrase = v;
                break;
            case ConceptRefinementCorner.GraphicDirection when result.GraphicDirection is { } v:
                _inspector.GraphicDirection = v;
                break;
        }
    }

    private void TruncateAfterCurrent()
    {
        if (_currentIndex >= 0 && _currentIndex < History.Count - 1)
        {
            for (var i = History.Count - 1; i > _currentIndex; i--)
            {
                History.RemoveAt(i);
            }

            NotifyHistoryChanged();
        }
    }

    private Guid EnsureSessionItemId()
    {
        if (_sessionItemId is not { } id)
        {
            throw new InvalidOperationException("No item is loaded for the refinement session.");
        }

        return id;
    }

    private void CancelInFlight()
    {
        if (_sessionCts is not null)
        {
            _sessionCts.Cancel();
            _sessionCts.Dispose();
            _sessionCts = null;
        }

        _sessionItemId = null;
        _operationSequence = 0;
    }

    private void RecomputeScore()
    {
        Score = DesignTriangleScore.FromValues(
            _inspector.ConceptIdea,
            _inspector.Phrase,
            _inspector.GraphicDirection);
    }

    private static bool HasNonWhitespace(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private bool HasAnyNonWhitespaceCorner() =>
        HasNonWhitespace(_inspector.ConceptIdea)
        || HasNonWhitespace(_inspector.Phrase)
        || HasNonWhitespace(_inspector.GraphicDirection);

    private void RaiseCommandStates()
    {
        OnPropertyChanged(nameof(AccessStatus));
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(UnavailableReason));
        OnPropertyChanged(nameof(CanInitialize));
        OnPropertyChanged(nameof(InitializeDisabledReason));
        OnPropertyChanged(nameof(CanFineTuneConceptIdea));
        OnPropertyChanged(nameof(CanFineTunePhrase));
        OnPropertyChanged(nameof(CanFineTuneGraphicDirection));
        OnPropertyChanged(nameof(CanChangeConceptIdea));
        OnPropertyChanged(nameof(CanChangePhrase));
        OnPropertyChanged(nameof(CanChangeGraphicDirection));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(CurrentEntryIndex));
        OnPropertyChanged(nameof(HasError));
    }

    // --- Inspector event handlers ---

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ItemInspectorViewModel.ConceptIdea)
            or nameof(ItemInspectorViewModel.Phrase)
            or nameof(ItemInspectorViewModel.GraphicDirection))
        {
            RecomputeScore();
            RaiseCommandStates();
        }
        else if (args.PropertyName is nameof(ItemInspectorViewModel.LoadedItemId))
        {
            ResetSession();
        }
        else if (args.PropertyName is nameof(ItemInspectorViewModel.CanEditStage))
        {
            RaiseCommandStates();
        }
    }

    private void OnInspectorSaved(object? sender, EventArgs args)
    {
        if (_isApplying || _isRollingBack)
        {
            // Skip; not a user-driven manual edit.
            return;
        }

        AppendManualCommitEntry();
    }

    private void AppendManualCommitEntry()
    {
        var insp = _inspector;
        // Only consider concept-field changes; non-Concept commits (title, notes, tags) are ignored.
        var currentConceptIdea = insp.ConceptIdea;
        var currentPhrase = insp.Phrase;
        var currentGraphic = insp.GraphicDirection;

        // Determine what to compare against: last entry or baseline
        string prevConceptIdea, prevPhrase, prevGraphic;
        if (History.Count > 0)
        {
            var last = History[^1];
            prevConceptIdea = last.ConceptIdea;
            prevPhrase = last.Phrase;
            prevGraphic = last.GraphicDirection;
        }
        else
        {
            prevConceptIdea = _baseline.ConceptIdea;
            prevPhrase = _baseline.Phrase;
            prevGraphic = _baseline.GraphicDirection;
        }

        // Determine which concept fields changed
        var changedFields = new List<string>();
        if (currentConceptIdea != prevConceptIdea)
        {
            changedFields.Add("Concept idea");
        }

        if (currentPhrase != prevPhrase)
        {
            changedFields.Add("Phrase");
        }

        if (currentGraphic != prevGraphic)
        {
            changedFields.Add("Graphic direction");
        }

        if (changedFields.Count == 0)
        {
            return;
        }

        var label = changedFields.Count == 1
            ? $"Edited {changedFields[0]}"
            : "Edited Concept fields";

        TruncateAfterCurrent();

        var entry = new ConceptRefinementHistoryEntry(
            label,
            currentConceptIdea,
            currentPhrase,
            currentGraphic,
            DateTimeOffset.UtcNow);
        History.Add(entry);
        _currentIndex = History.Count - 1;
        NotifyHistoryChanged();
    }

    private void OnAccessAvailabilityChanged(object? sender, EventArgs args)
    {
        RaiseCommandStates();
    }

    // --- INotifyPropertyChanged ---

    // Public setter for testing (VR-001)
    internal void SetErrorForTest(string message) => ErrorMessage = message;

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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static void Run(Task task) => _ = task;
}