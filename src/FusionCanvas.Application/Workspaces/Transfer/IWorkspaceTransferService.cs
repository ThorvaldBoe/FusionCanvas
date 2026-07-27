namespace FusionCanvas.Application.Workspaces.Transfer;

public interface IWorkspaceTransferService
{
    Task<WorkspaceTransferResult> ExportWorkspaceAsync(
        WorkspaceExportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<WorkspaceTransferResult> ImportWorkspaceAsync(
        WorkspaceImportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
