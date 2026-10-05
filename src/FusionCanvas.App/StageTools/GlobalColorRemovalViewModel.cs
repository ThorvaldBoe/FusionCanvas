using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;

namespace FusionCanvas.App.StageTools;

public sealed class GlobalColorRemovalViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IGlobalColorRemovalService _service;
    private readonly Guid _itemId;
    private readonly Guid _assetId;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private CancellationTokenSource? _previewCts;
    private CancellationTokenSource? _applyCts;
    private Stream? _sourceStream;
    private Stream? _overlayStream;
    private Bitmap? _sourceBitmap;
    private Bitmap? _overlayBitmap;
    private GlobalColorRemovalColor? _pickedColor;
    private string _pickedColorHex = string.Empty;
    private double _tolerancePercent;
    private bool _isBusy;
    private string? _errorMessage;
    private GlobalColorRemovalRasterPreview? _preview;
    private long _previewGeneration;
    private bool _disposed;

    public GlobalColorRemovalViewModel(
        IGlobalColorRemovalService service,
        Guid itemId,
        Guid assetId,
        string sourceName,
        bool isReadOnly = false)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _itemId = itemId;
        _assetId = assetId;
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? "Selected image" : sourceName;
        IsReadOnly = isReadOnly;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<GlobalColorRemovalApplyResult>? Applied;

    public string SourceName { get; }

    public bool IsReadOnly { get; }

    public Bitmap? SourceBitmap
    {
        get => _sourceBitmap;
        private set
        {
            if (ReferenceEquals(_sourceBitmap, value))
            {
                return;
            }

            _sourceBitmap?.Dispose();
            _sourceBitmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayBitmap));
        }
    }

    public Bitmap? OverlayBitmap
    {
        get => _overlayBitmap;
        private set
        {
            if (ReferenceEquals(_overlayBitmap, value))
            {
                return;
            }

            _overlayBitmap?.Dispose();
            _overlayBitmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayBitmap));
        }
    }

    public Bitmap? DisplayBitmap => OverlayBitmap ?? SourceBitmap;

    public string PickedColorHex
    {
        get => _pickedColorHex;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_pickedColorHex, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _pickedColorHex = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUseHexColor));
        }
    }

    public bool CanUseHexColor => GlobalColorRemovalColor.TryParse(PickedColorHex, out _)
        && !IsBusy
        && !IsReadOnly;

    public double TolerancePercent
    {
        get => _tolerancePercent;
        set
        {
            var normalized = Math.Clamp(value, 0, 100);
            if (Math.Abs(_tolerancePercent - normalized) < 0.001)
            {
                return;
            }

            _tolerancePercent = normalized;
            OnPropertyChanged();
            QueuePreviewRefresh();
        }
    }

    public bool HasPickedColor => _pickedColor is not null;

    public bool HasPreview => _preview is not null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanUseHexColor));
        }
    }

    public bool CanApply => !IsReadOnly
        && !IsBusy
        && _pickedColor is not null
        && _preview is { HasMatches: true, LeavesVisibleArtwork: true };

    public int MatchedPixelCount => _preview?.MatchedPixelCount ?? 0;

    public int VisiblePixelCount => _preview?.VisiblePixelCount ?? 0;

    public string StatusMessage
    {
        get
        {
            if (IsBusy)
            {
                return "Updating the removal preview…";
            }

            if (!HasPickedColor)
            {
                return "Pick a visible color from the image or enter a hex color.";
            }

            if (_preview is null)
            {
                return "Choose a color to preview the pixels that will be removed.";
            }

            if (!_preview.HasMatches)
            {
                return "No visible pixels match the selected color and tolerance.";
            }

            if (!_preview.LeavesVisibleArtwork)
            {
                return "This tolerance would remove all visible artwork. Reduce it before applying.";
            }

            return $"{_preview.MatchedPixelCount:N0} of {_preview.VisiblePixelCount:N0} visible pixels will become transparent.";
        }
    }

    public string WarningMessage => HasPickedColor
        ? "Global removal affects matching colors everywhere, including artwork details and shadows."
        : string.Empty;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (string.Equals(_errorMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public async Task<GlobalColorRemovalSourceResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _service.OpenSourcePreviewAsync(_itemId, _assetId, cancellationToken).ConfigureAwait(true);
        if (!result.Succeeded || result.Content is null)
        {
            ErrorMessage = result.Error;
            return result;
        }

        try
        {
            ReplaceSourceBitmap(result.Content);
            ErrorMessage = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"The selected image could not be displayed. {exception.Message}";
            return GlobalColorRemovalSourceResult.Failure(ErrorMessage);
        }

        return result;
    }

    public async Task PickColorAtAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        if (IsReadOnly || IsBusy)
        {
            return;
        }

        var result = await _service.SampleColorAsync(_itemId, _assetId, x, y, cancellationToken).ConfigureAwait(true);
        if (!result.Succeeded || result.Color is not { } color)
        {
            ErrorMessage = result.Error;
            return;
        }

        SetPickedColor(color);
        await RefreshPreviewAsync(debounce: false, cancellationToken).ConfigureAwait(true);
    }

    public async Task UseHexColorAsync(CancellationToken cancellationToken = default)
    {
        if (IsReadOnly || IsBusy)
        {
            return;
        }

        if (!GlobalColorRemovalColor.TryParse(PickedColorHex, out var color))
        {
            ErrorMessage = "Enter a color in #RRGGBB format.";
            return;
        }

        SetPickedColor(color);
        await RefreshPreviewAsync(debounce: false, cancellationToken).ConfigureAwait(true);
    }

    public async Task<GlobalColorRemovalApplyResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (!CanApply || _pickedColor is not { } color)
        {
            return GlobalColorRemovalApplyResult.Failure(StatusMessage);
        }

        var applyCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
        _applyCts = applyCts;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _service.ApplyAsync(
                _itemId,
                _assetId,
                new GlobalColorRemovalParameters(color, _tolerancePercent / 100d),
                applyCts.Token).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error;
                return result;
            }

            Applied?.Invoke(this, result);
            return result;
        }
        catch (OperationCanceledException) when (applyCts.IsCancellationRequested)
        {
            ErrorMessage = "Color removal was cancelled.";
            return GlobalColorRemovalApplyResult.Failure(ErrorMessage);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Color removal failed. {exception.Message}";
            return GlobalColorRemovalApplyResult.Failure(ErrorMessage);
        }
        finally
        {
            if (ReferenceEquals(_applyCts, applyCts))
            {
                _applyCts = null;
            }

            applyCts.Dispose();
            IsBusy = false;
        }
    }

    public void Cancel()
    {
        _previewCts?.Cancel();
        _applyCts?.Cancel();
        ErrorMessage = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _applyCts?.Cancel();
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
        OverlayBitmap = null;
        SourceBitmap = null;
        _overlayStream?.Dispose();
        _overlayStream = null;
        _sourceStream?.Dispose();
        _sourceStream = null;
    }

    private void SetPickedColor(GlobalColorRemovalColor color)
    {
        _pickedColor = color;
        PickedColorHex = color.ToHex();
        ErrorMessage = null;
        OnPropertyChanged(nameof(HasPickedColor));
        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(CanApply));
    }

    private void QueuePreviewRefresh()
    {
        if (_pickedColor is null || _disposed)
        {
            return;
        }

        _ = RefreshPreviewAsync(debounce: true, _lifetimeCts.Token);
    }

    private async Task RefreshPreviewAsync(bool debounce, CancellationToken cancellationToken)
    {
        if (_pickedColor is not { } color || _disposed)
        {
            return;
        }

        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
        var operationToken = _previewCts.Token;
        var generation = Interlocked.Increment(ref _previewGeneration);
        if (debounce)
        {
            try
            {
                await Task.Delay(100, operationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        IsBusy = true;
        try
        {
            var result = await _service.PreviewAsync(
                _itemId,
                _assetId,
                new GlobalColorRemovalParameters(color, _tolerancePercent / 100d),
                operationToken).ConfigureAwait(true);
            if (generation != Volatile.Read(ref _previewGeneration) || operationToken.IsCancellationRequested)
            {
                return;
            }

            if (!result.Succeeded || result.Preview is null)
            {
                ErrorMessage = result.Error;
                return;
            }

            ReplaceOverlayBitmap(result.Preview.OverlayPng);
            _preview = result.Preview;
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasPreview));
            OnPropertyChanged(nameof(MatchedPixelCount));
            OnPropertyChanged(nameof(VisiblePixelCount));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(CanApply));
        }
        catch (OperationCanceledException)
        {
            // A newer setting or disposed editor owns the replacement preview.
        }
        catch (Exception exception)
        {
            if (generation == Volatile.Read(ref _previewGeneration))
            {
                ErrorMessage = $"The color-removal preview failed. {exception.Message}";
            }
        }
        finally
        {
            if (generation == Volatile.Read(ref _previewGeneration))
            {
                IsBusy = false;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
    }

    private void ReplaceSourceBitmap(byte[] bytes)
    {
        _sourceStream?.Dispose();
        _sourceStream = new MemoryStream(bytes, writable: false);
        SourceBitmap = new Bitmap(_sourceStream);
    }

    private void ReplaceOverlayBitmap(byte[] bytes)
    {
        _overlayStream?.Dispose();
        _overlayStream = new MemoryStream(bytes, writable: false);
        OverlayBitmap = new Bitmap(_overlayStream);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
