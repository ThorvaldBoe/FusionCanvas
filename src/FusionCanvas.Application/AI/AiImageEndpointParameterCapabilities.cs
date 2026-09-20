namespace FusionCanvas.Application.AI;

public sealed record AiImageEndpointParameterCapabilities(
    IReadOnlyList<string> AspectRatios,
    IReadOnlyList<string> Resolutions,
    bool SupportsExplicitSize,
    bool SupportsOutputFormat,
    IReadOnlyList<string> Backgrounds,
    bool SupportsImageCount);
