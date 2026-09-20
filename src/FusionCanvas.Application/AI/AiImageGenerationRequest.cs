namespace FusionCanvas.Application.AI;

public sealed record AiImageGenerationRequest(
    string ModelId,
    string Prompt,
    AiImageSize ProviderSize,
    bool TransparentBackground,
    string ApiKey = "",
    bool RequireZeroDataRetention = true,
    string? ProviderTag = null,
    AiImageGenerationOptions? Options = null);
