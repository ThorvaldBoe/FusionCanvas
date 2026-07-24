using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Application.Tags;

public sealed class TagManagementService : ITagManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeStoreId;

    public TagManagementService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveStoreId => _activeStoreId;

    public void SetActiveStore(Guid? storeId) => _activeStoreId = storeId;

    public async Task<TagManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<TagManagementResult> CreateTagAsync(TagManagementCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var storeError = ValidateActiveStore(snapshot, request.StoreId);
        if (storeError is not null)
        {
            return TagManagementResult.Failure(storeError, BuildState(snapshot, request.StoreId));
        }

        var normalizedName = NormalizeName(request.Name);
        var nameError = ValidateName(normalizedName, snapshot, request.StoreId, existingTagId: null);
        if (nameError is not null)
        {
            return TagManagementResult.Failure(nameError, BuildState(snapshot, request.StoreId));
        }

        string? normalizedColor;
        try
        {
            normalizedColor = Tag.NormalizeColor(request.Color);
        }
        catch (ArgumentException ex)
        {
            return TagManagementResult.Failure(ex.Message, BuildState(snapshot, request.StoreId));
        }

        var now = _clock();
        var tag = new Tag(_newId(), request.StoreId, normalizedName, NormalizeOptional(request.Description), false, now, now, "{}", normalizedColor);
        var updated = snapshot with { Tags = [.. snapshot.Tags, tag] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = request.StoreId;
        return TagManagementResult.Success(ToSummary(tag), BuildState(updated, request.StoreId));
    }

    public async Task<TagManagementResult> UpdateTagAsync(TagManagementUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Tags.SingleOrDefault(tag => tag.Id == request.TagId);
        if (existing is null)
        {
            return TagManagementResult.Failure("Tag was not found.", BuildState(snapshot, _activeStoreId));
        }

        var normalizedName = NormalizeName(request.Name);
        var nameError = ValidateName(normalizedName, snapshot, existing.StoreId, existing.Id);
        if (nameError is not null)
        {
            return TagManagementResult.Failure(nameError, BuildState(snapshot, existing.StoreId));
        }

        string? normalizedColor;
        try
        {
            normalizedColor = Tag.NormalizeColor(request.Color);
        }
        catch (ArgumentException ex)
        {
            return TagManagementResult.Failure(ex.Message, BuildState(snapshot, existing.StoreId));
        }

        var updatedTag = existing with
        {
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            Color = normalizedColor,
            UpdatedAt = _clock()
        };
        var updated = snapshot with
        {
            Tags = snapshot.Tags.Select(tag => tag.Id == updatedTag.Id ? updatedTag : tag).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = existing.StoreId;
        return TagManagementResult.Success(ToSummary(updatedTag), BuildState(updated, existing.StoreId));
    }

    public async Task<TagManagementResult> ArchiveTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Tags.SingleOrDefault(tag => tag.Id == tagId);
        if (existing is null)
        {
            return TagManagementResult.Failure("Tag was not found.", BuildState(snapshot, _activeStoreId));
        }

        var archived = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Tags = snapshot.Tags.Select(tag => tag.Id == archived.Id ? archived : tag).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = existing.StoreId;
        return TagManagementResult.Success(ToSummary(archived), BuildState(updated, existing.StoreId));
    }

    public async Task<TagManagementResult> RestoreTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Tags.SingleOrDefault(tag => tag.Id == tagId);
        if (existing is null)
        {
            return TagManagementResult.Failure("Tag was not found.", BuildState(snapshot, _activeStoreId));
        }

        var duplicate = snapshot.Tags.Any(tag =>
            tag.Id != existing.Id &&
            tag.StoreId == existing.StoreId &&
            !tag.IsArchived &&
            string.Equals(tag.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return TagManagementResult.Failure("An active tag already uses this name in this store.", BuildState(snapshot, existing.StoreId));
        }

        var restored = existing with { IsArchived = false, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Tags = snapshot.Tags.Select(tag => tag.Id == restored.Id ? restored : tag).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = existing.StoreId;
        return TagManagementResult.Success(ToSummary(restored), BuildState(updated, existing.StoreId));
    }

    public async Task<TagManagementResult> DeleteTagAsync(TagManagementDeleteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Tags.SingleOrDefault(tag => tag.Id == request.TagId);
        if (existing is null)
        {
            return TagManagementResult.Failure("Tag was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return TagManagementResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, existing.StoreId));
        }

        var affectedListingCount = snapshot.ItemTags.Count(link => link.TagId == existing.Id);

        var updated = snapshot with
        {
            Tags = snapshot.Tags.Where(tag => tag.Id != existing.Id).ToArray(),
            ItemTags = snapshot.ItemTags.Where(link => link.TagId != existing.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = existing.StoreId;
        return TagManagementResult.Success(ToSummary(existing), BuildState(updated, existing.StoreId), affectedListingCount);
    }

    public async Task<TagApplicationResult> ApplyTagAsync(ApplyTagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (listing is null)
        {
            return TagApplicationResult.Failure("Listing was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (listing.IsArchived)
        {
            return TagApplicationResult.Failure("Archived listings cannot have tags applied.", BuildState(snapshot, _activeStoreId));
        }

        if (IsItemEffectivelyHidden(snapshot, listing))
        {
            return TagApplicationResult.Failure("Restore the listing or its parent topic before editing tags.", BuildState(snapshot, _activeStoreId));
        }

        var tag = snapshot.Tags.SingleOrDefault(candidate => candidate.Id == request.TagId);
        if (tag is null)
        {
            return TagApplicationResult.Failure("Tag was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (tag.StoreId != listing.StoreId)
        {
            return TagApplicationResult.Failure("Tags can only be applied to listings in the same store.", BuildState(snapshot, _activeStoreId));
        }

        if (snapshot.ItemTags.Any(link => link.ItemId == listing.Id && link.TagId == tag.Id))
        {
            return TagApplicationResult.Applied(ToSummary(tag), createdNewTag: false, BuildState(snapshot, tag.StoreId));
        }

        var updated = snapshot with
        {
            ItemTags = [.. snapshot.ItemTags, new ItemTag(listing.Id, tag.Id)]
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = tag.StoreId;
        return TagApplicationResult.Applied(ToSummary(tag), createdNewTag: false, BuildState(updated, tag.StoreId));
    }

    public async Task<TagManagementResult> RemoveTagAsync(RemoveTagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (listing is null)
        {
            return TagManagementResult.Failure("Listing was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (listing.IsArchived)
        {
            return TagManagementResult.Failure("Archived listings cannot have tags removed.", BuildState(snapshot, _activeStoreId));
        }

        if (IsItemEffectivelyHidden(snapshot, listing))
        {
            return TagManagementResult.Failure("Restore the listing or its parent topic before editing tags.", BuildState(snapshot, _activeStoreId));
        }

        var link = snapshot.ItemTags.SingleOrDefault(candidate => candidate.ItemId == request.ItemId && candidate.TagId == request.TagId);
        if (link is null)
        {
            return TagManagementResult.Success(null, BuildState(snapshot, _activeStoreId));
        }

        var updated = snapshot with
        {
            ItemTags = snapshot.ItemTags.Where(candidate => !(candidate.ItemId == request.ItemId && candidate.TagId == request.TagId)).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return TagManagementResult.Success(null, BuildState(updated, _activeStoreId));
    }

    public async Task<TagApplicationResult> ApplyOrCreateTagAsync(ApplyOrCreateTagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (listing is null)
        {
            return TagApplicationResult.Failure("Listing was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (listing.IsArchived)
        {
            return TagApplicationResult.Failure("Archived listings cannot have tags applied.", BuildState(snapshot, _activeStoreId));
        }

        if (IsItemEffectivelyHidden(snapshot, listing))
        {
            return TagApplicationResult.Failure("Restore the listing or its parent topic before editing tags.", BuildState(snapshot, _activeStoreId));
        }

        var normalizedName = NormalizeName(request.Name);
        var formatError = ValidateNameFormat(normalizedName);
        if (formatError is not null)
        {
            return TagApplicationResult.Failure(formatError, BuildState(snapshot, listing.StoreId));
        }

        var activeMatch = snapshot.Tags.FirstOrDefault(tag =>
            tag.StoreId == listing.StoreId &&
            !tag.IsArchived &&
            string.Equals(tag.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        if (activeMatch is not null)
        {
            if (snapshot.ItemTags.Any(link => link.ItemId == listing.Id && link.TagId == activeMatch.Id))
            {
                return TagApplicationResult.Applied(ToSummary(activeMatch), createdNewTag: false, BuildState(snapshot, listing.StoreId));
            }

            var updatedWithLink = snapshot with { ItemTags = [.. snapshot.ItemTags, new ItemTag(listing.Id, activeMatch.Id)] };
            await _repository.SaveAsync(updatedWithLink, cancellationToken).ConfigureAwait(false);
            _activeStoreId = listing.StoreId;
            return TagApplicationResult.Applied(ToSummary(activeMatch), createdNewTag: false, BuildState(updatedWithLink, listing.StoreId));
        }

        var archivedMatch = snapshot.Tags.FirstOrDefault(tag =>
            tag.StoreId == listing.StoreId &&
            tag.IsArchived &&
            string.Equals(tag.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        if (archivedMatch is not null)
        {
            return TagApplicationResult.NeedsRestoreConfirmation(ToSummary(archivedMatch), BuildState(snapshot, listing.StoreId));
        }

        var now = _clock();
        var newTag = new Tag(_newId(), listing.StoreId, normalizedName, null, false, now, now, "{}", null);
        var updated = snapshot with
        {
            Tags = [.. snapshot.Tags, newTag],
            ItemTags = [.. snapshot.ItemTags, new ItemTag(listing.Id, newTag.Id)]
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = listing.StoreId;
        return TagApplicationResult.Applied(ToSummary(newTag), createdNewTag: true, BuildState(updated, listing.StoreId));
    }

    public async Task<IReadOnlyList<TagSummary>> GetActiveTagVocabularyAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.Tags
            .Where(tag => tag.StoreId == storeId && !tag.IsArchived)
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
    }

    public async Task<IReadOnlyList<Guid>> GetItemTagsAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.ItemTags
            .Where(link => link.ItemId == itemId)
            .Select(link => link.TagId)
            .ToArray();
    }

    public async Task<int> GetTagApplicationCountAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.ItemTags.Count(link => link.TagId == tagId);
    }

    private TagManagementState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is Guid requestedStoreId)
        {
            _activeStoreId = snapshot.Stores.Any(store => store.Id == requestedStoreId && !store.IsArchived)
                ? requestedStoreId
                : null;
        }
        else if (_activeStoreId is Guid activeStoreId &&
                 !snapshot.Stores.Any(store => store.Id == activeStoreId && !store.IsArchived))
        {
            _activeStoreId = null;
        }

        var activeTags = _activeStoreId is Guid id
            ? snapshot.Tags
                .Where(tag => tag.StoreId == id && !tag.IsArchived)
                .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToSummary)
                .ToArray()
            : [];
        var archivedTags = _activeStoreId is Guid archiveStoreId
            ? snapshot.Tags
                .Where(tag => tag.StoreId == archiveStoreId && tag.IsArchived)
                .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToSummary)
                .ToArray()
            : [];

        return new TagManagementState(_activeStoreId, activeTags, archivedTags, _activeStoreId is not null && activeTags.Length == 0);
    }

    private static string? ValidateActiveStore(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId);
        if (store is null)
        {
            return "Active store is required before creating tags.";
        }

        if (store.IsArchived)
        {
            return "Archived stores must be restored before tags can be managed.";
        }

        return null;
    }

    private static string? ValidateName(string normalizedName, WorkspaceSnapshot snapshot, Guid storeId, Guid? existingTagId)
    {
        var formatError = ValidateNameFormat(normalizedName);
        if (formatError is not null)
        {
            return formatError;
        }

        var duplicate = snapshot.Tags.Any(tag =>
            tag.Id != existingTagId &&
            tag.StoreId == storeId &&
            !tag.IsArchived &&
            string.Equals(tag.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "An active tag already uses this name in this store." : null;
    }

    private static string? ValidateNameFormat(string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return "Tag name is required.";
        }

        if (normalizedName.Contains('\n') || normalizedName.Contains('\r'))
        {
            return "Tag name must be a single line.";
        }

        return null;
    }

    private static bool IsItemEffectivelyHidden(WorkspaceSnapshot snapshot, Item listing)
    {
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == listing.StoreId);
        if (store is null || store.IsArchived)
        {
            return true;
        }

        if (listing.NicheId is Guid nicheId)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId);
            if (niche is null || niche.IsArchived)
            {
                return true;
            }
        }

        if (listing.GroupId is Guid groupId)
        {
            var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId);
            if (group is null || group.IsArchived)
            {
                return true;
            }

            var parent = group.ParentGroupId;
            while (parent is Guid ancestorId)
            {
                var ancestor = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == ancestorId);
                if (ancestor is null || ancestor.IsArchived)
                {
                    return true;
                }

                parent = ancestor.ParentGroupId;
            }

            if (group.NicheId is Guid groupNicheId)
            {
                var groupNiche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == groupNicheId);
                if (groupNiche is null || groupNiche.IsArchived)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static TagSummary ToSummary(Tag tag) =>
        new(tag.Id, tag.StoreId, tag.Name, tag.Description, tag.Color, tag.IsArchived, tag.CreatedAt, tag.UpdatedAt);

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
