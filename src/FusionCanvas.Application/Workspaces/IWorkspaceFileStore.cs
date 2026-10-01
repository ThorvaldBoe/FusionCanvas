using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileStore : IWorkspaceFileReader, IWorkspaceFileDeleter
{
    string WorkspaceRoot { get; }

    string ResolvePath(string workspaceRelativePath);

    Task<ManagedWorkspaceFile> ImportAsync(
        string sourcePath,
        AssetKind kind,
        CancellationToken cancellationToken = default);

    Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default);
}
