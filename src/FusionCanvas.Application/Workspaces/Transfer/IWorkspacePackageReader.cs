namespace FusionCanvas.Application.Workspaces.Transfer;

public interface IWorkspacePackageReader
{
    Task<WorkspacePackageReadResult> ReadAsync(
        string packagePath,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
