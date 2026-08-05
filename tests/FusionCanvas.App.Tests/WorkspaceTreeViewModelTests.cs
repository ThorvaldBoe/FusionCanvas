using FusionCanvas.App.Navigation;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Items;
using FusionCanvas.Integration.Items;

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

        Assert.False(viewModel.HasEditingNode);
        await viewModel.BeginCreateAsync();
        Assert.True(viewModel.HasEditingNode);
        var draft = viewModel.SelectedNode!;
        draft.DraftName = "Campaign";
        await viewModel.CommitEditAsync(addAnotherSibling: true);

        Assert.Contains(repository.Snapshot.Groups, group => group.Name == "Campaign");
        Assert.True(viewModel.HasEditingNode);
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
        var listing = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, child.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Groups = [root, child], Items = [listing] };
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
    public async Task ItemInlineCaptureRenameAndExplicitTabFlowUseCanonicalSelection()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var items = new ItemManagementService(repository);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot, items: items);
        var openedTabs = 0;
        viewModel.OpenInTabRequested += (_, _) => openedTabs++;
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        await viewModel.BeginCreateItemAsync();
        Assert.True(viewModel.HasEditingNode);
        Assert.True(viewModel.SelectedNode!.IsDraft);
        Assert.Equal(WorkspaceEntityKind.Item, viewModel.SelectedNode.EntityKind);
        viewModel.SelectedNode.DraftName = " Zebra idea ";
        await viewModel.CommitEditAsync();
        Assert.False(viewModel.HasEditingNode);
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
    public async Task ItemTypedCopyCutPasteAndDropUseTopicDestinationsAndAlphabeticalProjection()
    {
        var sample = Sample.Create();
        var other = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var first = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Zulu", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var second = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Alpha", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [sample.Niche, other], Items = [first, second] };
        var repository = new TestRepository(snapshot);
        var items = new ItemManagementService(repository);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot, items: items);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var sourceRoot = viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id);
        Assert.Equal(["Alpha", "Zulu"], sourceRoot.Children.Select(node => node.Name));
        var source = sourceRoot.Children.Single(node => node.EntityId == first.Id);
        var target = viewModel.Roots.Single(root => root.EntityId == other.Id);

        Assert.True(viewModel.CanDrop(WorkspaceEntityKind.Item, first.Id, target, new GroupPlacement(GroupPlacementKind.Before, second.Id), out _));
        await viewModel.MoveAsync(WorkspaceEntityKind.Item, first.Id, target, new GroupPlacement(GroupPlacementKind.Before, second.Id));
        Assert.Equal(other.Id, repository.Snapshot.Items.Single(item => item.Id == first.Id).NicheId);

        viewModel.Copy();
        viewModel.SelectNodeCommand.Execute(target);
        await viewModel.PasteAsync();
        Assert.Equal(3, repository.Snapshot.Items.Count);
    }

    [Fact]
    public void TagFilter_NarrowsToItemsAndGuardsSiblingPositioning()
    {
        var sample = Sample.Create(withGroup: true);
        var tagged = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Tagged", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var untagged = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Untagged", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "Halloween", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Items = [tagged, untagged],
            Tags = [tag],
            ItemTags = [new ItemTag(tagged.Id, tag.Id)]
        };
        var group = snapshot.Groups.Single();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        Assert.Equal(3, viewModel.Roots.Single().Children.Count);

        viewModel.ToggleTagFilter(tag.Id);

        var remaining = viewModel.Roots.Single().Children.Single();
        Assert.Equal(tagged.Id, remaining.EntityId);
        Assert.True(viewModel.HasActiveFilters);

        viewModel.SelectNodeCommand.Execute(viewModel.Roots.Single());
        var allowed = viewModel.CanDrop(WorkspaceEntityKind.Group, group.Id, viewModel.Roots.Single(),
            new GroupPlacement(GroupPlacementKind.Before, Guid.NewGuid()), out var error);
        Assert.False(allowed);
        Assert.Contains("filtering", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SubtreeScope_PinsSelectedTopicAndRestrictsTree()
    {
        var sample = Sample.Create(withGroup: true);
        var otherNiche = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [sample.Niche, otherNiche] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        var nicheNode = viewModel.Roots.Single(root => root.EntityId == sample.Niche.Id);

        viewModel.SelectNodeCommand.Execute(nicheNode);
        Assert.True(viewModel.CanScopeToCurrentTopic);

        viewModel.ScopeToCurrentTopic = true;

        Assert.Equal(sample.Niche.Id, Assert.Single(viewModel.Roots).EntityId);
        Assert.DoesNotContain(viewModel.Roots, node => node.EntityId == otherNiche.Id);

        viewModel.ScopeToCurrentTopic = false;
        Assert.Equal(2, viewModel.Roots.Count);
    }

    [Fact]
    public void IncludeArchived_RevealsArchivedItemWithoutMakingItCanonicalContext()
    {
        var sample = Sample.Create();
        var archived = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Ghost", null, ItemStatus.Draft, WorkflowStage.Idea, true, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [archived] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);
        Assert.Empty(viewModel.Roots.Single().Children);

        viewModel.IncludeArchived = true;

        var archivedNode = Assert.Single(viewModel.Roots.Single().Children);
        Assert.True(archivedNode.IsInactive);
        WorkspaceTreeSelection? observed = null;
        viewModel.SelectionChanged += (_, selection) => observed = selection;
        viewModel.SelectNodeCommand.Execute(archivedNode);

        Assert.Equal(archivedNode, viewModel.SelectedNode);
        Assert.Null(observed);
    }

    [Fact]
    public void ClearAllFilters_ResetsEveryDimensionRestoresExpansionAndKeepsSelection()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var nicheRoot = viewModel.Roots.Single();
        var group = Assert.Single(nicheRoot.Children);
        nicheRoot.IsExpanded = true;
        viewModel.SelectNodeCommand.Execute(group);

        viewModel.QueryText = "no matching";
        Assert.True(viewModel.HasEmptyFilterResults);
        Assert.True(viewModel.HasActiveFilters);

        viewModel.ClearAllFilters();

        Assert.False(viewModel.HasActiveFilters);
        Assert.False(viewModel.HasEmptyFilterResults);
        var restoredRoot = viewModel.Roots.Single();
        Assert.True(restoredRoot.IsExpanded);
        Assert.Equal(group.EntityId, viewModel.SelectedNode!.EntityId);
    }

    [Fact]
    public void CanScopeToCurrentTopic_ReflectsSelectionResolvability()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        Assert.False(viewModel.CanScopeToCurrentTopic);

        viewModel.SelectNodeCommand.Execute(viewModel.Roots.Single());
        Assert.True(viewModel.CanScopeToCurrentTopic);

        var group = Assert.Single(viewModel.Roots.Single().Children);
        viewModel.SelectNodeCommand.Execute(group);
        Assert.True(viewModel.CanScopeToCurrentTopic);
    }

    [Fact]
    public void EmptyFilterResults_ExplainedWhenNothingMatches()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        viewModel.QueryText = "zzz nothing matches";

        Assert.True(viewModel.HasActiveFilters);
        Assert.True(viewModel.HasEmptyFilterResults);
        Assert.False(viewModel.HasVisibleResults);
    }

    [Fact]
    public void HasNonTextFilters_ReflectsOnlyNonTextDimensions()
    {
        var sample = Sample.Create(withGroup: true);
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "Halloween", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Tags = [tag] };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        Assert.False(viewModel.HasNonTextFilters);

        viewModel.QueryText = "anything";
        Assert.False(viewModel.HasNonTextFilters);

        viewModel.QueryText = string.Empty;
        viewModel.SetTagSelected(tag.Id, true);
        Assert.True(viewModel.HasNonTextFilters);
        Assert.Equal(1, viewModel.ActiveFilterCount);

        viewModel.SetTagSelected(tag.Id, false);
        viewModel.SelectNodeCommand.Execute(viewModel.Roots.Single());
        viewModel.ScopeToCurrentTopic = true;
        Assert.True(viewModel.HasNonTextFilters);

        viewModel.ScopeToCurrentTopic = false;
        viewModel.IncludeArchived = true;
        Assert.True(viewModel.HasNonTextFilters);
    }

    [Fact]
    public void AvailableTags_ListsActiveStoreTagsAlphabetically()
    {
        var sample = Sample.Create();
        var tea = new Tag(Guid.NewGuid(), sample.Store.Id, "Tea", null, false, sample.Now, sample.Now, "{}");
        var coffee = new Tag(Guid.NewGuid(), sample.Store.Id, "Coffee", null, false, sample.Now, sample.Now, "{}");
        var archived = new Tag(Guid.NewGuid(), sample.Store.Id, "Archived", null, true, sample.Now, sample.Now, "{}");
        var otherStore = new Store(Guid.NewGuid(), "Other", null, false, sample.Now, sample.Now, "{}");
        var otherNiche = new Niche(Guid.NewGuid(), otherStore.Id, "Other niche", null, false, sample.Now, sample.Now, "{}");
        var otherTag = new Tag(Guid.NewGuid(), otherStore.Id, "Other tag", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Stores = [sample.Store, otherStore],
            Niches = [sample.Niche, otherNiche],
            Tags = [tea, coffee, archived, otherTag]
        };
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        Assert.Equal(["Coffee", "Tea"], viewModel.AvailableTags.Select(entry => entry.Name));
    }

    [Fact]
    public void StageFilter_NarrowsToItemsAtSelectedStage()
    {
        var sample = Sample.Create();
        var idea = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Idea listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var design = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Design listing", null, ItemStatus.Draft, WorkflowStage.Design, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [idea, design] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        var allItems = viewModel.Roots.SelectMany(r => r.Children).Where(n => n.IsItem).ToArray();
        Assert.Equal(2, allItems.Length);

        viewModel.StageFilterIndex = 3;
        var designItems = viewModel.Roots.SelectMany(r => r.Children).Where(n => n.IsItem).ToArray();
        Assert.Single(designItems);
        Assert.Equal("Design listing", designItems[0].Name);

        viewModel.StageFilterIndex = 0;
        var restored = viewModel.Roots.SelectMany(r => r.Children).Where(n => n.IsItem).ToArray();
        Assert.Equal(2, restored.Length);
    }

    [Fact]
    public void StatusFilter_NarrowsToItemsWithSelectedStatus()
    {
        var sample = Sample.Create();
        var draft = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Draft listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var rejected = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Rejected listing", null, ItemStatus.Rejected, WorkflowStage.Concept, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [draft, rejected] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        viewModel.StatusFilterIndex = 4;
        var filtered = viewModel.Roots.SelectMany(r => r.Children).Where(n => n.IsItem).ToArray();
        Assert.Single(filtered);
        Assert.Equal("Rejected listing", filtered[0].Name);
        Assert.True(filtered[0].IsInactive);
    }

    [Fact]
    public void RejectedItem_MarkedInactiveInTree()
    {
        var sample = Sample.Create();
        var rejected = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Rejected", null, ItemStatus.Rejected, WorkflowStage.Concept, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [rejected] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        var node = viewModel.Roots.SelectMany(r => r.Children).Single(n => n.IsItem);
        Assert.True(node.IsInactive);
    }

    [Fact]
    public void StageAndStatusFilters_CombineWithAnd()
    {
        var sample = Sample.Create();
        var ideaDraft = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "IdeaDraft", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var ideaRejected = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "IdeaRejected", null, ItemStatus.Rejected, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var designDraft = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "DesignDraft", null, ItemStatus.Draft, WorkflowStage.Design, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [ideaDraft, ideaRejected, designDraft] };
        var viewModel = new WorkspaceTreeViewModel(new TestRepository(snapshot), new GroupManagementService(new TestRepository(snapshot)), snapshot);
        viewModel.SetStore(sample.Store.Id, snapshot);

        viewModel.StageFilterIndex = 1;
        viewModel.StatusFilterIndex = 1;

        var filtered = viewModel.Roots.SelectMany(r => r.Children).Where(n => n.IsItem).ToArray();
        Assert.Single(filtered);
        Assert.Equal("IdeaDraft", filtered[0].Name);
    }

    // --- Tree-actions toolbar: expand/collapse-all toggle behavior ---

    [Fact]
    public void DefaultState_ToggleIsExpandAllAndEnabled()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        Assert.True(viewModel.NextToggleExpands);
        Assert.Equal("Expand all groups", viewModel.ExpandCollapseAllTooltip);
        Assert.True(viewModel.CanToggleExpandCollapseAll);
    }

    [Fact]
    public void FirstToggle_ExpandsEveryTopicNodeIncludingNestedLevels()
    {
        var (snapshot, store, _, _, _) = CreateNestedSample();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(store.Id, snapshot);

        // All topic nodes start collapsed
        var allNodes = FlattenNodes(viewModel.Roots).ToArray();
        foreach (var node in allNodes.Where(n => n.HasChildren))
        {
            Assert.False(node.IsExpanded);
        }

        viewModel.ToggleExpandCollapseAllCommand.Execute(null);

        // After toggle, every topic node with children is expanded
        foreach (var node in allNodes.Where(n => n.HasChildren))
        {
            Assert.True(node.IsExpanded, $"Expected {node.Name} to be expanded");
        }
    }

    [Fact]
    public void SecondToggle_CollapsesEveryTopicNode()
    {
        var (snapshot, store, _, _, _) = CreateNestedSample();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(store.Id, snapshot);

        viewModel.ToggleExpandCollapseAllCommand.Execute(null); // expand all
        viewModel.ToggleExpandCollapseAllCommand.Execute(null); // collapse all

        var allNodes = FlattenNodes(viewModel.Roots).ToArray();
        foreach (var node in allNodes.Where(n => n.HasChildren))
        {
            Assert.False(node.IsExpanded, $"Expected {node.Name} to be collapsed");
        }
    }

    [Fact]
    public void RememberedState_ManualCollapseDoesNotRedirectToggle()
    {
        var (snapshot, store, _, group, _) = CreateNestedSample();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(store.Id, snapshot);

        // Expand all
        viewModel.ToggleExpandCollapseAllCommand.Execute(null);
        Assert.Equal("Collapse all groups", viewModel.ExpandCollapseAllTooltip);

        // Manually collapse one node (the group which has a child subgroup)
        var groupNode = FlattenNodes(viewModel.Roots).First(n => n.EntityId == group.Id);
        Assert.True(groupNode.HasChildren);
        groupNode.IsExpanded = false;

        // Toggle again — still performs collapse (remembered state)
        viewModel.ToggleExpandCollapseAllCommand.Execute(null);

        var allNodes = FlattenNodes(viewModel.Roots).ToArray();
        foreach (var node in allNodes.Where(n => n.HasChildren))
        {
            Assert.False(node.IsExpanded, $"Expected {node.Name} to be collapsed after remembered-state toggle");
        }
    }

    [Fact]
    public async Task ToggleExpansion_SurvivesTreeRefresh()
    {
        var (snapshot, store, _, _, _) = CreateNestedSample();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(store.Id, snapshot);

        viewModel.ToggleExpandCollapseAllCommand.Execute(null);

        // Reload (rebuilds projection)
        await viewModel.ReloadAsync();

        var allNodes = FlattenNodes(viewModel.Roots).ToArray();
        foreach (var node in allNodes.Where(n => n.HasChildren))
        {
            Assert.True(node.IsExpanded, $"Expected {node.Name} to remain expanded after refresh");
        }
    }

    [Fact]
    public void FilterActive_DisablesToggleAndShowsFilterTooltip()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        Assert.True(viewModel.CanToggleExpandCollapseAll);

        viewModel.QueryText = "non-existent";

        Assert.False(viewModel.CanToggleExpandCollapseAll);
        Assert.Equal("Filtering already expands all groups", viewModel.ExpandCollapseAllTooltip);

        viewModel.QueryText = string.Empty;

        Assert.True(viewModel.CanToggleExpandCollapseAll);
        Assert.Equal("Expand all groups", viewModel.ExpandCollapseAllTooltip);
    }

    [Fact]
    public void NoExpandableNodes_DisablesToggleWithNoGroupsTooltip()
    {
        var sample = Sample.Create(withGroup: false);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), sample.Snapshot);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);

        Assert.False(viewModel.CanToggleExpandCollapseAll);
        Assert.Equal("No groups to expand or collapse", viewModel.ExpandCollapseAllTooltip);
    }

    [Fact]
    public async Task CollapseAll_ProtectsDraftAncestorChain()
    {
        var (snapshot, store, _, group, subGroup, unrelatedNiche, unrelatedGroup) = CreateNestedSampleWithUnrelatedBranch();
        var repository = new TestRepository(snapshot);
        var viewModel = new WorkspaceTreeViewModel(repository, new GroupManagementService(repository), snapshot);
        viewModel.SetStore(store.Id, snapshot);

        // Expand all so tree is fully expanded
        viewModel.ToggleExpandCollapseAllCommand.Execute(null);

        // Verify unrelated branch is expanded before collapse
        var unrelatedNicheNode = viewModel.Roots.Single(n => n.EntityId == unrelatedNiche.Id);
        var unrelatedGroupNode = FlattenNodes(viewModel.Roots).First(n => n.EntityId == unrelatedGroup.Id);
        Assert.True(unrelatedNicheNode.IsExpanded, "Unrelated niche should be expanded before collapse");
        Assert.True(unrelatedGroupNode.IsExpanded, "Unrelated group should be expanded before collapse");

        // Begin create on the subgroup to set up a draft
        var subGroupNode = FlattenNodes(viewModel.Roots).First(n => n.EntityId == subGroup.Id);
        viewModel.SelectNodeCommand.Execute(subGroupNode);
        await viewModel.BeginCreateAsync();

        // Verify draft is in editing state
        Assert.NotNull(viewModel.SelectedNode);
        Assert.True(viewModel.SelectedNode!.IsEditing);

        // Now collapse all — the draft's ancestors should stay expanded,
        // but unrelated branches should collapse
        viewModel.ToggleExpandCollapseAllCommand.Execute(null);

        // The draft's parent chain (subgroup -> group -> niche) should be expanded
        var nicheNode = viewModel.Roots.Single(n => n.EntityId == store.DefaultNicheId);
        Assert.True(nicheNode.IsExpanded, "Niche (draft ancestor) should stay expanded");

        var groupNode = nicheNode.Children.Single(n => n.EntityId == group.Id);
        Assert.True(groupNode.IsExpanded, "Group (draft ancestor) should stay expanded");

        var draftParentNode = groupNode.Children.Single(n => n.EntityId == subGroup.Id);
        Assert.True(draftParentNode.IsExpanded, "Subgroup (draft parent) should stay expanded");

        // Unrelated branch nodes must be collapsed
        Assert.False(unrelatedNicheNode.IsExpanded, "Unrelated niche should be collapsed after collapse-all");
        Assert.False(unrelatedGroupNode.IsExpanded, "Unrelated group should be collapsed after collapse-all");

        // The draft node itself (which has no children) was never expanded, verify editing
        Assert.True(viewModel.SelectedNode!.IsEditing, "Draft should remain in editing state");
    }

    private static (WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup Group, TopicGroup SubGroup) CreateNestedSample()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var store = new Store(storeId, "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, storeId, "Coffee", null, false, now, now, "{}");
        var group = new TopicGroup(groupId, storeId, nicheId, null, "Campaign", null, false, now, now, "{}");
        var subGroup = new TopicGroup(subGroupId, storeId, null, groupId, "Sub-campaign", null, false, now, now, "{}");
        var item = new Item(itemId, storeId, nicheId, subGroupId, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

        var snapshot = new WorkspaceSnapshot([store], [niche], [group, subGroup], [item], [], [], [], [], []);
        return (snapshot, store, niche, group, subGroup);
    }

    private static (WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup Group, TopicGroup SubGroup, Niche UnrelatedNiche, TopicGroup UnrelatedGroup) CreateNestedSampleWithUnrelatedBranch()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var unrelatedNicheId = Guid.NewGuid();
        var unrelatedGroupId = Guid.NewGuid();
        var unrelatedItemId = Guid.NewGuid();

        var store = new Store(storeId, "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, storeId, "Coffee", null, false, now, now, "{}");
        var group = new TopicGroup(groupId, storeId, nicheId, null, "Campaign", null, false, now, now, "{}");
        var subGroup = new TopicGroup(subGroupId, storeId, null, groupId, "Sub-campaign", null, false, now, now, "{}");
        var item = new Item(itemId, storeId, nicheId, subGroupId, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var unrelatedNiche = new Niche(unrelatedNicheId, storeId, "Tea", null, false, now, now, "{}");
        var unrelatedGroup = new TopicGroup(unrelatedGroupId, storeId, unrelatedNicheId, null, "Tea Group", null, false, now, now, "{}");
        var unrelatedItem = new Item(unrelatedItemId, storeId, unrelatedNicheId, unrelatedGroupId, "Tea Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

        var snapshot = new WorkspaceSnapshot(
            [store],
            [niche, unrelatedNiche],
            [group, subGroup, unrelatedGroup],
            [item, unrelatedItem],
            [], [], [], [], []);
        return (snapshot, store, niche, group, subGroup, unrelatedNiche, unrelatedGroup);
    }

    private static IEnumerable<WorkspaceTreeNodeViewModel> FlattenNodes(IEnumerable<WorkspaceTreeNodeViewModel> nodes) =>
        nodes.SelectMany(node => new[] { node }.Concat(FlattenNodes(node.Children)));

    [Fact]
    public async Task ExportCsv_WithNullPickerWritesNothing()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var codec = new RecordingCsvCodec(new ItemCsvCodec());
        var viewModel = new WorkspaceTreeViewModel(
            repository,
            new GroupManagementService(repository),
            sample.Snapshot,
            csvCodec: codec);
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var group = Assert.Single(Assert.Single(viewModel.Roots).Children);

        await viewModel.ExportCsvAsync(group);

        Assert.Empty(codec.Writes);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ExportCsv_WritesRowsToChosenDestination()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var codec = new RecordingCsvCodec(new ItemCsvCodec());
        var viewModel = new WorkspaceTreeViewModel(
            repository,
            new GroupManagementService(repository),
            sample.Snapshot,
            csvCodec: codec,
            filePicker: new StubItemCsvFilePicker(() => new MemoryStream()));
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var group = Assert.Single(Assert.Single(viewModel.Roots).Children);

        await viewModel.ExportCsvAsync(group);

        Assert.Single(codec.Writes);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ExportCsv_SurfacesErrorWhenDestinationFails()
    {
        var sample = Sample.Create(withGroup: true);
        var repository = new TestRepository(sample.Snapshot);
        var viewModel = new WorkspaceTreeViewModel(
            repository,
            new GroupManagementService(repository),
            sample.Snapshot,
            filePicker: new StubItemCsvFilePicker(() => new ThrowingStream()));
        viewModel.SetStore(sample.Store.Id, sample.Snapshot);
        var group = Assert.Single(Assert.Single(viewModel.Roots).Children);

        await viewModel.ExportCsvAsync(group);

        Assert.NotNull(viewModel.ErrorMessage);
        Assert.Contains("could not be exported", viewModel.ErrorMessage);
    }

    private sealed class RecordingCsvCodec(IItemCsvCodec inner) : IItemCsvCodec
    {
        public List<IReadOnlyList<ItemCsvRow>> Writes { get; } = [];

        public Task WriteAsync(
            Stream stream,
            IReadOnlyList<ItemCsvRow> rows,
            CancellationToken cancellationToken = default)
        {
            Writes.Add(rows);
            return inner.WriteAsync(stream, rows, cancellationToken);
        }
    }

    private sealed class StubItemCsvFilePicker(Func<Stream?> factory) : IItemCsvFilePicker
    {
        public Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(factory());
        }
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override void Flush() => throw new IOException("write failed");

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            throw new IOException("write failed");
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
