using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ListingTopicReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? Kind
        : throw new ArgumentException("A listing topic must be a niche or group.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}

public sealed record ListingContext(
    string? Description = null,
    string? Notes = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<Guid>? TagIds = null);

public sealed record ListingManagementCreateRequest(ListingTopicReference Topic, string Name, ListingContext? Context = null);
public sealed record ListingManagementUpdateRequest(Guid ListingId, string Name, ListingContext? Context = null);
public sealed record ListingManagementMoveRequest(Guid ListingId, ListingTopicReference Destination);
public sealed record ListingManagementDuplicateRequest(Guid ListingId, ListingTopicReference? Destination = null);
public sealed record ListingManagementRestoreRequest(Guid ListingId, ListingTopicReference? Destination = null);
public sealed record ListingManagementDeleteRequest(Guid ListingId, bool ConfirmPermanentDeletion);
public sealed record ListingManagementSetStatusRequest(Guid ListingId, ListingStatus Status);
public sealed record ListingManagementMoveStageRequest(Guid ListingId, WorkflowStage Stage);

public sealed record ListingCreationDestinationResult(ListingTopicReference? Topic, string? Error)
{
    public bool Succeeded => Topic is not null;
}

public sealed record ListingDestination(ListingTopicReference Topic, Guid StoreId, Guid NicheId, string DisplayPath);

public sealed record ListingSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    ListingTopicReference Topic,
    string Name,
    ListingContext Context,
    ListingStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    IReadOnlyList<Guid> Path,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ListingManagementState(
    Guid? ActiveStoreId,
    Guid? ActiveListingId,
    ListingSummary? ActiveListing,
    IReadOnlyList<ListingSummary> ActiveListings,
    IReadOnlyList<ListingSummary> ArchivedListings,
    IReadOnlyList<ListingDestination> ValidDestinations,
    bool NeedsFirstListing);

public sealed record ListingManagementResult(
    bool Succeeded,
    string? Error,
    ListingSummary? Listing,
    ListingManagementState State)
{
    public static ListingManagementResult Success(ListingSummary? listing, ListingManagementState state) =>
        new(true, null, listing, state);

    public static ListingManagementResult Failure(string error, ListingManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Listing operation failed." : error, null, state);
}

public interface IListingManagementService
{
    Guid? ActiveWorkspaceId { get; }
    Guid? ActiveStoreId { get; }
    Guid? ActiveListingId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<ListingManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<ListingCreationDestinationResult> ResolveCreateTopicAsync(Guid storeId, WorkspaceTreeSelection? selection, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> CreateListingAsync(ListingManagementCreateRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> UpdateListingAsync(ListingManagementUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> MoveListingAsync(ListingManagementMoveRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> DuplicateListingAsync(ListingManagementDuplicateRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> ArchiveListingAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> RestoreListingAsync(ListingManagementRestoreRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> DeleteListingAsync(ListingManagementDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> SetListingStatusAsync(ListingManagementSetStatusRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> MoveListingStageAsync(ListingManagementMoveStageRequest request, CancellationToken cancellationToken = default);
    Task<ListingManagementResult> SelectListingAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class ListingManagementService : IListingManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IToolContextResolver _contextResolver;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public ListingManagementService(
        IWorkspaceRepository repository,
        IToolContextResolver? contextResolver = null,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contextResolver = contextResolver ?? new ToolContextResolver();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;
    public Guid? ActiveStoreId => _activeStoreId;
    public Guid? ActiveListingId => _activeListingId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        if (_activeWorkspaceId != workspaceId)
        {
            _activeStoreId = null;
            _activeListingId = null;
        }

        _activeWorkspaceId = workspaceId;
    }

    public async Task<ListingManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<ListingCreationDestinationResult> ResolveCreateTopicAsync(
        Guid storeId,
        WorkspaceTreeSelection? selection,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId && !candidate.IsArchived && StoreBelongsToActiveWorkspace(candidate));
        if (store is null)
        {
            return new(null, "Select an active store before creating listings.");
        }

        if (selection is not null)
        {
            var selectedTopic = selection.Kind switch
            {
                WorkspaceEntityKind.Niche => new ListingTopicReference(WorkspaceEntityKind.Niche, selection.Id),
                WorkspaceEntityKind.Group => new ListingTopicReference(WorkspaceEntityKind.Group, selection.Id),
                WorkspaceEntityKind.Listing => snapshot.Listings.SingleOrDefault(candidate => candidate.Id == selection.Id) is { } listing
                    ? ToTopic(listing)
                    : null,
                _ => null
            };

            if (selectedTopic is not null && TryResolveActiveTopic(snapshot, selectedTopic, out var selectedStoreId, out _, out _) && selectedStoreId == store.Id)
            {
                return new(selectedTopic, null);
            }
        }

        var activeNiches = snapshot.Niches.Where(niche => niche.StoreId == store.Id && !niche.IsArchived).ToArray();
        var defaultNiche = store.DefaultNicheId is Guid defaultNicheId
            ? activeNiches.SingleOrDefault(niche => niche.Id == defaultNicheId)
            : activeNiches.Length == 1 ? activeNiches[0] : null;

        return defaultNiche is null
            ? new(null, activeNiches.Length == 0
                ? "Create an active niche before creating listings."
                : "Select a topic or configure the store's default niche before creating listings.")
            : new(new ListingTopicReference(WorkspaceEntityKind.Niche, defaultNiche.Id), null);
    }

    public async Task<ListingManagementResult> CreateListingAsync(
        ListingManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!TryResolveActiveTopic(snapshot, request.Topic, out var storeId, out var nicheId, out var topicError))
        {
            return Failure(topicError!, snapshot, _activeStoreId);
        }

        var name = ListingMetadataCodec.NormalizeName(request.Name);
        var nameError = ListingMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, storeId);
        }

        var now = _clock();
        var context = request.Context ?? new ListingContext();
        var metadata = ResolveCreationMetadata(snapshot, request.Topic, context);
        var listing = new Listing(
            _newId(),
            storeId,
            nicheId,
            request.Topic.Kind == WorkspaceEntityKind.Group ? request.Topic.Id : null,
            name,
            ListingMetadataCodec.NormalizeOptional(context.Description),
            ListingStatus.Draft,
            WorkflowStage.Idea,
            false,
            now,
            now,
            ListingMetadataCodec.SerializeMetadata(metadata));

        IReadOnlyList<Guid> tagIds;
        try
        {
            tagIds = ResolveTagIds(snapshot, storeId, request.Topic, context);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message, snapshot, storeId);
        }
        var updated = snapshot with
        {
            Listings = [.. snapshot.Listings, listing],
            ListingTags = [.. snapshot.ListingTags, .. tagIds.Select(tagId => new ListingTag(listing.Id, tagId))]
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, storeId);
        }

        SetSelection(storeId, listing.Id);
        return Success(listing, updated);
    }

    public async Task<ListingManagementResult> UpdateListingAsync(
        ListingManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        var name = ListingMetadataCodec.NormalizeName(request.Name);
        var nameError = ListingMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, existing.StoreId);
        }

        var context = request.Context ?? ToContext(snapshot, existing);
        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        ApplyContextMetadata(metadata, context, replaceExplicitMetadata: context.Metadata is not null);
        var changed = existing with
        {
            Name = name,
            Description = ListingMetadataCodec.NormalizeOptional(context.Description),
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        IReadOnlyList<ListingTag> listingTags;
        try
        {
            listingTags = context.TagIds is null
                ? snapshot.ListingTags
                : [.. snapshot.ListingTags.Where(link => link.ListingId != existing.Id), .. ValidateTagIds(snapshot, existing.StoreId, context.TagIds).Select(tagId => new ListingTag(existing.Id, tagId))];
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message, snapshot, existing.StoreId);
        }
        var updated = snapshot with
        {
            Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray(),
            ListingTags = listingTags
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ListingManagementResult> MoveListingAsync(
        ListingManagementMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ListingHierarchy.IsEffectivelyActive(snapshot, existing))
        {
            return Failure("Archived listings or listings beneath inactive topics must be restored before moving.", snapshot, existing.StoreId);
        }

        if (!TryResolveActiveTopic(snapshot, request.Destination, out var destinationStoreId, out var nicheId, out var topicError))
        {
            return Failure(topicError!, snapshot, existing.StoreId);
        }

        if (destinationStoreId != existing.StoreId)
        {
            return Failure("A listing cannot be moved outside its store.", snapshot, existing.StoreId);
        }

        var changed = existing with
        {
            NicheId = nicheId,
            GroupId = request.Destination.Kind == WorkspaceEntityKind.Group ? request.Destination.Id : null,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ListingManagementResult> DuplicateListingAsync(
        ListingManagementDuplicateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var source = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (source is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ListingHierarchy.IsEffectivelyActive(snapshot, source))
        {
            return Failure("Archived listings or listings beneath inactive topics must be restored before duplication.", snapshot, source.StoreId);
        }

        var destination = request.Destination ?? ToTopic(source);
        if (!TryResolveActiveTopic(snapshot, destination, out var destinationStoreId, out var nicheId, out var topicError))
        {
            return Failure(topicError!, snapshot, source.StoreId);
        }

        if (destinationStoreId != source.StoreId)
        {
            return Failure("A listing cannot be duplicated outside its store.", snapshot, source.StoreId);
        }

        var now = _clock();
        var duplicate = source with
        {
            Id = _newId(),
            NicheId = nicheId,
            GroupId = destination.Kind == WorkspaceEntityKind.Group ? destination.Id : null,
            Name = UniqueCopyName(snapshot, source.Name),
            Status = ListingStatus.Draft,
            Stage = WorkflowStage.Idea,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        var sourceTagIds = snapshot.ListingTags.Where(link => link.ListingId == source.Id).Select(link => link.TagId);
        var updated = snapshot with
        {
            Listings = [.. snapshot.Listings, duplicate],
            ListingTags = [.. snapshot.ListingTags, .. sourceTagIds.Select(tagId => new ListingTag(duplicate.Id, tagId))]
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, source.StoreId);
        }

        SetSelection(source.StoreId, duplicate.Id);
        return Success(duplicate, updated);
    }

    public Task<ListingManagementResult> ArchiveListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        SetArchiveStateAsync(listingId, true, destination: null, cancellationToken);

    public Task<ListingManagementResult> RestoreListingAsync(ListingManagementRestoreRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SetArchiveStateAsync(request.ListingId, false, request.Destination, cancellationToken);
    }

    public async Task<ListingManagementResult> DeleteListingAsync(
        ListingManagementDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return Failure("Permanent deletion requires confirmation.", snapshot, existing.StoreId);
        }

        if (snapshot.Prompts.Any(prompt => prompt.ListingId == existing.Id) ||
            snapshot.AssetLinks.Any(link => link.EntityKind == WorkspaceEntityKind.Listing && link.EntityId == existing.Id))
        {
            return Failure("Listing has connected prompts or assets. Detach them or archive the listing instead.", snapshot, existing.StoreId);
        }

        var updated = snapshot with
        {
            Listings = snapshot.Listings.Where(candidate => candidate.Id != existing.Id).ToArray(),
            ListingTags = snapshot.ListingTags.Where(link => link.ListingId != existing.Id).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        if (_activeListingId == existing.Id)
        {
            _activeListingId = null;
        }
        _activeStoreId = existing.StoreId;
        return ListingManagementResult.Success(ToSummary(snapshot, existing), BuildState(updated, existing.StoreId));
    }

    public async Task<ListingManagementResult> SetListingStatusAsync(
        ListingManagementSetStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Status))
        {
            return Failure("Unknown listing lifecycle status.", await _repository.LoadAsync(cancellationToken).ConfigureAwait(false), _activeStoreId);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (existing.Status == request.Status)
        {
            SetSelection(existing.StoreId, existing.Id);
            return Success(existing, snapshot);
        }

        var changed = existing with { Status = request.Status, UpdatedAt = _clock() };
        var updated = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ListingManagementResult> MoveListingStageAsync(
        ListingManagementMoveStageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Stage))
        {
            return Failure("Unknown workflow stage.", await _repository.LoadAsync(cancellationToken).ConfigureAwait(false), _activeStoreId);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (existing.IsArchived || existing.Status == ListingStatus.Rejected)
        {
            return Failure("Inactive listings must be reactivated before moving workflow stages.", snapshot, existing.StoreId);
        }

        if (existing.Stage == request.Stage)
        {
            SetSelection(existing.StoreId, existing.Id);
            return Success(existing, snapshot);
        }

        var changed = existing with { Stage = request.Stage, UpdatedAt = _clock() };
        var updated = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ListingManagementResult> SelectListingAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (listing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ListingHierarchy.IsEffectivelyActive(snapshot, listing) || !StoreBelongsToActiveWorkspace(snapshot.Stores.Single(store => store.Id == listing.StoreId)))
        {
            return Failure("Archived listings or listings beneath inactive topics cannot become active context.", snapshot, listing.StoreId);
        }

        SetSelection(listing.StoreId, listing.Id);
        return Success(listing, snapshot);
    }

    private async Task<ListingManagementResult> SetArchiveStateAsync(
        Guid listingId,
        bool archived,
        ListingTopicReference? destination,
        CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        var topic = destination ?? ToTopic(existing);
        Guid nicheId;
        if (!archived)
        {
            if (!TryResolveActiveTopic(snapshot, topic, out var destinationStoreId, out nicheId, out var topicError))
            {
                return Failure($"The preserved topic path must be active before restoration. {topicError}", snapshot, existing.StoreId);
            }

            if (destinationStoreId != existing.StoreId)
            {
                return Failure("A listing cannot be restored outside its store.", snapshot, existing.StoreId);
            }
        }
        else
        {
            nicheId = ListingHierarchy.GetEffectiveNiche(snapshot, existing).Id;
        }

        var changed = existing with
        {
            NicheId = nicheId,
            GroupId = topic.Kind == WorkspaceEntityKind.Group ? topic.Id : null,
            IsArchived = archived,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        if (archived)
        {
            if (_activeListingId == existing.Id)
            {
                _activeListingId = null;
            }
            _activeStoreId = existing.StoreId;
        }
        else
        {
            SetSelection(existing.StoreId, existing.Id);
        }

        return Success(changed, updated);
    }

    private ListingManagementState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is Guid requestedStoreId)
        {
            _activeStoreId = snapshot.Stores.Any(store => store.Id == requestedStoreId && !store.IsArchived && StoreBelongsToActiveWorkspace(store))
                ? requestedStoreId
                : null;
        }
        else if (_activeStoreId is Guid activeStoreId &&
                 !snapshot.Stores.Any(store => store.Id == activeStoreId && !store.IsArchived && StoreBelongsToActiveWorkspace(store)))
        {
            _activeStoreId = null;
        }

        if (_activeListingId is Guid activeListingId &&
            snapshot.Listings.SingleOrDefault(candidate => candidate.Id == activeListingId) is not { } activeListing ||
            _activeListingId is Guid selectedId && snapshot.Listings.SingleOrDefault(candidate => candidate.Id == selectedId) is { } selected &&
            (_activeStoreId != selected.StoreId || !ListingHierarchy.IsEffectivelyActive(snapshot, selected)))
        {
            _activeListingId = null;
        }

        var storeListings = _activeStoreId is Guid id
            ? snapshot.Listings.Where(listing => listing.StoreId == id).ToArray()
            : [];
        var active = storeListings
            .Where(listing => ListingHierarchy.IsEffectivelyActive(snapshot, listing))
            .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
            .Select(listing => ToSummary(snapshot, listing))
            .ToArray();
        var archived = storeListings
            .Where(listing => !ListingHierarchy.IsEffectivelyActive(snapshot, listing))
            .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
            .Select(listing => ToSummary(snapshot, listing))
            .ToArray();
        var selectedSummary = _activeListingId is Guid listingId ? active.SingleOrDefault(listing => listing.Id == listingId) : null;
        return new ListingManagementState(
            _activeStoreId,
            _activeListingId,
            selectedSummary,
            active,
            archived,
            BuildDestinations(snapshot, _activeStoreId),
            _activeStoreId is not null && active.Length == 0);
    }

    private ListingManagementResult Success(Listing listing, WorkspaceSnapshot snapshot) =>
        ListingManagementResult.Success(ToSummary(snapshot, listing), BuildState(snapshot, listing.StoreId));

    private ListingManagementResult Failure(string error, WorkspaceSnapshot snapshot, Guid? storeId) =>
        ListingManagementResult.Failure(error, BuildState(snapshot, storeId));

    private bool TryResolveActiveTopic(
        WorkspaceSnapshot snapshot,
        ListingTopicReference topic,
        out Guid storeId,
        out Guid nicheId,
        out string? error)
    {
        storeId = Guid.Empty;
        nicheId = Guid.Empty;
        error = null;

        if (topic.Kind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == topic.Id);
            if (niche is null || niche.IsArchived)
            {
                error = "The destination niche must exist and be active.";
                return false;
            }

            var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == niche.StoreId && !candidate.IsArchived && StoreBelongsToActiveWorkspace(candidate));
            if (store is null)
            {
                error = "The destination store must be active in the current workspace.";
                return false;
            }

            storeId = store.Id;
            nicheId = niche.Id;
            return true;
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == topic.Id);
        if (group is null || !GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            error = "The destination group and its complete parent path must be active.";
            return false;
        }

        var groupStore = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == group.StoreId && !candidate.IsArchived && StoreBelongsToActiveWorkspace(candidate));
        if (groupStore is null)
        {
            error = "The destination store must be active in the current workspace.";
            return false;
        }

        storeId = groupStore.Id;
        nicheId = GroupHierarchy.GetEffectiveNiche(snapshot, group).Id;
        return true;
    }

    private Dictionary<string, string> ResolveCreationMetadata(
        WorkspaceSnapshot snapshot,
        ListingTopicReference topic,
        ListingContext context)
    {
        var resolution = _contextResolver.Resolve(new ToolContextResolveRequest(
            snapshot,
            ToolContextSelectionKind.Topic,
            topic.Kind,
            topic.Id));
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (resolution.IsAvailable)
        {
            foreach (var value in _contextResolver.ResolveCreationDefaults(resolution).Metadata)
            {
                metadata[value.Key] = value.Value;
                metadata[$"{ListingMetadataCodec.InheritedFromPrefix}{value.Key}"] = $"{value.Source.EntityKind}:{value.Source.EntityId}";
            }
        }

        ApplyContextMetadata(metadata, context, replaceExplicitMetadata: true);
        return metadata;
    }

    private IReadOnlyList<Guid> ResolveTagIds(
        WorkspaceSnapshot snapshot,
        Guid storeId,
        ListingTopicReference topic,
        ListingContext context)
    {
        var ids = context.TagIds is null ? new HashSet<Guid>() : ValidateTagIds(snapshot, storeId, context.TagIds).ToHashSet();
        var resolution = _contextResolver.Resolve(new ToolContextResolveRequest(snapshot, ToolContextSelectionKind.Topic, topic.Kind, topic.Id));
        if (resolution.IsAvailable)
        {
            foreach (var inherited in _contextResolver.ResolveCreationDefaults(resolution).Tags)
            {
                var tag = snapshot.Tags.SingleOrDefault(candidate => candidate.StoreId == storeId && !candidate.IsArchived && string.Equals(candidate.Name, inherited.Value, StringComparison.OrdinalIgnoreCase));
                if (tag is not null)
                {
                    ids.Add(tag.Id);
                }
            }
        }
        return ids.ToArray();
    }

    private static IReadOnlyList<Guid> ValidateTagIds(WorkspaceSnapshot snapshot, Guid storeId, IEnumerable<Guid> requested)
    {
        var ids = requested.Distinct().ToArray();
        if (ids.Any(id => id == Guid.Empty || !snapshot.Tags.Any(tag => tag.Id == id && tag.StoreId == storeId && !tag.IsArchived)))
        {
            throw new InvalidOperationException("Listing tags must be active tags in the listing store.");
        }
        return ids;
    }

    private static void ApplyContextMetadata(Dictionary<string, string> metadata, ListingContext context, bool replaceExplicitMetadata) =>
        ListingMetadataCodec.ApplyContextMetadata(metadata, context, replaceExplicitMetadata);

    private static ListingContext ToContext(WorkspaceSnapshot snapshot, Listing listing)
    {
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata.Remove(ListingMetadataCodec.NotesKey, out var notes);
        var explicitMetadata = metadata
            .Where(pair => !pair.Key.StartsWith(ListingMetadataCodec.InheritedFromPrefix, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var tagIds = snapshot.ListingTags.Where(link => link.ListingId == listing.Id).Select(link => link.TagId).ToArray();
        return new ListingContext(listing.Description, notes, explicitMetadata, tagIds);
    }

    private static ListingSummary ToSummary(WorkspaceSnapshot snapshot, Listing listing)
    {
        var topic = ToTopic(listing);
        var niche = ListingHierarchy.GetEffectiveNiche(snapshot, listing);
        var groupPath = listing.GroupId is Guid groupId
            ? GroupHierarchy.GetAncestors(snapshot, snapshot.Groups.Single(group => group.Id == groupId))
                .Select(group => group.Id)
                .Append(groupId)
                .ToArray()
            : [];
        var path = new[] { niche.Id }.Concat(groupPath).Append(listing.Id).ToArray();
        var names = new[] { niche.Name }
            .Concat(groupPath.Select(id => snapshot.Groups.Single(group => group.Id == id).Name))
            .Append(listing.Name);
        return new ListingSummary(
            listing.Id,
            listing.StoreId,
            niche.Id,
            topic,
            listing.Name,
            ToContext(snapshot, listing),
            listing.Status,
            listing.Stage,
            listing.IsArchived,
            ListingHierarchy.IsEffectivelyActive(snapshot, listing),
            path,
            string.Join(" / ", names),
            listing.CreatedAt,
            listing.UpdatedAt);
    }

    private static IReadOnlyList<ListingDestination> BuildDestinations(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is not Guid id)
        {
            return [];
        }

        var destinations = new List<ListingDestination>();
        foreach (var niche in snapshot.Niches.Where(candidate => candidate.StoreId == id && !candidate.IsArchived).OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase))
        {
            destinations.Add(new ListingDestination(new ListingTopicReference(WorkspaceEntityKind.Niche, niche.Id), id, niche.Id, niche.Name));
            foreach (var group in snapshot.Groups.Where(candidate => candidate.StoreId == id && GroupHierarchy.IsEffectivelyActive(snapshot, candidate) && GroupHierarchy.GetEffectiveNiche(snapshot, candidate).Id == niche.Id))
            {
                var path = GroupHierarchy.GetAncestors(snapshot, group).Select(ancestor => ancestor.Name).Append(group.Name);
                destinations.Add(new ListingDestination(new ListingTopicReference(WorkspaceEntityKind.Group, group.Id), id, niche.Id, $"{niche.Name} / {string.Join(" / ", path)}"));
            }
        }
        return destinations.OrderBy(destination => destination.DisplayPath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ListingTopicReference ToTopic(Listing listing) =>
        listing.GroupId is Guid groupId
            ? new ListingTopicReference(WorkspaceEntityKind.Group, groupId)
            : listing.NicheId is Guid nicheId
                ? new ListingTopicReference(WorkspaceEntityKind.Niche, nicheId)
                : throw new InvalidOperationException("A listing must belong beneath a niche or group.");

    private static string UniqueCopyName(WorkspaceSnapshot snapshot, string sourceName)
    {
        var names = snapshot.Listings.Select(listing => listing.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidate = $"{sourceName} Copy";
        if (!names.Contains(candidate))
        {
            return candidate;
        }
        for (var suffix = 2; ; suffix++)
        {
            candidate = $"{sourceName} Copy ({suffix})";
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private async Task<string?> TrySaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return $"The listing change could not be saved. {exception.Message}";
        }
    }

    private bool StoreBelongsToActiveWorkspace(Store store) => _activeWorkspaceId is null || store.WorkspaceId == _activeWorkspaceId;
    private void SetSelection(Guid storeId, Guid listingId) { _activeStoreId = storeId; _activeListingId = listingId; }
}
