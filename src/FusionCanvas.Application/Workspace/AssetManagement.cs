using System.IO;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Workspace;

public sealed record AssetContextReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is
        WorkspaceEntityKind.Item or
        WorkspaceEntityKind.Niche or
        WorkspaceEntityKind.Group or
        WorkspaceEntityKind.Store
        ? Kind
        : throw new ArgumentException("An asset context must be a listing, niche, group, or store.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}

public sealed record AssetContextDescriptor(
    Guid StoreId,
    AssetContextReference Reference,
    string DisplayName,
    string ContextKindLabel);

public sealed record AssetSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    AssetKind Kind,
    string WorkspaceRelativePath,
    string? OriginalSourcePath,
    string ManagedFileName,
    bool IsMissing,
    string? ContextLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AssetManagementState(
    AssetContextDescriptor? Context,
    IReadOnlyList<AssetSummary> Assets,
    IReadOnlyList<AssetKind> AvailablePurposes,
    string? Error)
{
    public static AssetManagementState Empty { get; } = new(null, [], AssetPurposePolicy.AvailablePurposes, null);
}

public sealed record AssetManagementImportRequest(AssetContextReference Context, string SourcePath, AssetKind Kind);
public sealed record AssetManagementRelabelRequest(Guid AssetId, AssetKind Kind);
public sealed record AssetManagementRemoveRequest(Guid AssetId, bool ConfirmPermanentRemoval);

public sealed record AssetManagementResult(
    bool Succeeded,
    string? Error,
    AssetSummary? Asset,
    AssetManagementState State)
{
    public static AssetManagementResult Success(AssetSummary? asset, AssetManagementState state) =>
        new(true, null, asset, state);

    public static AssetManagementResult Failure(string error, AssetManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Asset operation failed." : error, null, state);
}

public interface IAssetManagementService
{
    Guid? ActiveWorkspaceId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<AssetManagementState> LoadAsync(AssetContextReference context, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> ImportAssetAsync(AssetManagementImportRequest request, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> RelabelAssetAsync(AssetManagementRelabelRequest request, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> RemoveAssetAsync(AssetManagementRemoveRequest request, CancellationToken cancellationToken = default);
}

public static class AssetPurposePolicy
{
    private static readonly IReadOnlyDictionary<string, AssetKind> ExtensionMappings = new Dictionary<string, AssetKind>(StringComparer.OrdinalIgnoreCase)
    {
        [".afdesign"] = AssetKind.SourceDesign,
        [".psd"] = AssetKind.SourceDesign,
        [".ai"] = AssetKind.SourceDesign,
        [".eps"] = AssetKind.SourceDesign,
        [".png"] = AssetKind.ExportedImage,
        [".jpg"] = AssetKind.ExportedImage,
        [".jpeg"] = AssetKind.ExportedImage,
        [".webp"] = AssetKind.ExportedImage,
        [".tif"] = AssetKind.ExportedImage,
        [".tiff"] = AssetKind.ExportedImage,
        [".svg"] = AssetKind.Svg,
        [".ttf"] = AssetKind.Font,
        [".otf"] = AssetKind.Font,
        [".brush"] = AssetKind.Brush
    };

    public static IReadOnlyList<AssetKind> AvailablePurposes { get; } =
    [
        AssetKind.SourceDesign,
        AssetKind.ExportedImage,
        AssetKind.Svg,
        AssetKind.MockupImage,
        AssetKind.ReferenceImage,
        AssetKind.Texture,
        AssetKind.Brush,
        AssetKind.Font,
        AssetKind.Unknown,
        AssetKind.Other
    ];

    public static AssetKind SuggestKind(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return ExtensionMappings.TryGetValue(extension, out var kind) ? kind : AssetKind.Unknown;
    }
}

public sealed class AssetManagementService : IAssetManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;

    public AssetManagementService(
        IWorkspaceRepository repository,
        IWorkspaceFileStore fileStore,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;

    public void SetActiveWorkspace(Guid? workspaceId) => _activeWorkspaceId = workspaceId;

    public async Task<AssetManagementState> LoadAsync(AssetContextReference context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, context);
    }

    public async Task<AssetManagementResult> ImportAssetAsync(
        AssetManagementImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!TryResolveActiveContext(snapshot, request.Context, out var storeId, out var descriptor, out var contextError))
        {
            return Failure(contextError!, snapshot, request.Context);
        }

        if (string.IsNullOrWhiteSpace(request.SourcePath))
        {
            return Failure("A source file is required.", snapshot, request.Context);
        }

        ManagedWorkspaceFile managed;
        try
        {
            managed = await _fileStore.ImportAsync(request.SourcePath, request.Kind, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return Failure("The source file was not found.", snapshot, request.Context);
        }
        catch (DirectoryNotFoundException)
        {
            return Failure("The source file was not found.", snapshot, request.Context);
        }
        catch (NotSupportedException)
        {
            return Failure("The file type is not supported.", snapshot, request.Context);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure($"The file could not be imported. {exception.Message}", snapshot, request.Context);
        }

        var now = _clock();
        var asset = new Asset(
            _newId(),
            storeId,
            managed.Name,
            null,
            request.Kind,
            managed.WorkspaceRelativePath,
            managed.OriginalSourcePath,
            false,
            false,
            now,
            now,
            "{}");
        var link = new AssetLink(asset.Id, request.Context.Kind, request.Context.Id);

        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, link]
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            _fileStore.TryDelete(managed.WorkspaceRelativePath);
            return Failure(saveError, snapshot, request.Context);
        }

        var state = BuildState(updated, request.Context);
        var includeLabel = request.Context.Kind == WorkspaceEntityKind.Store;
        return AssetManagementResult.Success(ToSummary(updated, asset, descriptor, includeContextLabel: includeLabel, isMissing: false), state);
    }

    public async Task<AssetManagementResult> RelabelAssetAsync(
        AssetManagementRelabelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Assets.SingleOrDefault(asset => asset.Id == request.AssetId);
        if (existing is null)
        {
            return Failure("Asset was not found.", AssetManagementState.Empty);
        }

        var context = ResolveSingleContext(snapshot, existing);
        var now = _clock();
        var changed = new Asset(
            existing.Id,
            existing.StoreId,
            existing.Name,
            existing.Description,
            request.Kind,
            existing.WorkspaceRelativePath,
            existing.OriginalSourcePath,
            existing.IsMissing,
            existing.IsArchived,
            existing.CreatedAt,
            now,
            existing.MetadataJson);
        var updated = snapshot with
        {
            Assets = snapshot.Assets.Select(asset => asset.Id == changed.Id ? changed : asset).ToArray()
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, BuildState(snapshot, context));
        }

        var descriptor = context is null ? null : DescribeContext(snapshot, context);
        var includeLabel = context is null || context.Kind == WorkspaceEntityKind.Store;
        return AssetManagementResult.Success(ToSummary(updated, changed, descriptor, includeContextLabel: includeLabel, isMissing: IsMissing(changed)), BuildState(updated, context));
    }

    public async Task<AssetManagementResult> RemoveAssetAsync(
        AssetManagementRemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Assets.SingleOrDefault(asset => asset.Id == request.AssetId);
        if (existing is null)
        {
            return Failure("Asset was not found.", AssetManagementState.Empty);
        }

        if (!request.ConfirmPermanentRemoval)
        {
            return Failure("Permanent removal requires confirmation.", BuildState(snapshot, ResolveSingleContext(snapshot, existing)));
        }

        var context = ResolveSingleContext(snapshot, existing);
        var updated = snapshot with
        {
            Assets = snapshot.Assets.Where(asset => asset.Id != existing.Id).ToArray(),
            AssetLinks = snapshot.AssetLinks.Where(link => link.AssetId != existing.Id).ToArray()
        };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return Failure(saveError, BuildState(snapshot, context));
        }

        _fileStore.TryDelete(existing.WorkspaceRelativePath);
        return AssetManagementResult.Success(ToSummary(snapshot, existing, context is null ? null : DescribeContext(snapshot, context), includeContextLabel: context is null || context.Kind == WorkspaceEntityKind.Store, isMissing: false), BuildState(updated, context));
    }

    private AssetManagementState BuildState(WorkspaceSnapshot snapshot, AssetContextReference? context)
    {
        if (context is null)
        {
            return AssetManagementState.Empty;
        }

        if (!TryResolveActiveContext(snapshot, context, out _, out var descriptor, out var error))
        {
            return new AssetManagementState(null, [], AssetPurposePolicy.AvailablePurposes, error);
        }

        var includeContextLabel = context.Kind == WorkspaceEntityKind.Store;
        var assets = context.Kind == WorkspaceEntityKind.Store
            ? snapshot.Assets.Where(asset => asset.StoreId == descriptor!.StoreId).ToArray()
            : snapshot.AssetLinks
                .Where(link => link.EntityKind == context.Kind && link.EntityId == context.Id)
                .Join(snapshot.Assets, link => link.AssetId, asset => asset.Id, (_, asset) => asset)
                .ToArray();

        var summaries = assets
            .OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .Select(asset => ToSummary(snapshot, asset, descriptor, includeContextLabel, IsMissing(asset)))
            .ToArray();
        return new AssetManagementState(descriptor, summaries, AssetPurposePolicy.AvailablePurposes, null);
    }

    private static AssetSummary ToSummary(
        WorkspaceSnapshot snapshot,
        Asset asset,
        AssetContextDescriptor? descriptor,
        bool includeContextLabel,
        bool isMissing)
    {
        var contextLabel = includeContextLabel ? DeriveContextLabel(snapshot, asset) : null;
        return new AssetSummary(
            asset.Id,
            asset.StoreId,
            asset.Name,
            asset.Kind,
            asset.WorkspaceRelativePath,
            asset.OriginalSourcePath,
            Path.GetFileName(asset.WorkspaceRelativePath),
            isMissing,
            contextLabel,
            asset.CreatedAt,
            asset.UpdatedAt);
    }

    private bool IsMissing(Asset asset)
    {
        try
        {
            return !_fileStore.Exists(asset.WorkspaceRelativePath);
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static string? DeriveContextLabel(WorkspaceSnapshot snapshot, Asset asset)
    {
        var link = snapshot.AssetLinks.FirstOrDefault(candidate => candidate.AssetId == asset.Id);
        if (link is null)
        {
            return "—";
        }

        return link.EntityKind switch
        {
            WorkspaceEntityKind.Item => snapshot.Items.SingleOrDefault(candidate => candidate.Id == link.EntityId) is { } listing
                ? $"Item: {listing.Name}"
                : "—",
            WorkspaceEntityKind.Niche => snapshot.Niches.SingleOrDefault(candidate => candidate.Id == link.EntityId) is { } niche
                ? $"Niche: {niche.Name}"
                : "—",
            WorkspaceEntityKind.Group => snapshot.Groups.SingleOrDefault(candidate => candidate.Id == link.EntityId) is { } group
                ? $"Group: {group.Name}"
                : "—",
            WorkspaceEntityKind.Store => "Store",
            _ => "—"
        };
    }

    private static AssetContextReference? ResolveSingleContext(WorkspaceSnapshot snapshot, Asset asset)
    {
        var link = snapshot.AssetLinks.FirstOrDefault(candidate => candidate.AssetId == asset.Id);
        if (link is null)
        {
            return null;
        }

        return link.EntityKind is
            WorkspaceEntityKind.Item or
            WorkspaceEntityKind.Niche or
            WorkspaceEntityKind.Group or
            WorkspaceEntityKind.Store
            ? new AssetContextReference(link.EntityKind, link.EntityId)
            : null;
    }

    private bool TryResolveActiveContext(
        WorkspaceSnapshot snapshot,
        AssetContextReference context,
        out Guid storeId,
        out AssetContextDescriptor? descriptor,
        out string? error)
    {
        storeId = Guid.Empty;
        descriptor = null;
        error = null;

        switch (context.Kind)
        {
            case WorkspaceEntityKind.Store:
            {
                var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == context.Id);
                if (store is null || store.IsArchived || !StoreBelongsToActiveWorkspace(store))
                {
                    error = "The selected store must be active in the current workspace.";
                    return false;
                }

                storeId = store.Id;
                descriptor = new AssetContextDescriptor(store.Id, context, store.Name, "Store");
                return true;
            }
            case WorkspaceEntityKind.Niche:
            {
                var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == context.Id);
                if (niche is null || niche.IsArchived)
                {
                    error = "The selected niche must exist and be active.";
                    return false;
                }

                var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == niche.StoreId);
                if (store is null || store.IsArchived || !StoreBelongsToActiveWorkspace(store))
                {
                    error = "The selected store must be active in the current workspace.";
                    return false;
                }

                storeId = store.Id;
                descriptor = new AssetContextDescriptor(store.Id, context, niche.Name, "Niche");
                return true;
            }
            case WorkspaceEntityKind.Group:
            {
                var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == context.Id);
                if (group is null || !GroupHierarchy.IsEffectivelyActive(snapshot, group))
                {
                    error = "The selected group and its complete parent path must be active.";
                    return false;
                }

                var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == group.StoreId);
                if (store is null || store.IsArchived || !StoreBelongsToActiveWorkspace(store))
                {
                    error = "The selected store must be active in the current workspace.";
                    return false;
                }

                storeId = store.Id;
                descriptor = new AssetContextDescriptor(store.Id, context, group.Name, "Group");
                return true;
            }
            case WorkspaceEntityKind.Item:
            {
                var listing = snapshot.Items.SingleOrDefault(candidate => candidate.Id == context.Id);
                if (listing is null || !ItemHierarchy.IsEffectivelyActive(snapshot, listing))
                {
                    error = "The selected listing and its complete parent path must be active.";
                    return false;
                }

                var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == listing.StoreId);
                if (store is null || store.IsArchived || !StoreBelongsToActiveWorkspace(store))
                {
                    error = "The selected store must be active in the current workspace.";
                    return false;
                }

                storeId = store.Id;
                descriptor = new AssetContextDescriptor(store.Id, context, listing.Name, "Item");
                return true;
            }
            default:
                error = "The asset context is not supported.";
                return false;
        }
    }

    private static AssetContextDescriptor? DescribeContext(WorkspaceSnapshot snapshot, AssetContextReference context)
    {
        switch (context.Kind)
        {
            case WorkspaceEntityKind.Store:
                return snapshot.Stores.SingleOrDefault(candidate => candidate.Id == context.Id) is { } store
                    ? new AssetContextDescriptor(store.Id, context, store.Name, "Store")
                    : null;
            case WorkspaceEntityKind.Niche:
                return snapshot.Niches.SingleOrDefault(candidate => candidate.Id == context.Id) is { } niche
                    ? new AssetContextDescriptor(niche.StoreId, context, niche.Name, "Niche")
                    : null;
            case WorkspaceEntityKind.Group:
                return snapshot.Groups.SingleOrDefault(candidate => candidate.Id == context.Id) is { } group
                    ? new AssetContextDescriptor(group.StoreId, context, group.Name, "Group")
                    : null;
            case WorkspaceEntityKind.Item:
                return snapshot.Items.SingleOrDefault(candidate => candidate.Id == context.Id) is { } listing
                    ? new AssetContextDescriptor(listing.StoreId, context, listing.Name, "Item")
                    : null;
            default:
                return null;
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
            return $"The asset change could not be saved. {exception.Message}";
        }
    }

    private bool StoreBelongsToActiveWorkspace(Store store) =>
        _activeWorkspaceId is null || store.WorkspaceId == _activeWorkspaceId;

    private AssetManagementResult Failure(string error, AssetManagementState state) =>
        AssetManagementResult.Failure(error, state);

    private AssetManagementResult Failure(string error, WorkspaceSnapshot snapshot, AssetContextReference context)
    {
        var state = BuildState(snapshot, context);
        return AssetManagementResult.Failure(error, state with { Error = error });
    }
}
