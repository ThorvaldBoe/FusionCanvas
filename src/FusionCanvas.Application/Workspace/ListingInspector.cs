using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ListingInspectorCreativeFields(string? Idea, string? Audience, string? Phrase, string? GraphicDirection)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(Idea)
        || !string.IsNullOrWhiteSpace(Audience)
        || !string.IsNullOrWhiteSpace(Phrase)
        || !string.IsNullOrWhiteSpace(GraphicDirection);
}

public sealed record ListingInspectorTagEntry(Guid Id, string Name);

public sealed record ListingInspectorAssetEntry(Guid Id, string Name, AssetKind Kind, bool IsMissing);

public sealed record ListingInspectorState(
    Guid Id,
    string Title,
    string? Description,
    ListingInspectorCreativeFields Creative,
    string? Notes,
    ListingStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    string DisplayPath,
    IReadOnlyList<ListingInspectorTagEntry> Tags,
    IReadOnlyList<ListingInspectorAssetEntry> Assets,
    IReadOnlyList<string> AvailableTagNames,
    DateTimeOffset UpdatedAt)
{
    public bool IsReadOnly => !IsEffectivelyActive;
}

public sealed record ListingInspectorSaveRequest(
    Guid ListingId,
    string Title,
    string? Description,
    string? Idea,
    string? Audience,
    string? Phrase,
    string? GraphicDirection,
    string? Notes,
    IReadOnlyList<string> TagNames);

public sealed record ListingInspectorSaveResult(
    bool Succeeded,
    string? Error,
    ListingInspectorState? State)
{
    public static ListingInspectorSaveResult Success(ListingInspectorState state) =>
        new(true, null, state);

    public static ListingInspectorSaveResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Inspector save failed." : error, null);
}

public interface IListingInspectorService
{
    Task<ListingInspectorState?> LoadAsync(Guid listingId, CancellationToken cancellationToken = default);

    Task<ListingInspectorSaveResult> SaveAsync(ListingInspectorSaveRequest request, CancellationToken cancellationToken = default);
}

public sealed class ListingInspectorService : IListingInspectorService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public ListingInspectorService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<ListingInspectorState?> LoadAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return FindAndBuildState(snapshot, listingId);
    }

    public async Task<ListingInspectorSaveResult> SaveAsync(
        ListingInspectorSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return ListingInspectorSaveResult.Failure("Listing was not found.");
        }

        if (!ListingHierarchy.IsEffectivelyActive(snapshot, existing))
        {
            return ListingInspectorSaveResult.Failure(
                "Archived or inactive listings cannot be edited in the inspector. Restore the listing first.");
        }

        var name = ListingMetadataCodec.NormalizeName(request.Title);
        var nameError = ListingMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return ListingInspectorSaveResult.Failure(nameError);
        }

        List<string> tagErrors = [];
        var normalizedTagNames = NormalizeTagNames(request.TagNames, tagErrors);
        if (tagErrors.Count > 0)
        {
            return ListingInspectorSaveResult.Failure(tagErrors[0]);
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        var phrase = ListingMetadataCodec.NormalizeSingleLine(request.Phrase);
        ApplyInspectorField(metadata, ListingMetadataCodec.NotesKey, request.Notes);
        ApplyInspectorField(metadata, ListingMetadataCodec.IdeaKey, request.Idea);
        ApplyInspectorField(metadata, ListingMetadataCodec.IdeaAudienceKey, request.Audience);
        ApplyInspectorField(metadata, ListingMetadataCodec.PhraseKey, phrase);
        ApplyInspectorField(metadata, ListingMetadataCodec.GraphicDirectionKey, request.GraphicDirection);

        var (resolvedTagIds, createdTags) = ResolveOrCreateTags(snapshot, existing.StoreId, normalizedTagNames);

        var changed = existing with
        {
            Name = name,
            Description = ListingMetadataCodec.NormalizeOptional(request.Description),
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };

        var updated = snapshot with
        {
            Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray(),
            Tags = createdTags.Count > 0
                ? [.. snapshot.Tags, .. createdTags]
                : snapshot.Tags,
            ListingTags = [.. snapshot.ListingTags.Where(link => link.ListingId != existing.Id),
                           .. resolvedTagIds.Select(tagId => new ListingTag(existing.Id, tagId))]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ListingInspectorSaveResult.Failure($"The inspector change could not be saved. {exception.Message}");
        }

        return ListingInspectorSaveResult.Success(FindAndBuildState(updated, existing.Id)!);
    }

    private static void ApplyInspectorField(Dictionary<string, string> metadata, string key, string? value)
    {
        ListingMetadataCodec.SetOptional(metadata, key, value);
        metadata.Remove($"{ListingMetadataCodec.InheritedFromPrefix}{key}");
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

    private static ListingInspectorState? FindAndBuildState(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (listing is null)
        {
            return null;
        }

        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        var notes = metadata.TryGetValue(ListingMetadataCodec.NotesKey, out var notesValue)
            ? (string.IsNullOrWhiteSpace(notesValue) ? null : notesValue)
            : null;
        var idea = metadata.GetValueOrDefault(ListingMetadataCodec.IdeaKey);
        var audience = metadata.GetValueOrDefault(ListingMetadataCodec.IdeaAudienceKey);
        var phrase = metadata.GetValueOrDefault(ListingMetadataCodec.PhraseKey);
        var graphicDirection = metadata.GetValueOrDefault(ListingMetadataCodec.GraphicDirectionKey);
        var creative = new ListingInspectorCreativeFields(
            string.IsNullOrWhiteSpace(idea) ? null : idea,
            string.IsNullOrWhiteSpace(audience) ? null : audience,
            string.IsNullOrWhiteSpace(phrase) ? null : phrase,
            string.IsNullOrWhiteSpace(graphicDirection) ? null : graphicDirection);

        var tags = snapshot.ListingTags
            .Where(link => link.ListingId == listing.Id)
            .Select(link => snapshot.Tags.SingleOrDefault(tag => tag.Id == link.TagId))
            .Where(tag => tag is not null)
            .Select(tag => new ListingInspectorTagEntry(tag!.Id, tag.Name))
            .ToArray();

        var assetEntries = snapshot.AssetLinks
            .Where(link => link.EntityKind == WorkspaceEntityKind.Listing && link.EntityId == listing.Id)
            .Select(link => snapshot.Assets.SingleOrDefault(asset => asset.Id == link.AssetId))
            .Where(asset => asset is not null)
            .Select(asset => new ListingInspectorAssetEntry(asset!.Id, asset.Name, asset.Kind, asset.IsMissing))
            .ToArray();

        var availableTagNames = snapshot.Tags
            .Where(tag => tag.StoreId == listing.StoreId && !tag.IsArchived)
            .Select(tag => tag.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var displayPath = BuildDisplayPath(snapshot, listing);

        return new ListingInspectorState(
            listing.Id,
            listing.Name,
            listing.Description,
            creative,
            notes,
            listing.Status,
            listing.Stage,
            listing.IsArchived,
            ListingHierarchy.IsEffectivelyActive(snapshot, listing),
            displayPath,
            tags,
            assetEntries,
            availableTagNames,
            listing.UpdatedAt);
    }

    private static string BuildDisplayPath(WorkspaceSnapshot snapshot, Listing listing)
    {
        var niche = ListingHierarchy.GetEffectiveNiche(snapshot, listing);
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
