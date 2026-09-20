using FusionCanvas.App.StageTools;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.Tests;

public class DesignStageToolViewModelTests
{
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
