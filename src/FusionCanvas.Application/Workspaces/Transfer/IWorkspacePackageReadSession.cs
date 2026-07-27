using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspaces.Transfer;

public interface IWorkspacePackageReadSession : IAsyncDisposable
{
    WorkspacePackageManifest Manifest { get; }

    WorkspaceSnapshot Snapshot { get; }

    IReadOnlyList<WorkspacePackageReadEntry> Files { get; }

    IReadOnlyList<string> SkippedUnsupportedFiles { get; }
}
