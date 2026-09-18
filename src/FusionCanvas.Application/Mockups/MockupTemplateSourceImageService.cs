using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupTemplateSourceImageService : IMockupTemplateSourceImageService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly IRasterImageMetadataReader _metadata;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public MockupTemplateSourceImageService(IWorkspaceRepository repository, IWorkspaceFileStore fileStore, IRasterImageMetadataReader metadata, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == templateId && !value.IsArchived);
        if (template is null) return new([], [], false, "Mockup Template was not found.");
        var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == storeId && !value.IsArchived);
        if (offering is null) return new([], [], false, "Mockup Template does not belong to the selected Store.");
        var images = snapshot.MockupTemplateSourceImages.Where(value => value.MockupTemplateId == templateId && !value.IsArchived).ToArray();
        var summaries = images.Select(image => ToSummary(snapshot, image)).ToArray();
        var variants = snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id && !value.IsArchived && snapshot.OfferingPlaceholders.Any(area => area.Id == template.TargetPlaceholderId && area.VariantIds.Contains(value.Id))).ToArray();
        var resolutions = MockupTemplateSourcePolicy.Resolve(variants, images, snapshot.MockupTemplateSourceImageOptionValues, snapshot.OfferingOptionValues)
            .Select(value => new MockupTemplateSourceReadiness(value.VariantId, value.Kind, value.SourceImageIds)).ToArray();
        return new(summaries, resolutions, MockupTemplateReadinessEvaluator.Evaluate(snapshot, template).IsReadyForUse, null);
    }

    public async Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == request.StoreId);
        var offering = template is null ? null : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == request.StoreId);
        if (store is null || template is null || offering is null) return MockupTemplateSetupResult.Failure("Mockup Template was not found in the selected Store.", new(request.StoreId, false, [], [], []));
        if (store.IsArchived || template.IsArchived || offering.IsArchived) return MockupTemplateSetupResult.Failure("Archived catalog records are read-only.", new(request.StoreId, true, [], [], []));
        var ids = request.OptionValueIds?.Distinct().ToArray() ?? [];
        if (ids.Any(id => snapshot.OfferingOptionValues.All(value => value.Id != id || value.OfferingId != offering.Id || value.IsArchived)))
            return MockupTemplateSetupResult.Failure("Select active Option Values from this Offering.", await LoadForStoreAsync(request.StoreId, cancellationToken));

        RasterImageInfo dimensions;
        try { dimensions = await _metadata.ReadAsync(request.SourcePath, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return MockupTemplateSetupResult.Failure($"The source image could not be read. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }
        var mapping = request.ImageMapping;
        if (mapping is not null && (mapping.ImageWidth != dimensions.Width || mapping.ImageHeight != dimensions.Height))
            return MockupTemplateSetupResult.Failure("The mapping dimensions must match the source image.", await LoadForStoreAsync(request.StoreId, cancellationToken));

        ManagedWorkspaceFile managed;
        try { managed = await _fileStore.ImportAsync(request.SourcePath, AssetKind.MockupImage, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return MockupTemplateSetupResult.Failure($"The source image could not be imported. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }

        var now = _clock();
        var asset = new Asset(_newId(), store.Id, managed.Name, null, AssetKind.MockupImage, managed.WorkspaceRelativePath, managed.OriginalSourcePath, false, false, now, now, "{}");
        var image = new MockupTemplateSourceImage(_newId(), template.Id, asset.Id, mapping, false, now, now, dimensions.Width, dimensions.Height);
        var conditions = ids.Select(id => new MockupTemplateSourceImageOptionValue(image.Id, id)).ToArray();
        var nextImages = snapshot.MockupTemplateSourceImages.Append(image).ToArray();
        var nextConditions = snapshot.MockupTemplateSourceImageOptionValues.Concat(conditions).ToArray();
        var revisionNumber = template.CurrentRevision + 1;
        var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Local source image added");
        var revisionData = SnapshotActiveSourceImages(nextImages, nextConditions, template.Id, revision.Id);
        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, new AssetLink(asset.Id, WorkspaceEntityKind.Store, store.Id)],
            MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { CurrentRevision = revisionNumber, UpdatedAt = now } : value).ToArray(),
            MockupTemplateSourceImages = nextImages,
            MockupTemplateSourceImageOptionValues = nextConditions,
            MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
            MockupTemplateRevisionSourceImages = [.. snapshot.MockupTemplateRevisionSourceImages, .. revisionData.Images],
            MockupTemplateRevisionSourceImageOptionValues = [.. snapshot.MockupTemplateRevisionSourceImageOptionValues, .. revisionData.Conditions]
        };
        try { await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { _fileStore.TryDelete(managed.WorkspaceRelativePath); return MockupTemplateSetupResult.Failure($"The source image could not be saved. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }
        return MockupTemplateSetupResult.Success(await LoadForStoreAsync(request.StoreId, cancellationToken));
    }

    public async Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == request.StoreId);
        var offering = template is null ? null : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == request.StoreId);
        var image = snapshot.MockupTemplateSourceImages.SingleOrDefault(value => value.Id == request.SourceImageId && value.MockupTemplateId == request.TemplateId);
        if (store is null || template is null || offering is null || image is null) return MockupTemplateSetupResult.Failure("Mockup source image was not found in the selected Store.", await LoadForStoreAsync(request.StoreId, cancellationToken));
        if (store.IsArchived || template.IsArchived || offering.IsArchived) return MockupTemplateSetupResult.Failure("Archived catalog records are read-only.", await LoadForStoreAsync(request.StoreId, cancellationToken));
        var ids = request.OptionValueIds?.Distinct().ToArray() ?? [];
        if (ids.Any(id => snapshot.OfferingOptionValues.All(value => value.Id != id || value.OfferingId != offering.Id || value.IsArchived)))
            return MockupTemplateSetupResult.Failure("Select active Option Values from this Offering.", await LoadForStoreAsync(request.StoreId, cancellationToken));
        if (request.ImageMapping is { } mapping && (mapping.ImageWidth != image.ImageWidth || mapping.ImageHeight != image.ImageHeight))
            return MockupTemplateSetupResult.Failure("The mapping dimensions must match the source image.", await LoadForStoreAsync(request.StoreId, cancellationToken));

        var existingIds = snapshot.MockupTemplateSourceImageOptionValues
            .Where(value => value.SourceImageId == image.Id)
            .Select(value => value.OptionValueId)
            .ToHashSet();
        var currentRevision = snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
        var currentRevisionComplete = currentRevision is not null && RevisionMatchesActiveSources(snapshot, currentRevision, template.Id);
        if (image.ImageMapping == request.ImageMapping && image.IsArchived == request.Archive && existingIds.SetEquals(ids) && currentRevisionComplete)
            return MockupTemplateSetupResult.Success(await LoadForStoreAsync(request.StoreId, cancellationToken));

        var now = _clock();
        var updatedImage = image with { ImageMapping = request.ImageMapping, IsArchived = request.Archive, UpdatedAt = now };
        var nextImages = snapshot.MockupTemplateSourceImages.Select(value => value.Id == image.Id ? updatedImage : value).ToArray();
        var nextConditions = snapshot.MockupTemplateSourceImageOptionValues
            .Where(value => value.SourceImageId != image.Id)
            .Concat(ids.Select(id => new MockupTemplateSourceImageOptionValue(image.Id, id)))
            .ToArray();
        var revisionNumber = template.CurrentRevision + 1;
        var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Local source image metadata changed");
        var revisionData = SnapshotActiveSourceImages(nextImages, nextConditions, template.Id, revision.Id);
        var updated = snapshot with
        {
            MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { CurrentRevision = revisionNumber, UpdatedAt = now } : value).ToArray(),
            MockupTemplateSourceImages = nextImages,
            MockupTemplateSourceImageOptionValues = nextConditions,
            MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
            MockupTemplateRevisionSourceImages = [.. snapshot.MockupTemplateRevisionSourceImages, .. revisionData.Images],
            MockupTemplateRevisionSourceImageOptionValues = [.. snapshot.MockupTemplateRevisionSourceImageOptionValues, .. revisionData.Conditions]
        };
        try { await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return MockupTemplateSetupResult.Failure($"The source image could not be saved. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }
        return MockupTemplateSetupResult.Success(await LoadForStoreAsync(request.StoreId, cancellationToken));
    }

    private MockupTemplateSourceImageSummary ToSummary(WorkspaceSnapshot snapshot, MockupTemplateSourceImage image)
    {
        var asset = snapshot.Assets.Single(value => value.Id == image.SourceAssetId);
        var conditions = snapshot.MockupTemplateSourceImageOptionValues.Where(value => value.SourceImageId == image.Id).Select(value => value.OptionValueId).ToArray();
        var dimensions = new RasterImageInfo(image.ImageWidth, image.ImageHeight);
        return new(image.Id, asset.Id, asset.Name, asset.WorkspaceRelativePath, dimensions, image.ImageMapping, conditions, Path.Combine(_fileStore.WorkspaceRoot, asset.WorkspaceRelativePath));
    }

    private (MockupTemplateRevisionSourceImage[] Images, MockupTemplateRevisionSourceImageOptionValue[] Conditions) SnapshotActiveSourceImages(
        IReadOnlyList<MockupTemplateSourceImage> images,
        IReadOnlyList<MockupTemplateSourceImageOptionValue> conditions,
        Guid templateId,
        Guid revisionId)
    {
        var active = images.Where(value => value.MockupTemplateId == templateId && !value.IsArchived).ToArray();
        var revisionImages = active.ToDictionary(value => value.Id, value =>
            new MockupTemplateRevisionSourceImage(_newId(), revisionId, value.SourceAssetId, value.ImageMapping, value.ImageWidth, value.ImageHeight));
        var revisionConditions = active.SelectMany(value => conditions
            .Where(condition => condition.SourceImageId == value.Id)
            .Select(condition => new MockupTemplateRevisionSourceImageOptionValue(revisionImages[value.Id].Id, condition.OptionValueId)))
            .ToArray();
        return (revisionImages.Values.ToArray(), revisionConditions);
    }

    private static bool RevisionMatchesActiveSources(WorkspaceSnapshot snapshot, MockupTemplateRevision revision, Guid templateId)
    {
        var active = snapshot.MockupTemplateSourceImages.Where(value => value.MockupTemplateId == templateId && !value.IsArchived).ToArray();
        var revisionImages = snapshot.MockupTemplateRevisionSourceImages.Where(value => value.RevisionId == revision.Id).ToArray();
        if (active.Length != revisionImages.Length) return false;
        foreach (var image in active)
        {
            var revisionImage = revisionImages.SingleOrDefault(value => value.SourceAssetId == image.SourceAssetId
                && value.ImageMapping == image.ImageMapping
                && value.ImageWidth == image.ImageWidth
                && value.ImageHeight == image.ImageHeight);
            if (revisionImage is null) return false;
            var expected = snapshot.MockupTemplateSourceImageOptionValues.Where(value => value.SourceImageId == image.Id).Select(value => value.OptionValueId).ToHashSet();
            var actual = snapshot.MockupTemplateRevisionSourceImageOptionValues.Where(value => value.RevisionSourceImageId == revisionImage.Id).Select(value => value.OptionValueId).ToHashSet();
            if (!expected.SetEquals(actual)) return false;
        }
        return true;
    }

    private async Task<MockupTemplateSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken) =>
        await new MockupTemplateSetupService(_repository).LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(false);
}
