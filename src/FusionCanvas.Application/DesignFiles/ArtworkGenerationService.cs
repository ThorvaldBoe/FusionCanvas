using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.DesignFiles;

public sealed class ArtworkGenerationService : IArtworkGenerationService
{
    private const string GeneratedArtworkName = "generated artwork";
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly IAiImageGenerationProvider _provider;
    private readonly IRasterArtworkNormalizer _normalizer;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public ArtworkGenerationService(
        IWorkspaceRepository repository,
        IWorkspaceFileStore fileStore,
        IAiImageGenerationProvider provider,
        IRasterArtworkNormalizer normalizer,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == request.ItemId);
        if (item is null) return DesignStageResult.Failure("Item was not found.");
        if (item.IsArchived) return DesignStageResult.Failure("Archived Items are read-only.");

        var configuration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == item.Id);
        if (configuration is null) return DesignStageResult.Failure("Select a Listing Configuration before generating artwork.");
        var area = ResolveArea(snapshot, configuration.OfferingId, request.DesignAreaId);
        if (area is null) return DesignStageResult.Failure("The selected Design Area is not active for this Listing Configuration.");
        var row = snapshot.DesignVariantRows.SingleOrDefault(value => value.ItemId == item.Id && value.IsDefault);
        if (row is null || !snapshot.DesignVariantRowColors.Any(value => value.RowId == row.Id))
            return DesignStageResult.Failure("Select at least one product color before generating artwork.");

        var settings = new AiConfigurationSettings(request.RequireZeroDataRetention, false, request.ArtworkProfile,
            AiPurposeProfileSettings.InheritGeneral, AiPurposeProfileSettings.InheritGeneral, AiPurposeProfileSettings.InheritGeneral)
        { Artwork = request.ArtworkProfile };
        var resolution = AiConfigurationResolver.ResolveArtwork(settings, request.Models);
        if (resolution.Availability != AiConfigurationAvailability.Ready)
            return DesignStageResult.Failure(resolution.Errors.FirstOrDefault() ?? "Artwork AI is not ready.");

        var selection = AiImageEndpointPolicy.SelectEndpoint(request.Endpoints, request.ArtworkProfile.ModelId!, request.RequireZeroDataRetention, request.TransparentBackground, area.Size);
        if (selection is null) return DesignStageResult.Failure("No compatible image endpoint is available for this target and privacy policy.");

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        var sll = metadata.GetValueOrDefault(ItemMetadataCodec.SllKey);
        var fingerprint = metadata.GetValueOrDefault(ItemMetadataCodec.SllSourceFingerprintKey);
        var currentFingerprint = ItemMetadataCodec.ComputeSllSourceFingerprint(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey), metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey),
            metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey), metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey));
        var prompt = ArtworkPromptBuilder.Build(new ArtworkPromptContext(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey) ?? "", metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey) ?? "",
            metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey) ?? "", metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey) ?? "",
            area.Name, area.Position, area.Size, area.DecorationMethod, area.Guidance is null ? null : $"Recommended format: {area.Guidance.FileFormat ?? "PNG"}; background: {area.Guidance.Background ?? "transparent when requested"}.",
            metadata.GetValueOrDefault(ItemMetadataCodec.NotesKey), sll, !string.IsNullOrWhiteSpace(sll) && !string.Equals(fingerprint, currentFingerprint, StringComparison.Ordinal)));

        var dispatch = await _provider.GenerateAsync(new AiImageGenerationRequest(
            request.ArtworkProfile.ModelId!, prompt, selection.ProviderSize, request.TransparentBackground, request.ApiKey,
            request.RequireZeroDataRetention, selection.Endpoint.EndpointId), cancellationToken).ConfigureAwait(false);
        if (dispatch.Failure is not null || dispatch.Result is null)
            return DesignStageResult.Failure(dispatch.Failure?.Message ?? "The image provider returned no artwork.");

        var result = dispatch.Result;
        await using var source = new MemoryStream(result.ImageBytes, writable: false);
        RasterArtworkNormalizationResult normalized;
        try
        {
            normalized = await _normalizer.NormalizeAsync(source, new RasterArtworkNormalizationRequest(area.Size, request.TransparentBackground), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return DesignStageResult.Failure($"The generated artwork could not be normalized. {exception.Message}");
        }

        var now = _clock();
        var provenance = new AiImageProvenance(result.Provider, result.SelectedModelId, result.ResolvedModelId, prompt,
            selection.ProviderSize, normalized.FinalSize, request.TransparentBackground, normalized.HasTransparency, now,
            result.ProviderRequestId, result.Usage, normalized.Warnings, request.DesignAreaId);
        var managed = await _fileStore.SaveAsync($"generated-artwork-{_newId():N}.png", AssetKind.ExportedImage, new MemoryStream(normalized.PngBytes, writable: false), cancellationToken).ConfigureAwait(false);
        var assetId = _newId();
        var asset = new Asset(assetId, item.StoreId, $"{GeneratedArtworkName} - {area.Name}", null, AssetKind.ExportedImage,
            managed.WorkspaceRelativePath, null, false, false, now, now, AiImageProvenanceCodec.Serialize(provenance));
        var assignment = new DesignSlotAssignment(row.Id, request.DesignAreaId, assetId);
        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, new AssetLink(assetId, WorkspaceEntityKind.Item, item.Id)],
            DesignSlotAssignments = [.. snapshot.DesignSlotAssignments.Where(value => value.RowId != row.Id || value.DesignAreaId != request.DesignAreaId), assignment]
        };
        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _fileStore.TryDelete(managed.WorkspaceRelativePath);
            return DesignStageResult.Failure($"The generated artwork could not be saved. {exception.Message}");
        }

        return DesignStageResult.Success(BuildStateAfterSave(updated, item.Id));
    }

    private static ResolvedArtworkArea? ResolveArea(WorkspaceSnapshot snapshot, Guid offeringId, Guid areaId)
    {
        var placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == areaId && value.OfferingId == offeringId && !value.IsArchived);
        if (placeholder is not null)
            return new(areaId, placeholder.Name, placeholder.Position, placeholder.DecorationMethod, new AiImageSize(placeholder.Width, placeholder.Height), placeholder.ArtworkGuidance);
        var legacy = snapshot.DesignAreas.SingleOrDefault(value => value.Id == areaId && value.FulfillmentOfferingId == offeringId);
        return legacy is null ? null : new(areaId, legacy.Name, legacy.Position, legacy.DecorationMethod, new AiImageSize(legacy.Width, legacy.Height), null);
    }

    private static DesignStageState BuildStateAfterSave(WorkspaceSnapshot snapshot, Guid itemId)
    {
        // The caller's DesignStageService remains the single presentation-state builder.
        return new DesignStageState(itemId, false, string.Empty, snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId, null, null, null, [], [], [], [], []);
    }

    private sealed record ResolvedArtworkArea(Guid Id, string Name, string Position, string DecorationMethod, AiImageSize Size, FusionCanvas.Domain.Catalog.DesignAreaArtworkGuidance? Guidance);
}
