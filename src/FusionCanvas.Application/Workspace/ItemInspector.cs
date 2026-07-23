using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ItemInspectorCreativeFields(
    string? Idea,
    string? Audience,
    string? ConceptIdea,
    string? Phrase,
    string? GraphicDirection)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(Idea)
        || !string.IsNullOrWhiteSpace(Audience)
        || !string.IsNullOrWhiteSpace(ConceptIdea)
        || !string.IsNullOrWhiteSpace(Phrase)
        || !string.IsNullOrWhiteSpace(GraphicDirection);
}

public sealed record ItemInspectorTagEntry(Guid Id, string Name);

public sealed record ItemInspectorAssetEntry(Guid Id, string Name, AssetKind Kind, bool IsMissing);

public sealed record ItemInspectorState(
    Guid Id,
    string Title,
    string? Description,
    ItemInspectorCreativeFields Creative,
    string? Notes,
    ItemStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    string DisplayPath,
    IReadOnlyList<ItemInspectorTagEntry> Tags,
    IReadOnlyList<ItemInspectorAssetEntry> Assets,
    IReadOnlyList<string> AvailableTagNames,
    DateTimeOffset UpdatedAt)
{
    public bool IsReadOnly => !IsEffectivelyActive;
}

public sealed record ItemInspectorSaveRequest(
    Guid ItemId,
    string Title,
    string? Description,
    string? Idea,
    string? Audience,
    string? Phrase,
    string? GraphicDirection,
    string? Notes,
    IReadOnlyList<string> TagNames);

public sealed record ItemStageSavePayload(
    WorkflowStage Stage,
    string? Idea,
    string? ConceptIdea,
    string? Phrase,
    string? GraphicDirection);

public sealed record ItemStageAwareSaveRequest(
    Guid ItemId,
    WorkflowStage ExpectedCurrentStage,
    string Title,
    string? Notes,
    ItemStageSavePayload StagePayload,
    IReadOnlyList<string> TagNames);

public sealed record ItemInspectorSaveResult(
    bool Succeeded,
    string? Error,
    ItemInspectorState? State)
{
    public static ItemInspectorSaveResult Success(ItemInspectorState state) =>
        new(true, null, state);

    public static ItemInspectorSaveResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Inspector save failed." : error, null);
}

public interface IItemInspectorService
{
    Task<ItemInspectorState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<ItemInspectorSaveResult> SaveAsync(ItemInspectorSaveRequest request, CancellationToken cancellationToken = default);

    Task<ItemInspectorSaveResult> SaveStageAsync(ItemStageAwareSaveRequest request, CancellationToken cancellationToken = default);
}

public sealed class ItemInspectorService : IItemInspectorService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public ItemInspectorService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<ItemInspectorState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return FindAndBuildState(snapshot, itemId);
    }

    public async Task<ItemInspectorSaveResult> SaveAsync(
        ItemInspectorSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return ItemInspectorSaveResult.Failure("Listing was not found.");
        }

        if (!ItemHierarchy.IsEffectivelyActive(snapshot, existing))
        {
            return ItemInspectorSaveResult.Failure(
                "Archived or inactive items cannot be edited in the inspector. Restore the item first.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(existing, ItemOperationKind.StageContent);
        if (!editDecision.IsAllowed)
        {
            return ItemInspectorSaveResult.Failure(editDecision.Reason);
        }

        var name = ItemMetadataCodec.NormalizeName(request.Title);
        var nameError = ItemMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return ItemInspectorSaveResult.Failure(nameError);
        }

        List<string> tagErrors = [];
        var normalizedTagNames = NormalizeTagNames(request.TagNames, tagErrors);
        if (tagErrors.Count > 0)
        {
            return ItemInspectorSaveResult.Failure(tagErrors[0]);
        }

        var metadata = ItemMetadataCodec.ParseMetadata(existing.MetadataJson);
        var phrase = ItemMetadataCodec.NormalizeSingleLine(request.Phrase);
        ApplyInspectorField(metadata, ItemMetadataCodec.NotesKey, request.Notes);
        ApplyInspectorField(metadata, ItemMetadataCodec.IdeaKey, request.Idea);
        ApplyInspectorField(metadata, ItemMetadataCodec.IdeaAudienceKey, request.Audience);
        ApplyInspectorField(metadata, ItemMetadataCodec.PhraseKey, phrase);
        ApplyInspectorField(metadata, ItemMetadataCodec.GraphicDirectionKey, request.GraphicDirection);

        var (resolvedTagIds, createdTags) = ResolveOrCreateTags(snapshot, existing.StoreId, normalizedTagNames);

        var changed = existing with
        {
            Name = name,
            Description = ItemMetadataCodec.NormalizeOptional(request.Description),
            MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };

        var updated = snapshot with
        {
            Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray(),
            Tags = createdTags.Count > 0
                ? [.. snapshot.Tags, .. createdTags]
                : snapshot.Tags,
            ItemTags = [.. snapshot.ItemTags.Where(link => link.ItemId != existing.Id),
                           .. resolvedTagIds.Select(tagId => new ItemTag(existing.Id, tagId))]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ItemInspectorSaveResult.Failure($"The inspector change could not be saved. {exception.Message}");
        }

        return ItemInspectorSaveResult.Success(FindAndBuildState(updated, existing.Id)!);
    }

    public async Task<ItemInspectorSaveResult> SaveStageAsync(
        ItemStageAwareSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return ItemInspectorSaveResult.Failure("Item was not found.");
        }

        if (existing.Stage != request.ExpectedCurrentStage)
        {
            return ItemInspectorSaveResult.Failure(
                "The item's current stage has changed. Reload the latest item state before saving.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(existing, ItemOperationKind.StageContent);
        if (!editDecision.IsAllowed)
        {
            return ItemInspectorSaveResult.Failure(editDecision.Reason);
        }

        var name = ItemMetadataCodec.NormalizeName(request.Title);
        var nameError = ItemMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return ItemInspectorSaveResult.Failure(nameError);
        }

        List<string> tagErrors = [];
        var normalizedTagNames = NormalizeTagNames(request.TagNames, tagErrors);
        if (tagErrors.Count > 0)
        {
            return ItemInspectorSaveResult.Failure(tagErrors[0]);
        }

        var metadata = ItemMetadataCodec.ParseMetadata(existing.MetadataJson);
        ApplyInspectorField(metadata, ItemMetadataCodec.NotesKey, request.Notes);
        ApplyStagePayload(metadata, request.StagePayload, existing.Stage);

        var (resolvedTagIds, createdTags) = ResolveOrCreateTags(snapshot, existing.StoreId, normalizedTagNames);

        var changed = existing with
        {
            Name = name,
            MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };

        var updated = snapshot with
        {
            Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray(),
            Tags = createdTags.Count > 0
                ? [.. snapshot.Tags, .. createdTags]
                : snapshot.Tags,
            ItemTags = [.. snapshot.ItemTags.Where(link => link.ItemId != existing.Id),
                           .. resolvedTagIds.Select(tagId => new ItemTag(existing.Id, tagId))]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ItemInspectorSaveResult.Failure($"The item change could not be saved. {exception.Message}");
        }

        return ItemInspectorSaveResult.Success(FindAndBuildState(updated, existing.Id)!);
    }

    private static void ApplyStagePayload(Dictionary<string, string> metadata, ItemStageSavePayload payload, WorkflowStage currentStage)
    {
        if (payload.Stage != currentStage)
        {
            return;
        }

        switch (currentStage)
        {
            case WorkflowStage.Idea:
                ApplyInspectorField(metadata, ItemMetadataCodec.IdeaKey, payload.Idea);
                break;
            case WorkflowStage.Concept:
                ApplyInspectorField(metadata, ItemMetadataCodec.ConceptIdeaKey, payload.ConceptIdea);
                ApplyInspectorField(metadata, ItemMetadataCodec.PhraseKey,
                    string.IsNullOrWhiteSpace(payload.Phrase) ? null : ItemMetadataCodec.NormalizeSingleLine(payload.Phrase));
                ApplyInspectorField(metadata, ItemMetadataCodec.GraphicDirectionKey, payload.GraphicDirection);
                break;
            case WorkflowStage.Design:
            case WorkflowStage.Listing:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentStage), currentStage, "Unsupported workflow stage.");
        }
    }

    private static void ApplyInspectorField(Dictionary<string, string> metadata, string key, string? value)
    {
        ItemMetadataCodec.SetOptional(metadata, key, value);
        metadata.Remove($"{ItemMetadataCodec.InheritedFromPrefix}{key}");
    }

    private static List<string> NormalizeTagNames(IReadOnlyList<string>? tagNames, List<string> errors)
    {
        var normalized = new List<string>();
        if (tagNames is null)
        {
            return normalized;
        }

        foreach (var raw in tagNames)
        {
            var trimmed = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                errors.Add("Tag names must not be empty.");
                continue;
            }

            if (trimmed.Contains('\n') || trimmed.Contains('\r'))
            {
                errors.Add("Tag names must be a single line.");
                continue;
            }

            if (!normalized.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }

    private (IReadOnlyList<Guid> TagIds, List<Tag> CreatedTags) ResolveOrCreateTags(
        WorkspaceSnapshot snapshot,
        Guid storeId,
        IReadOnlyList<string> tagNames)
    {
        var resolved = new List<Guid>();
        var created = new List<Tag>();
        var now = _clock();

        foreach (var name in tagNames)
        {
            var existing = snapshot.Tags
                .Concat(created)
                .FirstOrDefault(candidate => candidate.StoreId == storeId
                    && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                resolved.Add(existing.Id);
                continue;
            }

            var tag = new Tag(_newId(), storeId, name, null, false, now, now, "{}");
            created.Add(tag);
            resolved.Add(tag.Id);
        }

        return (resolved, created);
    }

    private static ItemInspectorState? FindAndBuildState(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (listing is null)
        {
            return null;
        }

        var metadata = ItemMetadataCodec.ParseMetadata(listing.MetadataJson);
        var notes = metadata.TryGetValue(ItemMetadataCodec.NotesKey, out var notesValue)
            ? (string.IsNullOrWhiteSpace(notesValue) ? null : notesValue)
            : null;
        var idea = metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey);
        var audience = metadata.GetValueOrDefault(ItemMetadataCodec.IdeaAudienceKey);
        var conceptIdea = metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey);
        var phrase = metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey);
        var graphicDirection = metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey);
        var creative = new ItemInspectorCreativeFields(
            string.IsNullOrWhiteSpace(idea) ? null : idea,
            string.IsNullOrWhiteSpace(audience) ? null : audience,
            string.IsNullOrWhiteSpace(conceptIdea) ? null : conceptIdea,
            string.IsNullOrWhiteSpace(phrase) ? null : phrase,
            string.IsNullOrWhiteSpace(graphicDirection) ? null : graphicDirection);

        var tags = snapshot.ItemTags
            .Where(link => link.ItemId == listing.Id)
            .Select(link => snapshot.Tags.SingleOrDefault(tag => tag.Id == link.TagId))
            .Where(tag => tag is not null)
            .Select(tag => new ItemInspectorTagEntry(tag!.Id, tag.Name))
            .ToArray();

        var assetEntries = snapshot.AssetLinks
            .Where(link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == listing.Id)
            .Select(link => snapshot.Assets.SingleOrDefault(asset => asset.Id == link.AssetId))
            .Where(asset => asset is not null)
            .Where(asset => asset!.Kind != AssetKind.ExportedImage
                || !Path.GetExtension(asset.WorkspaceRelativePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            .Select(asset => new ItemInspectorAssetEntry(asset!.Id, asset.Name, asset.Kind, asset.IsMissing))
            .ToArray();

        var availableTagNames = snapshot.Tags
            .Where(tag => tag.StoreId == listing.StoreId && !tag.IsArchived)
            .Select(tag => tag.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var displayPath = BuildDisplayPath(snapshot, listing);

        return new ItemInspectorState(
            listing.Id,
            listing.Name,
            listing.Description,
            creative,
            notes,
            listing.Status,
            listing.Stage,
            listing.IsArchived,
            ItemHierarchy.IsEffectivelyActive(snapshot, listing),
            displayPath,
            tags,
            assetEntries,
            availableTagNames,
            listing.UpdatedAt);
    }

    private static string BuildDisplayPath(WorkspaceSnapshot snapshot, Item listing)
    {
        var niche = ItemHierarchy.GetEffectiveNiche(snapshot, listing);
        var names = new List<string> { niche.Name };

        if (listing.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.Single(candidate => candidate.Id == groupId);
            foreach (var ancestor in GroupHierarchy.GetAncestors(snapshot, group))
            {
                names.Add(ancestor.Name);
            }
            names.Add(group.Name);
        }

        return string.Join(" / ", names);
    }
}
