using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupOutputInvalidationService : IMockupOutputInvalidationService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileOutputStore _fileStore;

    public MockupOutputInvalidationService(IWorkspaceRepository repository, IWorkspaceFileOutputStore fileStore)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    public async Task<MockupOutputInvalidationResult> InvalidateAsync(
        Guid itemId,
        IReadOnlySet<Guid>? sourceDesignAssetIds = null,
        bool invalidateAll = false,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var outputAssets = snapshot.AssetLinks
            .Where(link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == itemId)
            .Join(snapshot.Assets, link => link.AssetId, asset => asset.Id, (_, asset) => asset)
            .Where(asset => asset.Kind == AssetKind.MockupImage)
            .Where(asset => invalidateAll || MatchesSource(asset, sourceDesignAssetIds))
            .ToArray();

        if (outputAssets.Length == 0)
        {
            return new(true, 0);
        }

        var outputIds = outputAssets.Select(asset => asset.Id).ToHashSet();
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        var updatedItems = snapshot.Items;
        if (item is not null)
        {
            var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
            metadata[ItemMetadataCodec.MockupInvalidationNoticeKey] =
                $"{outputAssets.Length} generated mockup{(outputAssets.Length == 1 ? string.Empty : "s")} were removed because the Design changed. Generate new mockups to review the updated Design.";
            var updatedItem = item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata) };
            updatedItems = [.. snapshot.Items.Where(value => value.Id != itemId), updatedItem];
        }

        var updated = snapshot with
        {
            Items = updatedItems,
            Assets = snapshot.Assets.Where(asset => !outputIds.Contains(asset.Id)).ToArray(),
            AssetLinks = snapshot.AssetLinks.Where(link => !outputIds.Contains(link.AssetId)).ToArray()
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(false, 0, $"Generated mockups could not be invalidated. {exception.Message}");
        }

        var diagnostics = new List<string>();
        foreach (var output in outputAssets)
        {
            var cleanup = await ManagedWorkspaceFileCleanup.TryDeleteAsync(_fileStore, output.WorkspaceRelativePath).ConfigureAwait(false);
            if (cleanup.Status is ManagedWorkspaceFileCleanup.Status.Failed or ManagedWorkspaceFileCleanup.Status.Uninspectable)
            {
                diagnostics.Add($"The managed mockup file '{output.Name}' could not be removed.");
            }
        }

        return new(true, outputAssets.Length, Diagnostics: diagnostics);
    }

    private static bool MatchesSource(Asset asset, IReadOnlySet<Guid>? sourceDesignAssetIds)
    {
        if (sourceDesignAssetIds is null || sourceDesignAssetIds.Count == 0)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(asset.MetadataJson);
            return document.RootElement.TryGetProperty("designAssetId", out var value)
                && value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var designAssetId)
                && sourceDesignAssetIds.Contains(designAssetId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
