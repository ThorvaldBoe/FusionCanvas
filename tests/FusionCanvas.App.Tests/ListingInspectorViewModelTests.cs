using FusionCanvas.App.Listings;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class ListingInspectorViewModelTests
{
    [Fact]
    public async Task Load_PopulatesFieldsFromStateAndClearsDirty()
    {
        var sample = Sample.Create(withRelationships: true);
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(sample.Listing.Id);

        Assert.True(viewModel.HasState);
        Assert.Equal(sample.Listing.Name, viewModel.Title);
        Assert.Equal("Description", viewModel.Description);
        Assert.Equal("idea-value", viewModel.Idea);
        Assert.Equal("audience-value", viewModel.Audience);
        Assert.Equal("phrase-value", viewModel.Phrase);
        Assert.Equal("graphic-value", viewModel.GraphicDirection);
        Assert.Equal("Notes", viewModel.Notes);
        Assert.Equal(sample.Tag.Name, viewModel.TagDraft.Single());
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.IsReadOnly);
    }

    [Fact]
    public async Task Load_MissingListingClearsState()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasState);
        Assert.Empty(viewModel.Title);
        Assert.Empty(viewModel.TagDraft);
    }

    [Fact]
    public async Task Load_ArchivedListingIsReadOnlyWithNoticeAndRestore()
    {
        var sample = Sample.Create();
        var archived = sample.Listing with { IsArchived = true };
        sample.Repository.Set(sample.Snapshot with { Listings = [archived] });
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(archived.Id);

        Assert.True(viewModel.IsReadOnly);
        Assert.NotEmpty(viewModel.InactiveNotice);
        Assert.False(viewModel.CanEdit);
        Assert.True(viewModel.CanRestore);
        Assert.False(viewModel.CanArchive);
    }

    [Fact]
    public async Task EditingFields_MarksDirty()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "Changed";
        viewModel.Idea = "new idea";
        viewModel.Audience = "new audience";
        viewModel.Description = "new description";

        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Commit_NoOpWhenClean()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        await viewModel.CommitEditsAsync();

        Assert.Equal(0, sample.Repository.SaveCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Commit_PersistsAndRefreshesBaseline()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "Renamed";
        viewModel.Idea = "new idea";
        viewModel.Audience = "coffee lovers";
        viewModel.Description = "new description";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("Renamed", viewModel.Title);
        Assert.Equal("new idea", viewModel.Idea);
        Assert.Equal("coffee lovers", viewModel.Audience);
        var persisted = sample.Repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal("new description", persisted.Description);
        Assert.Contains("\"idea\":\"new idea\"", persisted.MetadataJson);
        Assert.Contains("\"idea.audience\":\"coffee lovers\"", persisted.MetadataJson);
        Assert.Contains("\"unknown\":\"kept\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_InvalidTitleRevertsTitleButSavesOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "   ";
        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Listing.Name, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal(sample.Listing.Name, persisted.Name);
        Assert.Contains("\"notes\":\"changed notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_InvalidTitleAsOnlyChangeSkipsSave()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "   ";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Listing.Name, viewModel.Title);
        Assert.Equal(0, sample.Repository.SaveCount);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
    }

    [Fact]
    public async Task Commit_PersistenceFailureKeepsDraftAndReportsError()
    {
        var sample = Sample.Create();
        sample.Repository.FailSaves = true;
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("changed notes", viewModel.Notes);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
    }

    [Fact]
    public async Task Commit_RaisesSavedEvent()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);
        var raised = 0;
        viewModel.Saved += (_, _) => raised++;

        viewModel.Title = "Renamed";
        await viewModel.CommitEditsAsync();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task AddTag_CommitsImmediately()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.TagInput = "New Tag";
        viewModel.AddTagCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Contains("New Tag", viewModel.TagDraft);
        Assert.Contains(sample.Repository.Snapshot.Tags, tag => tag.Name == "New Tag");
        Assert.Equal(1, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task AddTag_RejectsInvalidInputWithoutSaving()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.TagInput = "   ";
        viewModel.AddTagCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(0, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task RemoveTag_CommitsImmediately()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.RemoveTagCommand.Execute(sample.Tag.Name);

        Assert.False(viewModel.HasError);
        Assert.Empty(viewModel.TagDraft);
        Assert.Empty(sample.Repository.Snapshot.ListingTags.Where(link => link.ListingId == sample.Listing.Id));
        Assert.Contains(sample.Repository.Snapshot.Tags, tag => tag.Id == sample.Tag.Id);
    }

    [Fact]
    public async Task ApplyStage_UpdatesEmphasisAndKeepsAllSectionsAccessible()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.ApplyStage(WorkflowStage.Concept);

        Assert.True(viewModel.EmphasizesConcept);
        Assert.False(viewModel.EmphasizesIdea);
        Assert.True(viewModel.HasState);
    }

    [Fact]
    public async Task Archive_ConfirmedArchivesAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);
        ListingInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestArchiveCommand.Execute(null);
        Assert.True(viewModel.ArchiveConfirmationVisible);
        viewModel.ConfirmArchiveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.ArchiveConfirmationVisible);
        Assert.NotNull(raised);
        Assert.False(raised!.Deleted);
        Assert.True(sample.Repository.Snapshot.Listings.Single().IsArchived);
    }

    [Fact]
    public async Task Restore_RestoresArchivedListingAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);
        viewModel.RequestArchiveCommand.Execute(null);
        viewModel.ConfirmArchiveCommand.Execute(null);
        await viewModel.LoadAsync(sample.Listing.Id);
        ListingInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RestoreCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.False(sample.Repository.Snapshot.Listings.Single().IsArchived);
    }

    [Fact]
    public async Task Delete_ConfirmedDeletesAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Listing.Id);
        ListingInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.DeleteConfirmationVisible);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.True(raised!.Deleted);
        Assert.Empty(sample.Repository.Snapshot.Listings);
    }

    [Fact]
    public async Task Delete_PersistenceFailureReportsErrorAndKeepsListing()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Listing.Id);
        sample.Repository.FailSaves = true;

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Single(sample.Repository.Snapshot.Listings);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; set; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Listing Listing, Tag Tag, TestRepository Repository)
    {
        public ListingInspectorViewModel CreateViewModel(Func<DateTimeOffset>? clock = null) =>
            new(
                new ListingInspectorService(Repository, clock: clock, newId: Guid.NewGuid),
                new ListingManagementService(Repository, clock: clock, newId: Guid.NewGuid));

        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ListingStatus.Draft, WorkflowStage.Design, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"idea.audience\":\"audience-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [], [], [tag],
                [new ListingTag(listing.Id, tag.Id)], []);
            return new(snapshot, now, store, niche, root, child, listing, tag, new TestRepository(snapshot));
        }
    }
}
