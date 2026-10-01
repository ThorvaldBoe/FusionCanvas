namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileDeleter
{
    bool TryDelete(string workspaceRelativePath);
}
