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
        var resolutions = MockupTemplateSourcePolicy.Resolve(variants, images, snapshot.MockupTemplateSourceImageOptionValues)
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
        if (ids.Length == 0 || ids.Any(id => snapshot.OfferingOptionValues.All(value => value.Id != id || value.OfferingId != offering.Id || value.IsArchived)))
            return MockupTemplateSetupResult.Failure("Select at least one active Option Value from this Offering.", await LoadForStoreAsync(request.StoreId, cancellationToken));

        RasterImageInfo dimensions;
        try { dimensions = await _metadata.ReadAsync(request.SourcePath, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return MockupTemplateSetupResult.Failure($"The source image could not be read. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }
        var mapping = request.ImageMapping ?? new MockupImageSpaceMapping(dimensions.Width, dimensions.Height, 0, 0, dimensions.Width, dimensions.Height);
        if (mapping.ImageWidth != dimensions.Width || mapping.ImageHeight != dimensions.Height)
            return MockupTemplateSetupResult.Failure("The mapping dimensions must match the source image.", await LoadForStoreAsync(request.StoreId, cancellationToken));

        ManagedWorkspaceFile managed;
        try { managed = await _fileStore.ImportAsync(request.SourcePath, AssetKind.MockupImage, cancellationToken).ConfigureAwait(false); }
        catch (Exception exception) when (exception is not OperationCanceledException) { return MockupTemplateSetupResult.Failure($"The source image could not be imported. {exception.Message}", await LoadForStoreAsync(request.StoreId, cancellationToken)); }

        var now = _clock();
        var asset = new Asset(_newId(), store.Id, managed.Name, null, AssetKind.MockupImage, managed.WorkspaceRelativePath, managed.OriginalSourcePath, false, false, now, now, "{}");
        var image = new MockupTemplateSourceImage(_newId(), template.Id, asset.Id, mapping, false, now, now);
        var conditions = ids.Select(id => new MockupTemplateSourceImageOptionValue(image.Id, id)).ToArray();
        var revisionNumber = template.CurrentRevision + 1;
        var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Local source image added");
        var revisionImage = new MockupTemplateRevisionSourceImage(_newId(), revision.Id, asset.Id, mapping);
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

    private MockupTemplateSourceImageSummary ToSummary(WorkspaceSnapshot snapshot, MockupTemplateSourceImage image)
    {
        var asset = snapshot.Assets.Single(value => value.Id == image.SourceAssetId);
        var conditions = snapshot.MockupTemplateSourceImageOptionValues.Where(value => value.SourceImageId == image.Id).Select(value => value.OptionValueId).ToArray();
        var dimensions = new RasterImageInfo(image.ImageMapping.ImageWidth, image.ImageMapping.ImageHeight);
        return new(image.Id, asset.Id, asset.Name, asset.WorkspaceRelativePath, dimensions, image.ImageMapping, conditions);
    }

    private async Task<MockupTemplateSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken) =>
        await new MockupTemplateSetupService(_repository).LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(false);
}
