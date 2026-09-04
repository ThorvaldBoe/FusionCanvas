using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupGenerationService : IMockupGenerationService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly IMockupTemplateSetupService _templates;
    private readonly IMockupRasterCompositor _compositor;
    private readonly Func<Guid> _newId;
    private readonly Func<DateTimeOffset> _clock;

    public MockupGenerationService(IWorkspaceRepository repository, IWorkspaceFileStore fileStore, IMockupTemplateSetupService templates, IMockupRasterCompositor compositor, Func<Guid>? newId = null, Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _compositor = compositor ?? throw new ArgumentNullException(nameof(compositor));
        _newId = newId ?? Guid.NewGuid;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        if (item is null) return new(itemId, null, true, "Item was not found.", [], null, [], [], "Item was not found.", null);
        var config = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId);
        if (config is null) return new(itemId, null, isReadOnly, readOnlyReason, [], null, Outputs(snapshot, itemId), [], "Select an Offering in Design before generating mockups.", null);
        var eligible = await _templates.GetEligibleTemplatesAsync(item.StoreId, config.OfferingId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var colors = snapshot.DesignSelectedColors.Where(value => value.ItemId == itemId).Select(value => value.ColorValue).OrderBy(value => value).ToArray();
        var outputs = Outputs(snapshot, itemId);
        var blocked = colors.Length == 0 ? "Select at least one product Color in Design before generating mockups." : eligible.Templates.Count == 0 ? "Configure and complete a Mockup Template for this Offering before generating mockups." : null;
        return new(itemId, config.OfferingId, isReadOnly, readOnlyReason, eligible.Templates, eligible.Templates.FirstOrDefault()?.Id, outputs, colors, blocked, eligible.Error);
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
        foreach (var color in colors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var colorOption = snapshot.OfferingOptionValues.FirstOrDefault(value => value.OfferingId == config.OfferingId && string.Equals(value.Value, color, StringComparison.OrdinalIgnoreCase) && !value.IsArchived);
            var source = colorOption is null ? null : revisionImages.FirstOrDefault(image => conditions[image.Id].Contains(colorOption.Id));
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
                managed = await _fileStore.SaveAsync($"{item.Name}-{color}-mockup.png", AssetKind.MockupImage, output, cancellationToken).ConfigureAwait(false);
                var now = _clock();
                var assetId = _newId();
                var asset = new Asset(assetId, item.StoreId, managed.Name, null, AssetKind.MockupImage, managed.WorkspaceRelativePath, null, false, false, now, now,
                    JsonSerializer.Serialize(new { itemId = item.Id, color, templateId = template.Id, templateRevision = revision.RevisionNumber, designAssetId = designAsset.Id }));
                var updated = snapshot with { Assets = [.. snapshot.Assets, asset], AssetLinks = [.. snapshot.AssetLinks, new AssetLink(asset.Id, WorkspaceEntityKind.Item, item.Id)] };
                await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
                snapshot = updated;
                results.Add(new(asset.Id, asset.Name, asset.WorkspaceRelativePath, color, template.Id, revision.RevisionNumber, designAsset.Id));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (managed is not null) _fileStore.TryDelete(managed.WorkspaceRelativePath);
                diagnostics.Add(new(color, exception.Message));
            }
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

    private static IReadOnlyList<MockupGenerationOutput> Outputs(WorkspaceSnapshot snapshot, Guid itemId) => snapshot.AssetLinks.Where(value => value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == itemId).Join(snapshot.Assets, link => link.AssetId, asset => asset.Id, (_, asset) => asset).Where(value => value.Kind == AssetKind.MockupImage).Select(value => new MockupGenerationOutput(value.Id, value.Name, value.WorkspaceRelativePath, "", Guid.Empty, 0, Guid.Empty)).ToArray();
}
