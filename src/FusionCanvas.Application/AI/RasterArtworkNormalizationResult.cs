namespace FusionCanvas.Application.AI;

public sealed record RasterArtworkNormalizationResult(byte[] PngBytes, AiImageSize FinalSize, bool HasTransparency, IReadOnlyList<string> Warnings);
