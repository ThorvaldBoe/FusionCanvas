using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.DesignFiles;

public sealed class ArtworkGenerationService : IArtworkGenerationService
{
    private const string GeneratedArtworkName = "generated artwork";
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileOutputStore _fileStore;
    private readonly IAiImageProvenanceCodec _provenanceCodec;
    private readonly IAiImageGenerationProvider _provider;
    private readonly IRasterArtworkNormalizer _normalizer;
    private readonly ITelemetryRecorder? _telemetry;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private readonly IContentRiskReviewService? _contentRiskReviews;

    public ArtworkGenerationService(
        IWorkspaceRepository repository,
        IWorkspaceFileOutputStore fileStore,
        IAiImageProvenanceCodec provenanceCodec,
        IAiImageGenerationProvider provider,
        IRasterArtworkNormalizer normalizer,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null,
        ITelemetryRecorder? telemetry = null,
        IContentRiskReviewService? contentRiskReviews = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _provenanceCodec = provenanceCodec ?? throw new ArgumentNullException(nameof(provenanceCodec));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _telemetry = telemetry;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
        _contentRiskReviews = contentRiskReviews;
    }

    public async Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var stage = "workspace_load";
        try
        {
            return await GenerateCoreAsync(request, cancellationToken, value => stage = value).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            try
            {
                await RecordStageAsync(stage, "Cancelled", "The operation was cancelled at this artwork-generation stage.").ConfigureAwait(false);
            }
            catch (Exception telemetryException)
            {
                exception.Data["FusionCanvas.ArtworkGeneration.CancellationTelemetryError"] = telemetryException;
            }

            throw;
        }
    }

    private async Task<DesignStageResult> GenerateCoreAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken, Action<string> setStage)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == request.ItemId);
        if (item is null) return DesignStageResult.Failure("Item was not found.");

        var configuration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == item.Id);
        var area = configuration is null
            ? null
            : ResolveArea(snapshot, configuration.OfferingId, request.DesignAreaId);
        var row = snapshot.DesignVariantRows.SingleOrDefault(value => value.ItemId == item.Id && value.IsDefault);
        var hasDefaultRowWithSelectedColor = row is not null;

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        var selectedStyle = Guid.TryParse(metadata.GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey), out var styleId)
            ? snapshot.ArtworkStyles.SingleOrDefault(value => value.Id == styleId && value.WorkspaceId == snapshot.Stores.SingleOrDefault(store => store.Id == item.StoreId)?.WorkspaceId)
            : null;
        var styleReferences = selectedStyle is null
            ? []
            : snapshot.ArtworkStyleReferences.Where(value => value.StyleId == selectedStyle.Id).OrderBy(value => value.SortOrder).ToArray();
        var designTriangleComplete = DesignTriangleScore.FromValues(
            metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey),
            metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey),
            metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey)) == 100;

        var settings = new AiConfigurationSettings(request.RequireZeroDataRetention, false, request.ArtworkProfile,
            AiPurposeProfileSettings.InheritGeneral, AiPurposeProfileSettings.InheritGeneral, AiPurposeProfileSettings.InheritGeneral)
        { Artwork = request.ArtworkProfile };
        var resolution = AiConfigurationResolver.ResolveArtwork(settings, request.Models);

        var selection = resolution.Availability == AiConfigurationAvailability.Ready
            && request.ArtworkProfile.ModelId is { Length: > 0 } modelId
            && area is not null
            ? AiImageEndpointPolicy.SelectEndpoint(request.EditExistingArtwork ? request.Endpoints : EndpointsForStyleReferences(request.Endpoints, styleReferences.Length), modelId, request.RequireZeroDataRetention,
                request.TransparentBackground, area.Size, request.EditExistingArtwork)
            : null;
        var readiness = ArtworkGenerationReadinessPolicy.Evaluate(
            ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage).IsAllowed,
            resolution.Availability == AiConfigurationAvailability.Ready,
            selection is not null,
            configuration is not null,
            area is not null,
            designTriangleComplete,
            hasDefaultRowWithSelectedColor);
        if (!readiness.IsReady)
        {
            if (!request.EditExistingArtwork && styleReferences.Length > 0 && !request.Endpoints.Any(endpoint =>
                    endpoint.ModelId == request.ArtworkProfile.ModelId && (endpoint.Parameters?.MaxInputReferences ?? 0) >= styleReferences.Length))
                return DesignStageResult.Failure($"The selected image model cannot accept all {styleReferences.Length} reference images in this style. Choose a model with a higher image-reference limit or select another style.");
            return DesignStageResult.Failure(readiness.Blockers[0]);
        }

        var readyArea = area!;
        var readySelection = selection!;
        var readyRow = row!;
        Asset? sourceArtwork = null;
        IReadOnlyList<AiImageInputReference>? inputReferences = null;
        if (request.EditExistingArtwork)
        {
            var sourceAssignment = snapshot.DesignSlotAssignments.SingleOrDefault(value =>
                value.RowId == readyRow.Id && value.DesignAreaId == request.DesignAreaId);
            sourceArtwork = sourceAssignment?.AssetId is Guid sourceAssetId
                ? snapshot.Assets.SingleOrDefault(value => value.Id == sourceAssetId)
                : null;
            if (sourceArtwork is null
                || sourceArtwork.Kind != AssetKind.ExportedImage
                || sourceArtwork.IsMissing
                || !Path.GetExtension(sourceArtwork.Name).Equals(".png", StringComparison.OrdinalIgnoreCase)
                || !snapshot.AssetLinks.Any(value => value.AssetId == sourceArtwork.Id
                    && value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == item.Id))
            {
                return DesignStageResult.Failure("The selected Design slot has no available final PNG artwork to adjust.");
            }

            if (string.IsNullOrWhiteSpace(request.AdjustmentPrompt))
            {
                return DesignStageResult.Failure("Enter adjustment instructions before adjusting existing artwork.");
            }

            inputReferences = [await ReadInputReferenceAsync(sourceArtwork, cancellationToken).ConfigureAwait(false)];
        }
        else if (styleReferences.Length > 0)
        {
            try
            {
                var references = new List<AiImageInputReference>(styleReferences.Length);
                foreach (var reference in styleReferences)
                    references.Add(await ReadStyleInputReferenceAsync(reference, cancellationToken).ConfigureAwait(false));
                inputReferences = references;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or InvalidOperationException or NotSupportedException)
            {
                return DesignStageResult.Failure($"A reference image in the selected style is unavailable for generation. {exception.Message}");
            }
        }

        var prompt = request.EditExistingArtwork
            ? request.AdjustmentPrompt
            : request.Prompt ?? ArtworkPromptCompiler.BuildDefault(snapshot, item, request.DesignAreaId, request.Contrast);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return DesignStageResult.Failure("The artwork prompt could not be built from the current Design context.");
        }

        setStage("provider_dispatch");
        var dispatch = await _provider.GenerateAsync(new AiImageGenerationRequest(
            request.ArtworkProfile.ModelId!, prompt, readySelection.ProviderSize, request.TransparentBackground, request.ApiKey,
            request.RequireZeroDataRetention, readySelection.Endpoint.EndpointId, readySelection.Options, inputReferences), cancellationToken).ConfigureAwait(false);
        if (dispatch.Failure is not null || dispatch.Result is null)
            return DesignStageResult.Failure(dispatch.Failure?.Message ?? "The image provider returned no artwork.");
        cancellationToken.ThrowIfCancellationRequested();

        if (!await IsArtworkContextCurrentAsync(snapshot, request, cancellationToken).ConfigureAwait(false))
        {
            return DesignStageResult.Failure("The Design context changed while artwork was generating. The result was discarded; try again.");
        }

        await RecordStageAsync("provider_dispatch", "Succeeded", "The image provider returned artwork.").ConfigureAwait(false);

        var result = dispatch.Result;
        await using var source = new MemoryStream(result.ImageBytes, writable: false);
        RasterArtworkNormalizationResult normalized;
        setStage("image_normalization");
        try
        {
            normalized = await _normalizer.NormalizeAsync(source, new RasterArtworkNormalizationRequest(readyArea.Size, request.TransparentBackground), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return DesignStageResult.Failure($"The generated artwork could not be normalized. {exception.Message}");
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsArtworkContextCurrentAsync(snapshot, request, cancellationToken).ConfigureAwait(false))
        {
            return DesignStageResult.Failure("The Design context changed while artwork was generating. The result was discarded; try again.");
        }

        await RecordStageAsync("image_normalization", "Succeeded", "Artwork normalization completed.").ConfigureAwait(false);

        var now = _clock();
        var provenance = new AiImageProvenance(result.Provider, result.SelectedModelId, result.ResolvedModelId, prompt,
            readySelection.ProviderSize, normalized.FinalSize, request.TransparentBackground, normalized.HasTransparency, now,
            result.ProviderRequestId, result.Usage, normalized.Warnings, request.DesignAreaId);
        setStage("file_storage");
        ManagedWorkspaceFile managed;
        ManagedWorkspaceFile? relatedManaged = null;
        try
        {
            if (sourceArtwork is not null)
            {
                await using var originalContent = new MemoryStream(inputReferences![0].ImageBytes, writable: false);
                relatedManaged = await _fileStore.SaveAsync($"before-ai-edit-{_newId():N}.png", AssetKind.ReferenceImage, originalContent, cancellationToken).ConfigureAwait(false);
            }

            await using var content = new MemoryStream(normalized.PngBytes, writable: false);
            managed = await _fileStore.SaveAsync($"generated-artwork-{_newId():N}.png", AssetKind.ExportedImage, content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (relatedManaged is not null)
                await CleanupGeneratedFileAfterFailureAsync(relatedManaged.WorkspaceRelativePath, exception).ConfigureAwait(false);
            throw;
        }
        await RecordStageAsync("file_storage", "Succeeded", "The normalized artwork file was stored.").ConfigureAwait(false);
        WorkspaceSnapshot currentSnapshot;
        try
        {
            currentSnapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsArtworkContextCurrent(snapshot, currentSnapshot, request))
            {
                var invalidated = new InvalidOperationException("The Design context changed while artwork was generating.");
                await CleanupGeneratedFileAfterFailureAsync(managed.WorkspaceRelativePath, invalidated).ConfigureAwait(false);
                if (relatedManaged is not null)
                    await CleanupGeneratedFileAfterFailureAsync(relatedManaged.WorkspaceRelativePath, invalidated).ConfigureAwait(false);
                return DesignStageResult.Failure($"{invalidated.Message} The result was discarded; try again.");
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException)
        {
            await CleanupGeneratedFileAfterFailureAsync(managed.WorkspaceRelativePath, exception).ConfigureAwait(false);
            if (relatedManaged is not null)
                await CleanupGeneratedFileAfterFailureAsync(relatedManaged.WorkspaceRelativePath, exception).ConfigureAwait(false);
            throw;
        }

        var assetId = _newId();
        var currentItem = currentSnapshot.Items.Single(value => value.Id == request.ItemId);
        var currentRow = currentSnapshot.DesignVariantRows.Single(value => value.Id == readyRow.Id);
        var updatedRow = currentRow with
        {
            ContrastIntent = request.Contrast switch
            {
                ArtworkContrast.LightOnDark => ArtworkContrastIntent.LightForDarkProduct,
                ArtworkContrast.DarkOnLight => ArtworkContrastIntent.DarkForLightProduct,
                _ => null
            }
        };
        var asset = new Asset(assetId, currentItem.StoreId, $"{GeneratedArtworkName} - {readyArea.Name}", null, AssetKind.ExportedImage,
            managed.WorkspaceRelativePath, null, false, false, now, now, _provenanceCodec.Serialize(provenance));
        var relatedAssetId = relatedManaged is null ? (Guid?)null : _newId();
        var relatedAsset = relatedManaged is null || sourceArtwork is null
            ? null
            : new Asset(relatedAssetId!.Value, currentItem.StoreId, $"Before AI edit - {readyArea.Name}", null,
                AssetKind.ReferenceImage, relatedManaged.WorkspaceRelativePath, null, false, false, now, now, sourceArtwork.MetadataJson);
        var assignment = new DesignSlotAssignment(currentRow.Id, request.DesignAreaId, assetId);
        var reviewTarget = new ContentRiskReviewTarget(assetId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");
        var review = ContentRiskReview.Unreviewed(reviewTarget, ContentRiskFingerprint.ForImage(reviewTarget, normalized.PngBytes));
        var updated = currentSnapshot with
        {
            Assets = relatedAsset is null ? [.. currentSnapshot.Assets, asset] : [.. currentSnapshot.Assets, asset, relatedAsset],
            AssetLinks = relatedAsset is null
                ? [.. currentSnapshot.AssetLinks, new AssetLink(assetId, WorkspaceEntityKind.Item, currentItem.Id)]
                : [.. currentSnapshot.AssetLinks, new AssetLink(assetId, WorkspaceEntityKind.Item, currentItem.Id), new AssetLink(relatedAsset.Id, WorkspaceEntityKind.Item, currentItem.Id)],
            DesignSlotAssignments = [.. currentSnapshot.DesignSlotAssignments.Where(value => value.RowId != currentRow.Id || value.DesignAreaId != request.DesignAreaId), assignment],
            DesignVariantRows = [.. currentSnapshot.DesignVariantRows.Where(value => value.Id != updatedRow.Id), updatedRow],
            ContentRiskReviews = relatedAsset is null
                ? [.. currentSnapshot.ContentRiskReviews.Where(existing => existing.Target != reviewTarget), review]
                : [.. currentSnapshot.ContentRiskReviews.Where(existing => existing.Target.OwnerId != relatedAsset.Id && existing.Target != reviewTarget), review,
                    ContentRiskReview.Unreviewed(
                        new ContentRiskReviewTarget(relatedAsset.Id, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset"),
                        ContentRiskFingerprint.ForImage(new ContentRiskReviewTarget(relatedAsset.Id, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset"), inputReferences![0].ImageBytes))]
        };
        try
        {
            setStage("workspace_save");
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var cleanup = await CleanupGeneratedFileAfterFailureAsync(managed.WorkspaceRelativePath, exception).ConfigureAwait(false);
            var relatedCleanup = relatedManaged is null
                ? null
                : await CleanupGeneratedFileAfterFailureAsync(relatedManaged.WorkspaceRelativePath, exception).ConfigureAwait(false);
            if (exception is OperationCanceledException)
            {
                throw;
            }

            var cleanupMessage = ManagedWorkspaceFileCleanup.FailureMessage(cleanup)
                + (relatedCleanup is null ? string.Empty : ManagedWorkspaceFileCleanup.FailureMessage(relatedCleanup));
            return DesignStageResult.Failure($"The generated artwork could not be saved. {exception.Message}{cleanupMessage}");
        }

        await RecordStageAsync("workspace_save", "Succeeded", "The artwork asset and design assignment were saved.").ConfigureAwait(false);
        if (_contentRiskReviews is not null)
        {
            try
            {
                await _contentRiskReviews.ReviewImageAsync(reviewTarget, "image/png", normalized.PngBytes, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The persisted Unreviewed state remains visible when advisory review cannot run.
            }
        }
        return DesignStageResult.Success(BuildStateAfterSave(updated, item.Id));
    }

    private async Task<bool> IsArtworkContextCurrentAsync(
        WorkspaceSnapshot expected,
        ArtworkGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var current = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return IsArtworkContextCurrent(expected, current, request);
    }

    private static bool IsArtworkContextCurrent(
        WorkspaceSnapshot expected,
        WorkspaceSnapshot current,
        ArtworkGenerationRequest request)
    {
        var expectedItem = expected.Items.SingleOrDefault(value => value.Id == request.ItemId);
        var currentItem = current.Items.SingleOrDefault(value => value.Id == request.ItemId);
        if (expectedItem is null || currentItem is null || !Equals(expectedItem, currentItem))
        {
            return false;
        }

        var expectedStyleId = Guid.TryParse(ItemMetadataCodec.ParseMetadata(expectedItem.MetadataJson).GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey), out var parsedExpectedStyleId) ? parsedExpectedStyleId : (Guid?)null;
        var currentStyleId = Guid.TryParse(ItemMetadataCodec.ParseMetadata(currentItem.MetadataJson).GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey), out var parsedCurrentStyleId) ? parsedCurrentStyleId : (Guid?)null;
        if (expectedStyleId != currentStyleId) return false;
        var expectedStyle = expectedStyleId is Guid expectedId ? expected.ArtworkStyles.SingleOrDefault(value => value.Id == expectedId) : null;
        var currentStyle = currentStyleId is Guid currentId ? current.ArtworkStyles.SingleOrDefault(value => value.Id == currentId) : null;
        if (!Equals(expectedStyle, currentStyle)) return false;
        var expectedReferences = expectedStyle is null ? [] : expected.ArtworkStyleReferences.Where(value => value.StyleId == expectedStyle.Id).OrderBy(value => value.SortOrder).ToArray();
        var currentReferences = currentStyle is null ? [] : current.ArtworkStyleReferences.Where(value => value.StyleId == currentStyle.Id).OrderBy(value => value.SortOrder).ToArray();
        if (!expectedReferences.SequenceEqual(currentReferences)) return false;

        var expectedConfiguration = expected.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == request.ItemId);
        var currentConfiguration = current.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == request.ItemId);
        if (!Equals(expectedConfiguration, currentConfiguration))
        {
            return false;
        }

        // The compiled prompt includes live Store and Niche guidance. Discard an
        // in-flight result when that source context changes during generation.
        var expectedStore = expected.Stores.SingleOrDefault(value => value.Id == expectedItem.StoreId);
        var currentStore = current.Stores.SingleOrDefault(value => value.Id == currentItem.StoreId);
        var expectedNiche = expectedItem.NicheId is Guid expectedNicheId
            ? expected.Niches.SingleOrDefault(value => value.Id == expectedNicheId)
            : null;
        var currentNiche = currentItem.NicheId is Guid currentNicheId
            ? current.Niches.SingleOrDefault(value => value.Id == currentNicheId)
            : null;
        if (!Equals(expectedStore, currentStore) || !Equals(expectedNiche, currentNiche))
        {
            return false;
        }

        var expectedRow = expected.DesignVariantRows.SingleOrDefault(value => value.ItemId == request.ItemId && value.IsDefault);
        var currentRow = current.DesignVariantRows.SingleOrDefault(value => value.ItemId == request.ItemId && value.IsDefault);
        if (!Equals(expectedRow, currentRow) || expectedRow is null || currentRow is null)
        {
            return false;
        }

        var expectedAssignment = expected.DesignSlotAssignments.SingleOrDefault(value => value.RowId == expectedRow.Id && value.DesignAreaId == request.DesignAreaId);
        var currentAssignment = current.DesignSlotAssignments.SingleOrDefault(value => value.RowId == currentRow.Id && value.DesignAreaId == request.DesignAreaId);
        if (!Equals(expectedAssignment, currentAssignment))
        {
            return false;
        }

        if (request.EditExistingArtwork)
        {
            var expectedAsset = expectedAssignment?.AssetId is Guid expectedAssetId
                ? expected.Assets.SingleOrDefault(value => value.Id == expectedAssetId)
                : null;
            var currentAsset = currentAssignment?.AssetId is Guid currentAssetId
                ? current.Assets.SingleOrDefault(value => value.Id == currentAssetId)
                : null;
            var expectedLink = expectedAsset is null ? null : expected.AssetLinks.SingleOrDefault(value =>
                value.AssetId == expectedAsset.Id && value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == request.ItemId);
            var currentLink = currentAsset is null ? null : current.AssetLinks.SingleOrDefault(value =>
                value.AssetId == currentAsset.Id && value.EntityKind == WorkspaceEntityKind.Item && value.EntityId == request.ItemId);
            if (!Equals(expectedAsset, currentAsset) || !Equals(expectedLink, currentLink))
            {
                return false;
            }
        }

        var expectedArea = expectedConfiguration is null ? null : ResolveArea(expected, expectedConfiguration.OfferingId, request.DesignAreaId);
        var currentArea = currentConfiguration is null ? null : ResolveArea(current, currentConfiguration.OfferingId, request.DesignAreaId);
        return Equals(expectedArea, currentArea);
    }

    private async Task<ManagedWorkspaceFileCleanup.Result> CleanupGeneratedFileAfterFailureAsync(string workspaceRelativePath, Exception primaryException)
    {
        var result = await ManagedWorkspaceFileCleanup.TryDeleteAsync(_fileStore, workspaceRelativePath).ConfigureAwait(false);
        ManagedWorkspaceFileCleanup.PreserveDiagnostic(primaryException, result, "Artwork generation");
        if (result.Status is ManagedWorkspaceFileCleanup.Status.Failed or ManagedWorkspaceFileCleanup.Status.Uninspectable)
        {
            var message = result.Status == ManagedWorkspaceFileCleanup.Status.Failed
                ? "The generated artwork file could not be removed after workspace persistence failed."
                : "The generated artwork file could not be inspected after a cleanup attempt.";
            try
            {
                await RecordStageAsync("file_cleanup", result.Status.ToString(), message).ConfigureAwait(false);
            }
            catch (Exception telemetryException)
            {
                primaryException.Data["FusionCanvas.ManagedWorkspaceFileCleanup.TelemetryError"] = telemetryException;
            }
        }

        return result;
    }

    private Task RecordStageAsync(string stage, string outcome, string message) => _telemetry?.IsCaptureEnabled == true
        ? _telemetry.RecordAsync(new TelemetryEventRequest(
            "Application.ArtworkGeneration", "GenerationStage", outcome == "Succeeded" ? "Information" : "Warning", outcome,
            message, MetadataJson: JsonSerializer.Serialize(new { stage })))
        : Task.CompletedTask;

    private static ResolvedArtworkArea? ResolveArea(WorkspaceSnapshot snapshot, Guid offeringId, Guid areaId)
    {
        var placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == areaId && value.OfferingId == offeringId && !value.IsArchived);
        if (placeholder is not null)
            return new(areaId, placeholder.Name, placeholder.Position, placeholder.DecorationMethod, new AiImageSize(placeholder.Width, placeholder.Height), placeholder.ArtworkGuidance);
        var legacy = snapshot.DesignAreas.SingleOrDefault(value => value.Id == areaId && value.FulfillmentOfferingId == offeringId);
        return legacy is null ? null : new(areaId, legacy.Name, legacy.Position, legacy.DecorationMethod, new AiImageSize(legacy.Width, legacy.Height), null);
    }

    private async Task<AiImageInputReference> ReadInputReferenceAsync(Asset asset, CancellationToken cancellationToken)
    {
        await using var source = await _fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0)
        {
            throw new InvalidDataException("The selected artwork is empty and cannot be used as an image reference.");
        }

        return new AiImageInputReference("image/png", buffer.ToArray());
    }

    private async Task<AiImageInputReference> ReadStyleInputReferenceAsync(
        FusionCanvas.Domain.ArtworkStyles.ArtworkStyleReference reference,
        CancellationToken cancellationToken)
    {
        if (!AiImageEndpointPolicy.IsApprovedRasterFormat(reference.MediaType))
            throw new NotSupportedException("The style image format is not supported by this image endpoint.");
        await using var source = await _fileStore.OpenReadAsync(reference.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0) throw new InvalidDataException("The style reference image is empty.");
        return new AiImageInputReference(reference.MediaType, buffer.ToArray());
    }

    private static IReadOnlyList<AiImageEndpointCapabilities> EndpointsForStyleReferences(
        IReadOnlyList<AiImageEndpointCapabilities> endpoints,
        int referenceCount) => referenceCount == 0
            ? endpoints
            : endpoints.Where(endpoint => (endpoint.Parameters?.MaxInputReferences ?? 0) >= referenceCount).ToArray();

    private static DesignStageState BuildStateAfterSave(WorkspaceSnapshot snapshot, Guid itemId)
    {
        // The caller's DesignStageService remains the single presentation-state builder.
        return new DesignStageState(itemId, false, string.Empty, snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId, null, null, null, [], [], [], [], []);
    }

    private sealed record ResolvedArtworkArea(Guid Id, string Name, string Position, string DecorationMethod, AiImageSize Size, FusionCanvas.Domain.Catalog.DesignAreaArtworkGuidance? Guidance);
}
