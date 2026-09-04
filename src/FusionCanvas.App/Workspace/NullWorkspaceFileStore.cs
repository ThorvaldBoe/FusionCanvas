using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;

namespace FusionCanvas.App.Workspace;

internal sealed class NullWorkspaceFileStore : IWorkspaceFileStore
{
    public static NullWorkspaceFileStore Instance { get; } = new();

    private NullWorkspaceFileStore()
    {
    }

    public string WorkspaceRoot => string.Empty;

    public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace file store is not configured. The composition root must inject it.");

    public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace file store is not configured. The composition root must inject it.");

    public bool Exists(string workspaceRelativePath) => false;

    public bool TryDelete(string workspaceRelativePath) => false;

    public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace file store is not configured. The composition root must inject it.");

    public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The workspace file store is not configured. The composition root must inject it.");
}
