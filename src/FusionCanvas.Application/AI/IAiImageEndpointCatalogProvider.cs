namespace FusionCanvas.Application.AI;

public interface IAiImageEndpointCatalogProvider
{
    Task<IReadOnlyList<AiImageEndpointCapabilities>> GetImageEndpointsAsync(
        string apiKey,
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}
