using FusionCanvas.Application.WorkspaceTree;

namespace FusionCanvas.Application.Groups;

public interface IGroupManagementService
{
    Guid? ActiveWorkspaceId { get; }
    Guid? ActiveStoreId { get; }
    Guid? ActiveNicheId { get; }
    Guid? ActiveGroupId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<GroupManagementState> LoadAsync(Guid? storeId, Guid? nicheId = null, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> CreateGroupAsync(GroupManagementCreateRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> UpdateGroupAsync(GroupManagementUpdateRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> MoveGroupAsync(GroupManagementMoveRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> CopyGroupAsync(GroupManagementCopyRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> DeleteGroupAsync(GroupManagementDeleteRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> ArchiveGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> RestoreGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> SelectGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> SetDefaultNicheAsync(Guid storeId, Guid nicheId, CancellationToken cancellationToken = default);
    Task<GroupCreationDestinationResult> ResolveCreateParentAsync(Guid storeId, WorkspaceTreeSelection? selection, CancellationToken cancellationToken = default);
}
