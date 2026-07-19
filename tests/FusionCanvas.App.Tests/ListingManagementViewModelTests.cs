using FusionCanvas.App.Listings;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class ListingManagementViewModelTests
{
    [Fact]
    public async Task PropertyDraftSaveArchiveRestoreDeleteAndCloseGuardsRemainCoherent()
    {
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{\"notes\":\"old\"}");
        var other = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Other", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var repository = new Repository(new WorkspaceSnapshot([store], [niche], [], [listing, other], [], [], [], [], []));
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository));
        var changes = 0;
        viewModel.WorkspaceStructureChanged += (_, _) => changes++;

        await viewModel.OpenForEditAsync(store.Id, listing.Id);
        viewModel.Description = "Description";
        viewModel.Notes = "new";
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanSave);
        viewModel.CloseCommand.Execute(null);
        Assert.True(viewModel.DiscardPromptVisible);
        viewModel.KeepEditingCommand.Execute(null);
        viewModel.SelectListingCommand.Execute(viewModel.ActiveListings.Single(item => item.Id == other.Id));
        Assert.True(viewModel.DiscardPromptVisible);
        Assert.Equal(listing.Id, viewModel.SelectedListing!.Id);
        viewModel.KeepEditingCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusy && !viewModel.HasUnsavedChanges);
        Assert.Equal("Description", repository.Snapshot.Listings.Single(item => item.Id == listing.Id).Description);

        viewModel.ArchiveCommand.Execute(null);
        await WaitForAsync(() => viewModel.SelectedListing?.IsArchived == true);
        Assert.True(viewModel.CanRestore);
        viewModel.RestoreCommand.Execute(null);
        await WaitForAsync(() => viewModel.SelectedListing?.IsArchived == false);
        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.DeleteConfirmationVisible);
        viewModel.CancelDeleteCommand.Execute(null);
        Assert.Equal(2, repository.Snapshot.Listings.Count);
        Assert.True(changes >= 3);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class Repository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default) { Snapshot = value; return Task.CompletedTask; }
    }
}
