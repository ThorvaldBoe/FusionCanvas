using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public interface IWorkspaceFileStore
{
    string WorkspaceRoot { get; }

    Task<ManagedWorkspaceFile> ImportAsync(
        string sourcePath,
        AssetKind kind,
        CancellationToken cancellationToken = default);

    bool Exists(string workspaceRelativePath);
}

public sealed record ManagedWorkspaceFile(
    string Name,
    AssetKind Kind,
    string WorkspaceRelativePath,
    string FullPath,
    string OriginalSourcePath);
