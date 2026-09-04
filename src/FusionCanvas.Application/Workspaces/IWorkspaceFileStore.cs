using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileStore
{
    string WorkspaceRoot { get; }

    Task<ManagedWorkspaceFile> ImportAsync(
        string sourcePath,
        AssetKind kind,
        CancellationToken cancellationToken = default);

    Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This workspace file store does not support generated file output.");

    bool Exists(string workspaceRelativePath);

    bool TryDelete(string workspaceRelativePath);

    Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default);

    Task<WorkspaceFileRestoreOutcome> RestoreAsync(
        string workspaceRelativePath,
        Stream content,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This workspace file store does not support restoring packaged files.");

    Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default);
}
