using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Integration.Files;

public sealed class InMemoryWorkspaceFileStore : IWorkspaceFileStore
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public string WorkspaceRoot => string.Empty;

    public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
    {
        var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
        _files[relativePath] = [];
        return Task.FromResult(new ManagedWorkspaceFile(
            Path.GetFileName(sourcePath),
            kind,
            relativePath,
            Path.Combine("workspace", relativePath),
            sourcePath));
    }

    public async Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var relativePath = $"assets/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        _files[relativePath] = buffer.ToArray();
        return new(Path.GetFileName(fileName), kind, relativePath, Path.Combine("workspace", relativePath), string.Empty);
    }

    public bool Exists(string workspaceRelativePath) => _files.ContainsKey(Normalize(workspaceRelativePath));

    public bool TryDelete(string workspaceRelativePath) => _files.Remove(Normalize(workspaceRelativePath));

    public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(workspaceRelativePath);
        if (!_files.TryGetValue(normalized, out var content))
        {
            throw new FileNotFoundException("The managed workspace file was not found.", normalized);
        }

        return Task.FromResult<Stream>(new MemoryStream(content, writable: false));
    }

    public async Task<WorkspaceFileRestoreOutcome> RestoreAsync(
        string workspaceRelativePath,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(workspaceRelativePath);
        if (_files.ContainsKey(normalized))
        {
            return WorkspaceFileRestoreOutcome.SkippedExisting;
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _files[normalized] = buffer.ToArray();
        return WorkspaceFileRestoreOutcome.Created;
    }

    public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    private static string Normalize(string workspaceRelativePath) =>
        WorkspaceFileReference.Normalize(workspaceRelativePath);
}
