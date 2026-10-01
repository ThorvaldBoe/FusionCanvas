namespace FusionCanvas.Application.Stores;

public interface IStoreContextReader
{
    Task<StoreSummary?> ResolveActiveStoreAsync(
        Guid workspaceId,
        Guid storeId,
        CancellationToken cancellationToken = default);
}
