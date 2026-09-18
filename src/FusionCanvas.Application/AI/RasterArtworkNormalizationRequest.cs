namespace FusionCanvas.Application.AI;

public sealed record RasterArtworkNormalizationRequest(AiImageSize TargetSize, bool TransparencyRequested, long MaximumBytes = 25_000_000, long MaximumPixels = 100_000_000);
