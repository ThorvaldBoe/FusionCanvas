using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record GroupParentReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? Kind
        : throw new ArgumentException("A group parent must be a niche or group.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}

public sealed record GroupContext(string? Description = null, string? Notes = null);

public sealed record GroupManagementCreateRequest(GroupParentReference Parent, string Name, GroupContext? Context = null);

public sealed record GroupManagementUpdateRequest(Guid GroupId, string Name, GroupContext? Context = null);

public enum GroupPlacementKind
{
    Append = 0,
    Before = 1,
    After = 2
}

public sealed record GroupPlacement(GroupPlacementKind Kind = GroupPlacementKind.Append, Guid? RelativeGroupId = null);

public sealed record GroupManagementMoveRequest(
    Guid GroupId,
    GroupParentReference Destination,
    GroupPlacement? Placement = null);

public sealed record GroupManagementCopyRequest(Guid GroupId, GroupParentReference Destination);

public sealed record WorkspaceTreeSelection(WorkspaceEntityKind Kind, Guid Id);

public sealed record GroupCreationDestinationResult(GroupParentReference? Parent, string? Error)
{
    public bool Succeeded => Parent is not null;
}

public sealed record GroupSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    GroupParentReference Parent,
    string Name,
    GroupContext Context,
    bool IsArchived,
    bool IsEffectivelyActive,
    IReadOnlyList<Guid> Path,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int SortOrder);

public sealed record GroupDestination(GroupParentReference Parent, Guid StoreId, Guid NicheId, string DisplayPath);

public sealed record GroupManagementState(
    Guid? ActiveStoreId,
    Guid? ActiveNicheId,
    Guid? ActiveGroupId,
    GroupSummary? ActiveGroup,
    IReadOnlyList<GroupSummary> ActiveGroups,
    IReadOnlyList<GroupSummary> ArchivedGroupRoots,
    IReadOnlyList<GroupSummary> ArchivedGroups,
    IReadOnlyList<GroupDestination> ValidDestinations,
    bool NeedsFirstGroup);

public sealed record GroupManagementResult(
    bool Succeeded,
    string? Error,
    GroupSummary? Group,
    GroupManagementState State)
{
    public static GroupManagementResult Success(GroupSummary? group, GroupManagementState state) =>
        new(true, null, group, state);

    public static GroupManagementResult Failure(string error, GroupManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Group operation failed." : error, null, state);
}

public interface IGroupManagementService
{
    Guid? ActiveWorkspaceId { get; }
    Guid? ActiveStoreId { get; }
    Guid? ActiveNicheId { get; }
    Guid? ActiveGroupId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<GroupManagementState> LoadAsync(Guid? storeId, Guid? nicheId = null, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> CreateGroupAsync(GroupManagementCreateRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> UpdateGroupAsync(GroupManagementUpdateRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> MoveGroupAsync(GroupManagementMoveRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> CopyGroupAsync(GroupManagementCopyRequest request, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> ArchiveGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> RestoreGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> SelectGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupManagementResult> SetDefaultNicheAsync(Guid storeId, Guid nicheId, CancellationToken cancellationToken = default);
    Task<GroupCreationDestinationResult> ResolveCreateParentAsync(Guid storeId, WorkspaceTreeSelection? selection, CancellationToken cancellationToken = default);
}

public sealed class GroupManagementService : IGroupManagementService
{
    private const string NotesKey = "notes";

    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeNicheId;
    private Guid? _activeGroupId;

    public GroupManagementService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;
    public Guid? ActiveStoreId => _activeStoreId;
    public Guid? ActiveNicheId => _activeNicheId;
    public Guid? ActiveGroupId => _activeGroupId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        if (_activeWorkspaceId != workspaceId)
        {
            _activeStoreId = null;
            _activeNicheId = null;
            _activeGroupId = null;
        }

        _activeWorkspaceId = workspaceId;
    }

    public async Task<GroupManagementState> LoadAsync(
        Guid? storeId,
        Guid? nicheId = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId, nicheId);
    }

    public async Task<GroupManagementResult> CreateGroupAsync(
        GroupManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!TryResolveActiveParent(snapshot, request.Parent, out var storeId, out var nicheId, out var parentError))
        {
            return Failure(parentError!, snapshot, _activeStoreId, _activeNicheId);
        }

        var name = NormalizeName(request.Name);
        var nameError = ValidateName(snapshot, request.Parent, name, existingGroupId: null);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, storeId, nicheId);
        }

        var now = _clock();
        var context = request.Context ?? new GroupContext();
        var group = new TopicGroup(
            _newId(),
            storeId,
            request.Parent.Kind == WorkspaceEntityKind.Niche ? request.Parent.Id : null,
            request.Parent.Kind == WorkspaceEntityKind.Group ? request.Parent.Id : null,
            name,
            NormalizeOptional(context.Description),
            false,
            now,
            now,
            ToMetadataJson(context),
            NextSortOrder(snapshot.Groups, request.Parent));
        var updated = snapshot with { Groups = [.. snapshot.Groups, group] };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, storeId, nicheId);
        }

        SetSelection(storeId, nicheId, group.Id);
        return Success(group, updated);
    }

    public async Task<GroupManagementResult> UpdateGroupAsync(
        GroupManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Groups.SingleOrDefault(group => group.Id == request.GroupId);
        if (existing is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        var parent = ToParent(existing);
        var name = NormalizeName(request.Name);
        var nameError = ValidateName(snapshot, parent, name, existing.Id);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        var context = request.Context ?? ToContext(existing);
        var changed = existing with
        {
            Name = name,
            Description = NormalizeOptional(context.Description),
            MetadataJson = ToMetadataJson(context, existing.MetadataJson),
            UpdatedAt = _clock()
        };
        var updated = snapshot with
        {
            Groups = snapshot.Groups.Select(group => group.Id == changed.Id ? changed : group).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        return Success(changed, updated);
    }

    public async Task<GroupManagementResult> MoveGroupAsync(
        GroupManagementMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Groups.SingleOrDefault(group => group.Id == request.GroupId);
        if (existing is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        if (!GroupHierarchy.IsEffectivelyActive(snapshot, existing))
        {
            return Failure("Archived groups must be restored before they can be moved.", snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        if (!TryResolveActiveParent(snapshot, request.Destination, out var destinationStoreId, out var destinationNicheId, out var destinationError))
        {
            return Failure(destinationError!, snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        if (destinationStoreId != existing.StoreId)
        {
            return Failure("A group cannot be moved outside its store.", snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        if (request.Destination.Kind == WorkspaceEntityKind.Group &&
            (request.Destination.Id == existing.Id || GroupHierarchy.IsDescendant(snapshot, request.Destination.Id, existing.Id)))
        {
            return Failure("A group cannot be moved beneath itself or one of its descendants.", snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        var placement = request.Placement ?? new GroupPlacement();
        if (placement.Kind is GroupPlacementKind.Before or GroupPlacementKind.After &&
            (placement.RelativeGroupId is not Guid relativeId ||
             snapshot.Groups.SingleOrDefault(group => group.Id == relativeId) is not { } relativeGroup ||
             ToParent(relativeGroup) != request.Destination ||
             relativeGroup.Id == existing.Id))
        {
            return Failure("The relative group must be a different sibling at the destination.", snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        if (request.Placement is null &&
            ToParent(existing) == request.Destination &&
            placement.Kind == GroupPlacementKind.Append)
        {
            SetSelection(existing.StoreId, destinationNicheId, existing.Id);
            return Success(existing, snapshot);
        }

        var nameError = ValidateName(snapshot, request.Destination, existing.Name, existing.Id);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        var moved = existing with
        {
            NicheId = request.Destination.Kind == WorkspaceEntityKind.Niche ? request.Destination.Id : null,
            ParentGroupId = request.Destination.Kind == WorkspaceEntityKind.Group ? request.Destination.Id : null,
            UpdatedAt = _clock()
        };
        var subtreeIds = GroupHierarchy.GetDescendants(snapshot, existing).Select(group => group.Id).Append(existing.Id).ToHashSet();
        var reorderedGroups = PlaceGroup(snapshot.Groups, existing, moved, request.Destination, placement);
        moved = reorderedGroups.Single(group => group.Id == moved.Id);
        var updated = snapshot with
        {
            Groups = reorderedGroups,
            Listings = snapshot.Listings.Select(listing =>
                listing.GroupId is Guid groupId && subtreeIds.Contains(groupId)
                    ? listing with { NicheId = destinationNicheId }
                    : listing).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId, EffectiveNicheId(snapshot, existing));
        }

        SetSelection(destinationStoreId, destinationNicheId, moved.Id);
        return Success(moved, updated);
    }

    public async Task<GroupManagementResult> CopyGroupAsync(
        GroupManagementCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var source = snapshot.Groups.SingleOrDefault(group => group.Id == request.GroupId);
        if (source is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        if (!GroupHierarchy.IsEffectivelyActive(snapshot, source))
        {
            return Failure("Archived groups must be restored before they can be copied.", snapshot, source.StoreId, TryEffectiveNicheId(snapshot, source));
        }

        if (!TryResolveActiveParent(snapshot, request.Destination, out var storeId, out var nicheId, out var destinationError))
        {
            return Failure(destinationError!, snapshot, source.StoreId, TryEffectiveNicheId(snapshot, source));
        }

        if (storeId != source.StoreId)
        {
            return Failure("A group cannot be copied outside its store.", snapshot, source.StoreId, TryEffectiveNicheId(snapshot, source));
        }

        var rootName = UniqueCopyName(snapshot, request.Destination, source.Name);
        var now = _clock();
        var idMap = new Dictionary<Guid, Guid> { [source.Id] = _newId() };
        var descendants = GroupHierarchy.GetDescendants(snapshot, source)
            .OrderBy(group => GroupHierarchy.GetAncestors(snapshot, group).Count)
            .ThenBy(group => group.SortOrder)
            .ToArray();
        foreach (var descendant in descendants)
        {
            idMap[descendant.Id] = _newId();
        }

        var copies = new List<TopicGroup>
        {
            source with
            {
                Id = idMap[source.Id],
                NicheId = request.Destination.Kind == WorkspaceEntityKind.Niche ? request.Destination.Id : null,
                ParentGroupId = request.Destination.Kind == WorkspaceEntityKind.Group ? request.Destination.Id : null,
                Name = rootName,
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now,
                SortOrder = NextSortOrder(snapshot.Groups, request.Destination)
            }
        };
        copies.AddRange(descendants.Select(descendant => descendant with
        {
            Id = idMap[descendant.Id],
            NicheId = null,
            ParentGroupId = idMap[descendant.ParentGroupId!.Value],
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        }));

        var updated = snapshot with { Groups = [.. snapshot.Groups, .. copies] };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, source.StoreId, TryEffectiveNicheId(snapshot, source));
        }

        var copiedRoot = copies[0];
        SetSelection(storeId, nicheId, copiedRoot.Id);
        return Success(copiedRoot, updated);
    }

    public async Task<GroupManagementResult> ArchiveGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Groups.SingleOrDefault(group => group.Id == groupId);
        if (existing is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        var nicheId = EffectiveNicheId(snapshot, existing);
        var archived = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Groups = snapshot.Groups.Select(group => group.Id == archived.Id ? archived : group).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, existing.StoreId, nicheId);
        }

        var archivedIds = GroupHierarchy.GetDescendants(snapshot, existing).Select(group => group.Id).Append(existing.Id).ToHashSet();
        if (_activeGroupId is Guid activeGroupId && archivedIds.Contains(activeGroupId))
        {
            SetSelection(existing.StoreId, nicheId, null);
        }

        return Success(archived, updated);
    }

    public async Task<GroupManagementResult> RestoreGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Groups.SingleOrDefault(group => group.Id == groupId);
        if (existing is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        var parent = ToParent(existing);
        if (!TryResolveActiveParent(snapshot, parent, out var storeId, out var nicheId, out var parentError))
        {
            return Failure($"The preserved parent path must be active before restoration. {parentError}", snapshot, existing.StoreId, TryEffectiveNicheId(snapshot, existing));
        }

        var nameError = ValidateName(snapshot, parent, existing.Name, existing.Id);
        if (nameError is not null)
        {
            return Failure(nameError, snapshot, storeId, nicheId);
        }

        var restored = existing with { IsArchived = false, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Groups = snapshot.Groups.Select(group => group.Id == restored.Id ? restored : group).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, snapshot, storeId, nicheId);
        }

        SetSelection(storeId, nicheId, restored.Id);
        return Success(restored, updated);
    }

    public async Task<GroupManagementResult> SelectGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId);
        if (group is null)
        {
            return Failure("Group was not found.", snapshot, _activeStoreId, _activeNicheId);
        }

        if (!GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            return Failure("Archived groups must be restored before they can become active context.", snapshot, group.StoreId, TryEffectiveNicheId(snapshot, group));
        }

        if (!StoreBelongsToActiveWorkspace(snapshot.Stores.Single(store => store.Id == group.StoreId)))
        {
            return Failure("Group does not belong to the active workspace.", snapshot, _activeStoreId, _activeNicheId);
        }

        SetSelection(group.StoreId, EffectiveNicheId(snapshot, group), group.Id);
        return Success(group, snapshot);
    }

    public async Task<GroupManagementResult> SetDefaultNicheAsync(
        Guid storeId,
        Guid nicheId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId && !candidate.IsArchived && StoreBelongsToActiveWorkspace(candidate));
        var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId && candidate.StoreId == storeId && !candidate.IsArchived);
        if (store is null || niche is null)
        {
            return Failure("The default niche must be an active niche in the selected store.", snapshot, storeId, nicheId);
        }

        var changedStore = store with { DefaultNicheId = niche.Id, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(candidate => candidate.Id == store.Id ? changedStore : candidate).ToArray()
        };
        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return saveError is null
            ? GroupManagementResult.Success(null, BuildState(updated, storeId, nicheId))
            : Failure(saveError, snapshot, storeId, nicheId);
    }

    public async Task<GroupCreationDestinationResult> ResolveCreateParentAsync(
        Guid storeId,
        WorkspaceTreeSelection? selection,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId && !candidate.IsArchived && StoreBelongsToActiveWorkspace(candidate));
        if (store is null)
        {
            return new GroupCreationDestinationResult(null, "Select an active store before creating groups.");
        }

        if (selection is not null)
        {
            if (selection.Kind == WorkspaceEntityKind.Group &&
                snapshot.Groups.SingleOrDefault(group => group.Id == selection.Id && group.StoreId == store.Id) is { } group &&
                GroupHierarchy.IsEffectivelyActive(snapshot, group))
            {
                return new GroupCreationDestinationResult(new GroupParentReference(WorkspaceEntityKind.Group, group.Id), null);
            }

            if (selection.Kind == WorkspaceEntityKind.Listing &&
                snapshot.Listings.SingleOrDefault(listing => listing.Id == selection.Id && listing.StoreId == store.Id && !listing.IsArchived) is { } listing)
            {
                if (listing.GroupId is Guid groupId && snapshot.Groups.Any(group => group.Id == groupId && GroupHierarchy.IsEffectivelyActive(snapshot, group)))
                {
                    return new GroupCreationDestinationResult(new GroupParentReference(WorkspaceEntityKind.Group, groupId), null);
                }

                if (listing.NicheId is Guid listingNicheId && snapshot.Niches.Any(niche => niche.Id == listingNicheId && !niche.IsArchived))
                {
                    return new GroupCreationDestinationResult(new GroupParentReference(WorkspaceEntityKind.Niche, listingNicheId), null);
                }
            }

            if (selection.Kind == WorkspaceEntityKind.Niche &&
                snapshot.Niches.Any(niche => niche.Id == selection.Id && niche.StoreId == store.Id && !niche.IsArchived))
            {
                return new GroupCreationDestinationResult(new GroupParentReference(WorkspaceEntityKind.Niche, selection.Id), null);
            }
        }

        var activeNiches = snapshot.Niches.Where(niche => niche.StoreId == store.Id && !niche.IsArchived).ToArray();
        var defaultNiche = store.DefaultNicheId is Guid defaultNicheId
            ? activeNiches.SingleOrDefault(niche => niche.Id == defaultNicheId)
            : activeNiches.Length == 1 ? activeNiches[0] : null;
        return defaultNiche is null
            ? new GroupCreationDestinationResult(null, activeNiches.Length == 0
                ? "Create an active niche before creating groups."
                : "Select a niche or configure the store's default niche before creating groups.")
            : new GroupCreationDestinationResult(new GroupParentReference(WorkspaceEntityKind.Niche, defaultNiche.Id), null);
    }

    private GroupManagementResult Success(TopicGroup group, WorkspaceSnapshot snapshot)
    {
        var summary = ToSummary(snapshot, group);
        return GroupManagementResult.Success(summary, BuildState(snapshot, group.StoreId, summary.NicheId));
    }

    private GroupManagementResult Failure(string error, WorkspaceSnapshot snapshot, Guid? storeId, Guid? nicheId) =>
        GroupManagementResult.Failure(error, BuildState(snapshot, storeId, nicheId));

    private GroupManagementState BuildState(WorkspaceSnapshot snapshot, Guid? storeId, Guid? nicheId)
    {
        var activeStore = storeId is Guid selectedStoreId
            ? snapshot.Stores.SingleOrDefault(store => store.Id == selectedStoreId && !store.IsArchived && StoreBelongsToActiveWorkspace(store))
            : null;
        _activeStoreId = activeStore?.Id;

        var activeNiche = nicheId is Guid selectedNicheId && activeStore is not null
            ? snapshot.Niches.SingleOrDefault(niche => niche.Id == selectedNicheId && niche.StoreId == activeStore.Id && !niche.IsArchived)
            : null;
        if (nicheId is not null)
        {
            _activeNicheId = activeNiche?.Id;
        }
        else if (_activeNicheId is Guid currentNicheId &&
            (activeStore is null || snapshot.Niches.All(niche => niche.Id != currentNicheId || niche.StoreId != activeStore.Id || niche.IsArchived)))
        {
            _activeNicheId = null;
        }

        if (_activeGroupId is Guid selectedGroupId &&
            (snapshot.Groups.SingleOrDefault(group => group.Id == selectedGroupId) is not { } selectedGroup ||
             activeStore is null || selectedGroup.StoreId != activeStore.Id || !GroupHierarchy.IsEffectivelyActive(snapshot, selectedGroup)))
        {
            _activeGroupId = null;
        }

        var storeGroups = activeStore is null
            ? []
            : snapshot.Groups.Where(group => group.StoreId == activeStore.Id).ToArray();
        var activeGroups = storeGroups
            .Where(group => GroupHierarchy.IsEffectivelyActive(snapshot, group))
            .Select(group => ToSummary(snapshot, group))
            .OrderBy(group => string.Join('/', group.Path))
            .ToArray();
        var archivedRoots = storeGroups
            .Where(group => group.IsArchived && !GroupHierarchy.GetAncestors(snapshot, group).Any(ancestor => ancestor.IsArchived))
            .Select(group => ToSummary(snapshot, group))
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var archivedGroups = storeGroups
            .Where(group => group.IsArchived || GroupHierarchy.GetAncestors(snapshot, group).Any(ancestor => ancestor.IsArchived))
            .Select(group => ToSummary(snapshot, group))
            .OrderBy(group => group.DisplayPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var activeGroup = _activeGroupId is Guid groupId
            ? activeGroups.SingleOrDefault(group => group.Id == groupId)
            : null;

        var destinations = BuildDestinations(snapshot, activeStore, movingGroupId: null);
        var hasGroupsInNiche = _activeNicheId is Guid id && activeGroups.Any(group => group.NicheId == id);
        return new GroupManagementState(
            _activeStoreId,
            _activeNicheId,
            _activeGroupId,
            activeGroup,
            activeGroups,
            archivedRoots,
            archivedGroups,
            destinations,
            _activeNicheId is not null && !hasGroupsInNiche);
    }

    private static IReadOnlyList<GroupDestination> BuildDestinations(WorkspaceSnapshot snapshot, Store? store, Guid? movingGroupId)
    {
        if (store is null)
        {
            return [];
        }

        var excluded = movingGroupId is Guid id
            ? GroupHierarchy.GetDescendants(snapshot, snapshot.Groups.Single(group => group.Id == id)).Select(group => group.Id).Append(id).ToHashSet()
            : [];
        var destinations = new List<GroupDestination>();
        foreach (var niche in snapshot.Niches.Where(niche => niche.StoreId == store.Id && !niche.IsArchived).OrderBy(niche => niche.Name, StringComparer.OrdinalIgnoreCase))
        {
            destinations.Add(new GroupDestination(new GroupParentReference(WorkspaceEntityKind.Niche, niche.Id), store.Id, niche.Id, niche.Name));
            foreach (var group in snapshot.Groups.Where(group => group.StoreId == store.Id && GroupHierarchy.IsEffectivelyActive(snapshot, group) && GroupHierarchy.GetEffectiveNiche(snapshot, group).Id == niche.Id && !excluded.Contains(group.Id)))
            {
                var path = GroupHierarchy.GetAncestors(snapshot, group).Select(ancestor => ancestor.Name).Append(group.Name);
                destinations.Add(new GroupDestination(new GroupParentReference(WorkspaceEntityKind.Group, group.Id), store.Id, niche.Id, $"{niche.Name} / {string.Join(" / ", path)}"));
            }
        }

        return destinations;
    }

    private bool TryResolveActiveParent(
        WorkspaceSnapshot snapshot,
        GroupParentReference parent,
        out Guid storeId,
        out Guid nicheId,
        out string? error)
    {
        if (parent.Kind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == parent.Id);
            if (niche is null)
            {
                return FailParent("Destination niche was not found.", out storeId, out nicheId, out error);
            }

            var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == niche.StoreId);
            if (store is null || store.IsArchived || niche.IsArchived || !StoreBelongsToActiveWorkspace(store))
            {
                return FailParent("Destination niche and store must be active in the current workspace.", out storeId, out nicheId, out error);
            }

            storeId = store.Id;
            nicheId = niche.Id;
            error = null;
            return true;
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == parent.Id);
        if (group is null)
        {
            return FailParent("Destination group was not found.", out storeId, out nicheId, out error);
        }

        var destinationStore = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == group.StoreId);
        if (destinationStore is null || !StoreBelongsToActiveWorkspace(destinationStore) || !GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            return FailParent("Destination group and its ancestor path must be active in the current workspace.", out storeId, out nicheId, out error);
        }

        storeId = group.StoreId;
        nicheId = EffectiveNicheId(snapshot, group);
        error = null;
        return true;
    }

    private static bool FailParent(string message, out Guid storeId, out Guid nicheId, out string? error)
    {
        storeId = Guid.Empty;
        nicheId = Guid.Empty;
        error = message;
        return false;
    }

    private static string? ValidateName(WorkspaceSnapshot snapshot, GroupParentReference parent, string name, Guid? existingGroupId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Group name is required.";
        }

        var duplicate = snapshot.Groups.Any(group =>
            group.Id != existingGroupId &&
            !group.IsArchived &&
            ToParent(group) == parent &&
            string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase));
        return duplicate ? "An active sibling group already uses this name." : null;
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
            return $"The group change could not be saved: {exception.Message}";
        }
    }

    private void SetSelection(Guid storeId, Guid nicheId, Guid? groupId)
    {
        _activeStoreId = storeId;
        _activeNicheId = nicheId;
        _activeGroupId = groupId;
    }

    private bool StoreBelongsToActiveWorkspace(Store store) =>
        _activeWorkspaceId is null || store.WorkspaceId == _activeWorkspaceId;

    private static GroupSummary ToSummary(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        var niche = GroupHierarchy.GetEffectiveNiche(snapshot, group);
        return new GroupSummary(
            group.Id,
            group.StoreId,
            niche.Id,
            ToParent(group),
            group.Name,
            ToContext(group),
            group.IsArchived,
            GroupHierarchy.IsEffectivelyActive(snapshot, group),
            [niche.Id, .. GroupHierarchy.GetAncestors(snapshot, group).Select(ancestor => ancestor.Id), group.Id],
            $"{niche.Name} / {string.Join(" / ", GroupHierarchy.GetAncestors(snapshot, group).Select(ancestor => ancestor.Name).Append(group.Name))}",
            group.CreatedAt,
            group.UpdatedAt,
            group.SortOrder);
    }

    private static GroupParentReference ToParent(TopicGroup group) =>
        new(GroupHierarchy.GetDirectParentKind(group), GroupHierarchy.GetDirectParentId(group));

    private static Guid EffectiveNicheId(WorkspaceSnapshot snapshot, TopicGroup group) =>
        GroupHierarchy.GetEffectiveNiche(snapshot, group).Id;

    private static Guid? TryEffectiveNicheId(WorkspaceSnapshot snapshot, TopicGroup group)
    {
        try
        {
            return EffectiveNicheId(snapshot, group);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static GroupContext ToContext(TopicGroup group)
    {
        var metadata = ParseMetadata(group.MetadataJson);
        var notes = metadata.TryGetValue(NotesKey, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
        return new GroupContext(group.Description, notes);
    }

    private static string ToMetadataJson(GroupContext context, string existingMetadataJson = "{}")
    {
        var metadata = ParseMetadata(existingMetadataJson);
        var notes = NormalizeOptional(context.Notes);
        if (notes is null)
        {
            metadata.Remove(NotesKey);
        }
        else
        {
            metadata[NotesKey] = JsonSerializer.SerializeToElement(notes);
        }

        return metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata);
    }

    private static Dictionary<string, JsonElement> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal)
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string NormalizeName(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int NextSortOrder(IReadOnlyList<TopicGroup> groups, GroupParentReference parent) =>
        groups.Where(group => ToParent(group) == parent).Select(group => group.SortOrder).DefaultIfEmpty(-1).Max() + 1;

    private static IReadOnlyList<TopicGroup> PlaceGroup(
        IReadOnlyList<TopicGroup> groups,
        TopicGroup original,
        TopicGroup moved,
        GroupParentReference destination,
        GroupPlacement placement)
    {
        var oldParent = ToParent(original);
        var result = groups.Where(group => group.Id != original.Id).Append(moved).ToArray();
        var orders = new Dictionary<Guid, int>();

        void Normalize(GroupParentReference parent, TopicGroup? inserted, GroupPlacement requestedPlacement)
        {
            var siblings = result
                .Where(group => group.Id != inserted?.Id && ToParent(group) == parent)
                .OrderBy(group => group.SortOrder)
                .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var index = siblings.Count;
            if (requestedPlacement.Kind is GroupPlacementKind.Before or GroupPlacementKind.After)
            {
                var relativeIndex = siblings.FindIndex(group => group.Id == requestedPlacement.RelativeGroupId);
                if (relativeIndex < 0)
                {
                    throw new InvalidOperationException("The relative group must be a sibling at the destination.");
                }

                index = relativeIndex + (requestedPlacement.Kind == GroupPlacementKind.After ? 1 : 0);
            }

            if (inserted is not null)
            {
                siblings.Insert(index, inserted);
            }

            for (var siblingIndex = 0; siblingIndex < siblings.Count; siblingIndex++)
            {
                orders[siblings[siblingIndex].Id] = siblingIndex;
            }
        }

        if (oldParent != destination)
        {
            Normalize(oldParent, null, new GroupPlacement());
        }

        Normalize(destination, moved, placement);
        return result.Select(group => orders.TryGetValue(group.Id, out var order) ? group with { SortOrder = order } : group).ToArray();
    }

    private static string UniqueCopyName(WorkspaceSnapshot snapshot, GroupParentReference parent, string sourceName)
    {
        var names = snapshot.Groups
            .Where(group => !group.IsArchived && ToParent(group) == parent)
            .Select(group => group.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseName = $"{sourceName} copy";
        if (!names.Contains(baseName))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName} ({suffix})";
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
