using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record WorkspaceContext(string? Description = null, string? Notes = null);

public sealed record WorkspaceManagementCreateRequest(string Name, WorkspaceContext? Context = null);

public sealed record WorkspaceManagementUpdateRequest(Guid WorkspaceId, string Name, WorkspaceContext? Context = null);

public sealed record WorkspaceManagementDeleteRequest(
    Guid WorkspaceId,
    bool ConfirmPermanentDeletion,
    string? ConfirmationName = null);

public sealed record WorkspaceSummary(
    Guid Id,
    string Name,
    WorkspaceContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WorkspaceManagementState(
    IReadOnlyList<WorkspaceSummary> ActiveWorkspaces,
    IReadOnlyList<WorkspaceSummary> ArchivedWorkspaces,
    Guid? ActiveWorkspaceId,
    WorkspaceSummary? ActiveWorkspace,
    bool NeedsFirstWorkspace);

public sealed record WorkspaceManagementResult(
    bool Succeeded,
    string? Error,
    WorkspaceSummary? Workspace,
    WorkspaceManagementState State)
{
    public static WorkspaceManagementResult Success(WorkspaceSummary? workspace, WorkspaceManagementState state) =>
        new(true, null, workspace, state);

    public static WorkspaceManagementResult Failure(string error, WorkspaceManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Workspace operation failed." : error, null, state);
}

public interface IWorkspaceManagementService
{
    Guid? ActiveWorkspaceId { get; }

    Task<WorkspaceManagementState> LoadAsync(CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> CreateWorkspaceAsync(WorkspaceManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> UpdateWorkspaceAsync(WorkspaceManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> ArchiveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> RestoreWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> DeleteWorkspaceAsync(WorkspaceManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<WorkspaceManagementResult> SelectWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}

public sealed class WorkspaceManagementService : IWorkspaceManagementService
{
    private const string NotesKey = "notes";

    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;

    public WorkspaceManagementService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;

    public async Task<WorkspaceManagementState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalized = EnsureDefaultWorkspace(snapshot);
        if (!ReferenceEquals(normalized, snapshot))
        {
            await _repository.SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
        }

        return BuildState(normalized);
    }

    public async Task<WorkspaceManagementResult> CreateWorkspaceAsync(
        WorkspaceManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existingWorkspaceId: null);
        if (validation is not null)
        {
            return WorkspaceManagementResult.Failure(validation, BuildState(snapshot));
        }

        var now = _clock();
        var context = request.Context ?? new WorkspaceContext();
        var workspace = new FusionCanvas.Domain.Workspace.Workspace(
            _newId(),
            normalizedName,
            NormalizeOptional(context.Description),
            false,
            now,
            now,
            ToMetadataJson(context));
        var updated = snapshot with { Workspaces = [.. snapshot.Workspaces, workspace] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeWorkspaceId = workspace.Id;
        return WorkspaceManagementResult.Success(ToSummary(workspace), BuildState(updated));
    }

    public async Task<WorkspaceManagementResult> UpdateWorkspaceAsync(
        WorkspaceManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == request.WorkspaceId);
        if (existing is null)
        {
            return WorkspaceManagementResult.Failure("Workspace was not found.", BuildState(snapshot));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existing.Id);
        if (validation is not null)
        {
            return WorkspaceManagementResult.Failure(validation, BuildState(snapshot));
        }

        var context = request.Context ?? ToContext(existing);
        var updatedWorkspace = existing with
        {
            Name = normalizedName,
            Description = NormalizeOptional(context.Description),
            UpdatedAt = _clock(),
            MetadataJson = ToMetadataJson(context, existing.MetadataJson)
        };
        var updated = snapshot with
        {
            Workspaces = snapshot.Workspaces.Select(workspace => workspace.Id == updatedWorkspace.Id ? updatedWorkspace : workspace).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return WorkspaceManagementResult.Success(ToSummary(updatedWorkspace), BuildState(updated));
    }

    public async Task<WorkspaceManagementResult> ArchiveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == workspaceId);
        if (existing is null)
        {
            return WorkspaceManagementResult.Failure("Workspace was not found.", BuildState(snapshot));
        }

        var archived = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Workspaces = snapshot.Workspaces.Select(workspace => workspace.Id == archived.Id ? archived : workspace).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeWorkspaceId == workspaceId)
        {
            _activeWorkspaceId = null;
        }

        return WorkspaceManagementResult.Success(ToSummary(archived), BuildState(updated));
    }

    public async Task<WorkspaceManagementResult> RestoreWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == workspaceId);
        if (existing is null)
        {
            return WorkspaceManagementResult.Failure("Workspace was not found.", BuildState(snapshot));
        }

        var duplicate = snapshot.Workspaces.Any(workspace =>
            workspace.Id != existing.Id &&
            !workspace.IsArchived &&
            string.Equals(workspace.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return WorkspaceManagementResult.Failure("An active workspace already uses this name.", BuildState(snapshot));
        }

        var restored = existing with { IsArchived = false, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Workspaces = snapshot.Workspaces.Select(workspace => workspace.Id == restored.Id ? restored : workspace).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return WorkspaceManagementResult.Success(ToSummary(restored), BuildState(updated));
    }

    public async Task<WorkspaceManagementResult> DeleteWorkspaceAsync(
        WorkspaceManagementDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == request.WorkspaceId);
        if (existing is null)
        {
            return WorkspaceManagementResult.Failure("Workspace was not found.", BuildState(snapshot));
        }

        if (snapshot.Workspaces.Count <= 1)
        {
            return WorkspaceManagementResult.Failure("At least one workspace is required.", BuildState(snapshot));
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return WorkspaceManagementResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot));
        }

        var storeIds = snapshot.Stores
            .Where(store => store.WorkspaceId == existing.Id)
            .Select(store => store.Id)
            .ToHashSet();
        if (storeIds.Count > 0 &&
            !string.Equals(request.ConfirmationName?.Trim(), existing.Name, StringComparison.Ordinal))
        {
            return WorkspaceManagementResult.Failure("Type the workspace name to confirm deleting all stores and data in this workspace.", BuildState(snapshot));
        }

        var nicheIds = snapshot.Niches.Where(niche => storeIds.Contains(niche.StoreId)).Select(niche => niche.Id).ToHashSet();
        var groupIds = snapshot.Groups.Where(group => storeIds.Contains(group.StoreId)).Select(group => group.Id).ToHashSet();
        var itemIds = snapshot.Items.Where(listing => storeIds.Contains(listing.StoreId)).Select(listing => listing.Id).ToHashSet();
        var assetIds = snapshot.Assets.Where(asset => storeIds.Contains(asset.StoreId)).Select(asset => asset.Id).ToHashSet();
        var promptIds = snapshot.Prompts.Where(prompt => storeIds.Contains(prompt.StoreId)).Select(prompt => prompt.Id).ToHashSet();
        var tagIds = snapshot.Tags.Where(tag => storeIds.Contains(tag.StoreId)).Select(tag => tag.Id).ToHashSet();
        var removedEntityIds = new HashSet<Guid>(storeIds);
        removedEntityIds.UnionWith(nicheIds);
        removedEntityIds.UnionWith(groupIds);
        removedEntityIds.UnionWith(itemIds);
        removedEntityIds.UnionWith(assetIds);
        removedEntityIds.UnionWith(promptIds);
        removedEntityIds.UnionWith(tagIds);

        var updated = snapshot with
        {
            Workspaces = snapshot.Workspaces.Where(workspace => workspace.Id != existing.Id).ToArray(),
            Stores = snapshot.Stores.Where(store => store.WorkspaceId != existing.Id).ToArray(),
            Niches = snapshot.Niches.Where(niche => !storeIds.Contains(niche.StoreId)).ToArray(),
            Groups = snapshot.Groups.Where(group => !storeIds.Contains(group.StoreId)).ToArray(),
            Items = snapshot.Items.Where(listing => !storeIds.Contains(listing.StoreId)).ToArray(),
            Assets = snapshot.Assets.Where(asset => !storeIds.Contains(asset.StoreId)).ToArray(),
            Prompts = snapshot.Prompts.Where(prompt => !storeIds.Contains(prompt.StoreId)).ToArray(),
            Tags = snapshot.Tags.Where(tag => !storeIds.Contains(tag.StoreId)).ToArray(),
            ItemTags = snapshot.ItemTags
                .Where(listingTag => !itemIds.Contains(listingTag.ItemId) && !tagIds.Contains(listingTag.TagId))
                .ToArray(),
            AssetLinks = snapshot.AssetLinks
                .Where(assetLink => !assetIds.Contains(assetLink.AssetId) && !removedEntityIds.Contains(assetLink.EntityId))
                .ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeWorkspaceId == existing.Id)
        {
            _activeWorkspaceId = null;
        }

        return WorkspaceManagementResult.Success(ToSummary(existing), BuildState(updated));
    }

    public async Task<WorkspaceManagementResult> SelectWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == workspaceId);
        if (existing is null)
        {
            return WorkspaceManagementResult.Failure("Workspace was not found.", BuildState(snapshot));
        }

        if (existing.IsArchived)
        {
            return WorkspaceManagementResult.Failure("Archived workspaces must be restored before they can become active.", BuildState(snapshot));
        }

        _activeWorkspaceId = existing.Id;
        return WorkspaceManagementResult.Success(ToSummary(existing), BuildState(snapshot));
    }

    private WorkspaceManagementState BuildState(WorkspaceSnapshot snapshot)
    {
        if (_activeWorkspaceId is Guid activeWorkspaceId &&
            snapshot.Workspaces.SingleOrDefault(workspace => workspace.Id == activeWorkspaceId) is not { IsArchived: false })
        {
            _activeWorkspaceId = null;
        }

        var activeWorkspaces = snapshot.Workspaces
            .Where(workspace => !workspace.IsArchived)
            .OrderBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();
        var archivedWorkspaces = snapshot.Workspaces
            .Where(workspace => workspace.IsArchived)
            .OrderBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToArray();

        if (_activeWorkspaceId is null && activeWorkspaces.Length > 0)
        {
            _activeWorkspaceId = activeWorkspaces[0].Id;
        }

        var activeWorkspace = _activeWorkspaceId is Guid id
            ? activeWorkspaces.SingleOrDefault(workspace => workspace.Id == id)
            : null;

        return new WorkspaceManagementState(activeWorkspaces, archivedWorkspaces, _activeWorkspaceId, activeWorkspace, snapshot.Workspaces.Count == 0);
    }

    private static WorkspaceSnapshot EnsureDefaultWorkspace(WorkspaceSnapshot snapshot)
    {
        if (snapshot.Workspaces.Count > 0 || snapshot.Stores.Count == 0)
        {
            return snapshot;
        }

        var timestamp = snapshot.Stores.Min(store => store.CreatedAt);
        return snapshot with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(timestamp)] };
    }

    private static string? ValidateName(string name, WorkspaceSnapshot snapshot, Guid? existingWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Workspace name is required.";
        }

        var duplicate = snapshot.Workspaces.Any(workspace =>
            workspace.Id != existingWorkspaceId &&
            !workspace.IsArchived &&
            string.Equals(workspace.Name, name, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "An active workspace already uses this name." : null;
    }

    private static WorkspaceSummary ToSummary(FusionCanvas.Domain.Workspace.Workspace workspace) =>
        new(workspace.Id, workspace.Name, ToContext(workspace), workspace.IsArchived, workspace.CreatedAt, workspace.UpdatedAt);

    private static WorkspaceContext ToContext(FusionCanvas.Domain.Workspace.Workspace workspace)
    {
        var metadata = ParseMetadata(workspace.MetadataJson);
        return new WorkspaceContext(workspace.Description, metadata.GetValueOrDefault(NotesKey));
    }

    private static string ToMetadataJson(WorkspaceContext context, string existingMetadataJson = "{}")
    {
        var metadata = ParseMetadata(existingMetadataJson);
        SetOptional(metadata, NotesKey, context.Notes);
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
