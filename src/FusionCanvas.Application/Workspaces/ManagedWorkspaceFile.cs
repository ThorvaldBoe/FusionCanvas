using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Workspaces;

public sealed record ManagedWorkspaceFile(
    string Name,
    AssetKind Kind,
    string WorkspaceRelativePath,
    string FullPath,
    string OriginalSourcePath);
