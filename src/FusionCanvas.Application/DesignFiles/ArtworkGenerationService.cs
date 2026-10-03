using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

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

    public ArtworkGenerationService(
        IWorkspaceRepository repository,
        IWorkspaceFileOutputStore fileStore,
        IAiImageProvenanceCodec provenanceCodec,
        IAiImageGenerationProvider provider,
        IRasterArtworkNormalizer normalizer,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null,
        ITelemetryRecorder? telemetry = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _provenanceCodec = provenanceCodec ?? throw new ArgumentNullException(nameof(provenanceCodec));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _telemetry = telemetry;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
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
        var hasDefaultRowWithSelectedColor = row is not null
            && snapshot.DesignVariantRowColors.Any(value => value.RowId == row.Id);

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
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
            ? AiImageEndpointPolicy.SelectEndpoint(request.Endpoints, modelId, request.RequireZeroDataRetention, request.TransparentBackground, area.Size)
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
            return DesignStageResult.Failure(readiness.Blockers[0]);

        var readyArea = area!;
        var readySelection = selection!;
        var readyRow = row!;
        var nicheContext = BuildNicheContext(snapshot, item.NicheId);
        var sll = metadata.GetValueOrDefault(ItemMetadataCodec.SllKey);
        var fingerprint = metadata.GetValueOrDefault(ItemMetadataCodec.SllSourceFingerprintKey);
        var currentFingerprint = ItemMetadataCodec.ComputeSllSourceFingerprint(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey), metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey),
            metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey), metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey));
        var prompt = ArtworkPromptBuilder.Build(new ArtworkPromptContext(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey) ?? "", metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey) ?? "",
            metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey) ?? "", metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey) ?? "",
            readyArea.Name, readyArea.Position, readyArea.Size, readyArea.DecorationMethod, readyArea.Guidance is null ? null : $"Recommended format: {readyArea.Guidance.FileFormat ?? "PNG"}; background: {readyArea.Guidance.Background ?? "transparent when requested"}.",
            metadata.GetValueOrDefault(ItemMetadataCodec.NotesKey), sll, !string.IsNullOrWhiteSpace(sll) && !string.Equals(fingerprint, currentFingerprint, StringComparison.Ordinal), nicheContext));

        setStage("provider_dispatch");
        var dispatch = await _provider.GenerateAsync(new AiImageGenerationRequest(
            request.ArtworkProfile.ModelId!, prompt, readySelection.ProviderSize, request.TransparentBackground, request.ApiKey,
            request.RequireZeroDataRetention, readySelection.Endpoint.EndpointId, readySelection.Options), cancellationToken).ConfigureAwait(false);
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
        await using (var content = new MemoryStream(normalized.PngBytes, writable: false))
        {
            managed = await _fileStore.SaveAsync($"generated-artwork-{_newId():N}.png", AssetKind.ExportedImage, content, cancellationToken).ConfigureAwait(false);
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
                return DesignStageResult.Failure($"{invalidated.Message} The result was discarded; try again.");
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException)
        {
            await CleanupGeneratedFileAfterFailureAsync(managed.WorkspaceRelativePath, exception).ConfigureAwait(false);
            throw;
        }

        var assetId = _newId();
        var currentItem = currentSnapshot.Items.Single(value => value.Id == request.ItemId);
        var currentRow = currentSnapshot.DesignVariantRows.Single(value => value.Id == readyRow.Id);
        var asset = new Asset(assetId, currentItem.StoreId, $"{GeneratedArtworkName} - {readyArea.Name}", null, AssetKind.ExportedImage,
            managed.WorkspaceRelativePath, null, false, false, now, now, _provenanceCodec.Serialize(provenance));
        var assignment = new DesignSlotAssignment(currentRow.Id, request.DesignAreaId, assetId);
        var updated = currentSnapshot with
        {
            Assets = [.. currentSnapshot.Assets, asset],
            AssetLinks = [.. currentSnapshot.AssetLinks, new AssetLink(assetId, WorkspaceEntityKind.Item, currentItem.Id)],
            DesignSlotAssignments = [.. currentSnapshot.DesignSlotAssignments.Where(value => value.RowId != currentRow.Id || value.DesignAreaId != request.DesignAreaId), assignment]
        };
        try
        {
            setStage("workspace_save");
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var cleanup = await CleanupGeneratedFileAfterFailureAsync(managed.WorkspaceRelativePath, exception).ConfigureAwait(false);
            if (exception is OperationCanceledException)
            {
                throw;
            }

            var cleanupMessage = ManagedWorkspaceFileCleanup.FailureMessage(cleanup);
            return DesignStageResult.Failure($"The generated artwork could not be saved. {exception.Message}{cleanupMessage}");
        }

        await RecordStageAsync("workspace_save", "Succeeded", "The artwork asset and design assignment were saved.").ConfigureAwait(false);
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

        var expectedConfiguration = expected.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == request.ItemId);
        var currentConfiguration = current.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == request.ItemId);
        if (!Equals(expectedConfiguration, currentConfiguration))
        {
            return false;
        }

        var expectedRow = expected.DesignVariantRows.SingleOrDefault(value => value.ItemId == request.ItemId && value.IsDefault);
        var currentRow = current.DesignVariantRows.SingleOrDefault(value => value.ItemId == request.ItemId && value.IsDefault);
        if (!Equals(expectedRow, currentRow) || expectedRow is null || currentRow is null)
        {
            return false;
        }

        var expectedColors = expected.DesignVariantRowColors.Where(value => value.RowId == expectedRow.Id).ToArray();
        var currentColors = current.DesignVariantRowColors.Where(value => value.RowId == currentRow.Id).ToArray();
        if (!expectedColors.SequenceEqual(currentColors))
        {
            return false;
        }

        var expectedAssignment = expected.DesignSlotAssignments.SingleOrDefault(value => value.RowId == expectedRow.Id && value.DesignAreaId == request.DesignAreaId);
        var currentAssignment = current.DesignSlotAssignments.SingleOrDefault(value => value.RowId == currentRow.Id && value.DesignAreaId == request.DesignAreaId);
        if (!Equals(expectedAssignment, currentAssignment))
        {
            return false;
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

    private static string? BuildNicheContext(WorkspaceSnapshot snapshot, Guid? nicheId)
    {
        if (nicheId is not Guid id || snapshot.Niches.SingleOrDefault(value => value.Id == id) is not { } niche)
            return null;

        var fields = new List<(string Label, string? Value)>
        {
            ("Name", niche.Name),
            ("Description", niche.Description)
        };
        try
        {
            using var document = JsonDocument.Parse(niche.MetadataJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                AddMetadataField("Audience", "audience");
                AddMetadataField("Humor style", "humorStyle");
                AddMetadataField("Visual style guidance", "visualStyleGuidance");
                AddMetadataField("Constraints", "constraints");
                AddMetadataField("Risks", "risks");
                AddMetadataField("Research notes", "researchNotes");
                AddMetadataField("Notes", "notes");

                void AddMetadataField(string label, string key)
                {
                    if (document.RootElement.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String)
                        fields.Add((label, property.GetString()));
                }
            }
        }
        catch (JsonException)
        {
            // Keep usable name/description context if legacy or damaged metadata is malformed.
        }

        var included = fields.Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => $"{field.Label}: {field.Value!.Trim()}")
            .ToArray();
        return included.Length == 0 ? null : string.Join(Environment.NewLine, included);
    }

    private static DesignStageState BuildStateAfterSave(WorkspaceSnapshot snapshot, Guid itemId)
    {
        // The caller's DesignStageService remains the single presentation-state builder.
        return new DesignStageState(itemId, false, string.Empty, snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId, null, null, null, [], [], [], [], []);
    }

    private sealed record ResolvedArtworkArea(Guid Id, string Name, string Position, string DecorationMethod, AiImageSize Size, FusionCanvas.Domain.Catalog.DesignAreaArtworkGuidance? Guidance);
}
