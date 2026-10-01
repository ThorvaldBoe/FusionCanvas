namespace FusionCanvas.Application.AI;

public interface IAiModelCatalogCache : IAiModelCatalogReader
{
    Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default);
}
