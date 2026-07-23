using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ItemTopicReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? Kind
        : throw new ArgumentException("A listing topic must be a niche or group.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}

public sealed record ItemContext(
    string? Description = null,
    string? Notes = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<Guid>? TagIds = null);

public sealed record ItemManagementCreateRequest(ItemTopicReference Topic, string Name, ItemContext? Context = null);
public sealed record ItemManagementUpdateRequest(Guid ItemId, string Name, ItemContext? Context = null);
public sealed record ItemManagementMoveRequest(Guid ItemId, ItemTopicReference Destination);
public sealed record ItemManagementDuplicateRequest(Guid ItemId, ItemTopicReference? Destination = null);
public sealed record ItemManagementRestoreRequest(Guid ItemId, ItemTopicReference? Destination = null);
public sealed record ItemManagementDeleteRequest(Guid ItemId, bool ConfirmPermanentDeletion);
public sealed record ItemManagementSetStatusRequest(Guid ItemId, ItemStatus Status, bool ConfirmProtectedTransition = false);
public sealed record ItemManagementMoveStageRequest(Guid ItemId, WorkflowStage Stage);

public sealed record ItemCreationDestinationResult(ItemTopicReference? Topic, string? Error)
{
    public bool Succeeded => Topic is not null;
}

public sealed record ItemDestination(ItemTopicReference Topic, Guid StoreId, Guid NicheId, string DisplayPath);

public sealed record ItemSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    ItemTopicReference Topic,
    string Name,
    ItemContext Context,
    ItemStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    IReadOnlyList<Guid> Path,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ItemManagementState(
    Guid? ActiveStoreId,
    Guid? ActiveItemId,
    ItemSummary? ActiveItem,
    IReadOnlyList<ItemSummary> ActiveItems,
    IReadOnlyList<ItemSummary> ArchivedListings,
    IReadOnlyList<ItemDestination> ValidDestinations,
    bool NeedsFirstListing);

public sealed record ItemManagementResult(
    bool Succeeded,
    string? Error,
    ItemSummary? Item,
    ItemManagementState State)
{
    public static ItemManagementResult Success(ItemSummary? listing, ItemManagementState state) =>
        new(true, null, listing, state);

    public static ItemManagementResult Failure(string error, ItemManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Listing operation failed." : error, null, state);
}

public interface IItemManagementService
{
    Guid? ActiveWorkspaceId { get; }
    Guid? ActiveStoreId { get; }
    Guid? ActiveItemId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<ItemManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<ItemCreationDestinationResult> ResolveCreateTopicAsync(Guid storeId, WorkspaceTreeSelection? selection, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> CreateItemAsync(ItemManagementCreateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> UpdateItemAsync(ItemManagementUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> MoveItemAsync(ItemManagementMoveRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> DuplicateItemAsync(ItemManagementDuplicateRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> RestoreItemAsync(ItemManagementRestoreRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> DeleteItemAsync(ItemManagementDeleteRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> SetItemStatusAsync(ItemManagementSetStatusRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> MoveItemStageAsync(ItemManagementMoveStageRequest request, CancellationToken cancellationToken = default);
    Task<ItemManagementResult> SelectItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public sealed class ItemManagementService : IItemManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IToolContextResolver _contextResolver;
    private readonly Func<DateTimeOffset> _clock;
    private readonly IItemIdGenerator _idGenerator;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeItemId;

    public ItemManagementService(
        IWorkspaceRepository repository,
        IToolContextResolver? contextResolver = null,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
        : this(repository, contextResolver, clock, newId is null ? null : new DelegateItemIdGenerator(newId))
    {
    }

    public ItemManagementService(
        IWorkspaceRepository repository,
        IToolContextResolver? contextResolver,
        Func<DateTimeOffset>? clock,
        IItemIdGenerator? idGenerator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contextResolver = contextResolver ?? new ToolContextResolver();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _idGenerator = idGenerator ?? new GuidItemIdGenerator();
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;
    public Guid? ActiveStoreId => _activeStoreId;
    public Guid? ActiveItemId => _activeItemId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        if (_activeWorkspaceId != workspaceId)
        {
            _activeStoreId = null;
            _activeItemId = null;
        }

        _activeWorkspaceId = workspaceId;
    }

    public async Task<ItemManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<ItemCreationDestinationResult> ResolveCreateTopicAsync(
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
                WorkspaceEntityKind.Niche => new ItemTopicReference(WorkspaceEntityKind.Niche, selection.Id),
                WorkspaceEntityKind.Group => new ItemTopicReference(WorkspaceEntityKind.Group, selection.Id),
                WorkspaceEntityKind.Item => snapshot.Items.SingleOrDefault(candidate => candidate.Id == selection.Id) is { } listing
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
            : new(new ItemTopicReference(WorkspaceEntityKind.Niche, defaultNiche.Id), null);
    }

    public async Task<ItemManagementResult> CreateItemAsync(
        ItemManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!TryResolveActiveTopic(snapshot, request.Topic, out var storeId, out var nicheId, out var topicError))
        {
            return Failure(topicError!, snapshot, _activeStoreId);
        }

        var name = ItemMetadataCodec.NormalizeName(request.Name);
        var nameError = ItemMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, storeId);
        }

        var now = _clock();
        var context = request.Context ?? new ItemContext();
        var metadata = ResolveCreationMetadata(snapshot, request.Topic, context);
        var itemId = _idGenerator.NewId();
        if (itemId == Guid.Empty)
        {
            return Failure("Item identity could not be generated. Try creating the item again.", snapshot, storeId);
        }

        var listing = new Item(
            itemId,
            storeId,
            nicheId,
            request.Topic.Kind == WorkspaceEntityKind.Group ? request.Topic.Id : null,
            name,
            ItemMetadataCodec.NormalizeOptional(context.Description),
            ItemStatus.Draft,
            WorkflowStage.Idea,
            false,
            now,
            now,
            ItemMetadataCodec.SerializeMetadata(metadata));

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
            Items = [.. snapshot.Items, listing],
            ItemTags = [.. snapshot.ItemTags, .. tagIds.Select(tagId => new ItemTag(listing.Id, tagId))]
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, storeId);
        }

        SetSelection(storeId, listing.Id);
        return Success(listing, updated);
    }

    public async Task<ItemManagementResult> UpdateItemAsync(
        ItemManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        var name = ItemMetadataCodec.NormalizeName(request.Name);
        var nameError = ItemMetadataCodec.ValidateName(name);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, existing.StoreId);
        }

        var context = request.Context ?? ToContext(snapshot, existing);
        var metadata = ItemMetadataCodec.ParseMetadata(existing.MetadataJson);
        ApplyContextMetadata(metadata, context, replaceExplicitMetadata: context.Metadata is not null);
        var changed = existing with
        {
            Name = name,
            Description = ItemMetadataCodec.NormalizeOptional(context.Description),
            MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        IReadOnlyList<ItemTag> listingTags;
        try
        {
            listingTags = context.TagIds is null
                ? snapshot.ItemTags
                : [.. snapshot.ItemTags.Where(link => link.ItemId != existing.Id), .. ValidateTagIds(snapshot, existing.StoreId, context.TagIds).Select(tagId => new ItemTag(existing.Id, tagId))];
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message, snapshot, existing.StoreId);
        }
        var updated = snapshot with
        {
            Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray(),
            ItemTags = listingTags
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ItemManagementResult> MoveItemAsync(
        ItemManagementMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ItemHierarchy.IsEffectivelyActive(snapshot, existing))
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
        var updated = snapshot with { Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ItemManagementResult> DuplicateItemAsync(
        ItemManagementDuplicateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var source = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (source is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ItemHierarchy.IsEffectivelyActive(snapshot, source))
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
            Id = _idGenerator.NewId(),
            NicheId = nicheId,
            GroupId = destination.Kind == WorkspaceEntityKind.Group ? destination.Id : null,
            Name = UniqueCopyName(snapshot, source.Name),
            Status = ItemStatus.Draft,
            Stage = WorkflowStage.Idea,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        var sourceTagIds = snapshot.ItemTags.Where(link => link.ItemId == source.Id).Select(link => link.TagId);
        var updated = snapshot with
        {
            Items = [.. snapshot.Items, duplicate],
            ItemTags = [.. snapshot.ItemTags, .. sourceTagIds.Select(tagId => new ItemTag(duplicate.Id, tagId))]
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, source.StoreId);
        }

        SetSelection(source.StoreId, duplicate.Id);
        return Success(duplicate, updated);
    }

    public Task<ItemManagementResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        SetArchiveStateAsync(itemId, true, destination: null, cancellationToken);

    public Task<ItemManagementResult> RestoreItemAsync(ItemManagementRestoreRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SetArchiveStateAsync(request.ItemId, false, request.Destination, cancellationToken);
    }

    public async Task<ItemManagementResult> DeleteItemAsync(
        ItemManagementDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return Failure("Permanent deletion requires confirmation.", snapshot, existing.StoreId);
        }

        if (snapshot.Prompts.Any(prompt => prompt.ItemId == existing.Id) ||
            snapshot.AssetLinks.Any(link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == existing.Id))
        {
            return Failure("Listing has connected prompts or assets. Detach them or archive the listing instead.", snapshot, existing.StoreId);
        }

        var updated = snapshot with
        {
            Items = snapshot.Items.Where(candidate => candidate.Id != existing.Id).ToArray(),
            ItemTags = snapshot.ItemTags.Where(link => link.ItemId != existing.Id).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        if (_activeItemId == existing.Id)
        {
            _activeItemId = null;
        }
        _activeStoreId = existing.StoreId;
        return ItemManagementResult.Success(ToSummary(snapshot, existing), BuildState(updated, existing.StoreId));
    }

    public async Task<ItemManagementResult> SetItemStatusAsync(
        ItemManagementSetStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Status))
        {
            return Failure("Unknown listing lifecycle status.", await _repository.LoadAsync(cancellationToken).ConfigureAwait(false), _activeStoreId);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return Failure("Item was not found.", snapshot, _activeStoreId);
        }

        if (existing.Status == request.Status)
        {
            SetSelection(existing.StoreId, existing.Id);
            return Success(existing, snapshot);
        }

        var decision = ItemWorkflowPolicy.DecideTransition(existing.Status, existing.Stage, request.Status);
        if (!decision.IsAllowed)
        {
            return Failure(decision.Reason, snapshot, existing.StoreId);
        }

        if (decision.RequiresConfirmation && !request.ConfirmProtectedTransition)
        {
            return Failure($"{decision.Reason} Confirm the protected status change to continue.", snapshot, existing.StoreId);
        }

        var changed = existing with { Status = request.Status, UpdatedAt = _clock() };
        var updated = snapshot with { Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ItemManagementResult> MoveItemStageAsync(
        ItemManagementMoveStageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Stage))
        {
            return Failure("Unknown workflow stage.", await _repository.LoadAsync(cancellationToken).ConfigureAwait(false), _activeStoreId);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == request.ItemId);
        if (existing is null)
        {
            return Failure("Item was not found.", snapshot, _activeStoreId);
        }

        var movementDecision = ItemWorkflowPolicy.CanPerformOperation(existing, ItemOperationKind.StageMovement);
        if (!movementDecision.IsAllowed)
        {
            return Failure(movementDecision.Reason, snapshot, existing.StoreId);
        }

        if (existing.Stage == request.Stage)
        {
            SetSelection(existing.StoreId, existing.Id);
            return Success(existing, snapshot);
        }

        if (!ItemWorkflowPolicy.CanMoveAdjacent(existing.Stage, request.Stage))
        {
            return Failure("Stage movement is only allowed between adjacent workflow stages.", snapshot, existing.StoreId);
        }

        var changed = existing with { Stage = request.Stage, UpdatedAt = _clock() };
        var updated = snapshot with { Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        SetSelection(existing.StoreId, existing.Id);
        return Success(changed, updated);
    }

    public async Task<ItemManagementResult> SelectItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (listing is null)
        {
            return Failure("Listing was not found.", snapshot, _activeStoreId);
        }

        if (!ItemHierarchy.IsEffectivelyActive(snapshot, listing) || !StoreBelongsToActiveWorkspace(snapshot.Stores.Single(store => store.Id == listing.StoreId)))
        {
            return Failure("Archived listings or listings beneath inactive topics cannot become active context.", snapshot, listing.StoreId);
        }

        SetSelection(listing.StoreId, listing.Id);
        return Success(listing, snapshot);
    }

    private async Task<ItemManagementResult> SetArchiveStateAsync(
        Guid itemId,
        bool archived,
        ItemTopicReference? destination,
        CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
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
            nicheId = ItemHierarchy.GetEffectiveNiche(snapshot, existing).Id;
        }

        var changed = existing with
        {
            NicheId = nicheId,
            GroupId = topic.Kind == WorkspaceEntityKind.Group ? topic.Id : null,
            IsArchived = archived,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Items = snapshot.Items.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId);
        }

        if (archived)
        {
            if (_activeItemId == existing.Id)
            {
                _activeItemId = null;
            }
            _activeStoreId = existing.StoreId;
        }
        else
        {
            SetSelection(existing.StoreId, existing.Id);
        }

        return Success(changed, updated);
    }

    private ItemManagementState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
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

        if (_activeItemId is Guid activeItemId &&
            snapshot.Items.SingleOrDefault(candidate => candidate.Id == activeItemId) is not { } activeListing ||
            _activeItemId is Guid selectedId && snapshot.Items.SingleOrDefault(candidate => candidate.Id == selectedId) is { } selected &&
            (_activeStoreId != selected.StoreId || !ItemHierarchy.IsEffectivelyActive(snapshot, selected)))
        {
            _activeItemId = null;
        }

        var storeListings = _activeStoreId is Guid id
            ? snapshot.Items.Where(listing => listing.StoreId == id).ToArray()
            : [];
        var active = storeListings
            .Where(listing => ItemHierarchy.IsEffectivelyActive(snapshot, listing))
            .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
            .Select(listing => ToSummary(snapshot, listing))
            .ToArray();
        var archived = storeListings
            .Where(listing => !ItemHierarchy.IsEffectivelyActive(snapshot, listing))
            .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
            .Select(listing => ToSummary(snapshot, listing))
            .ToArray();
        var selectedSummary = _activeItemId is Guid itemId ? active.SingleOrDefault(listing => listing.Id == itemId) : null;
        return new ItemManagementState(
            _activeStoreId,
            _activeItemId,
            selectedSummary,
            active,
            archived,
            BuildDestinations(snapshot, _activeStoreId),
            _activeStoreId is not null && active.Length == 0);
    }

    private ItemManagementResult Success(Item listing, WorkspaceSnapshot snapshot) =>
        ItemManagementResult.Success(ToSummary(snapshot, listing), BuildState(snapshot, listing.StoreId));

    private ItemManagementResult Failure(string error, WorkspaceSnapshot snapshot, Guid? storeId) =>
        ItemManagementResult.Failure(error, BuildState(snapshot, storeId));

    private bool TryResolveActiveTopic(
        WorkspaceSnapshot snapshot,
        ItemTopicReference topic,
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
        ItemTopicReference topic,
        ItemContext context)
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
                metadata[$"{ItemMetadataCodec.InheritedFromPrefix}{value.Key}"] = $"{value.Source.EntityKind}:{value.Source.EntityId}";
            }
        }

        ApplyContextMetadata(metadata, context, replaceExplicitMetadata: true);
        return metadata;
    }

    private IReadOnlyList<Guid> ResolveTagIds(
        WorkspaceSnapshot snapshot,
        Guid storeId,
        ItemTopicReference topic,
        ItemContext context)
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

    private static void ApplyContextMetadata(Dictionary<string, string> metadata, ItemContext context, bool replaceExplicitMetadata) =>
        ItemMetadataCodec.ApplyContextMetadata(metadata, context, replaceExplicitMetadata);

    private static ItemContext ToContext(WorkspaceSnapshot snapshot, Item listing)
    {
        var metadata = ItemMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata.Remove(ItemMetadataCodec.NotesKey, out var notes);
        var explicitMetadata = metadata
            .Where(pair => !pair.Key.StartsWith(ItemMetadataCodec.InheritedFromPrefix, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var tagIds = snapshot.ItemTags.Where(link => link.ItemId == listing.Id).Select(link => link.TagId).ToArray();
        return new ItemContext(listing.Description, notes, explicitMetadata, tagIds);
    }

    private static ItemSummary ToSummary(WorkspaceSnapshot snapshot, Item listing)
    {
        var topic = ToTopic(listing);
        var niche = ItemHierarchy.GetEffectiveNiche(snapshot, listing);
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
        return new ItemSummary(
            listing.Id,
            listing.StoreId,
            niche.Id,
            topic,
            listing.Name,
            ToContext(snapshot, listing),
            listing.Status,
            listing.Stage,
            listing.IsArchived,
            ItemHierarchy.IsEffectivelyActive(snapshot, listing),
            path,
            string.Join(" / ", names),
            listing.CreatedAt,
            listing.UpdatedAt);
    }

    private static IReadOnlyList<ItemDestination> BuildDestinations(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is not Guid id)
        {
            return [];
        }

        var destinations = new List<ItemDestination>();
        foreach (var niche in snapshot.Niches.Where(candidate => candidate.StoreId == id && !candidate.IsArchived).OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase))
        {
            destinations.Add(new ItemDestination(new ItemTopicReference(WorkspaceEntityKind.Niche, niche.Id), id, niche.Id, niche.Name));
            foreach (var group in snapshot.Groups.Where(candidate => candidate.StoreId == id && GroupHierarchy.IsEffectivelyActive(snapshot, candidate) && GroupHierarchy.GetEffectiveNiche(snapshot, candidate).Id == niche.Id))
            {
                var path = GroupHierarchy.GetAncestors(snapshot, group).Select(ancestor => ancestor.Name).Append(group.Name);
                destinations.Add(new ItemDestination(new ItemTopicReference(WorkspaceEntityKind.Group, group.Id), id, niche.Id, $"{niche.Name} / {string.Join(" / ", path)}"));
            }
        }
        return destinations.OrderBy(destination => destination.DisplayPath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ItemTopicReference ToTopic(Item listing) =>
        listing.GroupId is Guid groupId
            ? new ItemTopicReference(WorkspaceEntityKind.Group, groupId)
            : listing.NicheId is Guid nicheId
                ? new ItemTopicReference(WorkspaceEntityKind.Niche, nicheId)
                : throw new InvalidOperationException("A listing must belong beneath a niche or group.");

    private static string UniqueCopyName(WorkspaceSnapshot snapshot, string sourceName)
    {
        var names = snapshot.Items.Select(listing => listing.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
    private void SetSelection(Guid storeId, Guid itemId) { _activeStoreId = storeId; _activeItemId = itemId; }
}
