namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileReader
{
    bool Exists(string workspaceRelativePath);

    Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default);
}
