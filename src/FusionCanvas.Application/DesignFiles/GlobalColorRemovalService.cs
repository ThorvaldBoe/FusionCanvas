using System.Text.Json;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.ContentRisk;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.DesignFiles;

public sealed class GlobalColorRemovalService : IGlobalColorRemovalService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly IWorkspaceFileOutputStore _outputStore;
    private readonly IGlobalColorRemovalProcessor _processor;
    private readonly IContentRiskReviewService? _contentRiskReviews;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public GlobalColorRemovalService(
        IWorkspaceRepository repository,
        IWorkspaceFileStore fileStore,
        IWorkspaceFileOutputStore outputStore,
        IGlobalColorRemovalProcessor processor,
        IContentRiskReviewService? contentRiskReviews = null,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _outputStore = outputStore ?? throw new ArgumentNullException(nameof(outputStore));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _contentRiskReviews = contentRiskReviews;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<GlobalColorRemovalAvailabilityResult> CheckAvailabilityAsync(
        Guid itemId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var source = await ResolveSourceAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        if (!source.Succeeded)
        {
            return GlobalColorRemovalAvailabilityResult.Failure(source.Error!);
        }

        try
        {
            await using var stream = await _fileStore.OpenReadAsync(source.Asset!.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
            await _processor.ValidateAsync(stream, cancellationToken).ConfigureAwait(false);
            return GlobalColorRemovalAvailabilityResult.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalAvailabilityResult.Failure(
                $"Global color removal is unavailable for this image. {exception.Message}");
        }
    }

    public async Task<GlobalColorRemovalSourceResult> OpenSourcePreviewAsync(
        Guid itemId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var source = await ResolveSourceAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        if (!source.Succeeded)
        {
            return GlobalColorRemovalSourceResult.Failure(source.Error!);
        }

        try
        {
            var bytes = await ReadBytesAsync(source.Asset!, cancellationToken).ConfigureAwait(false);
            return GlobalColorRemovalSourceResult.Success(assetId, source.Asset!.Name, bytes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalSourceResult.Failure($"The selected image could not be opened. {exception.Message}");
        }
    }

    public async Task<GlobalColorRemovalSampleResult> SampleColorAsync(
        Guid itemId,
        Guid assetId,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        if (x < 0 || y < 0)
        {
            return GlobalColorRemovalSampleResult.Failure("The selected image coordinate is invalid.");
        }

        var source = await ResolveSourceAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        if (!source.Succeeded)
        {
            return GlobalColorRemovalSampleResult.Failure(source.Error!);
        }

        try
        {
            await using var stream = await _fileStore.OpenReadAsync(source.Asset!.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
            var color = await _processor.SampleAsync(stream, x, y, cancellationToken).ConfigureAwait(false);
            return color is { } value
                ? GlobalColorRemovalSampleResult.Success(value)
                : GlobalColorRemovalSampleResult.Failure("Pick a visible pixel from the image.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalSampleResult.Failure($"The selected image could not be sampled. {exception.Message}");
        }
    }

    public async Task<GlobalColorRemovalPreviewResult> PreviewAsync(
        Guid itemId,
        Guid assetId,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var source = await ResolveSourceAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        if (!source.Succeeded)
        {
            return GlobalColorRemovalPreviewResult.Failure(source.Error!);
        }

        try
        {
            await using var stream = await _fileStore.OpenReadAsync(source.Asset!.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
            var preview = await _processor.PreviewAsync(stream, parameters, cancellationToken).ConfigureAwait(false);
            return GlobalColorRemovalPreviewResult.Success(preview);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalPreviewResult.Failure($"The color-removal preview failed. {exception.Message}");
        }
    }

    public async Task<GlobalColorRemovalApplyResult> ApplyAsync(
        Guid itemId,
        Guid assetId,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var source = await ResolveSourceAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        if (!source.Succeeded)
        {
            return GlobalColorRemovalApplyResult.Failure(source.Error!);
        }

        GlobalColorRemovalRasterResult raster;
        try
        {
            await using var stream = await _fileStore.OpenReadAsync(source.Asset!.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
            raster = await _processor.ApplyAsync(stream, parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalApplyResult.Failure($"The color-removal operation failed. {exception.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (raster.MatchedPixelCount == 0)
        {
            return GlobalColorRemovalApplyResult.Failure("There are no visible pixels matching the selected color and tolerance.");
        }

        if (raster.RemainingVisiblePixelCount <= 0)
        {
            return GlobalColorRemovalApplyResult.Failure("The selected color and tolerance would remove all visible artwork.");
        }

        var fileName = BuildDerivedFileName(source.Asset!.Name);
        ManagedWorkspaceFile output;
        try
        {
            await using var content = new MemoryStream(raster.Png, writable: false);
            output = await _outputStore.SaveAsync(fileName, source.Asset.Kind, content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GlobalColorRemovalApplyResult.Failure($"The transparent image could not be written. {exception.Message}");
        }

        var newAssetId = _newId();
        var now = _clock();
        var newAsset = new Asset(
            newAssetId,
            source.Item!.StoreId,
            output.Name,
            source.Asset.Description,
            source.Asset.Kind,
            output.WorkspaceRelativePath,
            source.Asset.OriginalSourcePath,
            isMissing: false,
            isArchived: false,
            now,
            now,
            JsonSerializer.Serialize(new
            {
                derivedFromAssetId = source.Asset.Id,
                operation = "global-color-removal",
                color = parameters.Color.ToHex(),
                tolerance = parameters.Tolerance
            }));
        var newLink = new AssetLink(newAssetId, WorkspaceEntityKind.Item, itemId);
        var contentRiskReview = CreateUnreviewedReview(newAssetId, raster.Png);
        var updated = source.Snapshot! with
        {
            Assets = [.. source.Snapshot.Assets, newAsset],
            AssetLinks = [.. source.Snapshot.AssetLinks, newLink],
            ContentRiskReviews = contentRiskReview is null
                ? source.Snapshot.ContentRiskReviews
                : [.. source.Snapshot.ContentRiskReviews, contentRiskReview]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var cleanup = await ManagedWorkspaceFileCleanup.TryDeleteAsync(_outputStore, output.WorkspaceRelativePath).ConfigureAwait(false);
            ManagedWorkspaceFileCleanup.PreserveDiagnostic(exception, cleanup, "Global color removal");
            return GlobalColorRemovalApplyResult.Failure(
                $"The transparent image could not be added to the workspace. {exception.Message}{ManagedWorkspaceFileCleanup.FailureMessage(cleanup)}");
        }
        catch (OperationCanceledException)
        {
            await ManagedWorkspaceFileCleanup.TryDeleteAsync(_outputStore, output.WorkspaceRelativePath).ConfigureAwait(false);
            throw;
        }

        if (_contentRiskReviews is not null && contentRiskReview is not null)
        {
            try
            {
                await _contentRiskReviews.ReviewImageAsync(
                    contentRiskReview.Target,
                    "image/png",
                    raster.Png,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The persisted Unreviewed state remains visible when advisory review cannot run.
            }
        }

        return GlobalColorRemovalApplyResult.Success(
            new GlobalColorRemovalDerivedAsset(newAssetId, newAsset.Name, newAsset.WorkspaceRelativePath),
            raster.MatchedPixelCount);
    }

    private async Task<SourceResolution> ResolveSourceAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return SourceResolution.Failure("The Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return SourceResolution.Failure(editDecision.Reason);
        }

        var asset = snapshot.Assets.SingleOrDefault(candidate => candidate.Id == assetId);
        if (asset is null
            || !snapshot.AssetLinks.Any(link => link.AssetId == assetId
                && link.EntityKind == WorkspaceEntityKind.Item
                && link.EntityId == itemId))
        {
            return SourceResolution.Failure("The selected image is not attached to this Item.");
        }

        if (asset.Kind is not (AssetKind.ExportedImage or AssetKind.ReferenceImage))
        {
            return SourceResolution.Failure("Only managed Design or supporting images can use global color removal.");
        }

        if (asset.IsMissing || !_fileStore.Exists(asset.WorkspaceRelativePath))
        {
            return SourceResolution.Failure("The selected image is missing from managed workspace storage.");
        }

        return SourceResolution.Success(snapshot, item, asset);
    }

    private async Task<byte[]> ReadBytesAsync(Asset asset, CancellationToken cancellationToken)
    {
        await using var stream = await _fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static string BuildDerivedFileName(string sourceName)
    {
        var baseName = Path.GetFileNameWithoutExtension(sourceName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "image";
        }

        return $"{baseName} - color removed.png";
    }

    private static ContentRiskReview? CreateUnreviewedReview(Guid assetId, byte[] bytes)
    {
        var target = new ContentRiskReviewTarget(assetId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");
        return ContentRiskReview.Unreviewed(target, ContentRiskFingerprint.ForImage(target, bytes));
    }

    private sealed record SourceResolution(
        bool Succeeded,
        string? Error,
        WorkspaceSnapshot? Snapshot,
        Item? Item,
        Asset? Asset)
    {
        public static SourceResolution Success(WorkspaceSnapshot snapshot, Item item, Asset asset) =>
            new(true, null, snapshot, item, asset);

        public static SourceResolution Failure(string error) =>
            new(false, string.IsNullOrWhiteSpace(error) ? "The selected image is unavailable." : error, null, null, null);
    }
}
