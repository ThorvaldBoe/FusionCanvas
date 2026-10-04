using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileOutputStore : IWorkspaceFileDeleter
{
    Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default);

    Task<ManagedWorkspaceFile> SaveAsync(
        string fileName,
        AssetKind kind,
        Stream content,
        CancellationToken cancellationToken = default);

    async Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        await using var source = await OpenReadAsync(workspaceRelativePath, cancellationToken).ConfigureAwait(false);
        await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}
