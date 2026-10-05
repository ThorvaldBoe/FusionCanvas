namespace FusionCanvas.Application.AI;

public interface IAiModelEndpointCatalogProvider
{
    Task<AiModelEndpointCatalog> GetEndpointsAsync(
        string apiKey,
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}
