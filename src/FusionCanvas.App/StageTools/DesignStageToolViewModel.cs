using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

public sealed class DesignColorViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public DesignColorViewModel(string colorValue, bool isSelected, bool isReadOnly)
    {
        ColorValue = colorValue;
        _isSelected = isSelected;
        IsReadOnly = isReadOnly;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ColorValue { get; }

    public bool IsReadOnly { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsReadOnly && _isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DesignSlotViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isBusy;
    private Bitmap? _thumbnail;

    public DesignSlotViewModel(DesignSlotSummary summary, bool isReadOnly)
    {
        DesignAreaId = summary.DesignAreaId;
        AreaName = summary.AreaName;
        Position = summary.Position;
        DecorationMethod = summary.DecorationMethod;
        Width = summary.Width;
        Height = summary.Height;
        AssetId = summary.AssetId;
        ThumbnailPath = summary.ThumbnailPath;
        IsMissing = summary.IsMissing;
        CanPreview = summary.CanPreview;
        CanExport = summary.CanExport;
        IsReadOnly = isReadOnly;

        if (summary.ThumbnailPath is not null && File.Exists(summary.ThumbnailPath))
        {
            _thumbnail = new Bitmap(summary.ThumbnailPath);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid DesignAreaId { get; }
    public string AreaName { get; }
    public string? Position { get; }
    public string? DecorationMethod { get; }
    public int? Width { get; }
    public int? Height { get; }
    public string PlaceholderDetails => string.Join(" · ", new[]
    {
        Position,
        DecorationMethod,
        Width is int width && Height is int height ? $"{width}×{height}px" : null
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
    public Guid? AssetId { get; }
    public string? ThumbnailPath { get; }
    public bool IsMissing { get; }
    public bool CanPreview { get; }
    public bool CanExport { get; }
    public bool IsReadOnly { get; }
    public bool HasImage => AssetId is not null;

    /// <summary>Bitmap for thumbnail display. May be null when the managed file is missing.</summary>
    public Bitmap? Thumbnail => _thumbnail;

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public void Dispose()
    {
        var bmp = Interlocked.Exchange(ref _thumbnail, null);
        bmp?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DesignRowViewModel : INotifyPropertyChanged
{
    public DesignRowViewModel(DesignRowSummary summary, bool isReadOnly)
    {
        RowId = summary.RowId;
        IsDefault = summary.IsDefault;
        SortOrder = summary.SortOrder;
        ColorValues = [.. summary.ColorValues];
        IsReadOnly = isReadOnly;
        foreach (var s in summary.Slots)
        {
            Slots.Add(new DesignSlotViewModel(s, isReadOnly));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid RowId { get; }
    public bool IsDefault { get; }
    public int SortOrder { get; }
    public bool IsReadOnly { get; }
    public IReadOnlyList<string> ColorValues { get; }
    public string ColorChips => ColorValues.Count > 0 ? string.Join(", ", ColorValues) : "(no colors)";
    public ObservableCollection<DesignSlotViewModel> Slots { get; } = [];

    public bool CanRemove => !IsDefault && !IsReadOnly;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DesignStageToolViewModel : INotifyPropertyChanged
{
    private readonly IDesignStageService _designStageService;
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;
    private bool _isBusy;
    private bool _hasConfiguration;
    private string _configPrompt = "Select a listing configuration to begin.";
    private bool _showPreviewDialog;
    private Guid _previewAssetId;
    private Stream? _previewStream;
    private Bitmap? _previewBitmap;
    private string? _errorMessage;
    private string _noConfigMessage = "No listing configuration selected. Select a configuration from the offerings below to show the design slot grid.";
    private Guid _itemId;
    private FulfillmentOffering? _selectedOffering;
    private Guid? _selectedOfferingId;
    private string? _selectedOfferingName;
    private string? _selectedBlueprintName;
    private string? _providerNetworkWarning;
    private PendingRemovalAction? _pendingRemoval;
    private bool _isRemovalConfirmationVisible;
    private string _removalConfirmationMessage = string.Empty;

    public DesignStageToolViewModel(IDesignStageService designStageService)
    {
        _designStageService = designStageService ?? throw new ArgumentNullException(nameof(designStageService));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Configuration ---
    public bool HasConfiguration
    {
        get => _hasConfiguration;
        private set { _hasConfiguration = value; OnPropertyChanged(); }
    }

    public string ConfigPrompt
    {
        get => _configPrompt;
        set { _configPrompt = value; OnPropertyChanged(); }
    }

    public FulfillmentOffering? SelectedOffering
    {
        get => _selectedOffering;
        set
        {
            if (_selectedOffering?.Id != value?.Id)
            {
                _selectedOffering = value;
                OnPropertyChanged();
                if (value is not null)
                {
                    _ = SelectConfigurationAsync(value.Id);
                }
            }
        }
    }

    public Guid? SelectedOfferingId
    {
        get => _selectedOfferingId;
        private set { _selectedOfferingId = value; OnPropertyChanged(); }
    }

    public string? SelectedOfferingName
    {
        get => _selectedOfferingName;
        private set { _selectedOfferingName = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedOfferingStatus)); }
    }

    public string? SelectedBlueprintName
    {
        get => _selectedBlueprintName;
        private set { _selectedBlueprintName = value; OnPropertyChanged(); }
    }

    public string? ProviderNetworkWarning
    {
        get => _providerNetworkWarning;
        private set { _providerNetworkWarning = value; OnPropertyChanged(); }
    }

    public string? SelectedOfferingStatus
    {
        get
        {
            if (_selectedOfferingId is null) return null;
            var offering = AvailableOfferings.SingleOrDefault(o => o.Id == _selectedOfferingId);
            if (offering is null) return null;
            return offering.Kind == FulfillmentKind.PrintifyChoiceNetwork
                ? "Printify Choice network"
                : offering.ProviderName is not null
                    ? $"Fixed provider: {offering.ProviderName}"
                    : null;
        }
    }

    public string NoConfigMessage
    {
        get => _noConfigMessage;
        set { _noConfigMessage = value; OnPropertyChanged(); }
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

    public bool ShowPreviewDialog
    {
        get => _showPreviewDialog;
        set { _showPreviewDialog = value; OnPropertyChanged(); }
    }

    public Guid PreviewAssetId
    {
        get => _previewAssetId;
        private set { _previewAssetId = value; OnPropertyChanged(); }
    }

    /// <summary>Bitmap for the large preview dialog. Disposed when preview is closed or replaced.</summary>
    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set { _previewBitmap = value; OnPropertyChanged(); }
    }

    public Stream? PreviewStream
    {
        get => _previewStream;
        private set { _previewStream = value; OnPropertyChanged(); }
    }

    // --- Removal confirmation ---

    public bool IsRemovalConfirmationVisible
    {
        get => _isRemovalConfirmationVisible;
        private set { _isRemovalConfirmationVisible = value; OnPropertyChanged(); }
    }

    public string RemovalConfirmationMessage
    {
        get => _removalConfirmationMessage;
        private set { _removalConfirmationMessage = value; OnPropertyChanged(); }
    }

    public void RequestRemoveSlotImage(Guid rowId, Guid designAreaId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SlotImage, rowId, designAreaId, null);
        RemovalConfirmationMessage = "Remove this slot image? This cannot be undone.";
        IsRemovalConfirmationVisible = true;
    }

    public void RequestRemoveSupportingImage(Guid assetId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SupportingImage, Guid.Empty, Guid.Empty, assetId);
        RemovalConfirmationMessage = "Remove this supporting image? This cannot be undone.";
        IsRemovalConfirmationVisible = true;
    }

    public void RequestRemoveSpecificRow(Guid rowId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SpecificRow, rowId, Guid.Empty, null);
        RemovalConfirmationMessage = "Remove this specific row? Its colors will move back to the default row.";
        IsRemovalConfirmationVisible = true;
    }

    public async Task ConfirmPendingRemovalAsync(CancellationToken ct = default)
    {
        if (_pendingRemoval is null || IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            DesignStageResult? result = null;
            switch (_pendingRemoval.Kind)
            {
                case PendingRemovalKind.SlotImage:
                    result = await _designStageService.RemoveSlotImageAsync(_itemId, _pendingRemoval.RowId, _pendingRemoval.DesignAreaId, ct).ConfigureAwait(true);
                    break;
                case PendingRemovalKind.SupportingImage:
                    result = await _designStageService.RemoveSupportingImageAsync(_itemId, _pendingRemoval.AssetId!.Value, ct).ConfigureAwait(true);
                    break;
                case PendingRemovalKind.SpecificRow:
                    result = await _designStageService.RemoveSpecificRowAsync(_itemId, _pendingRemoval.RowId, ct).ConfigureAwait(true);
                    break;
            }
            ErrorMessage = result?.Error;
            if (result?.Succeeded == true)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
            _pendingRemoval = null;
            IsRemovalConfirmationVisible = false;
            RemovalConfirmationMessage = string.Empty;
        }
    }

    public void CancelPendingRemoval()
    {
        _pendingRemoval = null;
        IsRemovalConfirmationVisible = false;
        RemovalConfirmationMessage = string.Empty;
    }

    // --- Collections ---
    public ObservableCollection<FulfillmentOffering> AvailableOfferings { get; } = [];
    public ObservableCollection<DesignColorViewModel> AvailableColors { get; } = [];
    public ObservableCollection<DesignColorViewModel> SelectedColors { get; } = [];
    public ObservableCollection<DesignRowViewModel> Rows { get; } = [];
    public ObservableCollection<DesignSlotViewModel> SupportingImages { get; } = [];

    // --- Commands ---
    public async Task SelectConfigurationAsync(Guid offeringId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.SelectConfigurationAsync(_itemId, offeringId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ToggleColorAsync(string colorValue, bool add, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            DesignStageResult result;
            if (add)
            {
                result = await _designStageService.AddSelectedColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            }
            else
            {
                result = await _designStageService.RemoveSelectedColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            }
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task MakeSpecificForColorAsync(string colorValue, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.MakeSpecificForColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RemoveSpecificRowAsync(Guid rowId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSpecificRowAsync(_itemId, rowId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AssignSlotImageAsync(Guid rowId, Guid designAreaId, string sourcePath, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.AssignSlotImageAsync(_itemId, rowId, designAreaId, sourcePath, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RemoveSlotImageAsync(Guid rowId, Guid designAreaId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSlotImageAsync(_itemId, rowId, designAreaId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PreviewSlotImageAsync(Guid rowId, Guid designAreaId, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var stream = await _designStageService.OpenSlotPreviewAsync(rowId, designAreaId, ct).ConfigureAwait(true);
            PreviewStream?.Dispose();
            PreviewBitmap?.Dispose();
            PreviewStream = stream;
            PreviewBitmap = stream is not null ? new Bitmap(stream) : null;
            PreviewAssetId = Guid.Empty;
            ShowPreviewDialog = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _designStageService.ExportSlotImageAsync(rowId, designAreaId, destinationPath, ct).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _designStageService.ExportSupportingImageAsync(assetId, destinationPath, ct).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportSupportingImageAsync(string sourcePath, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.ImportSupportingImageAsync(_itemId, sourcePath, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void PreviewSupportingImage(Guid assetId, string? thumbnailPath)
    {
        // For supporting images, load the thumbnail directly from the managed file path
        try
        {
            PreviewStream?.Dispose();
            PreviewBitmap?.Dispose();
            PreviewStream = null;

            if (thumbnailPath is not null && File.Exists(thumbnailPath))
            {
                PreviewBitmap = new Bitmap(thumbnailPath);
            }
            else
            {
                PreviewBitmap = null;
                ErrorMessage = "Could not open supporting image preview (file not found).";
            }

            PreviewAssetId = assetId;
            ShowPreviewDialog = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Preview failed: {ex.Message}";
        }
    }

    public async Task RemoveSupportingImageAsync(Guid assetId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSupportingImageAsync(_itemId, assetId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ClosePreviewDialog()
    {
        ShowPreviewDialog = false;
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;
        PreviewStream?.Dispose();
        PreviewStream = null;
    }

    // --- Load ---
    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Design stage content is read-only while the item is protected or an earlier stage is being reviewed.";
        _itemId = itemId;

        var state = await _designStageService.LoadDesignStageStateAsync(itemId, cancellationToken).ConfigureAwait(true);

        HasConfiguration = state.SelectedOfferingId is not null;
        SelectedOfferingId = state.SelectedOfferingId;
        SelectedOfferingName = state.SelectedOfferingName;
        SelectedBlueprintName = state.SelectedBlueprintName;
        ProviderNetworkWarning = state.ProviderNetworkWarning;
        _selectedOffering = state.SelectedOfferingId is not null
            ? state.AvailableOfferings.SingleOrDefault(o => o.Id == state.SelectedOfferingId.Value)
            : null;
        if (state.IsReadOnly)
        {
            IsReadOnly = true;
            ReadOnlyReason = state.ReadOnlyReason;
        }

        // Available offerings
        AvailableOfferings.Clear();
        foreach (var offering in state.AvailableOfferings)
        {
            AvailableOfferings.Add(offering);
        }

        // Ensure SelectedOfferingStatus is re-evaluated now that offerings are populated
        OnPropertyChanged(nameof(SelectedOfferingStatus));

        // Available colors
        AvailableColors.Clear();
        foreach (var color in state.AvailableColors)
        {
            var isSelected = state.SelectedColors.Any(c => string.Equals(c, color, StringComparison.OrdinalIgnoreCase));
            AvailableColors.Add(new DesignColorViewModel(color, isSelected, IsReadOnly));
        }

        // Selected colors
        SelectedColors.Clear();
        foreach (var color in state.SelectedColors)
        {
            SelectedColors.Add(new DesignColorViewModel(color, true, IsReadOnly));
        }

        // Rows — dispose old slot bitmaps first
        foreach (var row in Rows)
        {
            foreach (var slot in row.Slots)
            {
                slot.Dispose();
            }
        }
        Rows.Clear();
        foreach (var row in state.Rows)
        {
            Rows.Add(new DesignRowViewModel(row, IsReadOnly));
        }

        // Supporting images — dispose old slot bitmaps first
        foreach (var img in SupportingImages)
        {
            img.Dispose();
        }
        SupportingImages.Clear();
        foreach (var img in state.SupportingImages)
        {
            SupportingImages.Add(new DesignSlotViewModel(img, IsReadOnly));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Kind of removal pending user confirmation.</summary>
internal enum PendingRemovalKind
{
    SlotImage,
    SupportingImage,
    SpecificRow
}

/// <summary>Describes a removal awaiting confirmation.</summary>
internal sealed record PendingRemovalAction(
    PendingRemovalKind Kind,
    Guid RowId,
    Guid DesignAreaId,
    Guid? AssetId);
