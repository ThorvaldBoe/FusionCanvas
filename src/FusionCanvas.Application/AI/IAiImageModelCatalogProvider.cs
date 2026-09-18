namespace FusionCanvas.Application.AI;

public interface IAiImageModelCatalogProvider
{
    Task<AiModelCatalog> GetImageModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}
