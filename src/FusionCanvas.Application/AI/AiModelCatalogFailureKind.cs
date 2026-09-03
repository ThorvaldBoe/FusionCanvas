namespace FusionCanvas.Application.AI;

public enum AiModelCatalogFailureKind
{
    Authentication,
    RateLimited,
    NetworkOrService,
    InvalidResponse,
    ZdrDataUnavailable
}
