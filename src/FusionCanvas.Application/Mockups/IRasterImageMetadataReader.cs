namespace FusionCanvas.Application.Mockups;

public interface IRasterImageMetadataReader
{
    Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
}
