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
            var placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == request.TargetPlaceholderId && value.OfferingId == request.OfferingId);
            if (offering is null || placeholder is null) return Failure(snapshot, request.StoreId, "Target Placeholder must belong to the selected Blueprint Offering.");
            if (offering.IsArchived || placeholder.IsArchived) return Failure(snapshot, request.StoreId, "Archived catalog records are read-only.");
            var now = _clock();
            var template = new MockupTemplate(_newId(), offering.Id, placeholder.Id, request.Name, request.Description, 1, false, now, now, request.PositionKey, null);
            MockupTemplateRevision revision;
            try
            {
                revision = new MockupTemplateRevision(_newId(), template.Id, 1, placeholder.Id, now, "Initial template configuration", request.ProviderMockupReference, request.ImageMapping);
            }
            catch (ArgumentException exception)
            {
                return Failure(snapshot, request.StoreId, exception.Message);
            }
            var updated = snapshot with { MockupTemplates = [.. snapshot.MockupTemplates, template], MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision] };
            return Success(updated, request.StoreId);
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
            var placeholder = template is null ? null : snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == template.TargetPlaceholderId && value.OfferingId == template.BlueprintOfferingId);
            if (template is null || offering is null || placeholder is null) return Failure(snapshot, request.StoreId, "Mockup Template or its target Placeholder was not found in the selected Store.");
            if (offering.IsArchived || placeholder.IsArchived) return Failure(snapshot, request.StoreId, "Restore the Blueprint Offering and target Placeholder before restoring this Mockup Template.");
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
            var targetId = request.TargetPlaceholderId ?? template.TargetPlaceholderId;
            var placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == targetId && value.OfferingId == template.BlueprintOfferingId && !value.IsArchived);
            if (placeholder is null) return Failure(snapshot, request.StoreId, "Target Placeholder must belong to the template's Blueprint Offering.");
            var now = _clock();
            var currentRevision = CurrentRevision(snapshot, template);
            var providerReference = request.ReplaceProviderImage ? request.ProviderMockupReference : currentRevision?.ProviderMockupReference;
            var imageMapping = request.ReplaceProviderImage ? request.ImageMapping : currentRevision?.ImageMapping;
            var activeBindings = snapshot.MockupTemplateColorVariants.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).ToArray();
            var activeColors = activeBindings.Select(value => value.ColorOptionValueId).ToHashSet();
            var nextColors = request.ReplaceColorOptionValueIds?.Distinct().ToHashSet() ?? activeColors;
            if (request.ReplaceColorOptionValueIds is not null && nextColors.Count == 0)
                return Failure(snapshot, request.StoreId, "Select at least one applicable Color.");
            var validColors = snapshot.OfferingOptionValues.Where(value => nextColors.Contains(value.Id) && value.OfferingId == template.BlueprintOfferingId && !value.IsArchived)
                .Where(value => snapshot.OfferingOptions.Any(option => option.Id == value.OptionId && option.OptionKind == OptionKind.Color && !option.IsArchived))
                .Select(value => value.Id).ToHashSet();
            if (nextColors.Count > 0 && !validColors.SetEquals(nextColors))
                return Failure(snapshot, request.StoreId, "Template applicability may only use active Color values from the same Offering.");
            var impliedVariants = snapshot.OfferingVariants.Where(value => value.OfferingId == template.BlueprintOfferingId && !value.IsArchived && value.OptionValueIds.Any(nextColors.Contains)).ToArray();
            var incompatibleVariants = impliedVariants.Where(value => !placeholder.VariantIds.Contains(value.Id)).Select(value => value.Name).ToArray();
            if (incompatibleVariants.Length > 0)
                return Failure(snapshot, request.StoreId, $"The target Design Area is incompatible with: {string.Join(", ", incompatibleVariants)}.");
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
        return MockupTemplateSetupResult.Success(BuildState(result.Snapshot!, storeId));
    }

    private static string? EnsureWritable(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == storeId);
        return store is null ? "Store was not found." : store.IsArchived ? "Archived Store catalogs are read-only." : null;
    }

    private static MockupTemplateSetupResult Failure(WorkspaceSnapshot snapshot, Guid storeId, string error) => new(false, error, BuildState(snapshot, storeId));
    private static MockupTemplateSetupResult Success(WorkspaceSnapshot snapshot, Guid storeId) => new(true, null, BuildState(snapshot, storeId), snapshot);
    private static MockupTemplateRevision? CurrentRevision(WorkspaceSnapshot snapshot, MockupTemplate template) =>
        snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
    private static MockupTemplateSetupState BuildState(WorkspaceSnapshot snapshot, Guid storeId) => new(storeId, snapshot.Stores.SingleOrDefault(value => value.Id == storeId)?.IsArchived ?? false, snapshot.MockupTemplates.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.BlueprintOfferingId && offering.StoreId == storeId)).ToArray(), snapshot.MockupTemplateColorVariants.Where(value => snapshot.MockupTemplates.Any(template => template.Id == value.MockupTemplateId && snapshot.BlueprintOfferings.Any(offering => offering.Id == template.BlueprintOfferingId && offering.StoreId == storeId))).ToArray(), snapshot.MockupTemplateRevisions.Where(value => snapshot.MockupTemplates.Any(template => template.Id == value.MockupTemplateId && snapshot.BlueprintOfferings.Any(offering => offering.Id == template.BlueprintOfferingId && offering.StoreId == storeId))).ToArray());
}
