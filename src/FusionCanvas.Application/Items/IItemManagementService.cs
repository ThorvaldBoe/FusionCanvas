using FusionCanvas.Application.WorkspaceTree;

namespace FusionCanvas.Application.Items;

public interface IItemManagementService
{
    Guid? ActiveWorkspaceId { get; }
    Guid? ActiveStoreId { get; }
    Guid? ActiveItemId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<ItemManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<ItemCreationDestinationResult> ResolveCreateTopicAsync(Guid storeId, WorkspaceTreeSelection? selection, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> CreateItemAsync(ItemManagementCreateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> UpdateItemAsync(ItemManagementUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> MoveItemAsync(ItemManagementMoveRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> DuplicateItemAsync(ItemManagementDuplicateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> RestoreItemAsync(ItemManagementRestoreRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> DeleteItemAsync(ItemManagementDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> SetItemStatusAsync(ItemManagementSetStatusRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> MoveItemStageAsync(ItemManagementMoveStageRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> SelectItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}
