using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.Application.Workspace;

public sealed record DesignFileSummary(
    Guid AssetId,
    string Name,
    string WorkspaceRelativePath,
    bool IsMissing,
    bool CanPreview,
    bool CanExport);

public sealed record DesignFileImportResult(bool Succeeded, string? Error, DesignFileSummary? File)
{
    public static DesignFileImportResult Success(DesignFileSummary file) => new(true, null, file);
    public static DesignFileImportResult Failure(string error) => new(false, error, null);
}

public sealed record DesignFileRemoveResult(bool Succeeded, string? Error)
{
    public static DesignFileRemoveResult Success() => new(true, null);
    public static DesignFileRemoveResult Failure(string error) => new(false, error);
}

public interface IDesignFileService
{
    Task<IReadOnlyList<DesignFileSummary>> ListForItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<DesignFileImportResult> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default);

    Task<Stream> OpenPreviewAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task ExportCopyAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default);

    Task<DesignFileRemoveResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default);
}

public sealed class DesignFileService : IDesignFileService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;

    public DesignFileService(IWorkspaceRepository repository, IWorkspaceFileStore fileStore)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    public async Task<IReadOnlyList<DesignFileSummary>> ListForItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return ListDesignFiles(snapshot, itemId);
    }

    public async Task<DesignFileImportResult> ImportAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return DesignFileImportResult.Failure("A source file path is required.");
        }

        if (!IsPng(sourcePath))
        {
            return DesignFileImportResult.Failure("Basic Design files must be PNG.");
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return DesignFileImportResult.Failure("The item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignFile);
        if (!editDecision.IsAllowed)
        {
            return DesignFileImportResult.Failure(editDecision.Reason);
        }

        ManagedWorkspaceFile imported;
        try
        {
            imported = await _fileStore.ImportAsync(sourcePath, AssetKind.ExportedImage, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return DesignFileImportResult.Failure("The selected source file was not found.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignFileImportResult.Failure($"The Design file could not be imported. {exception.Message}");
        }

        var assetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var asset = new Asset(
            assetId,
            item.StoreId,
            Path.GetFileName(sourcePath),
            null,
            AssetKind.ExportedImage,
            imported.WorkspaceRelativePath,
            imported.OriginalSourcePath,
            isMissing: false,
            isArchived: false,
            now,
            now,
            "{}");
        var link = new AssetLink(assetId, WorkspaceEntityKind.Item, itemId);

        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, link]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _fileStore.TryDelete(imported.WorkspaceRelativePath);
            return DesignFileImportResult.Failure($"The Design file record could not be persisted. {exception.Message}");
        }

        return DesignFileImportResult.Success(ToSummary(asset));
    }

    public async Task<Stream> OpenPreviewAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var asset = snapshot.Assets.SingleOrDefault(candidate => candidate.Id == assetId)
            ?? throw new InvalidOperationException("The Design file asset was not found.");

        return await _fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportCopyAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var asset = snapshot.Assets.SingleOrDefault(candidate => candidate.Id == assetId)
            ?? throw new InvalidOperationException("The Design file asset was not found.");

        await _fileStore.ExportCopyAsync(asset.WorkspaceRelativePath, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DesignFileRemoveResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return DesignFileRemoveResult.Failure("The item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignFile);
        if (!editDecision.IsAllowed)
        {
            return DesignFileRemoveResult.Failure(editDecision.Reason);
        }

        var asset = snapshot.Assets.SingleOrDefault(candidate => candidate.Id == assetId);
        if (asset is null)
        {
            return DesignFileRemoveResult.Failure("The Design file was not found.");
        }

        var updated = snapshot with
        {
            Assets = snapshot.Assets.Where(candidate => candidate.Id != assetId).ToArray(),
            AssetLinks = snapshot.AssetLinks.Where(link => link.AssetId != assetId).ToArray()
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignFileRemoveResult.Failure($"The Design file removal could not be persisted. {exception.Message}");
        }

        _fileStore.TryDelete(asset.WorkspaceRelativePath);
        return DesignFileRemoveResult.Success();
    }

    private static IReadOnlyList<DesignFileSummary> ListDesignFiles(WorkspaceSnapshot snapshot, Guid itemId)
    {
        return snapshot.AssetLinks
            .Where(link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == itemId)
            .Select(link => snapshot.Assets.SingleOrDefault(asset => asset.Id == link.AssetId))
            .Where(asset => asset is not null)
            .Where(asset => asset!.Kind == AssetKind.ExportedImage && IsPng(asset.WorkspaceRelativePath))
            .Select(asset => ToSummary(asset!))
            .ToArray();
    }

    private static DesignFileSummary ToSummary(Asset asset) =>
        new(asset.Id,
            asset.Name,
            asset.WorkspaceRelativePath,
            asset.IsMissing,
            CanPreview(asset),
            CanExport(asset));

    private static bool CanPreview(Asset asset) => !asset.IsMissing;

    private static bool CanExport(Asset asset) => !asset.IsMissing;

    private static bool IsPng(string path) =>
        Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase);
}
