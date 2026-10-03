using FusionCanvas.App.StageTools;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.App.Tests;

public class DesignStageToolViewModelTests
{
    [Fact]
    public async Task LoadAsync_OpaqueOnlyAspectRatioEndpointDisablesTransparencyAndKeepsGenerationAvailable()
    {
        var itemId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        const string modelId = "openai/gpt-5.4-image-2";
        var designService = new DelayedArtworkPreferenceService(itemId, targetId, Guid.NewGuid(), persistInitialTarget: true, initialTransparency: true);
        var endpoint = new AiImageEndpointCapabilities(
            "openai", modelId, false, true, ["png"], [], false, "OpenAI",
            new AiImageEndpointParameterCapabilities(["1:1", "2:3", "3:4"], [], false, false, ["auto", "opaque"], true));
        var aiConfiguration = new TestAiConfigurationProvider(
            AiConfigurationSettings.Default with
            {
                RequireZeroDataRetention = false,
                Artwork = AiProfileSettings.Empty with { ModelId = modelId }
            },
            [new AiModelDescriptor(modelId, modelId, null, null, ["text"], ["image"], [], 1000, null, null, null, false, null)],
            [endpoint]);
        var viewModel = new DesignStageToolViewModel(designService, new UnusedArtworkGenerationService(), aiConfiguration);

        await viewModel.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);

        Assert.Equal(targetId, viewModel.SelectedArtworkTargetId);
        Assert.False(viewModel.TransparentBackground);
        Assert.False(viewModel.CanUseTransparentBackground);
        Assert.True(viewModel.CanGenerateArtwork);
        Assert.Contains("opaque artwork", viewModel.ArtworkGenerationGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_IncompleteConceptTriangle_DisablesArtworkGenerationWithGuidance()
    {
        var viewModel = await CreateArtworkReadinessViewModelAsync(
            conceptComplete: false,
            includeDefaultRowColor: true);

        Assert.False(viewModel.CanGenerateArtwork);
        Assert.Contains("Concept", viewModel.ArtworkGenerationGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_DefaultRowWithoutSelectedColor_DisablesArtworkGenerationWithGuidance()
    {
        var viewModel = await CreateArtworkReadinessViewModelAsync(
            conceptComplete: true,
            includeDefaultRowColor: false);

        Assert.False(viewModel.CanGenerateArtwork);
        Assert.Contains("product color", viewModel.ArtworkGenerationGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaleConfiguration_SelectionRequiresConfirmationAndCancellationDoesNotPersist()
    {
        var service = new RecoveryDesignStageService();
        var viewModel = new DesignStageToolViewModel(service);

        await viewModel.LoadAsync(service.ItemId, canEdit: true, TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasStaleConfiguration);
        Assert.True(viewModel.IsReadOnly);
        Assert.True(viewModel.CanRecoverStaleConfiguration);
        var replacement = Assert.Single(viewModel.RecoveryOfferings);

        viewModel.SelectedRecoveryOffering = replacement;

        Assert.True(viewModel.IsRecoveryConfirmationVisible);
        Assert.Contains("Archived shirt", viewModel.RecoveryConfirmationMessage);
        Assert.Contains("Active shirt", viewModel.RecoveryConfirmationMessage);
        Assert.Equal(0, service.RecoveryCalls);

        viewModel.CancelStaleConfigurationRecovery();

        Assert.False(viewModel.IsRecoveryConfirmationVisible);
        Assert.Null(viewModel.SelectedRecoveryOffering);
        Assert.Equal(0, service.RecoveryCalls);
    }

    [Fact]
    public async Task ConfirmStaleConfigurationRecovery_SuppressesDuplicatesAndRefreshesAuthoritativeState()
    {
        var service = new RecoveryDesignStageService(delayRecovery: true);
        var viewModel = new DesignStageToolViewModel(service);
        await viewModel.LoadAsync(service.ItemId, canEdit: true, TestContext.Current.CancellationToken);
        viewModel.SelectedRecoveryOffering = Assert.Single(viewModel.RecoveryOfferings);

        var firstConfirmation = viewModel.ConfirmStaleConfigurationRecoveryAsync(TestContext.Current.CancellationToken);
        await service.RecoveryStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var duplicateConfirmation = viewModel.ConfirmStaleConfigurationRecoveryAsync(TestContext.Current.CancellationToken);

        Assert.True(duplicateConfirmation.IsCompletedSuccessfully);
        Assert.Equal(1, service.RecoveryCalls);
        Assert.False(viewModel.CanChooseRecoveryOffering);

        service.CompleteRecovery();
        await firstConfirmation;

        Assert.False(viewModel.HasStaleConfiguration);
        Assert.False(viewModel.IsReadOnly);
        Assert.True(viewModel.ShowsConfiguredState);
        Assert.Equal(service.ReplacementOffering.Id, viewModel.SelectedOfferingId);
        Assert.False(viewModel.IsRecoveryConfirmationVisible);
    }

    [Fact]
    public async Task ConfirmStaleConfigurationRecovery_FailureKeepsStaleStateAndActionableError()
    {
        var service = new RecoveryDesignStageService(recoveryError: "The replacement Offering is no longer active.");
        var viewModel = new DesignStageToolViewModel(service);
        await viewModel.LoadAsync(service.ItemId, canEdit: true, TestContext.Current.CancellationToken);
        viewModel.SelectedRecoveryOffering = Assert.Single(viewModel.RecoveryOfferings);

        await viewModel.ConfirmStaleConfigurationRecoveryAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, service.RecoveryCalls);
        Assert.True(viewModel.HasStaleConfiguration);
        Assert.True(viewModel.CanRecoverStaleConfiguration);
        Assert.Equal("The replacement Offering is no longer active.", viewModel.ErrorMessage);
        Assert.Single(viewModel.RecoveryOfferings);
        Assert.False(viewModel.IsRecoveryConfirmationVisible);
    }

    [Fact]
    public async Task LoadAsync_AfterTargetChange_WaitsForPendingPreferenceSave()
    {
        var itemId = Guid.NewGuid();
        var firstTargetId = Guid.NewGuid();
        var selectedTargetId = Guid.NewGuid();
        var service = new DelayedArtworkPreferenceService(itemId, firstTargetId, selectedTargetId);
        var viewModel = new DesignStageToolViewModel(service);

        await viewModel.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);
        viewModel.SelectedArtworkTargetId = selectedTargetId;
        await service.SaveStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        var reload = viewModel.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);
        Assert.False(reload.IsCompleted);

        service.CompleteSave();
        await reload;

        Assert.Equal(selectedTargetId, viewModel.SelectedArtworkTargetId);
    }

    [Fact]
    public async Task LoadAsync_DiscardsLateArtworkResultFromInvalidatedContext()
    {
        var itemId = SampleWorkspace.DesignNodeId;
        var targetId = Assert.Single(SampleWorkspace.Create().DesignAreas).Id;
        var designService = new DelayedArtworkPreferenceService(itemId, targetId, Guid.NewGuid(), persistInitialTarget: true);
        var artworkService = new DelayedArtworkGenerationService();
        var viewModel = await CreateArtworkReadinessViewModelAsync(
            conceptComplete: true,
            includeDefaultRowColor: true,
            artworkGenerationService: artworkService,
            designServiceOverride: designService);

        var generation = viewModel.GenerateArtworkAsync(TestContext.Current.CancellationToken);
        await artworkService.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        await viewModel.LoadAsync(itemId, canEdit: false, TestContext.Current.CancellationToken);
        artworkService.Complete(DesignStageResult.Failure("late result"));
        await generation;

        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ClearsPreviousArtworkError()
    {
        var viewModel = await CreateArtworkReadinessViewModelAsync(
            conceptComplete: true,
            includeDefaultRowColor: true,
            artworkGenerationService: new FailingArtworkGenerationService("Previous artwork failed."));

        await viewModel.GenerateArtworkAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Previous artwork failed.", viewModel.ErrorMessage);

        await viewModel.LoadAsync(SampleWorkspace.DesignNodeId, canEdit: true, TestContext.Current.CancellationToken);

        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task DisposeWhileSlotPreviewIsLoading_DisposesTheReturnedStream()
    {
        var service = new DelayedArtworkPreferenceService(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var viewModel = new DesignStageToolViewModel(service);
        var previewTask = viewModel.PreviewSlotImageAsync(Guid.NewGuid(), Guid.NewGuid(), TestContext.Current.CancellationToken);
        await service.PreviewStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        viewModel.Dispose();
        var stream = new TrackingMemoryStream();
        service.CompletePreview(stream);
        await previewTask;

        Assert.True(stream.WasDisposed);
        Assert.Null(viewModel.PreviewStream);
        Assert.Null(viewModel.PreviewBitmap);
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class DelayedArtworkPreferenceService : IDesignStageService
    {
        private readonly Guid _itemId;
        private readonly TaskCompletionSource _saveCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Stream> _previewCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DelayedArtworkPreferenceService(
            Guid itemId,
            Guid firstTargetId,
            Guid selectedTargetId,
            bool persistInitialTarget = false,
            bool? initialTransparency = null)
        {
            _itemId = itemId;
            var now = DateTimeOffset.UtcNow;
            var offeringId = Guid.NewGuid();
            State = new DesignStageState(itemId, false, string.Empty, offeringId, "Configured offering", null, null, [], [], [], [], [])
            {
                AvailablePlaceholders =
                [
                    new OfferingPlaceholder(firstTargetId, offeringId, "Front", null, "front", "DTG", 3000, 4500, [], false, now, now),
                    new OfferingPlaceholder(selectedTargetId, offeringId, "Back", null, "back", "DTG", 3000, 4500, [], false, now, now)
                ],
                HasPersistedArtworkTargetPreference = persistInitialTarget,
                PersistedArtworkTargetId = persistInitialTarget ? firstTargetId : null,
                PersistedTransparentBackground = initialTransparency,
                IsDesignTriangleComplete = true,
                HasDefaultRowWithSelectedColor = true
            };
        }

        public DesignStageState State { get; private set; }
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource PreviewStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DesignStageState> LoadDesignStageStateAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public async Task<DesignStageResult> SaveArtworkPreferencesAsync(Guid itemId, Guid? designAreaId, bool transparentBackground, CancellationToken cancellationToken = default)
        {
            Assert.Equal(_itemId, itemId);
            SaveStarted.TrySetResult();
            await _saveCompletion.Task.WaitAsync(cancellationToken);
            State = State with
            {
                HasPersistedArtworkTargetPreference = true,
                PersistedArtworkTargetId = designAreaId,
                PersistedTransparentBackground = transparentBackground
            };
            return DesignStageResult.Success(State);
        }

        public void CompleteSave() => _saveCompletion.TrySetResult();

        public void CompletePreview(Stream stream) => _previewCompletion.TrySetResult(stream);

        public Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RecoverStaleConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> AddSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> MakeSpecificForColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSpecificRowAsync(Guid itemId, Guid rowId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> AssignSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> ReplaceSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenSlotPreviewAsync(Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default)
        {
            PreviewStarted.TrySetResult();
            return _previewCompletion.Task;
        }
        public Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DesignSlotSummary>> ListSupportingImagesAsync(Guid itemId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> ImportSupportingImageAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSupportingImageAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecoveryDesignStageService : IDesignStageService
    {
        private readonly string? _recoveryError;
        private readonly TaskCompletionSource _recoveryCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecoveryDesignStageService(bool delayRecovery = false, string? recoveryError = null)
        {
            DelayRecovery = delayRecovery;
            _recoveryError = recoveryError;
            var now = DateTimeOffset.UtcNow;
            var productId = Guid.NewGuid();
            StaleOffering = new FulfillmentOffering(Guid.NewGuid(), productId, "Archived shirt", null,
                FulfillmentKind.FixedProvider, "Legacy provider", null, now, now, "{}");
            ReplacementOffering = new FulfillmentOffering(Guid.NewGuid(), productId, "Active shirt", null,
                FulfillmentKind.FixedProvider, "Current provider", null, now, now, "{}");
            State = CreateStaleState();
        }

        public Guid ItemId { get; } = Guid.NewGuid();
        public FulfillmentOffering StaleOffering { get; }
        public FulfillmentOffering ReplacementOffering { get; }
        public DesignStageState State { get; private set; }
        public bool DelayRecovery { get; }
        public int RecoveryCalls { get; private set; }
        public TaskCompletionSource RecoveryStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DesignStageState> LoadDesignStageStateAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(ItemId, itemId);
            return Task.FromResult(State);
        }

        public async Task<DesignStageResult> RecoverStaleConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(ItemId, itemId);
            Assert.Equal(ReplacementOffering.Id, offeringId);
            RecoveryCalls++;
            RecoveryStarted.TrySetResult();
            if (DelayRecovery)
            {
                await _recoveryCompletion.Task.WaitAsync(cancellationToken);
            }

            if (_recoveryError is not null)
            {
                return DesignStageResult.Failure(_recoveryError, State);
            }

            State = new DesignStageState(
                ItemId, false, string.Empty, ReplacementOffering.Id, ReplacementOffering.Name,
                ReplacementOffering.Kind, ReplacementOffering.ProviderName,
                [ReplacementOffering], [], [], [], []);
            return DesignStageResult.Success(State);
        }

        public void CompleteRecovery() => _recoveryCompletion.TrySetResult();

        private DesignStageState CreateStaleState() => new(
            ItemId, true, "The selected listing configuration is no longer active; existing Design data is read-only.",
            StaleOffering.Id, StaleOffering.Name, StaleOffering.Kind, StaleOffering.ProviderName,
            [StaleOffering], ["Black"], ["Black"], [], [])
        {
            HasStaleConfiguration = true,
            CanRecoverStaleConfiguration = true,
            StaleConfigurationDisplayName = StaleOffering.Name,
            RecoveryGuidance = "Choose an active replacement from this Store to continue Design work.",
            RecoveryOfferings = [ReplacementOffering]
        };

        public Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> SaveArtworkPreferencesAsync(Guid itemId, Guid? designAreaId, bool transparentBackground, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> AddSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> MakeSpecificForColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSpecificRowAsync(Guid itemId, Guid rowId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> AssignSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> ReplaceSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenSlotPreviewAsync(Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DesignSlotSummary>> ListSupportingImagesAsync(Guid itemId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> ImportSupportingImageAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RemoveSupportingImageAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestAiConfigurationProvider(
        AiConfigurationSettings settings,
        IReadOnlyList<AiModelDescriptor> models,
        IReadOnlyList<AiImageEndpointCapabilities> endpoints) : IAiConfigurationProvider
    {
        public AiConfigurationSettings Current { get; } = settings;
        public IReadOnlyList<AiModelDescriptor> AvailableModels { get; } = models;

        public Task<string?> ReadApiKeyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("secret");

        public Task<IReadOnlyList<AiImageEndpointCapabilities>> GetArtworkEndpointsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(endpoints);
    }

    private sealed class UnusedArtworkGenerationService : IArtworkGenerationService
    {
        public Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class DelayedArtworkGenerationService : IArtworkGenerationService
    {
        private readonly TaskCompletionSource<DesignStageResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete(DesignStageResult result) => _completion.TrySetResult(result);
    }

    private sealed class FailingArtworkGenerationService(string message) : IArtworkGenerationService
    {
        public Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignStageResult.Failure(message));
    }

    private static async Task<DesignStageToolViewModel> CreateArtworkReadinessViewModelAsync(
        bool conceptComplete,
        bool includeDefaultRowColor,
        IArtworkGenerationService? artworkGenerationService = null,
        DelayedArtworkPreferenceService? designServiceOverride = null)
    {
        const string modelId = "openai/gpt-5.4-image-2";
        var snapshot = SampleWorkspace.Create();
        var item = snapshot.Items.Single(value => value.Id == SampleWorkspace.DesignNodeId);
        var offering = Assert.Single(snapshot.FulfillmentOfferings);
        var area = Assert.Single(snapshot.DesignAreas);
        var rowId = Guid.NewGuid();
        var metadataJson = conceptComplete
            ? """{"concept.idea":"A thoughtful idea with substance","phrase":"A concise memorable phrase","graphicDirection":"A detailed graphic direction"}"""
            : "{}";
        var updatedItem = item with { MetadataJson = metadataJson };
        snapshot = snapshot with
        {
            Items = [.. snapshot.Items.Where(value => value.Id != item.Id), updatedItem],
            ItemListingConfigurations = [new ItemListingConfiguration(item.Id, offering.Id)],
            DesignSelectedColors = includeDefaultRowColor ? [new DesignSelectedColor(item.Id, "Black")] : [],
            DesignVariantRows = [new DesignVariantRow(rowId, item.Id, isDefault: true, sortOrder: 0)],
            DesignVariantRowColors = includeDefaultRowColor ? [new DesignVariantRowColor(rowId, "Black")] : []
        };

        var repository = new InMemoryWorkspaceRepository(snapshot);
        IDesignStageService designStageService = designServiceOverride is not null
            ? designServiceOverride
            : new DesignStageService(repository, new UnusedWorkspaceFileStore(), new AiImageProvenanceCodec());
        if (designServiceOverride is null)
        {
            var preferenceResult = await designStageService.SaveArtworkPreferencesAsync(
                item.Id, area.Id, transparentBackground: false, TestContext.Current.CancellationToken);
            Assert.True(preferenceResult.Succeeded);
        }

        var endpoint = new AiImageEndpointCapabilities(
            "openai", modelId, false, true, ["png"], [], false, "OpenAI",
            new AiImageEndpointParameterCapabilities(["1:1", "2:3", "3:4"], [], false, false, ["auto", "opaque"], true));
        var aiConfiguration = new TestAiConfigurationProvider(
            AiConfigurationSettings.Default with
            {
                RequireZeroDataRetention = false,
                Artwork = AiProfileSettings.Empty with { ModelId = modelId }
            },
            [new AiModelDescriptor(modelId, modelId, null, null, ["text"], ["image"], [], 1000, null, null, null, false, null)],
            [endpoint]);
        var viewModel = new DesignStageToolViewModel(designStageService, artworkGenerationService ?? new UnusedArtworkGenerationService(), aiConfiguration);
        await viewModel.LoadAsync(item.Id, canEdit: true, TestContext.Current.CancellationToken);

        if (designServiceOverride is null)
        {
            Assert.Equal(area.Id, viewModel.SelectedArtworkTargetId);
        }
        Assert.True(viewModel.HasConfiguration);
        Assert.False(viewModel.IsReadOnly);
        return viewModel;
    }

    private sealed class UnusedWorkspaceFileStore : IWorkspaceFileStore
    {
        public string WorkspaceRoot => string.Empty;

        public string ResolvePath(string workspaceRelativePath) => workspaceRelativePath;

        public Task<ManagedWorkspaceFile> ImportAsync(
            string sourcePath,
            AssetKind kind,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool Exists(string workspaceRelativePath) => false;

        public bool TryDelete(string workspaceRelativePath) => false;

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExportCopyAsync(
            string workspaceRelativePath,
            string destinationPath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
