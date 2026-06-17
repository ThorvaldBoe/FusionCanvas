using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public interface IWorkspaceRepository
{
    Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default);
}
