using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Assets;

namespace FusionCanvas.App.Assets;

public sealed record AssetPurposeOption(AssetKind Kind, string Label)
{
    public override string ToString() => Label;
}

public sealed class AssetRowViewModel : INotifyPropertyChanged
{
    private AssetPurposeOption _selectedPurpose;
    private bool _suppressRelabel;

    public AssetRowViewModel(AssetSummary summary, IReadOnlyList<AssetPurposeOption> purposes, AssetsViewModel parent)
    {
        Id = summary.Id;
        Name = summary.Name;
        ManagedFileName = summary.ManagedFileName;
        IsMissing = summary.IsMissing;
        ContextLabel = summary.ContextLabel;
        Purpose = summary.Kind;
        _selectedPurpose = purposes.SingleOrDefault(option => option.Kind == summary.Kind) ?? purposes[0];
        Parent = parent;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<AssetRowViewModel, AssetKind>? RelabelRequested;

    public Guid Id { get; }
    public string Name { get; }
    public string ManagedFileName { get; }
    public bool IsMissing { get; private set; }
    public string? ContextLabel { get; }
    public AssetKind Purpose { get; private set; }
    public AssetsViewModel Parent { get; }

    public AssetPurposeOption SelectedPurpose
    {
        get => _selectedPurpose;
        set
        {
            if (ReferenceEquals(_selectedPurpose, value) || _selectedPurpose == value)
            {
                return;
            }

            _selectedPurpose = value;
            OnPropertyChanged();
            if (!_suppressRelabel && value is not null && value.Kind != Purpose)
            {
                RelabelRequested?.Invoke(this, value.Kind);
            }
        }
    }

    public void ApplyRelabel(AssetKind kind, AssetPurposeOption option)
    {
        _suppressRelabel = true;
        Purpose = kind;
        _selectedPurpose = option;
        OnPropertyChanged(nameof(SelectedPurpose));
        _suppressRelabel = false;
    }

    public void RevertPurpose(AssetPurposeOption option)
    {
        _suppressRelabel = true;
        _selectedPurpose = option;
        OnPropertyChanged(nameof(SelectedPurpose));
        _suppressRelabel = false;
    }

    public void ApplyMissing(bool isMissing)
    {
        if (IsMissing == isMissing) return;
        IsMissing = isMissing;
        OnPropertyChanged(nameof(IsMissing));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class AssetsViewModel : INotifyPropertyChanged
{
    private readonly IAssetManagementService _service;
    private IAssetFilePicker _filePicker;
    private AssetContextReference? _context;
    private AssetContextDescriptor? _contextDescriptor;
    private string _contextTitle = string.Empty;
    private string _contextKindLabel = string.Empty;
    private bool _isOpen;
    private bool _isBusy;
    private bool _hasImportPending;
    private string? _pendingImportPath;
    private string? _pendingImportFileName;
    private AssetPurposeOption? _pendingImportPurpose;
    private AssetRowViewModel? _selectedAsset;
    private AssetRowViewModel? _removalCandidate;
    private bool _removalConfirmationVisible;
    private string? _errorMessage;

    public AssetsViewModel(IAssetManagementService service, IAssetFilePicker? filePicker = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _filePicker = filePicker ?? new NullAssetFilePicker();
        AvailablePurposes = BuildPurposeOptions();
        ImportCommand = new RelayCommand(_ => Run(BeginImportAsync()));
        ConfirmImportCommand = new RelayCommand(_ => Run(ConfirmImportAsync()));
        CancelImportCommand = new RelayCommand(_ => CancelImport());
        RequestRemoveCommand = new RelayCommand(parameter =>
        {
            if (parameter is AssetRowViewModel row) RequestRemove(row);
        });
        ConfirmRemoveCommand = new RelayCommand(_ => Run(ConfirmRemoveAsync()));
        CancelRemoveCommand = new RelayCommand(_ => CancelRemove());
        CloseCommand = new RelayCommand(_ => RequestClose());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? WorkspaceStructureChanged;

    public ObservableCollection<AssetRowViewModel> Assets { get; } = [];
    public IReadOnlyList<AssetPurposeOption> AvailablePurposes { get; }

    public string ContextTitle { get => _contextTitle; private set => SetField(ref _contextTitle, value); }
    public string ContextKindLabel { get => _contextKindLabel; private set => SetField(ref _contextKindLabel, value); }
    public bool IsOpen { get => _isOpen; set => SetField(ref _isOpen, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) RaiseActionState(); } }
    public bool HasImportPending { get => _hasImportPending; private set { if (SetField(ref _hasImportPending, value)) RaiseActionState(); } }
    public string? PendingImportFileName { get => _pendingImportFileName; private set => SetField(ref _pendingImportFileName, value); }
    public AssetPurposeOption? PendingImportPurpose
    {
        get => _pendingImportPurpose;
        set
        {
            if (SetField(ref _pendingImportPurpose, value)) RaiseActionState();
        }
    }
    public AssetRowViewModel? SelectedAsset
    {
        get => _selectedAsset;
        private set => SetField(ref _selectedAsset, value);
    }
    public bool RemovalConfirmationVisible { get => _removalConfirmationVisible; private set => SetField(ref _removalConfirmationVisible, value); }
    public string RemovalPromptMessage => _removalCandidate is null ? string.Empty : $"Permanently remove \"{_removalCandidate.Name}\"? The managed workspace copy will be deleted.";
    public string? ErrorMessage { get => _errorMessage; private set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasAssets => Assets.Count > 0;
    public bool IsEmpty => Assets.Count == 0 && !IsBusy && !HasImportPending;
    public bool CanImport => IsOpen && !IsBusy && !HasImportPending;
    public bool CanConfirmImport => HasImportPending && !IsBusy;
    public bool CanConfirmRemoval => RemovalConfirmationVisible && !IsBusy;

    public IAssetFilePicker FilePicker
    {
        get => _filePicker;
        set => _filePicker = value ?? new NullAssetFilePicker();
    }

    public ICommand ImportCommand { get; }
    public ICommand ConfirmImportCommand { get; }
    public ICommand CancelImportCommand { get; }
    public ICommand RequestRemoveCommand { get; }
    public ICommand ConfirmRemoveCommand { get; }
    public ICommand CancelRemoveCommand { get; }
    public ICommand CloseCommand { get; }

    public void SetActiveWorkspace(Guid? workspaceId) => _service.SetActiveWorkspace(workspaceId);

    public async Task OpenForContextAsync(AssetContextReference context, CancellationToken cancellationToken = default)
    {
        _context = context;
        IsOpen = true;
        HasImportPending = false;
        _pendingImportPath = null;
        PendingImportFileName = null;
        PendingImportPurpose = null;
        ErrorMessage = null;
        await LoadAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (_context is AssetContextReference context)
        {
            await LoadAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadAsync(AssetContextReference context, CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;
        var state = await _service.LoadAsync(context, cancellationToken).ConfigureAwait(false);
        ApplyState(state);
        IsBusy = false;
    }

    private void ApplyState(AssetManagementState state)
    {
        if (state.Context is AssetContextDescriptor descriptor)
        {
            _contextDescriptor = descriptor;
            ContextTitle = descriptor.DisplayName;
            ContextKindLabel = descriptor.ContextKindLabel;
        }
        else if (state.Error is not null && _contextDescriptor is null)
        {
            ContextTitle = string.Empty;
            ContextKindLabel = string.Empty;
        }

        var previouslySelected = _selectedAsset?.Id;
        Assets.Clear();
        foreach (var summary in state.Assets)
        {
            var row = new AssetRowViewModel(summary, AvailablePurposes, this);
            row.RelabelRequested += OnRelabelRequested;
            Assets.Add(row);
        }

        ErrorMessage = state.Error;
        _selectedAsset = previouslySelected is Guid id ? Assets.FirstOrDefault(row => row.Id == id) : null;
        OnPropertyChanged(nameof(SelectedAsset));
        OnPropertyChanged(nameof(HasAssets));
        OnPropertyChanged(nameof(IsEmpty));
        RaiseActionState();
    }

    private async Task BeginImportAsync()
    {
        if (!CanImport || _context is null) return;
        var path = await _filePicker.PickImportFileAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _pendingImportPath = path;
        PendingImportFileName = Path.GetFileName(path);
        PendingImportPurpose = AvailablePurposes.SingleOrDefault(option => option.Kind == AssetPurposePolicy.SuggestKind(path))
            ?? AvailablePurposes[0];
        HasImportPending = true;
        ErrorMessage = null;
    }

    private async Task ConfirmImportAsync()
    {
        if (!CanConfirmImport || _context is null || _pendingImportPath is null || PendingImportPurpose is null) return;
        IsBusy = true;
        ErrorMessage = null;
        var result = await _service.ImportAssetAsync(
            new AssetManagementImportRequest(_context, _pendingImportPath, PendingImportPurpose.Kind))
            .ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return;
        }

        ApplyState(result.State);
        if (result.Asset is { } imported)
        {
            SelectedAsset = Assets.FirstOrDefault(row => row.Id == imported.Id);
        }

        CancelImport();
        WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelImport()
    {
        _pendingImportPath = null;
        PendingImportFileName = null;
        PendingImportPurpose = null;
        HasImportPending = false;
    }

    private void OnRelabelRequested(AssetRowViewModel row, AssetKind kind)
    {
        Run(RelabelAsync(row, kind));
    }

    private async Task RelabelAsync(AssetRowViewModel row, AssetKind kind)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        var result = await _service.RelabelAssetAsync(new AssetManagementRelabelRequest(row.Id, kind)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            row.RevertPurpose(AvailablePurposes.SingleOrDefault(option => option.Kind == row.Purpose) ?? AvailablePurposes[0]);
            return;
        }

        var option = AvailablePurposes.SingleOrDefault(candidate => candidate.Kind == kind) ?? AvailablePurposes[0];
        row.ApplyRelabel(kind, option);
        ApplyState(result.State);
        WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RequestRemove(AssetRowViewModel row)
    {
        if (IsBusy) return;
        _removalCandidate = row;
        RemovalConfirmationVisible = true;
        OnPropertyChanged(nameof(RemovalPromptMessage));
        ErrorMessage = null;
    }

    private async Task ConfirmRemoveAsync()
    {
        if (_removalCandidate is not { } row || !CanConfirmRemoval) return;
        RemovalConfirmationVisible = false;
        IsBusy = true;
        ErrorMessage = null;
        var result = await _service.RemoveAssetAsync(new AssetManagementRemoveRequest(row.Id, ConfirmPermanentRemoval: true)).ConfigureAwait(false);
        IsBusy = false;
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            _removalCandidate = null;
            return;
        }

        var fallbackIndex = Math.Max(0, Assets.IndexOf(row) - 1);
        _removalCandidate = null;
        ApplyState(result.State);
        SelectedAsset = Assets.Count > 0 ? Assets[Math.Min(fallbackIndex, Assets.Count - 1)] : null;
        WorkspaceStructureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelRemove()
    {
        _removalCandidate = null;
        RemovalConfirmationVisible = false;
    }

    private void RequestClose()
    {
        CancelImport();
        CancelRemove();
        ErrorMessage = null;
        IsOpen = false;
    }

    private static IReadOnlyList<AssetPurposeOption> BuildPurposeOptions() =>
        AssetPurposePolicy.AvailablePurposes.Select(ToOption).ToArray();

    private static AssetPurposeOption ToOption(AssetKind kind) => kind switch
    {
        AssetKind.SourceDesign => new(kind, "Source design"),
        AssetKind.ExportedImage => new(kind, "Exported image"),
        AssetKind.Svg => new(kind, "SVG"),
        AssetKind.MockupImage => new(kind, "Mockup"),
        AssetKind.ReferenceImage => new(kind, "Reference"),
        AssetKind.Texture => new(kind, "Texture"),
        AssetKind.Brush => new(kind, "Brush"),
        AssetKind.Font => new(kind, "Font"),
        AssetKind.Unknown => new(kind, "Unknown"),
        AssetKind.Other => new(kind, "Other"),
        _ => new(kind, kind.ToString())
    };

    private void RaiseActionState()
    {
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(nameof(CanConfirmImport));
        OnPropertyChanged(nameof(CanConfirmRemoval));
        OnPropertyChanged(nameof(HasAssets));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static void Run(Task task) => _ = task;
}
