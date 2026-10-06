namespace FusionCanvas.Application.AI;

public interface IAiModelEndpointCatalogCache
{
    Task<AiModelEndpointCatalog?> LoadAsync(
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AiModelEndpointCatalog catalog,
        CancellationToken cancellationToken = default);
}
