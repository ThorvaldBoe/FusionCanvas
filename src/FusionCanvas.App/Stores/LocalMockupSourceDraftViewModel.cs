using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

public sealed class LocalMockupSourceDraftViewModel(string path, IReadOnlyList<Guid> optionValueIds, bool isManaged = false, MockupImageSpaceMapping? mapping = null, int imageWidth = 0, int imageHeight = 0, Guid? sourceImageId = null, string? previewPath = null, string? previewReadError = null) : INotifyPropertyChanged
{

    private readonly (int Width, int Height) _previewDimensions = imageWidth > 0 && imageHeight > 0 ? (imageWidth, imageHeight) : mapping is not null ? (mapping.ImageWidth, mapping.ImageHeight) : (0, 0);
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Path { get; } = path;
    public string DisplayName => System.IO.Path.GetFileName(Path);
    public IReadOnlyList<Guid> OptionValueIds { get; private set; } = optionValueIds;
    public bool IsManaged { get; private set; } = isManaged;
    public Guid? SourceImageId { get; private set; } = sourceImageId;
    public string PreviewPath { get; } = previewPath ?? path;
    public MockupImageSpaceMapping? Mapping { get; private set; } = mapping;
    public int ImageWidth => _previewDimensions.Width;
    public int ImageHeight => _previewDimensions.Height;
    public bool HasPreviewDimensions => ImageWidth > 0 && ImageHeight > 0;
    public string? PreviewReadError { get; } = previewReadError;
    public bool HasPreviewReadError => !string.IsNullOrWhiteSpace(PreviewReadError);
    public string ApplicabilitySummary { get; set; } = string.Empty;
    public bool IsComplete => OptionValueIds.Count > 0 && Mapping is not null;
    public string StatusLabel => IsComplete ? "Complete" : "Needs setup";
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public bool IsAlternateRow { get; private set; }
    internal void SetRowPresentationIndex(int index)
    {
        var isAlternate = index % 2 == 1;
        if (IsAlternateRow == isAlternate) return;
        IsAlternateRow = isAlternate;
        PropertyChanged?.Invoke(this, new(nameof(IsAlternateRow)));
    }
    public void UpdateMetadata(IReadOnlyList<Guid> optionValueIds, MockupImageSpaceMapping? mapping, string summary)
    {
        OptionValueIds = optionValueIds;
        Mapping = mapping;
        ApplicabilitySummary = summary;
        PropertyChanged?.Invoke(this, new(nameof(ApplicabilitySummary)));
        PropertyChanged?.Invoke(this, new(nameof(StatusLabel)));
    }

    public void MarkManaged(Guid sourceImageId)
    {
        IsManaged = true;
        SourceImageId = sourceImageId;
        PropertyChanged?.Invoke(this, new(nameof(IsManaged)));
        PropertyChanged?.Invoke(this, new(nameof(SourceImageId)));
    }
}

/// <summary>Presentation state for the authoritative normalized Blueprint Offering editor.</summary>
