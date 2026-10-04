using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.ContentRisk;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.ContentRisk;

namespace FusionCanvas.Application.DesignFiles;

public sealed class DesignFileService : IDesignFileService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private readonly IContentRiskReviewService? _contentRiskReviews;

    public DesignFileService(
        IWorkspaceRepository repository,
        IWorkspaceFileStore fileStore,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null,
        IContentRiskReviewService? contentRiskReviews = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
        _contentRiskReviews = contentRiskReviews;
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

        byte[] managedBytes;
        try
        {
            await using var importedStream = await _fileStore.OpenReadAsync(imported.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await importedStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            managedBytes = buffer.ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await ManagedWorkspaceFileCleanup.TryDeleteAsync(_fileStore, imported.WorkspaceRelativePath).ConfigureAwait(false);
            return DesignFileImportResult.Failure($"The imported Design file could not be reviewed. {exception.Message}");
        }

        var assetId = _newId();
        var now = _clock();
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
        var reviewTarget = new ContentRiskReviewTarget(assetId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");
        var review = ContentRiskReview.Unreviewed(reviewTarget, ContentRiskFingerprint.ForImage(reviewTarget, managedBytes));

        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, link],
            ContentRiskReviews = [.. snapshot.ContentRiskReviews.Where(existing => existing.Target != reviewTarget), review]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var cleanup = await ManagedWorkspaceFileCleanup.TryDeleteAsync(_fileStore, imported.WorkspaceRelativePath).ConfigureAwait(false);
            ManagedWorkspaceFileCleanup.PreserveDiagnostic(exception, cleanup, "Design file import");
            if (exception is OperationCanceledException)
            {
                throw;
            }

            return DesignFileImportResult.Failure(
                $"The Design file record could not be persisted. {exception.Message}{ManagedWorkspaceFileCleanup.FailureMessage(cleanup)}");
        }

        if (_contentRiskReviews is not null)
        {
            try
            {
                await _contentRiskReviews.ReviewImageAsync(reviewTarget, "image/png", managedBytes, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The persisted Unreviewed state remains visible when advisory review cannot run.
            }
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
