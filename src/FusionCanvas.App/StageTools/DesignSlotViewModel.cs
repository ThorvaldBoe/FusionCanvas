using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

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
    public string ArtworkUploadActionText => HasImage ? "Replace artwork..." : "Browse artwork...";
    public string ArtworkUploadAccessibleName => HasImage
        ? $"Replace final design artwork in {AreaName}"
        : $"Browse for final design artwork for {AreaName}";
    public string ArtworkPreviewAccessibleName => $"Enlarge final design artwork in {AreaName}";

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
