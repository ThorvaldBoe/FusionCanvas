namespace FusionCanvas.Application.Stores;

public interface IStoreManagementService
{
    Guid? ActiveWorkspaceId { get; }

    Guid? ActiveStoreId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default);

    Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
}
