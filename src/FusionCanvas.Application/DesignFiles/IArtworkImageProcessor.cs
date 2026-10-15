namespace FusionCanvas.Application.DesignFiles;

/// <summary>Inspects and renders final Design artwork without exposing a raster library to Application.</summary>
public interface IArtworkImageProcessor
{
    Task<ArtworkImageInspection> InspectAsync(Stream source, CancellationToken cancellationToken = default);

    Task<byte[]> TransformAsync(
        Stream source,
        ArtworkPixelRectangle sourceBounds,
        ArtworkPixelRectangle targetBounds,
        CancellationToken cancellationToken = default);
}
