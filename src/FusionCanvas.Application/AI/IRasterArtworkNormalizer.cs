namespace FusionCanvas.Application.AI;

public interface IRasterArtworkNormalizer
{
    Task<RasterArtworkNormalizationResult> NormalizeAsync(Stream source, RasterArtworkNormalizationRequest request, CancellationToken cancellationToken = default);
}
