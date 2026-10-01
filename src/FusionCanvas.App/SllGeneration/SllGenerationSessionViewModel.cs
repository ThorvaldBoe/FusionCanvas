using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Items;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.App.SllGeneration;

public sealed class SllGenerationSessionViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISllGenerationService _service;
    private readonly ISllAccessStatus _accessStatus;
    private readonly ISllDocumentCodec _codec;
    private readonly ItemInspectorViewModel _inspector;
    private bool _isBusy;
    private string? _errorMessage;
    private Guid? _sessionItemId;
    private CancellationTokenSource? _sessionCts;
    private readonly List<CancellationTokenSource> _retiredSessionCts = [];
    private int _operationSequence;
    private bool _isDisposed;
    internal Task PendingOperation { get; private set; } = Task.CompletedTask;
    private SllDocument? _current;
    private bool _localSourceChanged;
    private bool _keepAsReference;
    private sealed record CapturedOperation(int Sequence, Guid ItemId);

    public SllGenerationSessionViewModel(
        ISllGenerationService service,
        ISllAccessStatus accessStatus,
        ISllDocumentCodec codec,
        ItemInspectorViewModel inspector)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _accessStatus = accessStatus ?? throw new ArgumentNullException(nameof(accessStatus));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

        GenerateCommand = new RelayCommand(_ => Run(ExecuteGenerateAsync), () => CanGenerate);
        RegenerateCommand = new RelayCommand(_ => Run(ExecuteGenerateAsync), () => CanRegenerate);
        ResetSllCommand = new RelayCommand(_ => Run(ResetSllAsync), () => !_isDisposed && HasCurrentSll && !IsBusy && _inspector.CanEditStage);
        KeepSllReferenceCommand = new RelayCommand(_ => KeepSllAsReference(), () => !_isDisposed && IsStale && !IsBusy);

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

    public bool IsStale => HasCurrentSll && !_keepAsReference && (_localSourceChanged || _inspector.State?.IsSllStale == true || !IsComplete);

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
        !_isDisposed
        && !IsBusy
        && IsAvailable
        && IsComplete
        && _inspector.CanEditStage;

    public bool CanRegenerate =>
        HasCurrentSll
        && CanGenerate;

    // --- Commands ---

    public RelayCommand GenerateCommand { get; }
    public RelayCommand RegenerateCommand { get; }
    public RelayCommand ResetSllCommand { get; }
    public RelayCommand KeepSllReferenceCommand { get; }

    // --- Session lifecycle ---

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _inspector.PropertyChanged -= OnInspectorPropertyChanged;
        _accessStatus.AvailabilityChanged -= OnAccessAvailabilityChanged;
        CancelInFlight();
        _current = null;
        _localSourceChanged = false;
        _keepAsReference = false;
        IsBusy = false;
        RaiseCurrentChanged();
    }

    public void ResetSession()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_inspector.IsLoadingItem)
        {
            InvalidateSessionForItemLoad();
            return;
        }

        CancelInFlight();
        ErrorMessage = null;
        _sessionItemId = _inspector.LoadedItemId;
        _keepAsReference = false;
        LoadCurrentFromInspector();
        if (_sessionItemId is not null)
        {
            _sessionCts = new CancellationTokenSource();
        }

        RaiseCommandStates();
    }

    private void InvalidateSessionForItemLoad()
    {
        CancelInFlight();
        ErrorMessage = null;
        _sessionItemId = null;
        _current = null;
        _localSourceChanged = false;
        _keepAsReference = false;
        RaiseCurrentChanged();
        RaiseCommandStates();
    }

    public async Task RefreshAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        await _accessStatus.RefreshAsync(cancellationToken).ConfigureAwait(true);
        if (!_isDisposed)
        {
            RaiseCommandStates();
        }
    }

    // --- Execution ---

    private async Task ExecuteGenerateAsync()
    {
        if (!CanGenerate)
        {
            return;
        }

        var captured = new CapturedOperation(Interlocked.Increment(ref _operationSequence), EnsureSessionItemId());
        var sessionCts = _sessionCts;
        var cancellationToken = sessionCts?.Token ?? CancellationToken.None;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var triangle = new ConceptRefinementTriangle(
                _inspector.ConceptIdea,
                _inspector.Phrase,
                _inspector.GraphicDirection);
            var result = await _service.GenerateAsync(
                captured.ItemId,
                triangle,
                _inspector.Idea,
                cancellationToken).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentOperation(captured, sessionCts))
            {
                return;
            }

            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "The SLL generation failed.";
                return;
            }

            _current = result.Document;
            _localSourceChanged = false;
            _keepAsReference = false;
            _inspector.Sll = _codec.Serialize(result.Document!);

            await _inspector.CommitEditsAsync(cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentOperation(captured, sessionCts))
            {
                return;
            }

            RaiseCurrentChanged();
        }
        catch (OperationCanceledException)
        {
            // unchanged
        }
        catch (Exception exception)
        {
            if (IsCurrentOperation(captured, sessionCts))
            {
                ErrorMessage = exception.Message;
            }
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
        if (string.IsNullOrWhiteSpace(sllText) || !_codec.TryDeserialize(sllText, out var document))
        {
            _current = null;
            _localSourceChanged = false;
            _keepAsReference = false;
        }
        else
        {
            _current = document;
            _localSourceChanged = _inspector.State?.IsSllStale == true;
            _keepAsReference = false;
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

    private bool IsCurrentOperation(CapturedOperation captured, CancellationTokenSource? sessionCts) =>
        _sessionItemId == captured.ItemId
        && _operationSequence == captured.Sequence
        && ReferenceEquals(_sessionCts, sessionCts)
        && sessionCts?.IsCancellationRequested != true;

    private void CancelInFlight()
    {
        var source = _sessionCts;
        _sessionCts = null;
        _sessionItemId = null;
        _operationSequence = 0;
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        finally
        {
            if (IsBusy)
            {
                _retiredSessionCts.Add(source);
            }
            else
            {
                source.Dispose();
            }
        }
    }

    private void DisposeRetiredSessionSources()
    {
        if (IsBusy || _retiredSessionCts.Count == 0)
        {
            return;
        }

        var retiredSources = _retiredSessionCts.ToArray();
        _retiredSessionCts.Clear();
        foreach (var source in retiredSources)
        {
            source.Dispose();
        }
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
        ResetSllCommand.NotifyCanExecuteChanged();
        KeepSllReferenceCommand.NotifyCanExecuteChanged();

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

    private async Task ResetSllAsync()
    {
        if (_isDisposed || !HasCurrentSll || !_inspector.CanEditStage || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        var cancellationToken = _sessionCts?.Token ?? CancellationToken.None;

        try
        {
            var captured = new CapturedOperation(Interlocked.Increment(ref _operationSequence), EnsureSessionItemId());
            _inspector.Sll = string.Empty;
            _current = null;
            _localSourceChanged = false;
            _keepAsReference = false;
            await _inspector.CommitEditsAsync(cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();

            if (_sessionItemId != captured.ItemId || _operationSequence != captured.Sequence)
            {
                return;
            }

            if (_inspector.ErrorMessage is { } commitError)
            {
                ErrorMessage = commitError;
            }

            RaiseCurrentChanged();
            RaiseCommandStates();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void KeepSllAsReference()
    {
        if (_isDisposed || !IsStale)
            return;

        _keepAsReference = true;
        RaiseCurrentChanged();
        RaiseCommandStates();
    }

    // --- Inspector events ---

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_isDisposed)
        {
            return;
        }

        if (args.PropertyName is nameof(ItemInspectorViewModel.IsLoadingItem))
        {
            if (_inspector.IsLoadingItem)
            {
                InvalidateSessionForItemLoad();
            }
            else
            {
                ResetSession();
            }
        }
        else if (args.PropertyName is nameof(ItemInspectorViewModel.LoadedItemId))
        {
            if (!_inspector.IsLoadingItem)
            {
                ResetSession();
            }
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
            else if (HasCurrentSll && (args.PropertyName is nameof(ItemInspectorViewModel.Idea)
                or nameof(ItemInspectorViewModel.ConceptIdea)
                or nameof(ItemInspectorViewModel.Phrase)
                or nameof(ItemInspectorViewModel.GraphicDirection)))
            {
                _localSourceChanged = true;
            }

            RaiseCommandStates();
        }
    }

    private void OnAccessAvailabilityChanged(object? sender, EventArgs args)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!_isDisposed)
            {
                RaiseCommandStates();
            }
        });
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

    private void Run(Func<Task> operation)
    {
        var sessionItemId = _sessionItemId;
        var sessionCts = _sessionCts;
        PendingOperation = ObserveAsync(operation, sessionItemId, sessionCts);
    }

    private async Task ObserveAsync(
        Func<Task> operation,
        Guid? sessionItemId,
        CancellationTokenSource? sessionCts)
    {
        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            if (_sessionItemId == sessionItemId && ReferenceEquals(_sessionCts, sessionCts))
            {
                ErrorMessage = exception.Message;
            }
        }
        finally
        {
            DisposeRetiredSessionSources();
        }
    }
}
