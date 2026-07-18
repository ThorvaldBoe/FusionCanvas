using FusionCanvas.App.Groups;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class GroupManagementViewModelTests
{
    [Fact]
    public async Task OpenForCreateAsync_StartsUnpersistedDraftAndRequestsNameFocus()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new GroupManagementViewModel(new GroupManagementService(repository));
        var focusRequested = false;
        viewModel.GroupNameFocusRequested += (_, _) => focusRequested = true;

        await viewModel.OpenForCreateAsync(
            sample.Store.Id,
            sample.Niche.Id,
            new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id));

        Assert.True(viewModel.IsOpen);
        Assert.True(viewModel.IsCreating);
        Assert.True(viewModel.HasSelection);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.True(focusRequested);
        Assert.Empty(repository.Snapshot.Groups);
    }

    [Fact]
    public async Task SaveAsync_PersistsDraftAndSelectsCreatedGroup()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new GroupManagementViewModel(new GroupManagementService(repository));
        await viewModel.OpenForCreateAsync(sample.Store.Id, sample.Niche.Id, new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id));
        viewModel.Name = "Campaign";
        viewModel.Description = "Fall work";
        viewModel.Notes = "Start with mugs";

        await viewModel.SaveAsync();

        Assert.False(viewModel.IsCreating);
        Assert.NotNull(viewModel.SelectedGroup);
        Assert.Equal("Campaign", Assert.Single(repository.Snapshot.Groups).Name);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task SwitchingWithDirtyFields_OffersKeepOrDiscardAndPreservesIntent()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new GroupManagementViewModel(new GroupManagementService(repository));
        await viewModel.OpenForEditAsync(sample.Store.Id, sample.Niche.Id, sample.FirstGroup!.Id);
        viewModel.Name = "Changed draft";
        var secondSummary = viewModel.ActiveGroups.Single(group => group.Id == sample.SecondGroup!.Id);

        await viewModel.TrySelectAsync(secondSummary);

        Assert.True(viewModel.DiscardPromptVisible);
        Assert.Equal(sample.FirstGroup.Id, viewModel.SelectedGroup?.Id);
        viewModel.KeepEditingCommand.Execute(null);
        Assert.False(viewModel.DiscardPromptVisible);
        Assert.Equal("Changed draft", viewModel.Name);

        await viewModel.TrySelectAsync(secondSummary);
        await viewModel.DiscardAndContinueAsync();

        Assert.Equal(sample.SecondGroup.Id, viewModel.SelectedGroup?.Id);
        Assert.Equal(sample.SecondGroup!.Name, viewModel.Name);
        Assert.Equal(sample.FirstGroup.Name, repository.Snapshot.Groups.Single(group => group.Id == sample.FirstGroup.Id).Name);
    }

    [Fact]
    public async Task SaveFailure_KeepsDraftAndExposesRecoverableError()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var viewModel = new GroupManagementViewModel(new GroupManagementService(repository));
        await viewModel.OpenForEditAsync(sample.Store.Id, sample.Niche.Id, sample.FirstGroup!.Id);
        viewModel.Name = "Retry me";

        await viewModel.SaveAsync();

        Assert.True(viewModel.HasError);
        Assert.Contains("could not be saved", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Retry me", viewModel.Name);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal(sample.FirstGroup.Name, repository.Snapshot.Groups.Single(group => group.Id == sample.FirstGroup.Id).Name);
    }

    [Fact]
    public async Task StartingNewGroup_WithDirtyEditorRequiresExplicitDisposition()
    {
        var sample = Sample.CreateWithGroups();
        var viewModel = new GroupManagementViewModel(new GroupManagementService(new TestRepository(sample.Snapshot)));
        await viewModel.OpenForEditAsync(sample.Store.Id, sample.Niche.Id, sample.FirstGroup!.Id);
        viewModel.Name = "Unsaved";

        viewModel.TryBeginCreate();

        Assert.True(viewModel.DiscardPromptVisible);
        Assert.False(viewModel.IsCreating);
        viewModel.KeepEditingCommand.Execute(null);
        Assert.Equal("Unsaved", viewModel.Name);
    }

    [Fact]
    public async Task ArchiveAndRestore_KeepActiveAndArchivedProjectionsCoherent()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new GroupManagementViewModel(new GroupManagementService(repository));
        await viewModel.OpenForEditAsync(sample.Store.Id, sample.Niche.Id, sample.FirstGroup!.Id);

        viewModel.TryRequestArchive();
        Assert.True(viewModel.ArchiveWarningVisible);
        await viewModel.ConfirmArchiveAsync();

        Assert.DoesNotContain(viewModel.ActiveGroups, group => group.Id == sample.FirstGroup.Id);
        var archived = Assert.Single(viewModel.ArchivedGroups, group => group.Id == sample.FirstGroup.Id);
        await viewModel.TryRestoreAsync(archived);

        Assert.Contains(viewModel.ActiveGroups, group => group.Id == sample.FirstGroup.Id);
        Assert.Empty(viewModel.ArchivedGroups);
        Assert.Equal(sample.FirstGroup.Id, viewModel.SelectedGroup?.Id);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public bool FailSaves { get; init; }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("Test failure.");
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup? FirstGroup = null, TopicGroup? SecondGroup = null)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
            return new Sample(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []), store, niche);
        }

        public static Sample CreateWithGroups()
        {
            var sample = Create();
            var now = DateTimeOffset.UtcNow;
            var first = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "First", null, false, now, now, "{}");
            var second = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Second", null, false, now, now, "{}");
            return sample with { Snapshot = sample.Snapshot with { Groups = [first, second] }, FirstGroup = first, SecondGroup = second };
        }
    }
}
