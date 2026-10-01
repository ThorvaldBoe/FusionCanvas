namespace FusionCanvas.Application.AI;

public interface IAiModelCatalogReader
{
    Task<AiModelCatalog?> LoadAsync(
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}
