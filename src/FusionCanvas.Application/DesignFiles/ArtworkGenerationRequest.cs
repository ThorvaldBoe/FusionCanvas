using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.DesignFiles;

public sealed record ArtworkGenerationRequest(
    Guid ItemId,
    Guid DesignAreaId,
    string ApiKey,
    AiProfileSettings ArtworkProfile,
    IReadOnlyList<AiModelDescriptor> Models,
    IReadOnlyList<AiImageEndpointCapabilities> Endpoints,
    bool TransparentBackground,
    bool RequireZeroDataRetention = true);
