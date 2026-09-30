using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Application.Stores;

public sealed class StoreManagementService : IStoreManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IStoreContextMapper _contextMapper;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;

    public StoreManagementService(
        IWorkspaceRepository repository,
        IStoreContextMapper contextMapper,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null,
        Guid? initialActiveWorkspaceId = null,
        Guid? initialActiveStoreId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contextMapper = contextMapper ?? throw new ArgumentNullException(nameof(contextMapper));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
        _activeWorkspaceId = initialActiveWorkspaceId;
        _activeStoreId = initialActiveStoreId;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;

    public Guid? ActiveStoreId => _activeStoreId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        if (_activeWorkspaceId != workspaceId)
        {
            _activeStoreId = null;
        }

        _activeWorkspaceId = workspaceId;
    }

    public async Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        return BuildState(snapshot);
    }

    public async Task<StoreManagementResult> CreateStoreAsync(
        StoreManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (EnsureActiveWorkspace(snapshot) is null)
        {
            return StoreManagementResult.Failure("Active workspace is required before creating stores.", BuildState(snapshot));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existingStoreId: null);
        if (validation is not null)
        {
            return StoreManagementResult.Failure(validation, BuildState(snapshot));
        }

        if (!Enum.IsDefined(request.FulfillmentStrategy) || !FulfillmentStrategyPolicy.IsAvailable(request.FulfillmentStrategy))
        {
            return StoreManagementResult.Failure("That fulfillment strategy is not supported.", BuildState(snapshot));
        }

        var now = _clock();
        var context = request.Context ?? new StoreContext();
        var store = new Store(_newId(), _activeWorkspaceId!.Value, normalizedName, null, false, now, now, "{}", null, request.FulfillmentStrategy);
        store = _contextMapper.Apply(store, context with { Description = NormalizeOptional(context.Description) });
        var workspaces = snapshot.Workspaces.Any(workspace => workspace.Id == store.WorkspaceId)
            ? snapshot.Workspaces
            : [.. snapshot.Workspaces, WorkspaceSnapshot.DefaultWorkspace(now)];
        var updated = snapshot with { Workspaces = workspaces, Stores = [.. snapshot.Stores, store] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = store.Id;
        return StoreManagementResult.Success(ToSummary(store), BuildState(updated));
    }

    public async Task<StoreManagementResult> UpdateStoreAsync(
        StoreManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == request.StoreId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        if (existing.WorkspaceId != _activeWorkspaceId)
        {
            return StoreManagementResult.Failure("Store does not belong to the active workspace.", BuildState(snapshot));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existing.Id);
        if (validation is not null)
        {
            return StoreManagementResult.Failure(validation, BuildState(snapshot));
        }

        var context = request.Context ?? _contextMapper.Read(existing);
        var fulfillmentStrategy = request.FulfillmentStrategy ?? existing.FulfillmentStrategy;
        if (!Enum.IsDefined(fulfillmentStrategy) || !FulfillmentStrategyPolicy.IsAvailable(fulfillmentStrategy))
        {
            return StoreManagementResult.Failure("That fulfillment strategy is not supported.", BuildState(snapshot));
        }

        var updatedStore = _contextMapper.Apply(existing with
        {
            Name = normalizedName,
            FulfillmentStrategy = fulfillmentStrategy,
            UpdatedAt = _clock()
        }, context with { Description = NormalizeOptional(context.Description) });

        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(store => store.Id == updatedStore.Id ? updatedStore : store).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = updatedStore.Id;
        return StoreManagementResult.Success(ToSummary(updatedStore), BuildState(updated));
    }

    public async Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        if (existing.WorkspaceId != _activeWorkspaceId)
        {
            return StoreManagementResult.Failure("Store does not belong to the active workspace.", BuildState(snapshot));
        }

        var archived = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(store => store.Id == archived.Id ? archived : store).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeStoreId == storeId)
        {
            _activeStoreId = null;
        }

        return StoreManagementResult.Success(ToSummary(archived), BuildState(updated));
    }

    public async Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        if (existing.WorkspaceId != _activeWorkspaceId)
        {
            return StoreManagementResult.Failure("Store does not belong to the active workspace.", BuildState(snapshot));
        }

        var duplicate = snapshot.Stores.Any(store =>
            store.Id != existing.Id &&
            store.WorkspaceId == existing.WorkspaceId &&
            !store.IsArchived &&
            string.Equals(store.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return StoreManagementResult.Failure("An active store already uses this name.", BuildState(snapshot));
        }

        var restored = existing with { IsArchived = false, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(store => store.Id == restored.Id ? restored : store).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return StoreManagementResult.Success(ToSummary(restored), BuildState(updated));
    }

    public async Task<StoreManagementResult> DeleteStoreAsync(
        StoreManagementDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == request.StoreId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        if (existing.WorkspaceId != _activeWorkspaceId)
        {
            return StoreManagementResult.Failure("Store does not belong to the active workspace.", BuildState(snapshot));
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return StoreManagementResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot));
        }

        if (HasConnectedData(snapshot, existing.Id))
        {
            return StoreManagementResult.Failure("Store has connected data. Empty the store or archive it instead.", BuildState(snapshot));
        }

        var updated = snapshot with
        {
            Stores = snapshot.Stores.Where(store => store.Id != existing.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeStoreId == existing.Id)
        {
            _activeStoreId = null;
        }

        return StoreManagementResult.Success(ToSummary(existing), BuildState(updated));
    }

    public async Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureActiveWorkspace(snapshot);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        if (existing.WorkspaceId != _activeWorkspaceId)
        {
            return StoreManagementResult.Failure("Store does not belong to the active workspace.", BuildState(snapshot));
        }

        if (existing.IsArchived)
        {
            return StoreManagementResult.Failure("Archived stores must be restored before they can become active.", BuildState(snapshot));
        }

        _activeStoreId = existing.Id;
        return StoreManagementResult.Success(ToSummary(existing), BuildState(snapshot));
    }

    private StoreManagementState BuildState(WorkspaceSnapshot snapshot)
    {
        EnsureActiveWorkspace(snapshot);

        if (_activeStoreId is Guid activeStoreId &&
            snapshot.Stores.SingleOrDefault(store => store.Id == activeStoreId) is not { IsArchived: false } ||
            _activeStoreId is Guid activeId && snapshot.Stores.SingleOrDefault(store => store.Id == activeId)?.WorkspaceId != _activeWorkspaceId)
        {
            _activeStoreId = null;
        }

        var activeStores = snapshot.Stores
            .Where(store => store.WorkspaceId == _activeWorkspaceId && !store.IsArchived)
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
        var archivedStores = snapshot.Stores
            .Where(store => store.WorkspaceId == _activeWorkspaceId && store.IsArchived)
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
        var activeStore = _activeStoreId is Guid id
            ? activeStores.SingleOrDefault(store => store.Id == id)
            : null;

        return new StoreManagementState(_activeWorkspaceId, activeStores, archivedStores, _activeStoreId, activeStore, _activeWorkspaceId is not null && !snapshot.Stores.Any(store => store.WorkspaceId == _activeWorkspaceId));
    }

    private Guid? EnsureActiveWorkspace(WorkspaceSnapshot snapshot)
    {
        if (_activeWorkspaceId is Guid activeWorkspaceId &&
            snapshot.Workspaces.Any(workspace => workspace.Id == activeWorkspaceId && !workspace.IsArchived))
        {
            return _activeWorkspaceId;
        }

        _activeWorkspaceId = snapshot.Workspaces
            .Where(workspace => !workspace.IsArchived)
            .OrderBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .Select(workspace => (Guid?)workspace.Id)
            .FirstOrDefault();

        if (_activeWorkspaceId is null && snapshot.Workspaces.Count == 0)
        {
            _activeWorkspaceId = WorkspaceDefaults.DefaultWorkspaceId;
        }

        return _activeWorkspaceId;
    }

    private string? ValidateName(string name, WorkspaceSnapshot snapshot, Guid? existingStoreId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Store name is required.";
        }

        var duplicate = snapshot.Stores.Any(store =>
            store.Id != existingStoreId &&
            store.WorkspaceId == _activeWorkspaceId &&
            !store.IsArchived &&
            string.Equals(store.Name, name, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "An active store already uses this name." : null;
    }

    private static bool HasConnectedData(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var itemIds = snapshot.Items.Where(listing => listing.StoreId == storeId).Select(listing => listing.Id).ToHashSet();
        var tagIds = snapshot.Tags.Where(tag => tag.StoreId == storeId).Select(tag => tag.Id).ToHashSet();
        var assetIds = snapshot.Assets.Where(asset => asset.StoreId == storeId).Select(asset => asset.Id).ToHashSet();

        return snapshot.Niches.Any(niche => niche.StoreId == storeId)
            || snapshot.Groups.Any(group => group.StoreId == storeId)
            || snapshot.Items.Any(listing => listing.StoreId == storeId)
            || snapshot.Assets.Any(asset => asset.StoreId == storeId)
            || snapshot.Prompts.Any(prompt => prompt.StoreId == storeId)
            || snapshot.Tags.Any(tag => tag.StoreId == storeId)
            || snapshot.IdeationRejections.Any(rejection => rejection.StoreId == storeId)
            || snapshot.ItemTags.Any(link => itemIds.Contains(link.ItemId) || tagIds.Contains(link.TagId))
            || snapshot.AssetLinks.Any(link => assetIds.Contains(link.AssetId) || IsStoreScopedAssetTarget(snapshot, link, storeId));
    }

    private static bool IsStoreScopedAssetTarget(WorkspaceSnapshot snapshot, AssetLink link, Guid storeId) =>
        link.EntityKind switch
        {
            WorkspaceEntityKind.Store => link.EntityId == storeId,
            WorkspaceEntityKind.Niche => snapshot.Niches.Any(niche => niche.Id == link.EntityId && niche.StoreId == storeId),
            WorkspaceEntityKind.Group => snapshot.Groups.Any(group => group.Id == link.EntityId && group.StoreId == storeId),
            WorkspaceEntityKind.Item => snapshot.Items.Any(listing => listing.Id == link.EntityId && listing.StoreId == storeId),
            WorkspaceEntityKind.Asset => snapshot.Assets.Any(asset => asset.Id == link.EntityId && asset.StoreId == storeId),
            WorkspaceEntityKind.Prompt => snapshot.Prompts.Any(prompt => prompt.Id == link.EntityId && prompt.StoreId == storeId),
            _ => false
        };

    private StoreSummary ToSummary(Store store) =>
        new(store.Id, store.WorkspaceId, store.Name, _contextMapper.Read(store), store.IsArchived, store.CreatedAt, store.UpdatedAt, store.FulfillmentStrategy);

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
