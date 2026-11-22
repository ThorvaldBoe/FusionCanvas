using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.ArtworkStyles;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.ArtworkStyles;

public sealed class ArtworkStyleManagementService : IArtworkStyleManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public ArtworkStyleManagementService(
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

    public async Task<ArtworkStyleManagementState> LoadAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        EnsureWorkspaceExists(snapshot, workspaceId);
        return BuildState(snapshot, workspaceId);
    }

    public async Task<ArtworkStyleOperationResult> SaveAsync(
        Guid workspaceId,
        Guid? styleId,
        string title,
        string description,
        IReadOnlyList<ArtworkStyleImageSelection> images,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(images);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Workspaces.Any(value => value.Id == workspaceId))
            return ArtworkStyleOperationResult.Failure("Workspace was not found.");

        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim() ?? string.Empty;
        var existingStyle = styleId is Guid id
            ? snapshot.ArtworkStyles.SingleOrDefault(value => value.Id == id && value.WorkspaceId == workspaceId)
            : null;
        if (styleId is not null && existingStyle is null)
            return ArtworkStyleOperationResult.Failure("The selected artwork style was not found in this workspace.", BuildState(snapshot, workspaceId));
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            return ArtworkStyleOperationResult.Failure("Enter a style title.", BuildState(snapshot, workspaceId));
        if (string.IsNullOrWhiteSpace(normalizedDescription))
            return ArtworkStyleOperationResult.Failure("Enter a style description.", BuildState(snapshot, workspaceId));
        if (snapshot.ArtworkStyles.Any(value => value.WorkspaceId == workspaceId
                && value.Id != styleId
                && string.Equals(value.Title.Trim(), normalizedTitle, StringComparison.OrdinalIgnoreCase)))
            return ArtworkStyleOperationResult.Failure("A style with this title already exists in the workspace.", BuildState(snapshot, workspaceId));
        if (images.Count == 0)
            return ArtworkStyleOperationResult.Failure("Add at least one reference image.", BuildState(snapshot, workspaceId));

        var oldReferences = existingStyle is null
            ? []
            : snapshot.ArtworkStyleReferences.Where(value => value.StyleId == existingStyle.Id).OrderBy(value => value.SortOrder).ToArray();
        var oldByPath = oldReferences.ToDictionary(value => value.WorkspaceRelativePath, StringComparer.OrdinalIgnoreCase);
        var nextReferences = new List<ArtworkStyleReference>();
        var importedPaths = new List<string>();
        try
        {
            for (var index = 0; index < images.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var image = images[index];
                if (!string.IsNullOrWhiteSpace(image.SourcePath))
                {
                    var mediaType = MediaTypeForPath(image.SourcePath);
                    if (mediaType is null)
                    {
                        Cleanup(importedPaths);
                        return ArtworkStyleOperationResult.Failure("Reference images must be PNG, JPEG, or WebP files.", BuildState(snapshot, workspaceId));
                    }
                    var managed = await _fileStore.ImportAsync(image.SourcePath, AssetKind.ReferenceImage, cancellationToken).ConfigureAwait(false);
                    importedPaths.Add(managed.WorkspaceRelativePath);
                    nextReferences.Add(new ArtworkStyleReference(_newId(), styleId ?? Guid.Empty, managed.WorkspaceRelativePath, mediaType, index, _clock()));
                    continue;
                }

                var path = image.ExistingWorkspaceRelativePath;
                if (string.IsNullOrWhiteSpace(path) || !oldByPath.TryGetValue(path, out var oldReference))
                {
                    Cleanup(importedPaths);
                    return ArtworkStyleOperationResult.Failure("The reference image selection is invalid.", BuildState(snapshot, workspaceId));
                }
                nextReferences.Add(oldReference with { SortOrder = index });
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
        {
            Cleanup(importedPaths);
            return ArtworkStyleOperationResult.Failure($"A reference image could not be imported. {exception.Message}", BuildState(snapshot, workspaceId));
        }
        catch (OperationCanceledException)
        {
            Cleanup(importedPaths);
            throw;
        }

        if (nextReferences.Select(value => value.WorkspaceRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != nextReferences.Count)
        {
            Cleanup(importedPaths);
            return ArtworkStyleOperationResult.Failure("A reference image can be included only once in a style.", BuildState(snapshot, workspaceId));
        }

        var now = _clock();
        var savedStyle = existingStyle is null
            ? new ArtworkStyle(_newId(), workspaceId, normalizedTitle, normalizedDescription, now, now)
            : existingStyle with { Title = normalizedTitle, Description = normalizedDescription, UpdatedAt = now };
        nextReferences = nextReferences.Select(reference => reference with { StyleId = savedStyle.Id }).ToList();
        var updated = snapshot with
        {
            ArtworkStyles = [.. snapshot.ArtworkStyles.Where(value => value.Id != savedStyle.Id), savedStyle],
            ArtworkStyleReferences = [.. snapshot.ArtworkStyleReferences.Where(value => value.StyleId != savedStyle.Id), .. nextReferences]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Cleanup(importedPaths);
            return ArtworkStyleOperationResult.Failure($"The style could not be saved. {exception.Message}", BuildState(snapshot, workspaceId));
        }
        catch (OperationCanceledException)
        {
            Cleanup(importedPaths);
            throw;
        }

        var replacedPaths = oldReferences.Select(value => value.WorkspaceRelativePath)
            .Except(nextReferences.Select(value => value.WorkspaceRelativePath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        DeleteUnreferenced(updated, replacedPaths);
        return ArtworkStyleOperationResult.Success(BuildState(updated, workspaceId), savedStyle.Id);
    }

    public async Task<ArtworkStyleOperationResult> DeleteAsync(Guid workspaceId, Guid styleId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var style = snapshot.ArtworkStyles.SingleOrDefault(value => value.Id == styleId && value.WorkspaceId == workspaceId);
        if (style is null)
            return ArtworkStyleOperationResult.Failure("The selected artwork style was not found in this workspace.", BuildState(snapshot, workspaceId));

        var affectedItems = snapshot.Items.Where(item => item.StoreId is var storeId
                && snapshot.Stores.Any(store => store.Id == storeId && store.WorkspaceId == workspaceId))
            .Where(item => ItemMetadataCodec.ParseMetadata(item.MetadataJson).GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey) == styleId.ToString())
            .ToArray();
        var affectedIds = affectedItems.Select(item => item.Id).ToHashSet();
        var updatedItems = snapshot.Items.Select(item =>
        {
            if (!affectedIds.Contains(item.Id)) return item;
            var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
            metadata.Remove(ItemMetadataCodec.ArtworkStyleIdKey);
            return item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata), UpdatedAt = _clock() };
        }).ToArray();
        var removedPaths = snapshot.ArtworkStyleReferences.Where(value => value.StyleId == styleId).Select(value => value.WorkspaceRelativePath).ToArray();
        var updated = snapshot with
        {
            ArtworkStyles = [.. snapshot.ArtworkStyles.Where(value => value.Id != styleId)],
            ArtworkStyleReferences = [.. snapshot.ArtworkStyleReferences.Where(value => value.StyleId != styleId)],
            Items = updatedItems
        };
        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ArtworkStyleOperationResult.Failure($"The style could not be deleted. {exception.Message}", BuildState(snapshot, workspaceId));
        }

        DeleteUnreferenced(updated, removedPaths);
        return ArtworkStyleOperationResult.Success(BuildState(updated, workspaceId), affectedItemCount: affectedItems.Length);
    }

    private ArtworkStyleManagementState BuildState(WorkspaceSnapshot snapshot, Guid workspaceId)
    {
        var styles = snapshot.ArtworkStyles.Where(value => value.WorkspaceId == workspaceId).OrderBy(value => value.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        var references = styles.ToDictionary(
            style => style.Id,
            style => (IReadOnlyList<ArtworkStyleReferenceState>)snapshot.ArtworkStyleReferences
                .Where(value => value.StyleId == style.Id)
                .OrderBy(value => value.SortOrder)
                .Select(value => new ArtworkStyleReferenceState(value, SafeExists(value.WorkspaceRelativePath)))
                .ToArray());
        var counts = snapshot.Items
            .Select(item => Guid.TryParse(ItemMetadataCodec.ParseMetadata(item.MetadataJson).GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey), out var id) ? id : Guid.Empty)
            .Where(id => styles.Any(style => style.Id == id))
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());
        return new ArtworkStyleManagementState(workspaceId, styles, references) { AssignedItemCounts = counts };
    }

    private static void EnsureWorkspaceExists(WorkspaceSnapshot snapshot, Guid workspaceId)
    {
        if (!snapshot.Workspaces.Any(value => value.Id == workspaceId))
            throw new InvalidOperationException("Workspace was not found.");
    }

    private bool SafeExists(string path)
    {
        try { return _fileStore.Exists(path); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException) { return false; }
    }

    private void DeleteUnreferenced(WorkspaceSnapshot snapshot, IEnumerable<string> paths)
    {
        var referenced = snapshot.Assets.Select(value => value.WorkspaceRelativePath)
            .Concat(snapshot.ArtworkStyleReferences.Select(value => value.WorkspaceRelativePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase).Where(path => !referenced.Contains(path)))
            _fileStore.TryDelete(path);
    }

    private void Cleanup(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            _fileStore.TryDelete(path);
    }

    private static string? MediaTypeForPath(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => null
    };
}
