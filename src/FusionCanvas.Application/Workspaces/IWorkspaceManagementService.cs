namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceManagementService
{
    Guid? ActiveWorkspaceId { get; }

    Task<WorkspaceManagementState> LoadAsync(CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> CreateWorkspaceAsync(WorkspaceManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> UpdateWorkspaceAsync(WorkspaceManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> ArchiveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> RestoreWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> DeleteWorkspaceAsync(WorkspaceManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> SelectWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
