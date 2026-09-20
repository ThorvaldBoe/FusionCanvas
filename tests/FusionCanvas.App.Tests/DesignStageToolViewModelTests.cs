using FusionCanvas.App.StageTools;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.Tests;

public class DesignStageToolViewModelTests
{
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

    private sealed class DelayedArtworkPreferenceService : IDesignStageService
    {
        private readonly Guid _itemId;
        private readonly TaskCompletionSource _saveCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DelayedArtworkPreferenceService(Guid itemId, Guid firstTargetId, Guid selectedTargetId)
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
                ]
            };
        }

        public DesignStageState State { get; private set; }
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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

        public Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DesignStageResult> RecoverStaleConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
}
