using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Domain.Ideation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.App.Snowclones;
using Avalonia.Threading;

namespace FusionCanvas.App.Ideation;

public sealed class IdeationViewModel : INotifyPropertyChanged
{
    public const int DefaultCount = 5;
    public const int MinimumCount = 1;
    public const int MaximumCount = 20;

    private readonly IIdeationService _service;
    private readonly IIdeationAccessStatus _accessStatus;
    private readonly ISnowcloneLibraryService? _snowcloneLibrary;
    private IdeationScope? _scope;
    private string _guidance = string.Empty;
    private string _countText = DefaultCount.ToString();
    private string _rejectionReason = string.Empty;
    private IdeationMode _selectedMode = IdeationMode.Basic;
    private bool _isOpen;
    private bool _isBusy;
    private bool _isDiscardConfirmationVisible;
    private int _completed;
    private int _requested;
    private string? _error;
    private CancellationTokenSource? _generationCancellation;
    private long _generationToken;
    private bool _hasSnowclones;
    private bool _isSnowcloneLibraryOpen;

    public IdeationViewModel(
        IIdeationService service,
        IIdeationAccessStatus accessStatus,
        ISnowcloneLibraryService? snowcloneLibrary = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _accessStatus = accessStatus ?? throw new ArgumentNullException(nameof(accessStatus));
        _snowcloneLibrary = snowcloneLibrary;
        _accessStatus.AvailabilityChanged += (_, _) => Dispatcher.UIThread.Post(RaiseCommandState);
        GenerateCommand = new RelayCommand(_ => _ = GenerateAsync(), () => CanGenerate);
        IncrementCountCommand = new RelayCommand(_ => IncrementCount(), () => CanIncrementCount);
        DecrementCountCommand = new RelayCommand(_ => DecrementCount(), () => CanDecrementCount);
        CreateCandidateCommand = new RelayCommand(candidate =>
        {
            if (candidate is IdeaCandidateViewModel row)
            {
                _ = CreateCandidateAsync(row);
            }
        });
        RejectCandidateCommand = new RelayCommand(candidate =>
        {
            if (candidate is IdeaCandidateViewModel row)
            {
                RejectionCandidate = row;
                RejectionReason = string.Empty;
                OnPropertyChanged(nameof(IsRejectionVisible));
                OnPropertyChanged(nameof(RejectionCandidate));
                OnPropertyChanged(nameof(RejectionReason));
            }
        });
        ConfirmRejectCommand = new RelayCommand(_ => _ = ConfirmRejectAsync());
        CancelRejectCommand = new RelayCommand(_ => CancelReject());
        RequestClearCommand = new RelayCommand(_ => RequestDiscard(DiscardAction.Clear));
        RequestCloseCommand = new RelayCommand(_ => RequestDiscard(DiscardAction.Close));
        ConfirmDiscardCommand = new RelayCommand(_ => ConfirmDiscard());
        CancelDiscardCommand = new RelayCommand(_ => CancelDiscard());
        ManageSnowclonesCommand = new RelayCommand(
            _ => OpenSnowcloneLibrary(),
            () => IsSnowclonesMode && !IsBusy && !IsSnowcloneLibraryOpen && _snowcloneLibrary is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? WorkspaceChanged;

    public IReadOnlyList<IdeationMode> Modes { get; } = [IdeationMode.Basic, IdeationMode.Snowclones];

    public ObservableCollection<IdeaCandidateViewModel> Candidates { get; } = [];

    public ICommand GenerateCommand { get; }

    public ICommand IncrementCountCommand { get; }

    public ICommand DecrementCountCommand { get; }

    public ICommand CreateCandidateCommand { get; }

    public ICommand RejectCandidateCommand { get; }

    public ICommand ConfirmRejectCommand { get; }

    public ICommand CancelRejectCommand { get; }

    public ICommand RequestClearCommand { get; }

    public ICommand RequestCloseCommand { get; }

    public ICommand ConfirmDiscardCommand { get; }

    public ICommand CancelDiscardCommand { get; }
    public ICommand ManageSnowclonesCommand { get; }

    public string ScopeLabel => _scope?.DisplayPath ?? string.Empty;

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetField(ref _isOpen, value);
    }

    public string Guidance
    {
        get => _guidance;
        set => SetField(ref _guidance, value ?? string.Empty);
    }

    public string CountText
    {
        get => _countText;
        set
        {
            if (SetField(ref _countText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CountError));
                RaiseCommandState();
            }
        }
    }

    public string? CountError => TryGetCount(out _) ? null : $"Enter a whole number from {MinimumCount} to {MaximumCount}.";

    public bool CanIncrementCount =>
        !IsBusy && !(TryGetCount(out int n) && n == MaximumCount);

    public bool CanDecrementCount =>
        !IsBusy && !(TryGetCount(out int n) && n == MinimumCount);

    public IdeationMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetField(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsSnowclonesMode));
                OnPropertyChanged(nameof(SnowcloneLibraryMessage));
                RaiseCommandState();
                if (IsSnowclonesMode)
                {
                    _ = RefreshSnowcloneLibraryAsync();
                }
            }
        }
    }

    public bool IsSnowclonesMode => SelectedMode == IdeationMode.Snowclones;
    public bool CanManageSnowclones =>
        IsSnowclonesMode && !IsBusy && !IsSnowcloneLibraryOpen && _snowcloneLibrary is not null;
    public bool HasSnowclones => _hasSnowclones;
    public bool IsSnowcloneLibraryOpen => _isSnowcloneLibraryOpen;
    public SnowcloneLibraryViewModel? SnowcloneLibrary { get; private set; }
    public string? SnowcloneLibraryMessage =>
        IsSnowclonesMode && !HasSnowclones
            ? "Add or import at least one Snowclone before generating."
            : null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseCommandState();
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(CanDiscard));
            }
        }
    }

    public int Completed
    {
        get => _completed;
        private set
        {
            if (SetField(ref _completed, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public int Requested
    {
        get => _requested;
        private set
        {
            if (SetField(ref _requested, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => IsBusy ? $"Generating ideas… {Completed} of {Requested}" : string.Empty;

    public bool IsNotBusy => !IsBusy;

    public string? Error
    {
        get => _error;
        private set => SetField(ref _error, value);
    }

    public bool HasCandidates => Candidates.Count > 0;

    public bool CanDiscard => HasCandidates;

    public bool CanGenerate => IsOpen
        && !IsBusy
        && !IsSnowcloneLibraryOpen
        && TryGetCount(out _)
        && (!IsSnowclonesMode || HasSnowclones)
        && _accessStatus.GetAvailability().IsAvailable;

    public string? AccessMessage => _accessStatus.GetAvailability().UnavailableReason;

    public IdeaCandidateViewModel? RejectionCandidate { get; private set; }

    public string RejectionReason
    {
        get => _rejectionReason;
        set => SetField(ref _rejectionReason, value ?? string.Empty);
    }

    public bool IsRejectionVisible => RejectionCandidate is not null;

    public bool IsDiscardConfirmationVisible
    {
        get => _isDiscardConfirmationVisible;
        private set => SetField(ref _isDiscardConfirmationVisible, value);
    }

    public string DiscardConfirmationMessage => _pendingDiscard == DiscardAction.Close
        ? "Close Ideation and discard all undecided candidates or running generation?"
        : "Clear all undecided candidates?";

    private DiscardAction _pendingDiscard;

    public void Open(IdeationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        CancelGeneration();
        _scope = scope;
        Guidance = string.Empty;
        CountText = DefaultCount.ToString();
        SelectedMode = IdeationMode.Basic;
        Candidates.Clear();
        Completed = 0;
        Requested = 0;
        Error = null;
        RejectionCandidate = null;
        IsDiscardConfirmationVisible = false;
        IsOpen = true;
        OnPropertyChanged(nameof(ScopeLabel));
        OnPropertyChanged(nameof(HasCandidates));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(IsRejectionVisible));
        RaiseCommandState();
        _ = _accessStatus.RefreshAsync();
    }

    public async Task GenerateAsync()
    {
        if (!CanGenerate || _scope is null || !TryGetCount(out var count))
        {
            Error = CountError ?? AccessMessage ?? "Ideation is unavailable.";
            return;
        }

        var token = ++_generationToken;
        _generationCancellation = new CancellationTokenSource();
        var cancellationToken = _generationCancellation.Token;
        IsBusy = true;
        var requestMode = SelectedMode;
        Error = null;
        Requested = count;
        Completed = 0;
        var progress = new Progress<IdeationGenerationProgress>(value =>
        {
            if (token == _generationToken && IsOpen)
            {
                Completed = value.Completed;
                Requested = value.Requested;
            }
        });

        try
        {
            var result = await _service.GenerateAsync(
                new IdeationGenerationRequest(
                    _scope,
                    requestMode,
                    string.IsNullOrWhiteSpace(Guidance) ? null : Guidance.Trim(),
                    count,
                    Candidates.Select(candidate => candidate.Text).ToArray()),
                progress,
                cancellationToken);
            if (token != _generationToken || !IsOpen)
            {
                return;
            }

            var existing = Candidates.Select(candidate => Normalize(candidate.Text)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in result.Candidates.OrderBy(candidate => candidate.RequestIndex))
            {
                var normalized = Normalize(candidate.Text);
                if (existing.Add(normalized))
                {
                    Candidates.Add(new IdeaCandidateViewModel(normalized, requestMode));
                }
            }

            Completed = result.Completed;
            Error = result.Cancelled ? null : result.Error;
            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(CanDiscard));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (token == _generationToken)
            {
                _generationCancellation?.Dispose();
                _generationCancellation = null;
                IsBusy = false;
            }
        }
    }

    public async Task CreateCandidateAsync(IdeaCandidateViewModel candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (_scope is null || !Candidates.Contains(candidate) || candidate.IsBusy)
        {
            return;
        }

        candidate.IsBusy = true;
        candidate.Error = null;
        var result = await _service.CreateAsync(_scope, candidate.Text);
        if (result.Succeeded)
        {
            Candidates.Remove(candidate);
            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(CanDiscard));
            WorkspaceChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            candidate.Error = result.Error ?? "The idea could not be created.";
        }

        candidate.IsBusy = false;
    }

    public async Task ConfirmRejectAsync()
    {
        var candidate = RejectionCandidate;
        if (_scope is null || candidate is null || candidate.IsBusy)
        {
            return;
        }

        candidate.IsBusy = true;
        candidate.Error = null;
        var result = await _service.RejectAsync(
            _scope,
            candidate.Text,
            string.IsNullOrWhiteSpace(RejectionReason) ? null : RejectionReason.Trim(),
            candidate.Mode);
        if (result.Succeeded)
        {
            Candidates.Remove(candidate);
            CancelReject();
            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(CanDiscard));
            WorkspaceChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            candidate.Error = result.Error ?? "The rejection could not be saved.";
            candidate.IsBusy = false;
        }
    }

    public void CancelReject()
    {
        if (RejectionCandidate is { } candidate)
        {
            candidate.IsBusy = false;
        }

        RejectionCandidate = null;
        RejectionReason = string.Empty;
        OnPropertyChanged(nameof(RejectionCandidate));
        OnPropertyChanged(nameof(RejectionReason));
        OnPropertyChanged(nameof(IsRejectionVisible));
    }

    public void RequestClear() => RequestDiscard(DiscardAction.Clear);

    public void RequestClose() => RequestDiscard(DiscardAction.Close);

    public void ConfirmDiscard()
    {
        var action = _pendingDiscard;
        if (action == DiscardAction.Close)
        {
            CancelGeneration();
        }

        Candidates.Clear();
        OnPropertyChanged(nameof(HasCandidates));
        OnPropertyChanged(nameof(CanDiscard));
        IsDiscardConfirmationVisible = false;
        if (action == DiscardAction.Close)
        {
            CancelReject();
            IsOpen = false;
        }
    }

    public void CancelDiscard()
    {
        IsDiscardConfirmationVisible = false;
        _pendingDiscard = DiscardAction.None;
    }

    private void RequestDiscard(DiscardAction action)
    {
        if (!HasCandidates && (action == DiscardAction.Clear || !IsBusy))
        {
            _pendingDiscard = action;
            ConfirmDiscard();
            return;
        }

        _pendingDiscard = action;
        OnPropertyChanged(nameof(DiscardConfirmationMessage));
        IsDiscardConfirmationVisible = true;
    }

    private void CancelGeneration()
    {
        _generationToken++;
        _generationCancellation?.Cancel();
        _generationCancellation?.Dispose();
        _generationCancellation = null;
        IsBusy = false;
    }

    private bool TryGetCount(out int count) =>
        int.TryParse(CountText, out count) && count is >= MinimumCount and <= MaximumCount;

    private void IncrementCount() => CountText = GetNextCountText(1);

    private void DecrementCount() => CountText = GetNextCountText(-1);

    private string GetNextCountText(int direction)
    {
        if (int.TryParse(CountText, out int n))
        {
            n = Math.Clamp(n, MinimumCount, MaximumCount);
            n = Math.Clamp(n + direction, MinimumCount, MaximumCount);
            return n.ToString();
        }

        return (direction > 0 ? DefaultCount : MinimumCount).ToString();
    }

    private void RaiseCommandState()
    {
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(AccessMessage));
        OnPropertyChanged(nameof(CanManageSnowclones));
        OnPropertyChanged(nameof(CanIncrementCount));
        OnPropertyChanged(nameof(CanDecrementCount));
    }

    public void OpenSnowcloneLibrary()
    {
        if (_snowcloneLibrary is null || IsSnowcloneLibraryOpen || IsBusy || !IsSnowclonesMode)
        {
            return;
        }

        SnowcloneLibrary = new SnowcloneLibraryViewModel(_snowcloneLibrary);
        _isSnowcloneLibraryOpen = true;
        OnPropertyChanged(nameof(SnowcloneLibrary));
        OnPropertyChanged(nameof(IsSnowcloneLibraryOpen));
        RaiseCommandState();
    }

    public async Task CompleteSnowcloneLibraryAsync()
    {
        _isSnowcloneLibraryOpen = false;
        SnowcloneLibrary = null;
        OnPropertyChanged(nameof(SnowcloneLibrary));
        OnPropertyChanged(nameof(IsSnowcloneLibraryOpen));
        await RefreshSnowcloneLibraryAsync();
    }

    private async Task RefreshSnowcloneLibraryAsync()
    {
        if (_snowcloneLibrary is null)
        {
            return;
        }

        var result = await _snowcloneLibrary.LoadAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _hasSnowclones = result.Succeeded && result.State.AllSnowclones.Count > 0;
            OnPropertyChanged(nameof(HasSnowclones));
            OnPropertyChanged(nameof(SnowcloneLibraryMessage));
            RaiseCommandState();
        });
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();

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

    private enum DiscardAction
    {
        None,
        Clear,
        Close
    }
}
