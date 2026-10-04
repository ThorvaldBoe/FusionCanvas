using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceRepository : IWorkspaceSnapshotReader
{
    Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default);
}
