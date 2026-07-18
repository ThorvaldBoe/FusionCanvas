using FusionCanvas.App.Navigation;
using FusionCanvas.App.Groups;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class WorkspaceTreeViewModelTests
{
    [Fact]
    public async Task InlineCreate_CommitsAndStartsAnotherSiblingWithoutOpeningATab()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var ids = new Queue<Guid>([Guid.NewGuid()]);
        var groups = new GroupManagementService(repository, () => sample.Now.AddMinutes(1), () => ids.Dequeue());
        var viewModel = new WorkspaceTreeViewModel(repository, groups, sample.Snapshot);
        var openedTabs = 0;
        viewModel.OpenInTabRequested += (_, _) => openedTabs++;
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        viewModel.SelectNodeCommand.Execute(Assert.Single(viewModel.Roots));

        await viewModel.BeginCreateAsync();
        var draft = viewModel.SelectedNode!;
        draft.DraftName = "Campaign";
        await viewModel.CommitEditAsync(addAnotherSibling: true);

        Assert.Contains(repository.Snapshot.Groups, group => group.Name == "Campaign");
        Assert.True(viewModel.SelectedNode!.IsDraft);
        Assert.True(viewModel.SelectedNode.IsEditing);
        Assert.Equal(0, openedTabs);
    }

    [Fact]
    public void NormalSelectionDoesNotOpenTab_ButExplicitOpenDoes()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        WorkspaceTreeSelection? opened = null;
        viewModel.OpenInTabRequested += (_, selection) => opened = selection;
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var group = Assert.Single(Assert.Single(viewModel.Roots).Children);

        viewModel.SelectNodeCommand.Execute(group);
        Assert.Null(opened);

        viewModel.OpenInTabCommand.Execute(group);
        Assert.Equal(group.EntityId, opened!.Id);
    }

    [Fact]
    public async Task CutPasteMovesGroupToSelectedNicheAndClearsCutState()
    {
        var sample = Sample.Create(withGroup: true);
        var otherNiche = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [.. sample.Snapshot.Niches, otherNiche] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var source = viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id).Children.Single();
        var destination = viewModel.Roots.Single(root => root.EntityId == otherNiche.Id);

        viewModel.SelectNodeCommand.Execute(source);
        viewModel.CutCommand.Execute(null);
        Assert.True(source.IsCut);
        viewModel.SelectNodeCommand.Execute(destination);
        await viewModel.PasteAsync();

        Assert.Equal(otherNiche.Id, repository.Snapshot.Groups.Single().NicheId);
        Assert.False(viewModel.SelectedNode!.IsCut);
    }

    [Fact]
    public void FilteringPreservesCanonicalSelectionAndRestoresPreFilterExpansion()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var root = Assert.Single(viewModel.Roots);
        var group = Assert.Single(root.Children);
        root.IsExpanded = true;
        viewModel.SelectNodeCommand.Execute(group);

        viewModel.QueryText = "no matching group";
        Assert.Empty(viewModel.Roots);

        viewModel.QueryText = string.Empty;
        root = Assert.Single(viewModel.Roots);
        Assert.True(root.IsExpanded);
        Assert.Equal(group.EntityId, viewModel.SelectedNode!.EntityId);
    }

    [Fact]
    public void ClipboardStateEnablesPasteOnGroupContextRows()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var group = Assert.Single(Assert.Single(viewModel.Roots).Children);
        Assert.False(group.CanPaste);

        viewModel.SelectNodeCommand.Execute(group);
        viewModel.Copy();

        Assert.True(group.CanPaste);
    }

    [Fact]
    public async Task ConfirmedDeleteRaisesDeletedEntitiesClearsClipboardAndSelectsParent()
    {
        var sample = Sample.Create(withGroup: true);
        var root = Assert.Single(sample.Snapshot.Groups);
        var child = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, root.Id, "Child", null, false, sample.Now, sample.Now, "{}");
        var listing = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, child.Id, "Item", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Groups = [root, child], Listings = [listing] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var rootNode = Assert.Single(Assert.Single(viewModel.Roots).Children);
        var childNode = Assert.Single(rootNode.Children);
        IReadOnlySet<Guid>? deletedIds = null;
        viewModel.EntitiesDeleted += (_, ids) => deletedIds = ids;
        viewModel.SelectNodeCommand.Execute(childNode);
        viewModel.Cut();

        await viewModel.DeleteGroupAsync(child.Id, ConfirmPermanentDeletion: true);

        Assert.Equal(root.Id, viewModel.SelectedNode!.EntityId);
        Assert.Contains(child.Id, deletedIds!);
        Assert.Contains(listing.Id, deletedIds!);
        Assert.False(viewModel.SelectedNode.CanPaste);
        Assert.DoesNotContain(repository.Snapshot.Groups, group => group.Id == child.Id);
    }

    [Fact]
    public void DeleteConfirmationNamesIrreversibleSubgroupAndItemLoss()
    {
        var impact = new GroupDeleteImpact(Guid.NewGuid(), "Campaign", 2, 3, new HashSet<Guid>());

        var confirmation = new GroupDeleteConfirmationViewModel(impact);

        Assert.Contains("Campaign", confirmation.Title);
        Assert.Contains("2 subgroups", confirmation.WarningMessage);
        Assert.Contains("3 items", confirmation.WarningMessage);
    }

    [Fact]
    public void DragValidationRejectsDescendantsAndFilteredSiblingPlacementWithFeedback()
    {
        var sample = Sample.Create(withGroup: true);
        var root = Assert.Single(sample.Snapshot.Groups);
        var child = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, root.Id, "Child", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Groups = [root, child] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var rootNode = Assert.Single(Assert.Single(viewModel.Roots).Children);
        var childNode = Assert.Single(rootNode.Children);

        var descendantAllowed = viewModel.CanDrop(root.Id, childNode, new GroupPlacement(), out var descendantError);
        viewModel.QueryText = "Child";
        var filteredAllowed = viewModel.CanDrop(
            child.Id,
            viewModel.SelectedNode ?? viewModel.Roots.Single().Children.Single().Children.Single(),
            new GroupPlacement(GroupPlacementKind.Before, child.Id),
            out var filteredError);
        viewModel.ShowDropFeedback(descendantError);

        Assert.False(descendantAllowed);
        Assert.Contains("descendants", descendantError, StringComparison.OrdinalIgnoreCase);
        Assert.False(filteredAllowed);
        Assert.Contains("filtering", filteredError, StringComparison.OrdinalIgnoreCase);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public async Task MoveSaveFailureRetainsConfirmedTreeSelectionAndShowsRecoverableError()
    {
        var sample = Sample.Create(withGroup: true);
        var otherNiche = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [sample.Niche, otherNiche] };
        var repository = new TestRepository(snapshot) { FailSaves = true };
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var source = viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id).Children.Single();
        var destination = viewModel.Roots.Single(root => root.EntityId == otherNiche.Id);
        viewModel.SelectNodeCommand.Execute(source);

        await viewModel.MoveAsync(source.EntityId, destination, new GroupPlacement());

        Assert.Equal(source.EntityId, viewModel.SelectedNode!.EntityId);
        Assert.Single(viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id).Children);
        Assert.Empty(viewModel.Roots.Single(root => root.EntityId == otherNiche.Id).Children);
        Assert.True(viewModel.HasError);
        Assert.Contains("could not be saved", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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

    private sealed record Sample(WorkspaceSnapshot Snapshot, Store Store, Niche Niche, DateTimeOffset Now)
    {
        public static Sample Create(bool withGroup = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Coffee", null, false, now, now, "{}");
            var groups = withGroup
                ? new[] { new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Campaign", null, false, now, now, "{}") }
                : [];
            return new Sample(new WorkspaceSnapshot([store], [niche], groups, [], [], [], [], [], []), store, niche, now);
        }
    }
}
