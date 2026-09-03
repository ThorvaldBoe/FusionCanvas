using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupTemplateSetupService : IMockupTemplateSetupService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public MockupTemplateSetupService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<MockupTemplateSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public Task<MockupTemplateSetupResult> CreateTemplateAsync(CreateMockupTemplateRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == request.OfferingId && value.StoreId == request.StoreId);
            if (offering is null) return Failure(snapshot, request.StoreId, "Blueprint Offering was not found in the selected Store.");
            if (offering.IsArchived) return Failure(snapshot, request.StoreId, "Archived catalog records are read-only.");
            OfferingPlaceholder? placeholder = null;
            if (request.TargetPlaceholderId is not null)
            {
                placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == request.TargetPlaceholderId && value.OfferingId == request.OfferingId && !value.IsArchived);
                if (placeholder is null) return Failure(snapshot, request.StoreId, "Target Placeholder must be active and belong to the selected Blueprint Offering.");
            }
            var colorIds = request.ColorOptionValueIds?.Distinct().ToArray() ?? [];
            var colorError = ValidateColors(snapshot, offering.Id, colorIds);
            if (colorError is not null) return Failure(snapshot, request.StoreId, colorError);
            var compatibilityError = ValidateCompatibility(snapshot, offering.Id, placeholder, colorIds);
            if (compatibilityError is not null) return Failure(snapshot, request.StoreId, compatibilityError);
            var now = _clock();
            MockupTemplate template;
            MockupTemplateRevision revision;
            try
            {
                template = new MockupTemplate(_newId(), offering.Id, placeholder?.Id, request.Name, request.Description, 1, false, now, now, request.PositionKey, null);
                revision = new MockupTemplateRevision(_newId(), template.Id, 1, placeholder?.Id, now, "Initial template configuration", request.ProviderMockupReference, request.ImageMapping);
            }
            catch (ArgumentException exception)
            {
                return Failure(snapshot, request.StoreId, exception.Message);
            }
            var bindings = colorIds.Select(id => new MockupTemplateColorVariant(_newId(), template.Id, id, false, now, now)).ToArray();
            var revisionColors = colorIds.Select(id => new MockupTemplateRevisionColor(_newId(), revision.Id, id)).ToArray();
            var updated = snapshot with
            {
                MockupTemplates = [.. snapshot.MockupTemplates, template],
                MockupTemplateColorVariants = [.. snapshot.MockupTemplateColorVariants, .. bindings],
                MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
                MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors]
            };
            return Success(updated, request.StoreId, template.Id);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> DuplicateTemplateAsync(DuplicateMockupTemplateRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);

            var source = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId && !value.IsArchived);
            var offering = source is null
                ? null
                : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == source.BlueprintOfferingId && value.StoreId == request.StoreId && !value.IsArchived);
            if (source is null || offering is null)
                return Failure(snapshot, request.StoreId, "Mockup Template was not found in the selected Store.");

            var currentRevision = CurrentRevision(snapshot, source);
            var activeColors = snapshot.MockupTemplateColorVariants
                .Where(value => value.MockupTemplateId == source.Id && !value.IsArchived)
                .ToArray();
            var activeImages = snapshot.MockupTemplateSourceImages
                .Where(value => value.MockupTemplateId == source.Id && !value.IsArchived)
                .ToArray();
            var sourceConditions = snapshot.MockupTemplateSourceImageOptionValues
                .Where(value => activeImages.Any(image => image.Id == value.SourceImageId))
                .ToArray();
            var now = _clock();
            var duplicate = new MockupTemplate(
                _newId(),
                source.BlueprintOfferingId,
                source.TargetPlaceholderId,
                NextCopyName(snapshot, source),
                source.Description,
                1,
                false,
                now,
                now,
                source.PositionKey,
                source.FutureAssetState,
                source.MetadataJson);
            var revision = new MockupTemplateRevision(
                _newId(),
                duplicate.Id,
                1,
                source.TargetPlaceholderId,
                now,
                "Duplicated template configuration",
                currentRevision?.ProviderMockupReference,
                currentRevision?.ImageMapping);
            var colors = activeColors
                .Select(value => new MockupTemplateColorVariant(_newId(), duplicate.Id, value.ColorOptionValueId, false, now, now, value.SourceAssetId))
                .ToArray();
            var revisionColors = colors
                .Select(value => new MockupTemplateRevisionColor(_newId(), revision.Id, value.ColorOptionValueId, value.SourceAssetId))
                .ToArray();
            var imageIdMap = activeImages.ToDictionary(value => value.Id, _ => _newId());
            var images = activeImages
                .Select(value => new MockupTemplateSourceImage(
                    imageIdMap[value.Id], duplicate.Id, value.SourceAssetId, value.ImageMapping, false, now, now, value.ImageWidth, value.ImageHeight))
                .ToArray();
            var imageConditions = sourceConditions
                .Select(value => new MockupTemplateSourceImageOptionValue(imageIdMap[value.SourceImageId], value.OptionValueId))
                .ToArray();
            var revisionImageMap = images.ToDictionary(value => value.Id, _ => _newId());
            var revisionImages = images
                .Select(value => new MockupTemplateRevisionSourceImage(
                    revisionImageMap[value.Id], revision.Id, value.SourceAssetId, value.ImageMapping, value.ImageWidth, value.ImageHeight))
                .ToArray();
            var revisionImageConditions = imageConditions
                .Select(value => new MockupTemplateRevisionSourceImageOptionValue(revisionImageMap[value.SourceImageId], value.OptionValueId))
                .ToArray();
            var updated = snapshot with
            {
                MockupTemplates = [.. snapshot.MockupTemplates, duplicate],
                MockupTemplateColorVariants = [.. snapshot.MockupTemplateColorVariants, .. colors],
                MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
                MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors],
                MockupTemplateSourceImages = [.. snapshot.MockupTemplateSourceImages, .. images],
                MockupTemplateSourceImageOptionValues = [.. snapshot.MockupTemplateSourceImageOptionValues, .. imageConditions],
                MockupTemplateRevisionSourceImages = [.. snapshot.MockupTemplateRevisionSourceImages, .. revisionImages],
                MockupTemplateRevisionSourceImageOptionValues = [.. snapshot.MockupTemplateRevisionSourceImageOptionValues, .. revisionImageConditions]
            };
            return Success(updated, request.StoreId, duplicate.Id);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> AddColorAsync(AddMockupTemplateColorRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
            var value = snapshot.OfferingOptionValues.SingleOrDefault(candidate => candidate.Id == request.ColorOptionValueId);
            var option = value is null ? null : snapshot.OfferingOptions.SingleOrDefault(candidate => candidate.Id == value.OptionId);
            if (template is null || value is null || option is null || value.OfferingId != template.BlueprintOfferingId || option.OptionKind != OptionKind.Color)
                return Failure(snapshot, request.StoreId, "Template colors must reference a Color Option Value from the same offering.");
            if (template.IsArchived || value.IsArchived || option.IsArchived)
                return Failure(snapshot, request.StoreId, "Archived catalog records are read-only.");
            if (snapshot.MockupTemplateColorVariants.Any(candidate => candidate.MockupTemplateId == template.Id && candidate.ColorOptionValueId == value.Id && !candidate.IsArchived))
                return Failure(snapshot, request.StoreId, "This Color already has an active template binding.");
            var now = _clock();
            var binding = new MockupTemplateColorVariant(_newId(), template.Id, value.Id, false, now, now);
            var nextColors = snapshot.MockupTemplateColorVariants.Where(candidate => candidate.MockupTemplateId == template.Id && !candidate.IsArchived).Select(candidate => candidate.ColorOptionValueId).Append(value.Id).ToHashSet();
            var revisionNumber = template.CurrentRevision + 1;
            var previousRevision = CurrentRevision(snapshot, template);
            var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Color configuration changed", previousRevision?.ProviderMockupReference, previousRevision?.ImageMapping);
            var revisionColors = nextColors.Select(colorId => new MockupTemplateRevisionColor(_newId(), revision.Id, colorId)).ToArray();
            var updatedTemplate = template with { CurrentRevision = revisionNumber, UpdatedAt = now };
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(candidate => candidate.Id == template.Id ? updatedTemplate : candidate).ToArray(),
                MockupTemplateColorVariants = [.. snapshot.MockupTemplateColorVariants, binding],
                MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
                MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors]
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> ArchiveColorAsync(ArchiveMockupTemplateColorRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var binding = snapshot.MockupTemplateColorVariants.SingleOrDefault(value => value.Id == request.TemplateColorId);
            if (binding is null) return Failure(snapshot, request.StoreId, "Template color was not found.");
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == binding.MockupTemplateId);
            var offering = template is null ? null : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == request.StoreId);
            if (template is null || offering is null)
                return Failure(snapshot, request.StoreId, "Template does not belong to the selected Store.");
            if (binding.IsArchived) return Failure(snapshot, request.StoreId, "Template color is already archived.");
            var now = _clock();
            var revisionNumber = template.CurrentRevision + 1;
            var previousRevision = CurrentRevision(snapshot, template);
            var remainingColors = snapshot.MockupTemplateColorVariants
                .Where(value => value.MockupTemplateId == template.Id && value.Id != binding.Id && !value.IsArchived)
                .Select(value => value.ColorOptionValueId)
                .ToArray();
            var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Color configuration changed", previousRevision?.ProviderMockupReference, previousRevision?.ImageMapping);
            var revisionColors = remainingColors.Select(colorId => new MockupTemplateRevisionColor(_newId(), revision.Id, colorId)).ToArray();
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { CurrentRevision = revisionNumber, UpdatedAt = now } : value).ToArray(),
                MockupTemplateColorVariants = snapshot.MockupTemplateColorVariants.Select(value => value.Id == binding.Id ? value with { IsArchived = true, UpdatedAt = now } : value).ToArray(),
                MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
                MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors]
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> ArchiveTemplateAsync(ArchiveMockupTemplateRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
            var offering = template is null ? null : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == request.StoreId);
            if (template is null || offering is null) return Failure(snapshot, request.StoreId, "Mockup Template was not found in the selected Store.");
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { IsArchived = true, UpdatedAt = _clock() } : value).ToArray(),
                MockupTemplateColorVariants = snapshot.MockupTemplateColorVariants.Select(value => value.MockupTemplateId == template.Id ? value with { IsArchived = true, UpdatedAt = _clock() } : value).ToArray()
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> RestoreTemplateAsync(ArchiveMockupTemplateRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
            var offering = template is null ? null : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == template.BlueprintOfferingId && value.StoreId == request.StoreId);
            var placeholder = template?.TargetPlaceholderId is null ? null : snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == template.TargetPlaceholderId && value.OfferingId == template.BlueprintOfferingId);
            if (template is null || offering is null) return Failure(snapshot, request.StoreId, "Mockup Template was not found in the selected Store.");
            if (offering.IsArchived || (template.TargetPlaceholderId is not null && (placeholder is null || placeholder.IsArchived)))
                return Failure(snapshot, request.StoreId, "Restore the Blueprint Offering and any configured target Placeholder before restoring this Mockup Template.");
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? value with { IsArchived = false, UpdatedAt = _clock() } : value).ToArray(),
                MockupTemplateColorVariants = snapshot.MockupTemplateColorVariants.Select(value => value.MockupTemplateId == template.Id ? value with { IsArchived = false, UpdatedAt = _clock() } : value).ToArray()
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<MockupTemplateSetupResult> UpdateTemplateAsync(UpdateMockupTemplateRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var error = EnsureWritable(snapshot, request.StoreId);
            if (error is not null) return Failure(snapshot, request.StoreId, error);
            var template = snapshot.MockupTemplates.SingleOrDefault(value => value.Id == request.TemplateId);
            if (template is null) return Failure(snapshot, request.StoreId, "Mockup Template was not found in the selected Store.");
            var targetId = request.ReplaceTargetPlaceholder ? request.TargetPlaceholderId : template.TargetPlaceholderId;
            OfferingPlaceholder? placeholder = null;
            if (targetId is not null)
            {
                placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == targetId && value.OfferingId == template.BlueprintOfferingId && !value.IsArchived);
                if (placeholder is null) return Failure(snapshot, request.StoreId, "Target Placeholder must be active and belong to the template's Blueprint Offering.");
            }
            var now = _clock();
            var currentRevision = CurrentRevision(snapshot, template);
            var providerReference = request.ReplaceProviderImage ? request.ProviderMockupReference : currentRevision?.ProviderMockupReference;
            var imageMapping = request.ReplaceProviderImage ? request.ImageMapping : currentRevision?.ImageMapping;
            var activeBindings = snapshot.MockupTemplateColorVariants.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).ToArray();
            var activeColors = activeBindings.Select(value => value.ColorOptionValueId).ToHashSet();
            var nextColors = request.ReplaceColorOptionValueIds?.Distinct().ToHashSet() ?? activeColors;
            var colorError = ValidateColors(snapshot, template.BlueprintOfferingId, nextColors);
            if (colorError is not null) return Failure(snapshot, request.StoreId, colorError);
            var compatibilityError = ValidateCompatibility(snapshot, template.BlueprintOfferingId, placeholder, nextColors);
            if (compatibilityError is not null) return Failure(snapshot, request.StoreId, compatibilityError);
            var outputChanged = MockupTemplatePolicy.IsOutputAffectingChange(
                template.TargetPlaceholderId,
                targetId,
                activeColors,
                nextColors,
                currentRevision?.ProviderMockupReference,
                providerReference,
                currentRevision?.ImageMapping,
                imageMapping);
            var revisionNumber = outputChanged ? template.CurrentRevision + 1 : template.CurrentRevision;
            var updatedTemplate = template with
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? template.Name : request.Name.Trim(),
                Description = request.Description ?? template.Description,
                TargetPlaceholderId = targetId,
                PositionKey = request.PositionKey ?? template.PositionKey,
                CurrentRevision = revisionNumber,
                UpdatedAt = now
            };
            MockupTemplateRevision? revision = null;
            MockupTemplateRevisionColor[] revisionColors = [];
            if (outputChanged)
            {
                try
                {
                    revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, targetId, now, "Template configuration changed", providerReference, imageMapping);
                }
                catch (ArgumentException exception)
                {
                    return Failure(snapshot, request.StoreId, exception.Message);
                }
                revisionColors = nextColors.Select(colorId => new MockupTemplateRevisionColor(_newId(), revision.Id, colorId)).ToArray();
            }
            var colorsChanged = !activeColors.SetEquals(nextColors);
            var nextBindings = colorsChanged
                ? nextColors.Select(colorId => new MockupTemplateColorVariant(_newId(), template.Id, colorId, false, now, now)).ToArray()
                : [];
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? updatedTemplate : value).ToArray(),
                MockupTemplateColorVariants = colorsChanged
                    ? [.. snapshot.MockupTemplateColorVariants.Select(value => value.MockupTemplateId == template.Id && !value.IsArchived ? value with { IsArchived = true, UpdatedAt = now } : value), .. nextBindings]
                    : snapshot.MockupTemplateColorVariants,
                MockupTemplateRevisions = revision is null ? snapshot.MockupTemplateRevisions : [.. snapshot.MockupTemplateRevisions, revision],
                MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors]
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    private async Task<MockupTemplateSetupResult> MutateAsync(Guid storeId, Func<WorkspaceSnapshot, MockupTemplateSetupResult> mutation, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var result = mutation(snapshot);
        if (!result.Succeeded) return result;
        await _repository.SaveAsync(result.Snapshot!, cancellationToken).ConfigureAwait(false);
        return MockupTemplateSetupResult.Success(BuildState(result.Snapshot!, storeId), result.TemplateId);
    }

    private static string? EnsureWritable(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == storeId);
        return store is null ? "Store was not found." : store.IsArchived ? "Archived Store catalogs are read-only." : null;
    }

    private static MockupTemplateSetupResult Failure(WorkspaceSnapshot snapshot, Guid storeId, string error) => new(false, error, BuildState(snapshot, storeId));
    private static MockupTemplateSetupResult Success(WorkspaceSnapshot snapshot, Guid storeId, Guid? templateId = null) => new(true, null, BuildState(snapshot, storeId), snapshot, templateId);
    private static MockupTemplateRevision? CurrentRevision(WorkspaceSnapshot snapshot, MockupTemplate template) =>
        snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);

    private static string NextCopyName(WorkspaceSnapshot snapshot, MockupTemplate source)
    {
        var existing = snapshot.MockupTemplates
            .Where(value => value.BlueprintOfferingId == source.BlueprintOfferingId && !value.IsArchived)
            .Select(value => value.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseName = $"Copy of {source.Name}";
        if (!existing.Contains(baseName)) return baseName;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName} ({suffix})";
            if (!existing.Contains(candidate)) return candidate;
        }
    }

    private static string? ValidateColors(WorkspaceSnapshot snapshot, Guid offeringId, IEnumerable<Guid> colorIds)
    {
        var requested = colorIds.Distinct().ToHashSet();
        var valid = snapshot.OfferingOptionValues
            .Where(value => requested.Contains(value.Id) && value.OfferingId == offeringId && !value.IsArchived)
            .Where(value => snapshot.OfferingOptions.Any(option => option.Id == value.OptionId && option.OfferingId == offeringId && option.OptionKind == OptionKind.Color && !option.IsArchived))
            .Select(value => value.Id)
            .ToHashSet();
        return valid.SetEquals(requested) ? null : "Template applicability may only use active Color values from the same Offering.";
    }

    public async Task<EligibleMockupTemplateResult> GetEligibleTemplatesAsync(Guid storeId, Guid offeringId, Guid? requestedTemplateId = null, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var state = BuildState(snapshot, storeId);
        var candidates = state.Templates.Where(value => value.BlueprintOfferingId == offeringId && !value.IsArchived).ToArray();
        var eligibleIds = (state.Readiness ?? []).Where(value => value.Lifecycle == MockupTemplateLifecycle.ReadyForUse).Select(value => value.TemplateId).ToHashSet();
        if (requestedTemplateId is not null && !eligibleIds.Contains(requestedTemplateId.Value))
        {
            var readiness = (state.Readiness ?? []).SingleOrDefault(value => value.TemplateId == requestedTemplateId.Value);
            return new(false, readiness is null ? "Mockup Template was not found." : "Mockup Template is still a Draft.", [], readiness?.Blockers ?? []);
        }
        return new(true, null, candidates.Where(value => eligibleIds.Contains(value.Id) && (requestedTemplateId is null || value.Id == requestedTemplateId)).ToArray(), []);
    }

    private static string? ValidateCompatibility(WorkspaceSnapshot snapshot, Guid offeringId, OfferingPlaceholder? placeholder, IEnumerable<Guid> colorIds)
    {
        if (placeholder is null) return null;
        var requested = colorIds.ToHashSet();
        var incompatible = snapshot.OfferingVariants
            .Where(value => value.OfferingId == offeringId && !value.IsArchived && value.OptionValueIds.Any(requested.Contains))
            .Where(value => !placeholder.VariantIds.Contains(value.Id))
            .Select(value => value.Name)
            .OrderBy(value => value)
            .ToArray();
        return incompatible.Length == 0 ? null : $"The target Design Area is incompatible with: {string.Join(", ", incompatible)}.";
    }

    private static MockupTemplateSetupState BuildState(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var templates = snapshot.MockupTemplates.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.BlueprintOfferingId && offering.StoreId == storeId)).ToArray();
        var colors = snapshot.MockupTemplateColorVariants.Where(value => templates.Any(template => template.Id == value.MockupTemplateId)).ToArray();
        var revisions = snapshot.MockupTemplateRevisions.Where(value => templates.Any(template => template.Id == value.MockupTemplateId)).ToArray();
        var readiness = templates.Select(template =>
        {
            var revision = revisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision)
                ?? new MockupTemplateRevision(template.Id, template.Id, template.CurrentRevision, template.TargetPlaceholderId, template.CreatedAt);
            var activeColors = colors.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).Select(value => value.ColorOptionValueId).ToArray();
            var result = MockupTemplateReadinessPolicy.Evaluate(new(template, revision, activeColors, snapshot.OfferingOptions, snapshot.OfferingOptionValues, snapshot.OfferingVariants, snapshot.OfferingPlaceholders));
            return new MockupTemplateReadinessSummary(template.Id, result.Lifecycle, result.Blockers);
        }).ToArray();
        return new(storeId, snapshot.Stores.SingleOrDefault(value => value.Id == storeId)?.IsArchived ?? false, templates, colors, revisions, readiness);
    }
}
