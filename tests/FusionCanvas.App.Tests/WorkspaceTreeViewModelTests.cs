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

    [Fact]
    public async Task ListingInlineCaptureRenameAndExplicitTabFlowUseCanonicalSelection()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listings = new ListingManagementService(repository);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot, listings: listings);
        var openedTabs = 0;
        viewModel.OpenInTabRequested += (_, _) => openedTabs++;
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        await viewModel.BeginCreateListingAsync();
        Assert.True(viewModel.SelectedNode!.IsDraft);
        Assert.Equal(WorkspaceEntityKind.Listing, viewModel.SelectedNode.EntityKind);
        viewModel.SelectedNode.DraftName = " Zebra idea ";
        await viewModel.CommitEditAsync();
        Assert.Equal("Zebra idea", viewModel.SelectedNode!.Name);
        Assert.Equal(0, openedTabs);

        viewModel.BeginRename();
        viewModel.SelectedNode.DraftName = "Alpha idea";
        await viewModel.CommitEditAsync();
        Assert.Equal("Alpha idea", viewModel.SelectedNode!.Name);
        viewModel.OpenInTabCommand.Execute(viewModel.SelectedNode);
        Assert.Equal(1, openedTabs);
    }

    [Fact]
    public async Task ListingTypedCopyCutPasteAndDropUseTopicDestinationsAndAlphabeticalProjection()
    {
        var sample = Sample.Create();
        var other = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var first = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Zulu", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var second = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Alpha", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [sample.Niche, other], Listings = [first, second] };
        var repository = new TestRepository(snapshot);
        var listings = new ListingManagementService(repository);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot, listings: listings);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var sourceRoot = viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id);
        Assert.Equal(["Alpha", "Zulu"], sourceRoot.Children.Select(node => node.Name));
        var source = sourceRoot.Children.Single(node => node.EntityId == first.Id);
        var target = viewModel.Roots.Single(root => root.EntityId == other.Id);

        Assert.True(viewModel.CanDrop(WorkspaceEntityKind.Listing, first.Id, target, new GroupPlacement(GroupPlacementKind.Before, second.Id), out _));
        await viewModel.MoveAsync(WorkspaceEntityKind.Listing, first.Id, target, new GroupPlacement(GroupPlacementKind.Before, second.Id));
        Assert.Equal(other.Id, repository.Snapshot.Listings.Single(item => item.Id == first.Id).NicheId);

        viewModel.Copy();
        viewModel.SelectNodeCommand.Execute(target);
        await viewModel.PasteAsync();
        Assert.Equal(3, repository.Snapshot.Listings.Count);
    }

    [Fact]
    public void TagFilter_ExcludesArchivedTagsAndFiltersListingsWithAndSemantics()
    {
        var sample = Sample.Create();
        var firstTag = new Tag(Guid.NewGuid(), sample.Store.Id, "evergreen", null, false, sample.Now, sample.Now, "{}", "#1ABC9C");
        var secondTag = new Tag(Guid.NewGuid(), sample.Store.Id, "seasonal", null, false, sample.Now, sample.Now, "{}", "#FF8800");
        var archivedTag = new Tag(Guid.NewGuid(), sample.Store.Id, "paused", null, true, sample.Now, sample.Now, "{}", null);
        var first = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Matching", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var second = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Missing", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Listings = [first, second],
            Tags = [firstTag, secondTag, archivedTag],
            ListingTags = [new ListingTag(first.Id, firstTag.Id), new ListingTag(first.Id, secondTag.Id)]
        };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        Assert.Equal(2, viewModel.AvailableTagFilters.Count);
        Assert.DoesNotContain(viewModel.AvailableTagFilters, tag => tag.IsArchived);

        viewModel.ToggleTagFilter(firstTag.Id);
        Assert.Equal("Matching", Assert.Single(Assert.Single(viewModel.Roots).Children).Name);
        Assert.True(viewModel.IsTagFilterActive);

        viewModel.ToggleTagFilter(secondTag.Id);
        Assert.Equal("Matching", Assert.Single(Assert.Single(viewModel.Roots).Children).Name);

        viewModel.ToggleTagFilter(firstTag.Id);
        Assert.Equal("Matching", Assert.Single(Assert.Single(viewModel.Roots).Children).Name);

        viewModel.ToggleTagFilter(secondTag.Id);
        Assert.False(viewModel.IsTagFilterActive);
        Assert.Equal(2, Assert.Single(viewModel.Roots).Children.Count);
    }

    [Fact]
    public void TagFilter_PreservesCanonicalSelectionAndReportsFilteredOutSelection()
    {
        var sample = Sample.Create();
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "evergreen", null, false, sample.Now, sample.Now, "{}", null);
        var matching = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Matching", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var hidden = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Hidden", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Listings = [matching, hidden],
            Tags = [tag],
            ListingTags = [new ListingTag(matching.Id, tag.Id)]
        };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        viewModel.SelectEntity(hidden.Id);
        Assert.Equal(hidden.Id, viewModel.SelectedNode!.EntityId);

        viewModel.ToggleTagFilter(tag.Id);
        Assert.Null(viewModel.SelectedNode);
        Assert.True(viewModel.HasFilteredOutSelection);
        Assert.Contains("Hidden", viewModel.FilteredOutSelectionMessage);

        viewModel.ClearTagFilters();
        Assert.False(viewModel.HasFilteredOutSelection);
        Assert.Equal(hidden.Id, viewModel.SelectedNode!.EntityId);
    }

    [Fact]
    public void TagFilter_RestoresPreFilterExpansionWhenCleared()
    {
        var sample = Sample.Create();
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "evergreen", null, false, sample.Now, sample.Now, "{}", null);
        var listing = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Tagged", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Listings = [listing], Tags = [tag], ListingTags = [new ListingTag(listing.Id, tag.Id)] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var niche = Assert.Single(viewModel.Roots);
        niche.IsExpanded = true;

        viewModel.ToggleTagFilter(tag.Id);
        Assert.True(Assert.Single(viewModel.Roots).IsExpanded);

        viewModel.ClearTagFilters();
        niche = Assert.Single(viewModel.Roots);
        Assert.True(niche.IsExpanded);
        Assert.Single(niche.Children);
    }

    [Fact]
    public void TagFilter_DropsTagsThatLeaveTheActiveStore()
    {
        var sample = Sample.Create();
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "evergreen", null, false, sample.Now, sample.Now, "{}", null);
        var snapshot = sample.Snapshot with { Tags = [tag] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        Assert.True(viewModel.HasTagFiltersAvailable);

        var otherStore = new Store(Guid.NewGuid(), "Other", null, false, sample.Now, sample.Now, "{}", Guid.NewGuid());
        var otherNiche = new Niche(otherStore.DefaultNicheId!.Value, otherStore.Id, "OtherNiche", null, false, sample.Now, sample.Now, "{}");
        var otherSnapshot = new WorkspaceSnapshot([otherStore], [otherNiche], [], [], [], [], [], [], []);
        viewModel.SetStore(otherStore.Id, otherSnapshot);

        Assert.Empty(viewModel.AvailableTagFilters);
        Assert.False(viewModel.IsTagFilterActive);
    }

    [Fact]
    public void ListingNodesCarryAppliedTagColorsAndOverflowAfterThree()
    {
        var sample = Sample.Create();
        var first = new Tag(Guid.NewGuid(), sample.Store.Id, "alpha", null, false, sample.Now, sample.Now, "{}", "#1abc9c");
        var second = new Tag(Guid.NewGuid(), sample.Store.Id, "beta", null, false, sample.Now, sample.Now, "{}", "#ff8800");
        var third = new Tag(Guid.NewGuid(), sample.Store.Id, "gamma", null, false, sample.Now, sample.Now, "{}", null);
        var fourth = new Tag(Guid.NewGuid(), sample.Store.Id, "delta", null, false, sample.Now, sample.Now, "{}", "#ff0000");
        var listing = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Tagged", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Listings = [listing],
            Tags = [first, second, third, fourth],
            ListingTags = [
                new ListingTag(listing.Id, first.Id),
                new ListingTag(listing.Id, second.Id),
                new ListingTag(listing.Id, third.Id),
                new ListingTag(listing.Id, fourth.Id)
            ]
        };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        var listingNode = viewModel.Roots.Single().Children.Single();
        Assert.Equal(WorkspaceEntityKind.Listing, listingNode.EntityKind);
        Assert.Equal(4, listingNode.AppliedTagColors.Count);
        Assert.Equal(3, listingNode.VisibleTagChipCount);
        Assert.Equal(1, listingNode.HiddenTagCount);
        Assert.True(listingNode.HasAppliedTags);
        Assert.True(listingNode.HasHiddenTags);
        Assert.Equal("+1", listingNode.HiddenTagLabel);
        Assert.Equal("#1ABC9C", listingNode.AppliedTagColors[0]);
        Assert.Equal("#FF8800", listingNode.AppliedTagColors[1]);
        Assert.Equal("#FF0000", listingNode.AppliedTagColors[2]);
        Assert.Equal("#243447", listingNode.AppliedTagColors[3]);
    }

    [Fact]
    public void ListingNodesHaveNoAppliedTagColorsWhenUntagged()
    {
        var sample = Sample.Create();
        var listing = new Listing(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Untagged", null, ListingStatus.Draft, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Listings = [listing] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        var listingNode = viewModel.Roots.Single().Children.Single();
        Assert.False(listingNode.HasAppliedTags);
        Assert.Empty(listingNode.AppliedTagColors);
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
