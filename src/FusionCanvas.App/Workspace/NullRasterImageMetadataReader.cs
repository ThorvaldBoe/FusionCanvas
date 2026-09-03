using FusionCanvas.Application.Mockups;

namespace FusionCanvas.App.Workspace;

internal sealed class NullRasterImageMetadataReader : IRasterImageMetadataReader
{
    public static NullRasterImageMetadataReader Instance { get; } = new();

    private NullRasterImageMetadataReader()
    {
    }

    public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Raster image metadata is not configured. The composition root must inject it.");
}
