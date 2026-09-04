using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.DesignFiles;

/// <summary>Summary of a design slot assignment for the UI.</summary>
public sealed record DesignSlotSummary(
    Guid DesignAreaId,
    string AreaName,
    Guid? AssetId,
    string? ThumbnailPath,
    bool IsMissing,
    bool CanPreview,
    bool CanExport)
{
    public Guid PlaceholderId => DesignAreaId;
    public string? Position { get; init; }
    public string? DecorationMethod { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

/// <summary>Summary of a design variant row for the UI.</summary>
