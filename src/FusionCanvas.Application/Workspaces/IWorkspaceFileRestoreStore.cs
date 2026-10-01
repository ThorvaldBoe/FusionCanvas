namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileRestoreStore : IWorkspaceFileStore
{
    Task<WorkspaceFileRestoreOutcome> RestoreAsync(
        string workspaceRelativePath,
        Stream content,
        CancellationToken cancellationToken = default);
}
