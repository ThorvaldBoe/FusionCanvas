using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceSnapshotReader
{
    Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default);
}
