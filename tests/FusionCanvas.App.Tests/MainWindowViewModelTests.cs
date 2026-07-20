using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void OpenFromNavigation_OpensTabAndCoordinatesNavigationAndWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var navigationContext = ReadyListingContext(viewModel);

        viewModel.OpenFromNavigation(navigationContext);

        Assert.Single(viewModel.DocumentWindow.Tabs);
        Assert.Equal(navigationContext.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(navigationContext.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.RevealedNodeId);
        Assert.Equal(navigationContext.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.SelectedNodeId);
        Assert.Equal(WorkflowStage.Design, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void OpenFromNavigation_DoesNotDiscardExistingTabs()
    {
        var viewModel = new MainWindowViewModel();
        var first = DraftListingContext(viewModel);
        var second = ActiveListingContext(viewModel);

        viewModel.OpenFromNavigation(first);
        viewModel.OpenFromNavigation(second);

        Assert.Equal(2, viewModel.DocumentWindow.Tabs.Count);
        Assert.Equal(second.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
    }

    [Fact]
    public void SelectingOpenTab_RecoordinatesNavigationAndWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var draft = DraftListingContext(viewModel);
        var active = ActiveListingContext(viewModel);
        var first = viewModel.DocumentWindow.Open(draft.Context);
        viewModel.DocumentWindow.Open(active.Context);

        viewModel.DocumentWindow.SelectTab(first);

        Assert.Equal(draft.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(draft.Context.Id, viewModel.WorkspaceTree.SelectedNode?.EntityId);
        Assert.Equal(draft.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.RevealedNodeId);
        Assert.Equal(WorkflowStage.Idea, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void SelectWorkflowStage_UpdatesDocumentAndNavigator()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(DraftListingContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Listing);

        Assert.Equal(WorkflowStage.Listing, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.Equal("listing-stage-tool", viewModel.DocumentWindow.ActiveDetailViewKey);
        Assert.Equal(WorkflowStage.Listing, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void ClosingLastTab_KeepsWorkingContextAndWorkflowNavigator()
    {
        var viewModel = new MainWindowViewModel();
        var tab = viewModel.DocumentWindow.Open(DraftListingContext(viewModel).Context);

        viewModel.DocumentWindow.CloseTab(tab);

        Assert.True(viewModel.DocumentWindow.HasActiveDocument);
        Assert.True(viewModel.WorkflowNavigator.HasActiveItem);
    }

    [Fact]
    public void NormalTreeSelectionReusesCurrentTab_WhileControlOpenAddsAndKeepsExistingTab()
    {
        var viewModel = new MainWindowViewModel();
        var nicheNode = Assert.Single(viewModel.WorkspaceTree.Roots);
        var groupNode = nicheNode.Children.First(node => node.EntityKind == WorkspaceEntityKind.Group);
        var listingNode = groupNode.Children.First(node => node.EntityKind == WorkspaceEntityKind.Listing);

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(nicheNode);
        Assert.Single(viewModel.DocumentWindow.Tabs);
        Assert.Equal(nicheNode.EntityId, viewModel.DocumentWindow.ActiveContext?.Id);

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(groupNode);
        Assert.Single(viewModel.DocumentWindow.Tabs);
        Assert.Equal(groupNode.EntityId, viewModel.DocumentWindow.ActiveContext?.Id);

        viewModel.WorkspaceTree.OpenInTabCommand.Execute(listingNode);
        Assert.Equal(2, viewModel.DocumentWindow.Tabs.Count);
        Assert.Contains(viewModel.DocumentWindow.Tabs, tab => tab.Context.Id == groupNode.EntityId);
        Assert.Equal(listingNode.EntityId, viewModel.DocumentWindow.ActiveContext?.Id);
    }

    [Fact]
    public void OpenFromNavigation_ResolvesVisibleToolContextScope()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFromNavigation(GroupContext(viewModel));

        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Equal("Idea", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("Topic: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
        Assert.Contains("Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeDescription);
    }

    [Fact]
    public void ChangeToolScopeCommand_ReResolvesVisibleToolScope()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.DocumentWindow.ChangeToolScopeCommand.Execute(ToolContextScopeKind.CurrentSubtree);

        Assert.Equal("Subtree: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
    }

    [Fact]
    public void SelectWorkflowStage_RefreshesStageToolHost()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(DraftListingContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Design);

        Assert.Equal("Design", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("built-in-design-tool", viewModel.DocumentWindow.SelectedStageTool?.Id);
        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
    }

    [Fact]
    public void TopicContextInItemBoundStage_ShowsItemRequiredState()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Concept);

        Assert.False(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Contains("requires a selected item", viewModel.DocumentWindow.ActiveStageToolUnavailableMessage);
    }

    [Fact]
    public async Task SelectingWorkspace_RefreshesStoresAndNavigationForWorkspace()
    {
        var now = DateTimeOffset.UtcNow;
        var personal = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Personal", null, false, now, now, "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Client", null, false, now, now, "{}");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var clientStore = new Store(Guid.NewGuid(), client.Id, "Client Store", null, false, now, now, "{}");
        var clientNiche = new Niche(Guid.NewGuid(), clientStore.Id, "Client Niche", null, false, now, now, "{}");
        var clientListing = new Listing(Guid.NewGuid(), clientStore.Id, clientNiche.Id, null, "Client Listing", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore, clientStore], [clientNiche], [], [clientListing], [], [], [], [], [], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new FusionCanvas.App.DocumentWindow.DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot);

        var clientWorkspace = viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id);
        await viewModel.WorkspaceManagement.SelectWorkspaceAsync(clientWorkspace, TestContext.Current.CancellationToken);

        Assert.Equal(client.Id, viewModel.WorkspaceManagement.SelectedWorkspace?.Id);
        Assert.DoesNotContain(viewModel.StoreManagement.ActiveStores, store => store.Id == personalStore.Id);
        Assert.Contains(viewModel.StoreManagement.ActiveStores, store => store.Id == clientStore.Id);
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Title == "Client Listing");
    }

    [Fact]
    public async Task WorkspaceManagementOpen_DefersFirstStorePromptUntilClosed()
    {
        var now = DateTimeOffset.UtcNow;
        var personal = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Alpha Personal", null, false, now, now, "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Zulu Client", null, false, now, now, "{}");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new FusionCanvas.App.DocumentWindow.DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot);

        viewModel.WorkspaceManagement.OpenWorkspaceManagementCommand.Execute(null);
        await viewModel.WorkspaceManagement.SelectWorkspaceAsync(viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id), TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShouldShowFirstStorePrompt);

        viewModel.WorkspaceManagement.CloseWorkspaceManagementCommand.Execute(null);

        Assert.True(viewModel.ShouldShowFirstStorePrompt);
    }

    [Fact]
    public void GroupActions_FollowCanonicalSelectionCoordinatedFromTabsAndTree()
    {
        var viewModel = new MainWindowViewModel();
        var niche = viewModel.NavigationContexts.Single(context => context.Context.EntityKind == WorkspaceEntityKind.Niche);
        var group = GroupContext(viewModel);
        var nicheNode = Assert.Single(viewModel.WorkspaceTree.Roots);
        var groupNode = viewModel.WorkspaceTree.Roots.SelectMany(root => root.Children).Single(node => node.EntityId == group.Context.Id);

        viewModel.OpenFromNavigation(niche);
        Assert.True(viewModel.CanCreateGroup);
        Assert.False(viewModel.CanManageGroup);

        viewModel.OpenFromNavigation(group);
        Assert.True(viewModel.CanCreateGroup);
        Assert.True(viewModel.CanManageGroup);
        Assert.Equal(group.Context.Id, viewModel.WorkspaceTree.SelectedNode?.EntityId);

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(nicheNode);
        Assert.False(viewModel.CanManageGroup);

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(groupNode);
        Assert.True(viewModel.CanManageGroup);
    }

    [Fact]
    public async Task PermanentGroupDeletionClosesTabsForDeletedEntities()
    {
        var viewModel = new MainWindowViewModel();
        var groupNode = viewModel.WorkspaceTree.Roots
            .SelectMany(root => root.Children)
            .Single(node => node.EntityId == GroupContext(viewModel).Context.Id);
        viewModel.WorkspaceTree.OpenInTabCommand.Execute(groupNode);
        Assert.Equal(groupNode.EntityId, viewModel.DocumentWindow.ActiveContext!.Id);

        await viewModel.WorkspaceTree.DeleteGroupAsync(groupNode.EntityId, ConfirmPermanentDeletion: true);

        Assert.DoesNotContain(viewModel.DocumentWindow.Tabs, tab => tab.Context.Id == groupNode.EntityId);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == groupNode.EntityId);
    }

    [Fact]
    public async Task GroupChanges_RefreshNavigationRevealResultsAndPreserveDocumentContextOnArchive()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var snapshot = WorkspaceSnapshot.FromStores([store], [niche], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new FusionCanvas.App.DocumentWindow.DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot);
        var nicheContext = Assert.Single(viewModel.NavigationContexts, context => context.Context.EntityKind == WorkspaceEntityKind.Niche);
        viewModel.OpenFromNavigation(nicheContext);
        await viewModel.GroupManagement.OpenForCreateAsync(store.Id, niche.Id, new GroupParentReference(WorkspaceEntityKind.Niche, niche.Id));
        viewModel.GroupManagement.Name = "Campaign";

        await viewModel.GroupManagement.SaveAsync();

        var created = Assert.Single(repository.Snapshot.Groups);
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == created.Id);
        Assert.Equal(created.Id, viewModel.NavigationState.SelectedNodeId);
        var groupContext = viewModel.NavigationContexts.Single(context => context.Context.Id == created.Id);
        viewModel.OpenFromNavigation(groupContext);
        var documentId = viewModel.DocumentWindow.ActiveContext?.Id;

        viewModel.GroupManagement.TryRequestArchive();
        await viewModel.GroupManagement.ConfirmArchiveAsync();

        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == created.Id);
        Assert.Equal(niche.Id, viewModel.NavigationState.SelectedNodeId);
        Assert.Equal(documentId, viewModel.DocumentWindow.ActiveContext?.Id);

        var archived = Assert.Single(viewModel.GroupManagement.ArchivedGroups);
        await viewModel.GroupManagement.TryRestoreAsync(archived);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == created.Id);
        Assert.Equal(created.Id, viewModel.NavigationState.SelectedNodeId);
    }

    private static NavigationDocumentContext GroupContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Group &&
            context.Context.Title == "Dogs and coffee");

    private static NavigationDocumentContext DraftListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Morning coffee idea");

    private static NavigationDocumentContext ReadyListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Retro mug design");

    private static NavigationDocumentContext ActiveListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Espresso listing draft");

    [Fact]
    public void ListingInspector_LoadsForActiveListingTabAndClearsForNonItemContext()
    {
        var viewModel = new MainWindowViewModel();
        var listing = ReadyListingContext(viewModel);
        var group = GroupContext(viewModel);

        viewModel.OpenFromNavigation(listing);

        Assert.True(viewModel.ListingInspector.HasState);
        Assert.Equal(listing.Context.Id, viewModel.ListingInspector.LoadedListingId);

        viewModel.OpenFromNavigation(group);

        Assert.False(viewModel.ListingInspector.HasState);
    }

    [Fact]
    public void DirtyInspector_GuardsTabSwitchUntilResolved()
    {
        var viewModel = new MainWindowViewModel();
        var first = viewModel.DocumentWindow.Open(ReadyListingContext(viewModel).Context);
        var secondTab = viewModel.DocumentWindow.Open(ActiveListingContext(viewModel).Context);
        viewModel.RequestSelectTabCommand.Execute(first);

        Assert.True(viewModel.ListingInspector.HasState);
        viewModel.ListingInspector.Title = "Edited unsaved title";
        Assert.True(viewModel.ListingInspector.HasUnsavedChanges);

        viewModel.RequestSelectTabCommand.Execute(secondTab);

        Assert.Equal(first.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.True(viewModel.ListingInspector.DiscardPromptVisible);

        viewModel.ListingInspector.KeepEditingCommand.Execute(null);

        Assert.False(viewModel.ListingInspector.DiscardPromptVisible);
        Assert.Equal(first.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.True(viewModel.ListingInspector.HasUnsavedChanges);
    }

    [Fact]
    public void DirtyInspector_DiscardAndContinueProceedsWithTransition()
    {
        var viewModel = new MainWindowViewModel();
        var first = viewModel.DocumentWindow.Open(ReadyListingContext(viewModel).Context);
        var secondTab = viewModel.DocumentWindow.Open(ActiveListingContext(viewModel).Context);
        viewModel.RequestSelectTabCommand.Execute(first);
        viewModel.ListingInspector.Title = "Edited unsaved title";

        viewModel.RequestSelectTabCommand.Execute(secondTab);
        viewModel.ListingInspector.DiscardAndContinueCommand.Execute(null);

        Assert.Equal(secondTab.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.False(viewModel.ListingInspector.HasUnsavedChanges);
    }

    [Fact]
    public async Task DirtyInspector_SaveAndContinueProceedsAfterPersisting()
    {
        var viewModel = new MainWindowViewModel();
        var first = viewModel.DocumentWindow.Open(ReadyListingContext(viewModel).Context);
        var secondTab = viewModel.DocumentWindow.Open(ActiveListingContext(viewModel).Context);
        viewModel.RequestSelectTabCommand.Execute(first);
        viewModel.ListingInspector.Title = "Persisted title";
        Assert.True(viewModel.ListingInspector.HasUnsavedChanges);

        viewModel.RequestSelectTabCommand.Execute(secondTab);
        viewModel.ListingInspector.SaveAndContinueCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(secondTab.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.False(viewModel.ListingInspector.HasUnsavedChanges);
        Assert.False(viewModel.ListingInspector.DiscardPromptVisible);

        viewModel.RequestSelectTabCommand.Execute(first);
        await Task.Yield();
        Assert.Equal("Persisted title", viewModel.ListingInspector.Title);
    }

    [Fact]
    public void StageMoveControls_ReflectActiveListingBoundaries()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFromNavigation(DraftListingContext(viewModel));
        Assert.False(viewModel.CanMoveStageBack);
        Assert.True(viewModel.CanMoveStageForward);

        viewModel.OpenFromNavigation(ActiveListingContext(viewModel));
        Assert.True(viewModel.CanMoveStageBack);
        Assert.False(viewModel.CanMoveStageForward);
    }

    [Fact]
    public async Task MoveStageForward_AdvancesStageAndUpdatesView()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(ReadyListingContext(viewModel));

        viewModel.MoveStageForwardCommand.Execute(null);
        await Task.Delay(200);

        Assert.Null(viewModel.StageMoveError);
        Assert.Equal(WorkflowStage.Listing, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.True(viewModel.WorkflowNavigator.Stages.Single(stage => stage.Stage == WorkflowStage.Listing).IsCurrent);
        Assert.False(viewModel.CanMoveStageForward);
    }

    [Fact]
    public async Task MoveStageBack_RegressesStage()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(ReadyListingContext(viewModel));

        viewModel.MoveStageBackCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(WorkflowStage.Concept, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.True(viewModel.WorkflowNavigator.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsCurrent);
    }

    [Fact]
    public async Task SetListingStatus_ChangesStatusWithoutMovingStage()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(ReadyListingContext(viewModel));

        viewModel.SetListingStatusCommand.Execute(ListingStatus.Paused);
        await Task.Delay(100);

        Assert.Equal(ListingStatus.Paused, viewModel.ActiveListingStatus);
        Assert.Equal(WorkflowStage.Design, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
    }

    [Fact]
    public async Task StageMoveControls_DisabledForRejectedListing()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(ReadyListingContext(viewModel));
        viewModel.SetListingStatusCommand.Execute(ListingStatus.Rejected);
        await Task.Delay(100);

        Assert.False(viewModel.CanMoveStageForward);
        Assert.False(viewModel.CanMoveStageBack);
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public WorkspaceSnapshot Snapshot => _snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
