using FusionCanvas.App.Items;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Tests;

public class ItemInspectorViewModelTests
{
    [Fact]
    public async Task Load_PopulatesFieldsFromStateAndClearsDirty()
    {
        var sample = Sample.Create(withRelationships: true);
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(sample.Item.Id);

        Assert.True(viewModel.HasState);
        Assert.Equal(sample.Item.Name, viewModel.Title);
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
    public async Task Load_MissingItemClearsState()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasState);
        Assert.Empty(viewModel.Title);
        Assert.Empty(viewModel.TagDraft);
    }

    [Fact]
    public async Task Load_ArchivedItemIsReadOnlyWithNoticeAndRestore()
    {
        var sample = Sample.Create();
        var archived = sample.Item with { IsArchived = true };
        sample.Repository.Set(sample.Snapshot with { Items = [archived] });
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
        await viewModel.LoadAsync(sample.Item.Id);

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
        await viewModel.LoadAsync(sample.Item.Id);

        await viewModel.CommitEditsAsync();

        Assert.Equal(0, sample.Repository.SaveCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Commit_DesignStagePersistsSharedFieldsWithoutChangingUpstreamMetadata()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "Renamed";
        viewModel.Idea = "new idea";
        viewModel.Audience = "coffee lovers";
        viewModel.Description = "new description";
        viewModel.Notes = "new notes";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("Renamed", viewModel.Title);
        Assert.Equal("idea-value", viewModel.Idea);
        Assert.Equal("audience-value", viewModel.Audience);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal("Description", persisted.Description);
        Assert.Contains("\"idea\":\"idea-value\"", persisted.MetadataJson);
        Assert.Contains("\"idea.audience\":\"audience-value\"", persisted.MetadataJson);
        Assert.Contains("\"notes\":\"new notes\"", persisted.MetadataJson);
        Assert.Contains("\"unknown\":\"kept\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_BlankOptionalTitleSavesWithOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "   ";
        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal(string.Empty, persisted.Name);
        Assert.Contains("\"notes\":\"changed notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_MultiLineTitleRevertsTitleAndPersistsOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        viewModel.Notes = "kept notes";

        viewModel.Title = "line one\nline two";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Item.Name, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal(sample.Item.Name, persisted.Name);
        Assert.Contains("\"notes\":\"kept notes\"", persisted.MetadataJson);
        Assert.Equal(1, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_MultiLineOnlyTitleSkipsSaveButReportsRevert()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "line one\nline two";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Item.Name, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(0, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_SerializedDrainPersistsLatestEditAndNoStaleOverwrite()
    {
        var sample = Sample.Create();
        sample.Repository.SaveDelay = TimeSpan.FromMilliseconds(150);
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "first";
        var firstCommit = viewModel.CommitEditsAsync();
        viewModel.Notes = "second";
        var secondCommit = viewModel.CommitEditsAsync();

        await Task.WhenAll(firstCommit, secondCommit);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("second", viewModel.Notes);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Contains("\"notes\":\"second\"", persisted.MetadataJson);
        Assert.Equal(2, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_MidFlightEditIsPreservedAcrossSave()
    {
        var sample = Sample.Create();
        sample.Repository.SaveDelay = TimeSpan.FromMilliseconds(150);
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "first";
        var commit = viewModel.CommitEditsAsync();
        viewModel.Notes = "second";
        await commit;

        Assert.Equal("second", viewModel.Notes);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Commit_PersistenceFailureKeepsDraftAndReportsError()
    {
        var sample = Sample.Create();
        sample.Repository.FailSaves = true;
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("changed notes", viewModel.Notes);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
    }

    [Fact]
    public async Task TagChange_PersistsImmediatelyWithoutReplacingTextDraft()
    {
        var sample = Sample.Create();
        var viewModel = new ItemInspectorViewModel(
            new ItemInspectorService(sample.Repository),
            new ItemManagementService(sample.Repository),
            new TagManagementService(sample.Repository));
        await viewModel.LoadAsync(sample.Item.Id, TestContext.Current.CancellationToken);
        viewModel.Title = "pending title";
        viewModel.TagInput = "Immediate";

        viewModel.AddTagCommand.Execute(null);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal("pending title", viewModel.Title);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Contains("Immediate", viewModel.TagDraft);
        Assert.Contains(sample.Repository.Snapshot.ItemTags, link =>
            link.ItemId == sample.Item.Id
            && sample.Repository.Snapshot.Tags.Single(tag => tag.Id == link.TagId).Name == "Immediate");
    }

    [Fact]
    public async Task Commit_RaisesSavedEvent()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
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
        await viewModel.LoadAsync(sample.Item.Id);

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
        await viewModel.LoadAsync(sample.Item.Id);

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
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.RemoveTagCommand.Execute(sample.Tag.Name);

        Assert.False(viewModel.HasError);
        Assert.Empty(viewModel.TagDraft);
        Assert.Empty(sample.Repository.Snapshot.ItemTags.Where(link => link.ItemId == sample.Item.Id));
        Assert.Contains(sample.Repository.Snapshot.Tags, tag => tag.Id == sample.Tag.Id);
    }

    [Fact]
    public async Task ApplyStage_UpdatesEmphasisAndKeepsAllSectionsAccessible()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

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
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestArchiveCommand.Execute(null);
        Assert.True(viewModel.ArchiveConfirmationVisible);
        viewModel.ConfirmArchiveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.ArchiveConfirmationVisible);
        Assert.NotNull(raised);
        Assert.False(raised!.Deleted);
        Assert.True(sample.Repository.Snapshot.Items.Single().IsArchived);
    }

    [Fact]
    public async Task Restore_RestoresArchivedItemAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        viewModel.RequestArchiveCommand.Execute(null);
        viewModel.ConfirmArchiveCommand.Execute(null);
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RestoreCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.False(sample.Repository.Snapshot.Items.Single().IsArchived);
    }

    [Fact]
    public async Task Delete_ConfirmedDeletesAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.DeleteConfirmationVisible);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.True(raised!.Deleted);
        Assert.Empty(sample.Repository.Snapshot.Items);
    }

    [Fact]
    public async Task Delete_PersistenceFailureReportsErrorAndKeepsItem()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);
        sample.Repository.FailSaves = true;

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Single(sample.Repository.Snapshot.Items);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; set; }
        public TimeSpan? SaveDelay { get; set; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public async Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (SaveDelay is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            Snapshot = value;
            SaveCount++;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Item Item, Tag Tag, TestRepository Repository)
    {
        public ItemInspectorViewModel CreateViewModel(Func<DateTimeOffset>? clock = null) =>
            new(
                new ItemInspectorService(Repository, clock: clock, newId: Guid.NewGuid),
                new ItemManagementService(Repository, clock: clock, newId: Guid.NewGuid));

        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Item(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ItemStatus.Draft, WorkflowStage.Design, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"idea.audience\":\"audience-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [], [], [tag],
                [new ItemTag(listing.Id, tag.Id)], []);
            return new(snapshot, now, store, niche, root, child, listing, tag, new TestRepository(snapshot));
        }
    }
}
