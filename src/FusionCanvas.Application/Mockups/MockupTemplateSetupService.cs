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
            var revision = new MockupTemplateRevision(_newId(), template.Id, 1, placeholder.Id, now, "Initial template configuration");
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
            var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, template.TargetPlaceholderId, now, "Color configuration changed");
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
            var updated = snapshot with { MockupTemplateColorVariants = snapshot.MockupTemplateColorVariants.Select(value => value.Id == binding.Id ? value with { IsArchived = true, UpdatedAt = _clock() } : value).ToArray() };
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
            var revisionNumber = template.CurrentRevision + 1;
            var updatedTemplate = template with
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? template.Name : request.Name.Trim(),
                Description = request.Description ?? template.Description,
                TargetPlaceholderId = targetId,
                PositionKey = request.PositionKey ?? template.PositionKey,
                CurrentRevision = revisionNumber,
                UpdatedAt = now
            };
            var revision = new MockupTemplateRevision(_newId(), template.Id, revisionNumber, targetId, now, "Template configuration changed");
            var activeColors = snapshot.MockupTemplateColorVariants.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).Select(value => value.ColorOptionValueId).ToArray();
            var revisionColors = activeColors.Select(colorId => new MockupTemplateRevisionColor(_newId(), revision.Id, colorId)).ToArray();
            var updated = snapshot with
            {
                MockupTemplates = snapshot.MockupTemplates.Select(value => value.Id == template.Id ? updatedTemplate : value).ToArray(),
                MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
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
    private static MockupTemplateSetupState BuildState(WorkspaceSnapshot snapshot, Guid storeId) => new(storeId, snapshot.Stores.SingleOrDefault(value => value.Id == storeId)?.IsArchived ?? false, snapshot.MockupTemplates.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.BlueprintOfferingId && offering.StoreId == storeId)).ToArray(), snapshot.MockupTemplateColorVariants.Where(value => snapshot.MockupTemplates.Any(template => template.Id == value.MockupTemplateId && snapshot.BlueprintOfferings.Any(offering => offering.Id == template.BlueprintOfferingId && offering.StoreId == storeId))).ToArray(), snapshot.MockupTemplateRevisions.Where(value => snapshot.MockupTemplates.Any(template => template.Id == value.MockupTemplateId && snapshot.BlueprintOfferings.Any(offering => offering.Id == template.BlueprintOfferingId && offering.StoreId == storeId))).ToArray());
}
