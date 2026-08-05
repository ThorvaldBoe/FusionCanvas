using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Items;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.App.SllGeneration;

public sealed class SllGenerationSessionViewModel : INotifyPropertyChanged
{
    private readonly ISllGenerationService _service;
    private readonly ISllAccessStatus _accessStatus;
    private readonly ItemInspectorViewModel _inspector;
    private bool _isBusy;
    private string? _errorMessage;
    private Guid? _sessionItemId;
    private CancellationTokenSource? _sessionCts;
    private int _operationSequence;
    private SllDocument? _current;
    private sealed record CapturedOperation(int Sequence, Guid ItemId);

    public SllGenerationSessionViewModel(
        ISllGenerationService service,
        ISllAccessStatus accessStatus,
        ItemInspectorViewModel inspector)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _accessStatus = accessStatus ?? throw new ArgumentNullException(nameof(accessStatus));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

        GenerateCommand = new RelayCommand(_ => Run(ExecuteGenerateAsync()), () => CanGenerate);
        RegenerateCommand = new RelayCommand(_ => Run(ExecuteGenerateAsync()), () => CanRegenerate);

        _inspector.PropertyChanged += OnInspectorPropertyChanged;
        _accessStatus.AvailabilityChanged += OnAccessAvailabilityChanged;

        ResetSession();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Availability ---

    public SllAccessAvailability AccessStatus => _accessStatus.GetAvailability();

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

    // --- Current SLL ---

    public SllDocument? Current => _current;

    public bool HasCurrentSll => _current is not null;

    public string AsciiSketch => _current?.AsciiSketch ?? string.Empty;

    public bool IsStale => HasCurrentSll && !IsComplete;

    private bool IsComplete =>
        DesignTriangleScore.FromValues(
            _inspector.ConceptIdea,
            _inspector.Phrase,
            _inspector.GraphicDirection) == 100;

    public string? GenerateDisabledReason
    {
        get
        {
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
                return "An SLL operation is in progress.";
            }

            if (!IsComplete)
            {
                return "Complete all three corners of the design triangle before generating an SLL.";
            }

            return null;
        }
    }

    public string? RegenerateDisabledReason
    {
        get
        {
            if (!HasCurrentSll)
            {
                return "Generate an SLL first before regenerating.";
            }

            return GenerateDisabledReason;
        }
    }

    // --- Can ---

    public bool CanGenerate =>
        !IsBusy
        && IsAvailable
        && IsComplete
        && _inspector.CanEditStage;

    public bool CanRegenerate =>
        HasCurrentSll
        && CanGenerate;

    // --- Commands ---

    public RelayCommand GenerateCommand { get; }
    public RelayCommand RegenerateCommand { get; }

    // --- Session lifecycle ---

    public void ResetSession()
    {
        CancelInFlight();
        ErrorMessage = null;
        _sessionItemId = _inspector.LoadedItemId;
        LoadCurrentFromInspector();
        if (_sessionItemId is not null)
        {
            _sessionCts = new CancellationTokenSource();
        }

        RaiseCommandStates();
    }

    public async Task RefreshAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        await _accessStatus.RefreshAsync(cancellationToken).ConfigureAwait(true);
        RaiseCommandStates();
    }

    // --- Execution ---

    private async Task ExecuteGenerateAsync()
    {
        if (!CanGenerate)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        var sequence = Interlocked.Increment(ref _operationSequence);
        var captured = new CapturedOperation(sequence, EnsureSessionItemId());

        try
        {
            var ct = _sessionCts?.Token ?? CancellationToken.None;
            var triangle = new ConceptRefinementTriangle(
                _inspector.ConceptIdea,
                _inspector.Phrase,
                _inspector.GraphicDirection);
            var result = await _service.GenerateAsync(
                captured.ItemId,
                triangle,
                _inspector.Idea,
                ct).ConfigureAwait(true);

            ct.ThrowIfCancellationRequested();

            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "The SLL generation failed.";
                return;
            }

            if (_sessionItemId != captured.ItemId || _operationSequence != captured.Sequence)
            {
                return;
            }

            _current = result.Document;
            _inspector.Sll = result.Document!.Serialize();

            await _inspector.CommitEditsAsync(ct).ConfigureAwait(true);
            RaiseCurrentChanged();
        }
        catch (OperationCanceledException)
        {
            // unchanged
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

    private void LoadCurrentFromInspector()
    {
        var sllText = _inspector.Sll;
        if (string.IsNullOrWhiteSpace(sllText) || !SllDocument.TryDeserialize(sllText, out var document))
        {
            _current = null;
        }
        else
        {
            _current = document;
        }

        RaiseCurrentChanged();
    }

    private Guid EnsureSessionItemId()
    {
        if (_sessionItemId is not { } id)
        {
            throw new InvalidOperationException("No item is loaded for the SLL session.");
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

    private void RaiseCurrentChanged()
    {
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(HasCurrentSll));
        OnPropertyChanged(nameof(AsciiSketch));
        OnPropertyChanged(nameof(IsStale));
    }

    private void RaiseCommandStates()
    {
        GenerateCommand.NotifyCanExecuteChanged();
        RegenerateCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(AccessStatus));
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(UnavailableReason));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(CanRegenerate));
        OnPropertyChanged(nameof(GenerateDisabledReason));
        OnPropertyChanged(nameof(RegenerateDisabledReason));
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(HasError));
    }

    // --- Inspector events ---

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ItemInspectorViewModel.LoadedItemId))
        {
            ResetSession();
        }
        else if (args.PropertyName is nameof(ItemInspectorViewModel.ConceptIdea)
            or nameof(ItemInspectorViewModel.Phrase)
            or nameof(ItemInspectorViewModel.GraphicDirection)
            or nameof(ItemInspectorViewModel.Sll)
            or nameof(ItemInspectorViewModel.Idea)
            or nameof(ItemInspectorViewModel.CanEditStage))
        {
            if (args.PropertyName == nameof(ItemInspectorViewModel.Sll))
            {
                LoadCurrentFromInspector();
            }

            RaiseCommandStates();
        }
    }

    private void OnAccessAvailabilityChanged(object? sender, EventArgs args)
    {
        Dispatcher.UIThread.Post(RaiseCommandStates);
    }

    // Public setters for testing (section state visibility)
    internal void SetErrorForTest(string message) => ErrorMessage = message;
    internal void SetBusyForTest(bool value) => IsBusy = value;

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
