namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileRestoreStore : IWorkspaceFileReader, IWorkspaceFileDeleter
{
    Task<WorkspaceFileRestoreOutcome> RestoreAsync(
        string workspaceRelativePath,
        Stream content,
        CancellationToken cancellationToken = default);
}
