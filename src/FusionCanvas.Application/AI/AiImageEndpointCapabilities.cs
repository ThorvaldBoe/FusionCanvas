namespace FusionCanvas.Application.AI;

public sealed record AiImageEndpointCapabilities(
    string EndpointId,
    string ModelId,
    bool ZeroDataRetentionCompatible,
    bool SupportsImageOutput,
    IReadOnlyList<string> RasterFormats,
    IReadOnlyList<AiImageSize> SupportedSizes,
    bool SupportsTransparency,
    string? ProviderName = null,
    AiImageEndpointParameterCapabilities? Parameters = null);
