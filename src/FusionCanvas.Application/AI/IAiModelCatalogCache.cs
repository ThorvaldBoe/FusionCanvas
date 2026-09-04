namespace FusionCanvas.Application.AI;

public interface IAiModelCatalogCache
{
    Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default);
    Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default);
}
