using FusionCanvas.App.Groups;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Groups;

namespace FusionCanvas.App.Tests;

public class GroupDetailsViewModelTests
{
    [Fact]
    public async Task Load_PopulatesFieldsDestinationsAndClearsDirty()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);

        Assert.True(viewModel.HasState);
        Assert.Equal("Root", viewModel.Name);
        Assert.Equal("Purpose", viewModel.Description);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.IsReadOnly);
        Assert.NotEmpty(viewModel.Destinations);
    }

    [Fact]
    public async Task Load_MissingGroupClearsState()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id);

        Assert.False(viewModel.HasState);
        Assert.Empty(viewModel.Name);
    }

    [Fact]
    public async Task Commit_NoOpWhenClean()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);

        await viewModel.CommitEditsAsync();

        Assert.Equal(0, sample.Repository.SaveCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Commit_PersistsNameDescriptionAndNotes()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);
        GroupSummary? changed = null;
        viewModel.StructureChanged += (_, group) => changed = group;

        viewModel.Name = "Renamed";
        viewModel.Description = "New purpose";
        viewModel.Notes = "Working notes";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.NotNull(changed);
        var persisted = sample.Repository.Snapshot.Groups.Single(group => group.Id == sample.Group.Id);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal("New purpose", persisted.Description);
        Assert.Contains("Working notes", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_EmptyNameRevertsButSavesOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);

        viewModel.Name = "   ";
        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Root", viewModel.Name);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Groups.Single(group => group.Id == sample.Group.Id);
        Assert.Equal("Root", persisted.Name);
        Assert.Contains("changed notes", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_DuplicateSiblingNameReverts()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);

        viewModel.Name = "Sibling";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Root", viewModel.Name);
        Assert.Equal("Root", sample.Repository.Snapshot.Groups.Single(group => group.Id == sample.Group.Id).Name);
    }

    [Fact]
    public async Task Commit_PersistenceFailureKeepsDraftAndReportsError()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);
        sample.Repository.FailSaves = true;

        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
    }

    [Fact]
    public async Task Move_CommitsPendingEditsThenMovesToDestination()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);
        var destination = viewModel.Destinations.Single(candidate =>
            candidate.Parent.Kind == WorkspaceEntityKind.Group && candidate.Parent.Id == sample.Sibling.Id);
        GroupSummary? changed = null;
        viewModel.StructureChanged += (_, group) => changed = group;

        viewModel.Notes = "notes before move";
        viewModel.SelectedDestination = destination;
        Assert.True(viewModel.CanMove);
        viewModel.MoveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(changed);
        var persisted = sample.Repository.Snapshot.Groups.Single(group => group.Id == sample.Group.Id);
        Assert.Equal(sample.Sibling.Id, persisted.ParentGroupId);
        Assert.Null(persisted.NicheId);
        Assert.Contains("notes before move", persisted.MetadataJson);
    }

    [Fact]
    public async Task Archive_ConfirmedArchivesAndRaisesStructureChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);
        GroupSummary? changed = null;
        viewModel.StructureChanged += (_, group) => changed = group;

        viewModel.RequestArchiveCommand.Execute(null);
        Assert.True(viewModel.ArchiveConfirmationVisible);
        viewModel.ConfirmArchiveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(changed);
        Assert.True(sample.Repository.Snapshot.Groups.Single(group => group.Id == sample.Group.Id).IsArchived);
    }

    [Fact]
    public async Task ArchivedGroupIsReadOnlyAndRestores()
    {
        var sample = Sample.Create();
        var archived = sample.Group with { IsArchived = true };
        sample.Repository.Set(sample.Snapshot with { Groups = [archived, sample.Sibling] });
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(archived.Id, sample.Store.Id, sample.Niche.Id);
        GroupSummary? changed = null;
        viewModel.StructureChanged += (_, group) => changed = group;

        Assert.True(viewModel.IsReadOnly);
        Assert.False(viewModel.CanEdit);
        Assert.True(viewModel.CanRestore);
        Assert.NotEmpty(viewModel.InactiveNotice);

        viewModel.RestoreCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(changed);
        Assert.False(sample.Repository.Snapshot.Groups.Single(group => group.Id == archived.Id).IsArchived);
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

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Group, TopicGroup Sibling, TestRepository Repository)
    {
        public GroupDetailsViewModel CreateViewModel() => new(new GroupManagementService(Repository));

        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", "Purpose", false, now, now, "{}");
            var sibling = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Sibling", null, false, now, now, "{}", 1);
            var snapshot = new WorkspaceSnapshot([store], [niche], [group, sibling], [], [], [], [], [], []);
            return new(snapshot, now, store, niche, group, sibling, new TestRepository(snapshot));
        }
    }
}
