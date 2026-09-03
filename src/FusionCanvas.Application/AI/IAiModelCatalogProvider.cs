namespace FusionCanvas.Application.AI;

public interface IAiModelCatalogProvider
{
    Task<AiModelCatalog> GetModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}
