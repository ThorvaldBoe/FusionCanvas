namespace FusionCanvas.Application.DesignFiles;

public interface IDesignFileService
{
    Task<IReadOnlyList<DesignFileSummary>> ListForItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<DesignFileImportResult> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default);

    Task<Stream> OpenPreviewAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task ExportCopyAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default);

    Task<DesignFileRemoveResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default);
}
