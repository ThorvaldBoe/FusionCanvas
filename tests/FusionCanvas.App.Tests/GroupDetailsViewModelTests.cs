using FusionCanvas.App.Groups;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.WorkspaceTree;

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

    [Theory]
    [InlineData(FailingOperation.Move)]
    [InlineData(FailingOperation.Archive)]
    [InlineData(FailingOperation.Restore)]
    public async Task Commands_ObserveThrownFailures(FailingOperation operation)
    {
        var sample = Sample.Create();
        if (operation == FailingOperation.Restore)
        {
            sample.Repository.Set(sample.Snapshot with
            {
                Groups = [sample.Group with { IsArchived = true }, sample.Sibling]
            });
        }

        var state = await new GroupManagementService(sample.Repository).LoadAsync(sample.Store.Id, sample.Niche.Id);
        var viewModel = new GroupDetailsViewModel(new FailingGroupManagementService(state, operation));
        await viewModel.LoadAsync(sample.Group.Id, sample.Store.Id, sample.Niche.Id);

        switch (operation)
        {
            case FailingOperation.Move:
                viewModel.SelectedDestination = viewModel.Destinations.Single(candidate =>
                    candidate.Parent.Kind == WorkspaceEntityKind.Group && candidate.Parent.Id == sample.Sibling.Id);
                viewModel.MoveCommand.Execute(null);
                break;
            case FailingOperation.Archive:
                viewModel.RequestArchiveCommand.Execute(null);
                viewModel.ConfirmArchiveCommand.Execute(null);
                break;
            case FailingOperation.Restore:
                viewModel.RestoreCommand.Execute(null);
                break;
        }

        Assert.Equal($"{operation.ToString().ToLowerInvariant()} failed", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    public enum FailingOperation
    {
        Move,
        Archive,
        Restore
    }

    private sealed class FailingGroupManagementService(
        GroupManagementState state,
        FailingOperation operation) : IGroupManagementService
    {
        public Guid? ActiveWorkspaceId => null;
        public Guid? ActiveStoreId => state.ActiveStoreId;
        public Guid? ActiveNicheId => state.ActiveNicheId;
        public Guid? ActiveGroupId => state.ActiveGroupId;

        public void SetActiveWorkspace(Guid? workspaceId)
        {
        }

        public Task<GroupManagementState> LoadAsync(
            Guid? storeId,
            Guid? nicheId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(state);

        public Task<GroupManagementResult> CreateGroupAsync(
            GroupManagementCreateRequest request,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupManagementResult> UpdateGroupAsync(
            GroupManagementUpdateRequest request,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupManagementResult> MoveGroupAsync(
            GroupManagementMoveRequest request,
            CancellationToken cancellationToken = default) => FailIf(FailingOperation.Move);

        public Task<GroupManagementResult> CopyGroupAsync(
            GroupManagementCopyRequest request,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupManagementResult> DeleteGroupAsync(
            GroupManagementDeleteRequest request,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupManagementResult> ArchiveGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) => FailIf(FailingOperation.Archive);

        public Task<GroupManagementResult> RestoreGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) => FailIf(FailingOperation.Restore);

        public Task<GroupManagementResult> SelectGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupManagementResult> SetDefaultNicheAsync(
            Guid storeId,
            Guid nicheId,
            CancellationToken cancellationToken = default) => UnsupportedResult();

        public Task<GroupCreationDestinationResult> ResolveCreateParentAsync(
            Guid storeId,
            WorkspaceTreeSelection? selection,
            CancellationToken cancellationToken = default) =>
            Task.FromException<GroupCreationDestinationResult>(new NotSupportedException());

        private Task<GroupManagementResult> FailIf(FailingOperation expected) =>
            operation == expected
                ? Task.FromException<GroupManagementResult>(new IOException($"{expected.ToString().ToLowerInvariant()} failed"))
                : UnsupportedResult();

        private static Task<GroupManagementResult> UnsupportedResult() =>
            Task.FromException<GroupManagementResult>(new NotSupportedException());
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
