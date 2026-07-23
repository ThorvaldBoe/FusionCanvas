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

    bool TryDelete(string workspaceRelativePath);

    Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default);

    Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default);
}

public sealed record ManagedWorkspaceFile(
    string Name,
    AssetKind Kind,
    string WorkspaceRelativePath,
    string FullPath,
    string OriginalSourcePath);
