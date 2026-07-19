using FusionCanvas.App.DocumentWindow;
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
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);

        await viewModel.LoadAsync(sample.Listing.Id);

        Assert.True(viewModel.HasState);
        Assert.Equal(sample.Listing.Name, viewModel.Title);
        Assert.Equal("idea-value", viewModel.Idea);
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
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasState);
        Assert.Empty(viewModel.Title);
        Assert.Empty(viewModel.TagDraft);
    }

    [Fact]
    public async Task Load_ArchivedListingIsReadOnlyWithNotice()
    {
        var sample = Sample.Create();
        var archived = sample.Listing with { IsArchived = true };
        sample.Repository.Set(sample.Snapshot with { Listings = [archived] });
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);

        await viewModel.LoadAsync(archived.Id);

        Assert.True(viewModel.IsReadOnly);
        Assert.NotEmpty(viewModel.InactiveNotice);
        Assert.False(viewModel.CanEdit);
    }

    [Fact]
    public async Task EditingFieldsMarksDirtyAndRevertRestoresBaseline()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "Changed";
        viewModel.Idea = "new idea";
        viewModel.TagDraft.Clear();
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanSave);

        viewModel.RevertCommand.Execute(null);

        Assert.Equal(sample.Listing.Name, viewModel.Title);
        Assert.Equal("idea-value", viewModel.Idea);
        Assert.Equal(sample.Tag.Name, viewModel.TagDraft.Single());
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task AddTag_AppendsNameAndRejectsInvalidInput()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.TagInput = "New Tag";
        viewModel.AddTagCommand.Execute(null);

        Assert.Contains("New Tag", viewModel.TagDraft);
        Assert.True(viewModel.HasUnsavedChanges);

        viewModel.TagInput = "   ";
        viewModel.AddTagCommand.Execute(null);
        Assert.True(viewModel.HasError);
        Assert.Equal(2, viewModel.TagDraft.Count);
    }

    [Fact]
    public async Task Save_PersistsAndRefreshesBaseline()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "Renamed";
        viewModel.Idea = "new idea";
        viewModel.SaveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("Renamed", viewModel.Title);
        Assert.Equal("new idea", viewModel.Idea);
        var persisted = sample.Repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Contains("\"idea\":\"new idea\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Save_InvalidTitleKeepsDraftAndReportsError()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.Title = "   ";
        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Save_RaisesSavedEvent()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        var raised = 0;
        viewModel.Saved += (_, _) => raised++;

        viewModel.Title = "Renamed";
        viewModel.SaveCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task ApplyStage_UpdatesEmphasisAndKeepsAllSectionsAccessible()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        viewModel.ApplyStage(WorkflowStage.Concept);

        Assert.True(viewModel.EmphasizesConcept);
        Assert.False(viewModel.EmphasizesIdea);
        Assert.True(viewModel.HasState);
    }

    [Fact]
    public async Task RequestLeave_ProceedsImmediatelyWhenClean()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);

        var proceeded = false;
        viewModel.RequestLeave(() => proceeded = true);

        Assert.True(proceeded);
        Assert.False(viewModel.DiscardPromptVisible);
    }

    [Fact]
    public async Task RequestLeave_PromptsWhenDirtyAndCancelKeepsContext()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);
        viewModel.Title = "Changed";

        var proceeded = false;
        viewModel.RequestLeave(() => proceeded = true);

        Assert.False(proceeded);
        Assert.True(viewModel.DiscardPromptVisible);

        viewModel.KeepEditingCommand.Execute(null);

        Assert.False(viewModel.DiscardPromptVisible);
        Assert.False(proceeded);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task RequestLeave_DiscardAndContinueRevertsAndProceeds()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);
        viewModel.Title = "Changed";

        var proceeded = false;
        viewModel.RequestLeave(() => proceeded = true);

        viewModel.DiscardAndContinueCommand.Execute(null);

        Assert.True(proceeded);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(sample.Listing.Name, viewModel.Title);
    }

    [Fact]
    public async Task RequestLeave_SaveAndContinuePersistsAndProceeds()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(sample.Repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);
        var viewModel = new ListingInspectorViewModel(service);
        await viewModel.LoadAsync(sample.Listing.Id);
        viewModel.Title = "Changed";

        var proceeded = false;
        viewModel.RequestLeave(() => proceeded = true);

        viewModel.SaveAndContinueCommand.Execute(null);

        Assert.True(proceeded);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal("Changed", persisted.Name);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Listing Listing, Tag Tag, TestRepository Repository)
    {
        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ListingStatus.Ready, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [], [], [tag],
                [new ListingTag(listing.Id, tag.Id)], []);
            return new(snapshot, now, store, niche, root, child, listing, tag, new TestRepository(snapshot));
        }
    }
}
