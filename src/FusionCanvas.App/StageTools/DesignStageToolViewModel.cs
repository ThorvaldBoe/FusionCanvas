using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Products;

namespace FusionCanvas.App.StageTools;

public sealed class DesignFileViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isMissing;
    private bool _canPreview;
    private bool _canExport;
    private bool _isBusy;

    public DesignFileViewModel(DesignFileSummary summary)
    {
        AssetId = summary.AssetId;
        _name = summary.Name;
        _isMissing = summary.IsMissing;
        _canPreview = summary.CanPreview;
        _canExport = summary.CanExport;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid AssetId { get; }

    public string Name
    {
        get => _name;
        private set { _name = value; OnPropertyChanged(); }
    }

    public bool IsMissing
    {
        get => _isMissing;
        private set { _isMissing = value; OnPropertyChanged(); }
    }

    public bool CanPreview
    {
        get => _canPreview;
        private set { _canPreview = value; OnPropertyChanged(); }
    }

    public bool CanExport
    {
        get => _canExport;
        private set { _canExport = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A selectable store design-area target displayed in the Design tool.</summary>
public sealed class DesignAreaTargetViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public DesignAreaTargetViewModel(DesignAreaTargetOption option, bool readOnly)
    {
        DesignAreaId = option.DesignAreaId;
        ProductName = option.ProductName;
        OfferingName = option.OfferingName;
        Position = option.Position;
        DecorationMethod = option.DecorationMethod;
        Width = option.Width;
        Height = option.Height;
        IsChoiceNetwork = option.IsChoiceNetwork;
        _isSelected = option.IsSelected;
        IsReadOnly = readOnly;
        Summary = $"{option.ProductName} · {option.OfferingName} · {option.Position}, {option.Width}×{option.Height} ({option.DecorationMethod})";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid DesignAreaId { get; }

    public string ProductName { get; }

    public string OfferingName { get; }

    public string Position { get; }

    public string DecorationMethod { get; }

    public int Width { get; }

    public int Height { get; }

    public bool IsChoiceNetwork { get; }

    public bool IsReadOnly { get; }

    public string Summary { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsReadOnly && SetField(ref _isSelected, value))
            {
                OnPropertyChanged();
            }
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DesignStageToolViewModel : INotifyPropertyChanged
{
    public string ChoiceNetworkWarning =>
        "Printify Choice is a variable fulfillment network. Its panel dimensions and placement can vary by fulfillment provider.";

    private readonly IDesignFileService _designFileService;
    private readonly IProductSupplierSetupService? _productService;
    private readonly Func<CancellationToken> _cancellationTokenProvider;
    private string _emptyState = "No Design files yet. Import a PNG to begin.";
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;
    private bool _isBusy;
    private DesignFileViewModel? _selectedFile;
    private string? _errorMessage;
    private string? _targetErrorMessage;
    private bool _isSavingTargets;
    private Guid _itemId;
    private string _targetEmptyState = "No printable areas configured for this item's Store.";

    public DesignStageToolViewModel(
        IDesignFileService designFileService,
        IProductSupplierSetupService? productService = null,
        Func<CancellationToken>? cancellationTokenProvider = null)
    {
        _designFileService = designFileService ?? throw new ArgumentNullException(nameof(designFileService));
        _productService = productService;
        _cancellationTokenProvider = cancellationTokenProvider ?? (() => CancellationToken.None);
        SaveDesignTargetsCommand = new RelayCommand(_ => _ = SaveTargetsAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveDesignTargetsCommand { get; }

    public ObservableCollection<DesignFileViewModel> Files { get; } = [];

    public ObservableCollection<DesignAreaTargetViewModel> Targets { get; } = [];

    public DesignFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set { _selectedFile = value; OnPropertyChanged(); }
    }

    public string EmptyState
    {
        get => _emptyState;
        set { _emptyState = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public bool HasTargets => Targets.Count > 0;

    public string TargetEmptyState
    {
        get => _targetEmptyState;
        set { _targetEmptyState = value; OnPropertyChanged(); }
    }

    public string? TargetErrorMessage
    {
        get => _targetErrorMessage;
        set { _targetErrorMessage = value; OnPropertyChanged(); }
    }

    public bool IsSavingTargets
    {
        get => _isSavingTargets;
        private set { _isSavingTargets = value; OnPropertyChanged(); }
    }

    public string SelectedTargetsSummary => HasSelectedChoiceTarget
        ? "Design is set to one or more Printify Choice printable areas."
        : HasTargets
            ? $"{Targets.Count(o => o.IsSelected)} printable {Pluralize(Targets.Count(o => o.IsSelected), "area")} selected."
            : "No printable area selected.";

    public bool HasSelectedChoiceTarget => Targets.Any(target => target.IsSelected && target.IsChoiceNetwork);

    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Design files and targets are read-only while the item is protected or an earlier stage is being reviewed.";
        _itemId = itemId;
        var files = await _designFileService.ListForItemAsync(itemId, cancellationToken).ConfigureAwait(true);
        Files.Clear();
        foreach (var summary in files)
        {
            Files.Add(new DesignFileViewModel(summary));
        }

        SelectedFile = Files.FirstOrDefault();
        EmptyState = Files.Count == 0 ? "No Design files yet. Import a PNG to begin." : string.Empty;
        await LoadTargetsAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task<bool> SaveTargetsAsync(CancellationToken cancellationToken = default)
    {
        if (_productService is null || IsReadOnly || IsSavingTargets)
        {
            return false;
        }

        IsSavingTargets = true;
        try
        {
            var requested = Targets.Where(target => target.IsSelected).Select(target => target.DesignAreaId).ToArray();
            var result = await _productService.ReplaceDesignTargetsAsync(
                new ReplaceDesignTargetsRequest(_itemId, requested),
                cancellationToken).ConfigureAwait(true);
            TargetErrorMessage = result.Error;
            if (result.Succeeded)
            {
                ApplyTargetState(result.State);
                return true;
            }

            ApplyTargetState(result.State);
            return false;
        }
        finally
        {
            IsSavingTargets = false;
        }
    }

    private async Task LoadTargetsAsync(CancellationToken cancellationToken)
    {
        if (_productService is null)
        {
            return;
        }

        try
        {
            var state = await _productService.LoadDesignTargetsAsync(_itemId, cancellationToken).ConfigureAwait(true);
            ApplyTargetState(state);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TargetErrorMessage = $"Design targets could not be loaded. {exception.Message}";
        }
    }

    private void ApplyTargetState(DesignTargetSelectionState state)
    {
        var targetsReadOnly = state.IsReadOnly || IsReadOnly;
        Targets.Clear();
        foreach (var option in state.Options)
        {
            Targets.Add(new DesignAreaTargetViewModel(option, targetsReadOnly));
        }

        TargetEmptyState = Targets.Count == 0 ? "No printable areas configured for this item's Store." : string.Empty;
        OnPropertyChanged(nameof(HasTargets));
        OnPropertyChanged(nameof(SelectedTargetsSummary));
        OnPropertyChanged(nameof(HasSelectedChoiceTarget));
        if (state.IsReadOnly)
        {
            IsReadOnly = true;
            ReadOnlyReason = string.IsNullOrWhiteSpace(ReadOnlyReason)
                ? "Design files and targets are read-only while the item is protected or an earlier stage is being reviewed."
                : ReadOnlyReason;
        }

        foreach (var target in Targets)
        {
            target.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DesignAreaTargetViewModel.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedTargetsSummary));
                    OnPropertyChanged(nameof(HasSelectedChoiceTarget));
                }
            };
        }
    }

    public async Task<bool> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (IsBusy || IsReadOnly)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            var result = await _designFileService.ImportAsync(itemId, sourcePath, cancellationToken).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                return false;
            }

            ErrorMessage = null;
            await LoadAsync(itemId, !IsReadOnly, cancellationToken).ConfigureAwait(true);
            if (result.File is not null)
            {
                SelectedFile = Files.SingleOrDefault(file => file.AssetId == result.File.AssetId);
            }

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<Stream?> PreviewAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || !file.CanPreview || IsBusy)
        {
            return null;
        }

        file.IsBusy = true;
        try
        {
            return await _designFileService.OpenPreviewAsync(file.AssetId, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"Preview could not be loaded. {exception.Message}";
            return null;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    public async Task<bool> ExportAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || !file.CanExport || IsBusy)
        {
            return false;
        }

        file.IsBusy = true;
        try
        {
            await _designFileService.ExportCopyAsync(file.AssetId, destinationPath, cancellationToken).ConfigureAwait(true);
            ErrorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed. {exception.Message}";
            return false;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    public async Task<bool> RemoveAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (SelectedFile is not { } file || IsBusy || IsReadOnly)
        {
            return false;
        }

        file.IsBusy = true;
        try
        {
            var result = await _designFileService.RemoveAsync(itemId, file.AssetId, cancellationToken).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                return false;
            }

            ErrorMessage = null;
            await LoadAsync(itemId, !IsReadOnly, cancellationToken).ConfigureAwait(true);
            SelectedFile = Files.FirstOrDefault();
            return true;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    private static string Pluralize(int count, string word) => count == 1 ? word : $"{word}s";

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
