using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public interface IWorkspaceBatchRollbackService
{
    Task<WorkspaceBatchRollbackResult> RestoreAsync(
        WorkspaceSnapshot originalSnapshot,
        CancellationToken cancellationToken = default);
}
