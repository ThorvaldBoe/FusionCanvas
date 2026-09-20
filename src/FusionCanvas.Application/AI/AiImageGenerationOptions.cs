namespace FusionCanvas.Application.AI;

public sealed record AiImageGenerationOptions(
    AiImageSize? Size = null,
    string? Resolution = null,
    string? AspectRatio = null,
    string? OutputFormat = null,
    string? Background = null,
    int? Count = null);
