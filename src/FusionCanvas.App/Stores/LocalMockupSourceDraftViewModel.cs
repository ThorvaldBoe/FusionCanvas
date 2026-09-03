using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

public sealed class LocalMockupSourceDraftViewModel(string path, IReadOnlyList<Guid> optionValueIds, bool isManaged = false, MockupImageSpaceMapping? mapping = null, int imageWidth = 0, int imageHeight = 0, Guid? sourceImageId = null, string? previewPath = null) : INotifyPropertyChanged
{
    private static (int Width, int Height) ReadPreviewDimensions(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                using var bitmap = new Bitmap(path);
                return (bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            }
        }
        catch { }
        return (0, 0);
    }

    private readonly (int Width, int Height) _previewDimensions = imageWidth > 0 && imageHeight > 0 ? (imageWidth, imageHeight) : mapping is not null ? (mapping.ImageWidth, mapping.ImageHeight) : ReadPreviewDimensions(previewPath ?? path);
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Path { get; } = path;
    public string DisplayName => System.IO.Path.GetFileName(Path);
    public IReadOnlyList<Guid> OptionValueIds { get; private set; } = optionValueIds;
    public bool IsManaged { get; } = isManaged;
    public Guid? SourceImageId { get; } = sourceImageId;
    public string PreviewPath { get; } = previewPath ?? path;
    public MockupImageSpaceMapping? Mapping { get; private set; } = mapping;
    public int ImageWidth => _previewDimensions.Width;
    public int ImageHeight => _previewDimensions.Height;
    public string ApplicabilitySummary { get; set; } = string.Empty;
    public bool IsComplete => OptionValueIds.Count > 0 && Mapping is not null;
    public string StatusLabel => IsComplete ? "Complete" : "Needs setup";
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public void UpdateMetadata(IReadOnlyList<Guid> optionValueIds, MockupImageSpaceMapping? mapping, string summary)
    {
        OptionValueIds = optionValueIds;
        Mapping = mapping;
        ApplicabilitySummary = summary;
    }
}

/// <summary>Presentation state for the authoritative normalized Blueprint Offering editor.</summary>
