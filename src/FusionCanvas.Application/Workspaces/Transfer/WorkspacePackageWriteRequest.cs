using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspacePackageWriteRequest(
    string DestinationPath,
    WorkspaceSnapshot Snapshot,
    WorkspacePackageManifest Manifest,
    IWorkspaceFileStore FileStore);
