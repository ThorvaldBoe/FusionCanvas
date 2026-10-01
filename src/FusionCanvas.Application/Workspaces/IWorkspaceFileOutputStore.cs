using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Workspaces;

public interface IWorkspaceFileOutputStore : IWorkspaceFileStore
{
    Task<ManagedWorkspaceFile> SaveAsync(
        string fileName,
        AssetKind kind,
        Stream content,
        CancellationToken cancellationToken = default);
}
