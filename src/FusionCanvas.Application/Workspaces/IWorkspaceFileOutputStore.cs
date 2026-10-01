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
}
