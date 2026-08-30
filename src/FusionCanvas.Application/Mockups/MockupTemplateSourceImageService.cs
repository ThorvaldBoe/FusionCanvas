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
        return new(summaries, resolutions, MockupTemplateSourcePolicy.IsReady(resolutions.Select(value => new MockupTemplateSourceResolution(value.VariantId, value.Kind, value.SourceImageIds))), null);
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
        var revisionNumber = template.CurrentRevision + 1;
        var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Local source image added");
        var revisionImage = new MockupTemplateRevisionSourceImage(_newId(), revision.Id, asset.Id, mapping, dimensions.Width, dimensions.Height);
        var revisionConditions = ids.Select(id => new MockupTemplateRevisionSourceImageOptionValue(revisionImage.Id, id)).ToArray();
        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, new AssetLink(asset.Id, WorkspaceEntityKind.Store, store.Id)],
            MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { CurrentRevision = revisionNumber, UpdatedAt = now } : value).ToArray(),
            MockupTemplateSourceImages = [.. snapshot.MockupTemplateSourceImages, image],
            MockupTemplateSourceImageOptionValues = [.. snapshot.MockupTemplateSourceImageOptionValues, .. conditions],
            MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
            MockupTemplateRevisionSourceImages = [.. snapshot.MockupTemplateRevisionSourceImages, revisionImage],
            MockupTemplateRevisionSourceImageOptionValues = [.. snapshot.MockupTemplateRevisionSourceImageOptionValues, .. revisionConditions]
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

        var now = _clock();
        var updatedImage = image with { ImageMapping = request.ImageMapping, IsArchived = request.Archive, UpdatedAt = now };
        var nextImages = snapshot.MockupTemplateSourceImages.Select(value => value.Id == image.Id ? updatedImage : value).ToArray();
        var nextConditions = snapshot.MockupTemplateSourceImageOptionValues
            .Where(value => value.SourceImageId != image.Id)
            .Concat(ids.Select(id => new MockupTemplateSourceImageOptionValue(image.Id, id)))
            .ToArray();
        var revisionNumber = template.CurrentRevision + 1;
        var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Local source image metadata changed");
        var revisionImages = nextImages.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived)
            .Select(value => new MockupTemplateRevisionSourceImage(_newId(), revision.Id, value.SourceAssetId, value.ImageMapping, value.ImageWidth, value.ImageHeight)).ToArray();
        var revisionConditions = revisionImages.SelectMany((value, index) =>
        {
            var current = nextImages.Where(source => source.MockupTemplateId == template.Id && !source.IsArchived).ElementAt(index);
            return nextConditions.Where(condition => condition.SourceImageId == current.Id).Select(condition => new MockupTemplateRevisionSourceImageOptionValue(value.Id, condition.OptionValueId));
        }).ToArray();
        var updated = snapshot with
        {
            MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { CurrentRevision = revisionNumber, UpdatedAt = now } : value).ToArray(),
            MockupTemplateSourceImages = nextImages,
            MockupTemplateSourceImageOptionValues = nextConditions,
            MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
            MockupTemplateRevisionSourceImages = [.. snapshot.MockupTemplateRevisionSourceImages, .. revisionImages],
            MockupTemplateRevisionSourceImageOptionValues = [.. snapshot.MockupTemplateRevisionSourceImageOptionValues, .. revisionConditions]
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
        return new(image.Id, asset.Id, asset.Name, asset.WorkspaceRelativePath, dimensions, image.ImageMapping, conditions);
    }

    private async Task<MockupTemplateSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken) =>
        await new MockupTemplateSetupService(_repository).LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(false);
}
