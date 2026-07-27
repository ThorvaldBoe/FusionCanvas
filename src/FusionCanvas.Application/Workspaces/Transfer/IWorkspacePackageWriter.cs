namespace FusionCanvas.Application.Workspaces.Transfer;

public interface IWorkspacePackageWriter
{
    int CurrentFormatVersion { get; }

    int CurrentSchemaVersion { get; }

    string AppVersion { get; }

    Task<WorkspacePackageWriteResult> WriteAsync(
        WorkspacePackageWriteRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
