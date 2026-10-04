using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed class WorkspaceBatchRollbackService : IWorkspaceBatchRollbackService
{
    private readonly IWorkspaceRepository _repository;

    public WorkspaceBatchRollbackService(IWorkspaceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<WorkspaceBatchRollbackResult> RestoreAsync(
        WorkspaceSnapshot originalSnapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(originalSnapshot);

        try
        {
            await _repository.SaveAsync(originalSnapshot, cancellationToken).ConfigureAwait(false);
            return WorkspaceBatchRollbackResult.Success();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return WorkspaceBatchRollbackResult.Failure(ex.Message);
        }
    }
}
