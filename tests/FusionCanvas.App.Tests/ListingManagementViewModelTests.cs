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
        var repository = new Repository(WorkspaceSnapshot.FromStores([store], [niche], [], [listing, other], [], [], [], [], []));
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

    [Fact]
    public async Task TagEditor_AppliesAndRemovesTagsIndependentlyFromDescriptionSave()
    {
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Evergreen", null, false, now, now, "{}", "#1ABC9C");
        var repository = new Repository(WorkspaceSnapshot.FromStores([store], [niche], [], [listing], [], [], [tag], [], []));
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository), new TagManagementService(repository));

        await viewModel.OpenForEditAsync(store.Id, listing.Id);
        Assert.True(viewModel.CanEditTags);
        Assert.Empty(viewModel.AppliedTags);

        viewModel.ApplyTagSuggestionCommand.Execute(viewModel.SuggestedTags.Single(suggestion => suggestion.Id == tag.Id));
        await WaitForAsync(() => viewModel.AppliedTags.Count == 1);

        viewModel.Description = "Draft change";
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanSave);
        Assert.True(viewModel.AppliedTags.Count == 1);

        var link = Assert.Single(repository.Snapshot.ListingTags);
        Assert.Equal(listing.Id, link.ListingId);
        Assert.Equal(tag.Id, link.TagId);
        var stillDraftListing = repository.Snapshot.Listings.Single(item => item.Id == listing.Id);
        Assert.Null(stillDraftListing.Description);

        viewModel.RemoveTagCommand.Execute(viewModel.AppliedTags.Single());
        await WaitForAsync(() => viewModel.AppliedTags.Count == 0);
        Assert.Empty(repository.Snapshot.ListingTags);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("Draft change", viewModel.Description);
    }

    [Fact]
    public async Task TagEditor_CreateOnTheFlyAppliesNewTagAndMatchesExistingActiveTag()
    {
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var existing = new Tag(Guid.NewGuid(), store.Id, "Evergreen", null, false, now, now, "{}", null);
        var repository = new Repository(WorkspaceSnapshot.FromStores([store], [niche], [], [listing], [], [], [existing], [], []));
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository), new TagManagementService(repository, () => now, () => Guid.NewGuid()));

        await viewModel.OpenForEditAsync(store.Id, listing.Id);

        viewModel.TagInput = "evergreen";
        viewModel.ApplyOrCreateTagCommand.Execute(null);
        await WaitForAsync(() => viewModel.AppliedTags.Count == 1);
        Assert.Equal(existing.Id, viewModel.AppliedTags.Single().Id);
        Assert.Equal(string.Empty, viewModel.TagInput);

        viewModel.TagInput = "Brand new";
        viewModel.ApplyOrCreateTagCommand.Execute(null);
        await WaitForAsync(() => repository.Snapshot.Tags.Any(tag => tag.Name == "Brand new"));
        Assert.Equal(2, viewModel.AppliedTags.Count);
        var newTag = repository.Snapshot.Tags.Single(tag => tag.Name == "Brand new");
        Assert.Equal(newTag.Id, viewModel.AppliedTags.Single(applied => applied.Name == "Brand new").Id);
    }

    [Fact]
    public async Task TagEditor_OffersRestoreForArchivedTagMatch()
    {
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var archived = new Tag(Guid.NewGuid(), store.Id, "Paused", null, true, now, now, "{}", null);
        var repository = new Repository(WorkspaceSnapshot.FromStores([store], [niche], [], [listing], [], [], [archived], [], []));
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository), new TagManagementService(repository));

        await viewModel.OpenForEditAsync(store.Id, listing.Id);

        viewModel.TagInput = "paused";
        viewModel.ApplyOrCreateTagCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusyTags);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.TagErrorMessage));
        Assert.Empty(viewModel.AppliedTags);
        Assert.Empty(repository.Snapshot.ListingTags);
    }

    [Fact]
    public async Task TagEditor_BlocksEditsWhenListingIsHiddenByArchivedAncestor()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var archivedNicheId = Guid.NewGuid();
        var store = new Store(storeId, "Store", null, false, now, now, "{}", archivedNicheId);
        var archivedNiche = new Niche(archivedNicheId, storeId, "Archived", null, true, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), storeId, archivedNicheId, null, "Hidden", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), storeId, "Evergreen", null, false, now, now, "{}", null);
        var snapshot = WorkspaceSnapshot.FromStores([store], [archivedNiche], [], [listing], [], [], [tag], [], []);
        var repository = new Repository(snapshot);
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository), new TagManagementService(repository));

        await viewModel.OpenForEditAsync(storeId, listing.Id);

        Assert.False(viewModel.CanEditTags);
        viewModel.ApplyOrCreateTagCommand.Execute(null);
        await WaitForAsync(() => !string.IsNullOrWhiteSpace(viewModel.TagErrorMessage));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.TagErrorMessage));
        Assert.Empty(repository.Snapshot.ListingTags);
    }

    [Fact]
    public async Task TagEditor_ClearsInputOnEscapeAndKeepsExistingChips()
    {
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Evergreen", null, false, now, now, "{}", null);
        var link = new ListingTag(listing.Id, tag.Id);
        var repository = new Repository(WorkspaceSnapshot.FromStores([store], [niche], [], [listing], [], [], [tag], [link], []));
        var viewModel = new ListingManagementViewModel(new ListingManagementService(repository), new TagManagementService(repository));

        await viewModel.OpenForEditAsync(store.Id, listing.Id);
        Assert.Single(viewModel.AppliedTags);

        viewModel.TagInput = "ever";
        viewModel.ClearTagInputCommand.Execute(null);
        Assert.Equal(string.Empty, viewModel.TagInput);
        Assert.Single(viewModel.AppliedTags);
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
