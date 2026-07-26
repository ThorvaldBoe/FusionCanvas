using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Integration.Workspace;

public sealed class InMemoryWorkspaceFileStore : IWorkspaceFileStore
{
    private readonly HashSet<string> _existing = [];

    public string WorkspaceRoot => string.Empty;

    public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
    {
        var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
        _existing.Add(relativePath);
        return Task.FromResult(new ManagedWorkspaceFile(
            Path.GetFileName(sourcePath),
            kind,
            relativePath,
            Path.Combine("workspace", relativePath),
            sourcePath));
    }

    public bool Exists(string workspaceRelativePath) => _existing.Contains(workspaceRelativePath.Replace('\\', '/'));

    public bool TryDelete(string workspaceRelativePath) => _existing.Remove(workspaceRelativePath.Replace('\\', '/'));

    public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
