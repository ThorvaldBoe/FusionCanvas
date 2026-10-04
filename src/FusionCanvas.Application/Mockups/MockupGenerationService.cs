using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupGenerationService : IMockupGenerationService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileOutputStore _fileStore;
    private readonly IMockupTemplateSetupService _templates;
    private readonly IMockupRasterCompositor _compositor;
    private readonly IMockupOutputInvalidationService _invalidation;
    private readonly Func<Guid> _newId;
    private readonly Func<DateTimeOffset> _clock;

    public MockupGenerationService(IWorkspaceRepository repository, IWorkspaceFileOutputStore fileStore, IMockupTemplateSetupService templates, IMockupRasterCompositor compositor, Func<Guid>? newId = null, Func<DateTimeOffset>? clock = null, IMockupOutputInvalidationService? invalidation = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _compositor = compositor ?? throw new ArgumentNullException(nameof(compositor));
        _invalidation = invalidation ?? new MockupOutputInvalidationService(repository, fileStore);
        _newId = newId ?? Guid.NewGuid;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        if (item is null) return new(itemId, null, true, "Item was not found.", [], null, [], [], "Item was not found.", null);
        var config = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId);
        if (config is null) return new(itemId, null, isReadOnly, readOnlyReason, [], null, Outputs(snapshot, itemId), [], "Select an Offering in Design before generating mockups.", null, [], ReadNotice(item.MetadataJson));
        var eligible = await _templates.GetEligibleTemplatesAsync(item.StoreId, config.OfferingId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var colors = snapshot.DesignSelectedColors.Where(value => value.ItemId == itemId).Select(value => value.ColorValue).OrderBy(value => value).ToArray();
        var outputs = Outputs(snapshot, itemId);
        var blocked = colors.Length == 0
            ? "Select at least one product Color in Design before generating mockups."
            : eligible.Error is not null
                ? null
                : eligible.Templates.Count > 0
                    ? null
                    : eligible.CandidateDiagnostics.Count == 0
                        ? "No Mockup Templates are configured for this Offering. Add one in Store settings."
                        : "No ready Mockup Templates are available. Complete the requirements shown below in Store settings.";
        var diagnostics = eligible.Templates.Count == 0 ? eligible.CandidateDiagnostics : [];
        return new(itemId, config.OfferingId, isReadOnly, readOnlyReason, eligible.Templates, eligible.Templates.FirstOrDefault()?.Id, outputs, colors, blocked, eligible.Error, diagnostics, ReadNotice(item.MetadataJson));
    }

    public async Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == request.ItemId);
        var config = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == request.ItemId);
        var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId && !value.IsArchived);
        if (item is null || config is null || template is null || template.BlueprintOfferingId != config.OfferingId)
            return MockupGenerationResult.Failure("Select a ready Mockup Template for the Item's active Offering.");

        var eligible = await _templates.GetEligibleTemplatesAsync(item.StoreId, config.OfferingId, request.TemplateId, cancellationToken).ConfigureAwait(false);
        if (!eligible.Succeeded || eligible.Templates.Count == 0) return MockupGenerationResult.Failure(eligible.Error ?? "The selected Mockup Template is not ready.");
        var revision = snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
        if (revision is null) return MockupGenerationResult.Failure("The selected Mockup Template revision was not found.");

        var revisionImages = snapshot.MockupTemplateRevisionSourceImages.Where(value => value.RevisionId == revision.Id).ToArray();
        var conditions = snapshot.MockupTemplateRevisionSourceImageOptionValues
            .Where(value => revisionImages.Any(image => image.Id == value.RevisionSourceImageId))
            .ToLookup(value => value.RevisionSourceImageId, value => value.OptionValueId);
        var colors = snapshot.DesignSelectedColors.Where(value => value.ItemId == item.Id).Select(value => value.ColorValue).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var results = new List<MockupGenerationOutput>();
        var diagnostics = new List<MockupGenerationDiagnostic>();
        try
        {
            foreach (var color in colors)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var colorOption = snapshot.OfferingOptionValues.FirstOrDefault(value => value.OfferingId == config.OfferingId
                    && string.Equals(value.Value, color, StringComparison.OrdinalIgnoreCase)
                    && !value.IsArchived
                    && snapshot.OfferingOptions.Any(option => option.Id == value.OptionId && option.OptionKind == OptionKind.Color && !option.IsArchived));
                var compatibleVariants = colorOption is null
                    ? Array.Empty<OfferingVariant>()
                    : snapshot.OfferingVariants.Where(variant => !variant.IsArchived
                        && variant.OfferingId == config.OfferingId
                        && variant.OptionValueIds.Contains(colorOption.Id)
                        && snapshot.OfferingPlaceholders.Any(area => area.Id == template.TargetPlaceholderId && area.VariantIds.Contains(variant.Id)))
                        .ToArray();
                var sources = revisionImages.Select(image => new MockupTemplateSourceImage(
                    image.Id, template.Id, image.SourceAssetId, image.ImageMapping, false, revision.CreatedAt, revision.CreatedAt,
                    image.ImageWidth, image.ImageHeight)).ToArray();
                var sourceConditions = revisionImages.SelectMany(image => conditions[image.Id]
                    .Select(optionValueId => new MockupTemplateSourceImageOptionValue(image.Id, optionValueId))).ToArray();
                var resolutions = MockupTemplateSourcePolicy.Resolve(compatibleVariants, sources, sourceConditions,
                    snapshot.OfferingOptionValues.Where(value => value.OfferingId == config.OfferingId));
                var resolvedSourceIds = resolutions.SelectMany(value => value.SourceImageIds).Distinct().ToArray();
                if (resolutions.Count == 0 || resolutions.Any(value => value.Kind != MockupTemplateSourceResolutionKind.Resolved) || resolvedSourceIds.Length != 1)
                {
                    var message = resolvedSourceIds.Length > 1 || resolutions.Any(value => value.Kind == MockupTemplateSourceResolutionKind.Ambiguous)
                        ? "Multiple template source images match this Color; refine applicability so generation has one deterministic source."
                        : "No template source image is configured for this Color and its compatible Variants.";
                    diagnostics.Add(new(color, message));
                    continue;
                }
                var source = revisionImages.Single(image => image.Id == resolvedSourceIds[0]);
                var design = FindDesignAsset(snapshot, item.Id, color, template.TargetPlaceholderId);
                if (source is null) { diagnostics.Add(new(color, "No template source image is configured for this Color.")); continue; }
                if (design is null) { diagnostics.Add(new(color, "No Design PNG is assigned for this Color and Design Area.")); continue; }
                if (source.ImageMapping is null) { diagnostics.Add(new(color, "The template source image has no valid placement mapping.")); continue; }

                Asset? sourceAsset = snapshot.Assets.SingleOrDefault(value => value.Id == source.SourceAssetId);
                Asset? designAsset = snapshot.Assets.SingleOrDefault(value => value.Id == design.Value);
                if (sourceAsset is null || designAsset is null) { diagnostics.Add(new(color, "A source file record is missing.")); continue; }
                ManagedWorkspaceFile? managed = null;
                try
                {
                    await using var templateStream = await _fileStore.OpenReadAsync(sourceAsset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
                    await using var designStream = await _fileStore.OpenReadAsync(designAsset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
                    await using var output = await _compositor.ComposeAsync(templateStream, designStream, source.ImageMapping, cancellationToken).ConfigureAwait(false);
                    managed = await _fileStore.SaveAsync($"{SafeFileNamePart(item.Name)}-{SafeFileNamePart(color)}-mockup.png", AssetKind.MockupImage, output, cancellationToken).ConfigureAwait(false);
                    var now = _clock();
                    var assetId = _newId();
                    var asset = new Asset(assetId, item.StoreId, managed.Name, null, AssetKind.MockupImage, managed.WorkspaceRelativePath, null, false, false, now, now,
                        JsonSerializer.Serialize(new { itemId = item.Id, color, templateId = template.Id, templateRevision = revision.RevisionNumber, designAssetId = designAsset.Id }));
                    var updated = snapshot with { Assets = [.. snapshot.Assets, asset], AssetLinks = [.. snapshot.AssetLinks, new AssetLink(asset.Id, WorkspaceEntityKind.Item, item.Id)] };
                    if (results.Count == 0)
                    {
                        updated = ClearNotice(updated, item.Id);
                    }
                    await _repository.SaveAsync(updated, CancellationToken.None).ConfigureAwait(false);
                    snapshot = updated;
                    results.Add(new(asset.Id, asset.Name, asset.WorkspaceRelativePath, color, template.Id, revision.RevisionNumber, designAsset.Id));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    if (managed is not null) _fileStore.TryDelete(managed.WorkspaceRelativePath);
                    diagnostics.Add(new(color, exception.Message));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var message = results.Count == 0
                ? "Mockup generation was cancelled."
                : "Mockup generation was cancelled; previously persisted mockups were retained.";
            return new(results.Count > 0, message, results, diagnostics);
        }
        return new(results.Count > 0, diagnostics.Count == 0 ? null : "Some mockups could not be generated.", results, diagnostics);
    }

    private static Guid? FindDesignAsset(WorkspaceSnapshot snapshot, Guid itemId, string color, Guid? targetAreaId)
    {
        var rows = snapshot.DesignVariantRows.Where(value => value.ItemId == itemId).Join(snapshot.DesignVariantRowColors, row => row.Id, colorRow => colorRow.RowId, (row, colorRow) => new { row, colorRow })
            .Where(value => string.Equals(value.colorRow.ColorValue, color, StringComparison.OrdinalIgnoreCase)).OrderBy(value => value.row.IsDefault ? 0 : 1).ThenBy(value => value.row.SortOrder).ToArray();
        foreach (var row in rows)
        {
            var assignment = snapshot.DesignSlotAssignments.FirstOrDefault(value => value.RowId == row.row.Id && (targetAreaId is null || value.DesignAreaId == targetAreaId) && value.AssetId is not null);
            if (assignment?.AssetId is not null && snapshot.Assets.Any(value => value.Id == assignment.AssetId && value.Kind == AssetKind.ExportedImage)) return assignment.AssetId;
        }
        return null;
    }

    private static string SafeFileNamePart(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars()
            .Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*'])
            .ToHashSet();
        var sanitized = new string(value.Select(character =>
            invalidCharacters.Contains(character) || char.IsControl(character) ? '-' : character).ToArray()).TrimEnd(' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "untitled" : sanitized;
    }

    public async Task<Stream> OpenPreviewAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await ResolveOutputAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
        return await _fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MockupOutputOperationResult> ExportCopyAsync(Guid itemId, Guid assetId, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return MockupOutputOperationResult.Failure("Choose a destination for the mockup copy.");
        }

        try
        {
            var asset = await ResolveOutputAsync(itemId, assetId, cancellationToken).ConfigureAwait(false);
            await _fileStore.ExportCopyAsync(asset.WorkspaceRelativePath, destinationPath, cancellationToken).ConfigureAwait(false);
            return MockupOutputOperationResult.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MockupOutputOperationResult.Failure($"The mockup could not be saved. {exception.Message}");
        }
    }

    public async Task<MockupOutputOperationResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        if (item is null)
        {
            return MockupOutputOperationResult.Failure("The Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.RelatedAssetLink);
        if (!editDecision.IsAllowed)
        {
            return MockupOutputOperationResult.Failure(editDecision.Reason);
        }

        var asset = snapshot.Assets.SingleOrDefault(value => value.Id == assetId && value.Kind == AssetKind.MockupImage);
        var linked = snapshot.AssetLinks.Any(value => value.AssetId == assetId && value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == itemId);
        if (asset is null || !linked)
        {
            return MockupOutputOperationResult.Failure("The generated mockup was not found for this Item.");
        }

        var updated = snapshot with
        {
            Assets = snapshot.Assets.Where(value => value.Id != assetId).ToArray(),
            AssetLinks = snapshot.AssetLinks.Where(value => value.AssetId != assetId).ToArray()
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MockupOutputOperationResult.Failure($"The mockup could not be removed. {exception.Message}");
        }

        var cleanup = await ManagedWorkspaceFileCleanup.TryDeleteAsync(_fileStore, asset.WorkspaceRelativePath).ConfigureAwait(false);
        return cleanup.Status is ManagedWorkspaceFileCleanup.Status.Failed or ManagedWorkspaceFileCleanup.Status.Uninspectable
            ? MockupOutputOperationResult.Failure($"The mockup record was removed, but its managed file could not be removed.")
            : MockupOutputOperationResult.Success();
    }

    public Task<MockupOutputInvalidationResult> InvalidateForDesignChangeAsync(Guid itemId, IReadOnlySet<Guid>? sourceDesignAssetIds = null, bool invalidateAll = false, CancellationToken cancellationToken = default) =>
        _invalidation.InvalidateAsync(itemId, sourceDesignAssetIds, invalidateAll, cancellationToken);

    private async Task<Asset> ResolveOutputAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var asset = snapshot.Assets.SingleOrDefault(value => value.Id == assetId && value.Kind == AssetKind.MockupImage);
        if (asset is null || !snapshot.AssetLinks.Any(value => value.AssetId == assetId && value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == itemId))
        {
            throw new InvalidOperationException("The generated mockup was not found for this Item.");
        }

        return asset;
    }

    private IReadOnlyList<MockupGenerationOutput> Outputs(WorkspaceSnapshot snapshot, Guid itemId) => snapshot.AssetLinks
        .Where(value => value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == itemId)
        .Join(snapshot.Assets, link => link.AssetId, asset => asset.Id, (_, asset) => asset)
        .Where(value => value.Kind == AssetKind.MockupImage)
        .Select(asset => ToOutput(snapshot, asset))
        .ToArray();

    private MockupGenerationOutput ToOutput(WorkspaceSnapshot snapshot, Asset asset)
    {
        var metadata = ParseOutputMetadata(asset.MetadataJson);
        var templateName = metadata.TemplateId is Guid templateId
            ? snapshot.MockupTemplates.SingleOrDefault(value => value.Id == templateId)?.Name
            : null;
        return new(asset.Id, asset.Name, asset.WorkspaceRelativePath, metadata.ColorValue ?? string.Empty,
            metadata.TemplateId ?? Guid.Empty, metadata.TemplateRevision, metadata.DesignAssetId ?? Guid.Empty,
            false, templateName);
    }

    private static (string? ColorValue, Guid? TemplateId, int TemplateRevision, Guid? DesignAssetId) ParseOutputMetadata(string metadataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var root = document.RootElement;
            return (
                root.TryGetProperty("color", out var color) ? color.GetString() : null,
                TryReadGuid(root, "templateId"),
                root.TryGetProperty("templateRevision", out var revision) && revision.TryGetInt32(out var revisionNumber) ? revisionNumber : 0,
                TryReadGuid(root, "designAssetId"));
        }
        catch (JsonException)
        {
            return (null, null, 0, null);
        }
    }

    private static Guid? TryReadGuid(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var id)
            ? id
            : null;

    private static string? ReadNotice(string? metadataJson)
    {
        var metadata = ItemMetadataCodec.ParseMetadata(metadataJson ?? "{}");
        return metadata.GetValueOrDefault(ItemMetadataCodec.MockupInvalidationNoticeKey);
    }

    private static WorkspaceSnapshot ClearNotice(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        if (item is null)
        {
            return snapshot;
        }

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        if (!metadata.Remove(ItemMetadataCodec.MockupInvalidationNoticeKey))
        {
            return snapshot;
        }

        var updatedItem = item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata) };
        return snapshot with { Items = [.. snapshot.Items.Where(value => value.Id != itemId), updatedItem] };
    }
}
