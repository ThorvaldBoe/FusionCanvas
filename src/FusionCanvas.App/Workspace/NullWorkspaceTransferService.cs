using FusionCanvas.Application.Workspaces.Transfer;

namespace FusionCanvas.App.Workspace;

internal sealed class NullWorkspaceTransferService : IWorkspaceTransferService
{
    public static NullWorkspaceTransferService Instance { get; } = new();

    private NullWorkspaceTransferService()
    {
    }

    public Task<WorkspaceTransferResult> ExportWorkspaceAsync(
        WorkspaceExportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkspaceTransferResult.Failure("Workspace transfer is not configured."));

    public Task<WorkspaceTransferResult> ImportWorkspaceAsync(
        WorkspaceImportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkspaceTransferResult.Failure("Workspace transfer is not configured."));
}
