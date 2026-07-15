using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record StoreContext(
    string? Description = null,
    string? Notes = null,
    string? TargetMarket = null,
    string? BrandDirection = null,
    string? PlanningContext = null);

public sealed record StoreManagementCreateRequest(string Name, StoreContext? Context = null);

public sealed record StoreManagementUpdateRequest(Guid StoreId, string Name, StoreContext? Context = null);

public sealed record StoreManagementDeleteRequest(Guid StoreId, bool ConfirmPermanentDeletion);

public sealed record StoreSummary(
    Guid Id,
    string Name,
    StoreContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record StoreManagementState(
    IReadOnlyList<StoreSummary> ActiveStores,
    IReadOnlyList<StoreSummary> ArchivedStores,
    Guid? ActiveStoreId,
    StoreSummary? ActiveStore,
    bool NeedsFirstStore);

public sealed record StoreManagementResult(
    bool Succeeded,
    string? Error,
    StoreSummary? Store,
    StoreManagementState State)
{
    public static StoreManagementResult Success(StoreSummary? store, StoreManagementState state) =>
        new(true, null, store, state);

    public static StoreManagementResult Failure(string error, StoreManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Store operation failed." : error, null, state);
}

public interface IStoreManagementService
{
    Guid? ActiveStoreId { get; }

    Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default);

    Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
}

public sealed class StoreManagementService : IStoreManagementService
{
    private const string NotesKey = "notes";
    private const string TargetMarketKey = "targetMarket";
    private const string BrandDirectionKey = "brandDirection";
    private const string PlanningContextKey = "planningContext";

    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeStoreId;

    public StoreManagementService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveStoreId => _activeStoreId;

    public async Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot);
    }

    public async Task<StoreManagementResult> CreateStoreAsync(
        StoreManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existingStoreId: null);
        if (validation is not null)
        {
            return StoreManagementResult.Failure(validation, BuildState(snapshot));
        }

        var now = _clock();
        var context = request.Context ?? new StoreContext();
        var store = new Store(_newId(), normalizedName, NormalizeOptional(context.Description), false, now, now, ToMetadataJson(context));
        var updated = snapshot with { Stores = [.. snapshot.Stores, store] };
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
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == request.StoreId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existing.Id);
        if (validation is not null)
        {
            return StoreManagementResult.Failure(validation, BuildState(snapshot));
        }

        var context = request.Context ?? ToContext(existing);
        var updatedStore = existing with
        {
            Name = normalizedName,
            Description = NormalizeOptional(context.Description),
            UpdatedAt = _clock(),
            MetadataJson = ToMetadataJson(context, existing.MetadataJson)
        };

        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(store => store.Id == updatedStore.Id ? updatedStore : store).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return StoreManagementResult.Success(ToSummary(updatedStore), BuildState(updated));
    }

    public async Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
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
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
        }

        var duplicate = snapshot.Stores.Any(store =>
            store.Id != existing.Id &&
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
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == request.StoreId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
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
        var existing = snapshot.Stores.SingleOrDefault(store => store.Id == storeId);
        if (existing is null)
        {
            return StoreManagementResult.Failure("Store was not found.", BuildState(snapshot));
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
        if (_activeStoreId is Guid activeStoreId &&
            snapshot.Stores.SingleOrDefault(store => store.Id == activeStoreId) is not { IsArchived: false })
        {
            _activeStoreId = null;
        }

        var activeStores = snapshot.Stores
            .Where(store => !store.IsArchived)
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
        var archivedStores = snapshot.Stores
            .Where(store => store.IsArchived)
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
        var activeStore = _activeStoreId is Guid id
            ? activeStores.SingleOrDefault(store => store.Id == id)
            : null;

        return new StoreManagementState(activeStores, archivedStores, _activeStoreId, activeStore, snapshot.Stores.Count == 0);
    }

    private static string? ValidateName(string name, WorkspaceSnapshot snapshot, Guid? existingStoreId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Store name is required.";
        }

        var duplicate = snapshot.Stores.Any(store =>
            store.Id != existingStoreId &&
            !store.IsArchived &&
            string.Equals(store.Name, name, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "An active store already uses this name." : null;
    }

    private static bool HasConnectedData(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var listingIds = snapshot.Listings.Where(listing => listing.StoreId == storeId).Select(listing => listing.Id).ToHashSet();
        var tagIds = snapshot.Tags.Where(tag => tag.StoreId == storeId).Select(tag => tag.Id).ToHashSet();
        var assetIds = snapshot.Assets.Where(asset => asset.StoreId == storeId).Select(asset => asset.Id).ToHashSet();

        return snapshot.Niches.Any(niche => niche.StoreId == storeId)
            || snapshot.Groups.Any(group => group.StoreId == storeId)
            || snapshot.Listings.Any(listing => listing.StoreId == storeId)
            || snapshot.Assets.Any(asset => asset.StoreId == storeId)
            || snapshot.Prompts.Any(prompt => prompt.StoreId == storeId)
            || snapshot.Tags.Any(tag => tag.StoreId == storeId)
            || snapshot.ListingTags.Any(link => listingIds.Contains(link.ListingId) || tagIds.Contains(link.TagId))
            || snapshot.AssetLinks.Any(link => assetIds.Contains(link.AssetId) || IsStoreScopedAssetTarget(snapshot, link, storeId));
    }

    private static bool IsStoreScopedAssetTarget(WorkspaceSnapshot snapshot, AssetLink link, Guid storeId) =>
        link.EntityKind switch
        {
            WorkspaceEntityKind.Store => link.EntityId == storeId,
            WorkspaceEntityKind.Niche => snapshot.Niches.Any(niche => niche.Id == link.EntityId && niche.StoreId == storeId),
            WorkspaceEntityKind.Group => snapshot.Groups.Any(group => group.Id == link.EntityId && group.StoreId == storeId),
            WorkspaceEntityKind.Listing => snapshot.Listings.Any(listing => listing.Id == link.EntityId && listing.StoreId == storeId),
            WorkspaceEntityKind.Asset => snapshot.Assets.Any(asset => asset.Id == link.EntityId && asset.StoreId == storeId),
            WorkspaceEntityKind.Prompt => snapshot.Prompts.Any(prompt => prompt.Id == link.EntityId && prompt.StoreId == storeId),
            _ => false
        };

    private static StoreSummary ToSummary(Store store) =>
        new(store.Id, store.Name, ToContext(store), store.IsArchived, store.CreatedAt, store.UpdatedAt);

    private static StoreContext ToContext(Store store)
    {
        var metadata = ParseMetadata(store.MetadataJson);
        return new StoreContext(
            store.Description,
            metadata.GetValueOrDefault(NotesKey),
            metadata.GetValueOrDefault(TargetMarketKey),
            metadata.GetValueOrDefault(BrandDirectionKey),
            metadata.GetValueOrDefault(PlanningContextKey));
    }

    private static string ToMetadataJson(StoreContext context, string existingMetadataJson = "{}")
    {
        var metadata = ParseMetadata(existingMetadataJson);
        SetOptional(metadata, NotesKey, context.Notes);
        SetOptional(metadata, TargetMarketKey, context.TargetMarket);
        SetOptional(metadata, BrandDirectionKey, context.BrandDirection);
        SetOptional(metadata, PlanningContextKey, context.PlanningContext);

        return metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata);
    }

    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    private static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
