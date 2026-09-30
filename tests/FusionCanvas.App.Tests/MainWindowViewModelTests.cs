using FusionCanvas.App.Stores;
using FusionCanvas.App.Settings;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Application.Ideation;

namespace FusionCanvas.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task StartupAccessStatusRefresh_ObservesFailures()
    {
        var telemetry = new RecordingTelemetryService();
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            telemetryService: telemetry);
        var snapshot = SampleWorkspace.Create();
        var repository = new InMemoryWorkspaceRepository(snapshot);

        _ = MainWindowViewModelFactory.CreateFromSnapshot(
            snapshot,
            repository,
            ideationAccessStatus: new FailingIdeationAccessStatus(),
            settings: settings);

        var entry = await telemetry.Recorded.WaitAsync(
            TimeSpan.FromSeconds(3),
            TestContext.Current.CancellationToken);

        Assert.Equal("Workspace", entry.Area);
        Assert.Equal("CommandFailed", entry.Name);
        Assert.Equal(nameof(InvalidOperationException), entry.Message);
    }

    [Fact]
    public void OpenFromNavigation_OpensTabAndCoordinatesNavigationAndWorkflow()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var navigationContext = ReadyItemContext(viewModel);

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
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var first = DraftItemContext(viewModel);
        var second = ActiveItemContext(viewModel);

        viewModel.OpenFromNavigation(first);
        viewModel.OpenFromNavigation(second);

        Assert.Equal(2, viewModel.DocumentWindow.Tabs.Count);
        Assert.Equal(second.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
    }

    [Fact]
    public void SelectingOpenTab_RecoordinatesNavigationAndWorkflow()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var draft = DraftItemContext(viewModel);
        var active = ActiveItemContext(viewModel);
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
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(DraftItemContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Listing);

        Assert.Equal(WorkflowStage.Listing, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.Equal("listing-stage-tool", viewModel.DocumentWindow.ActiveDetailViewKey);
        Assert.Equal("listing-stage-tool", viewModel.ActiveStageToolContentKey);
        Assert.Equal(StageToolContentKind.Listing, viewModel.ActiveStageToolContent?.Kind);
        Assert.Equal(WorkflowStage.Listing, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void ClosingLastTab_KeepsWorkingContextAndWorkflowNavigator()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var tab = viewModel.DocumentWindow.Open(DraftItemContext(viewModel).Context);

        viewModel.DocumentWindow.CloseTab(tab);

        Assert.True(viewModel.DocumentWindow.HasActiveDocument);
        Assert.True(viewModel.WorkflowNavigator.HasActiveItem);
    }

    [Fact]
    public void NormalTreeSelectionReusesCurrentTab_WhileControlOpenAddsAndKeepsExistingTab()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var nicheNode = Assert.Single(viewModel.WorkspaceTree.Roots);
        var groupNode = nicheNode.Children.First(node => node.EntityKind == WorkspaceEntityKind.Group);
        var listingNode = groupNode.Children.First(node => node.EntityKind == WorkspaceEntityKind.Item);

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
        var viewModel = MainWindowViewModelFactory.CreateSample();

        viewModel.OpenFromNavigation(GroupContext(viewModel));

        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Equal("Idea", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("Topic: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
        Assert.Contains("Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeDescription);
    }

    [Fact]
    public void ChangeToolScopeCommand_ReResolvesVisibleToolScope()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.DocumentWindow.ChangeToolScopeCommand.Execute(ToolContextScopeKind.CurrentSubtree);

        Assert.Equal("Subtree: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
    }

    [Fact]
    public void SelectWorkflowStage_RefreshesStageToolHost()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(DraftItemContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Design);

        Assert.Equal("Design", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("built-in-design-tool", viewModel.DocumentWindow.SelectedStageTool?.Id);
        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
    }

    [Fact]
    public void TopicContextInItemBoundStage_ShowsItemRequiredState()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Concept);

        Assert.False(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Contains("requires a selected item", viewModel.DocumentWindow.ActiveStageToolUnavailableMessage);
    }

    [Fact]
    public void StartupStoreManagementLoadsOnCapturedSynchronizationContext()
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Personal", null, false, now, now, "{}");
        var store = new Store(Guid.NewGuid(), workspace.Id, "Personal Store", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([workspace], [store], [], [], [], [], [], [], [], []);
        var repository = new GateableWorkspaceRepository(snapshot);
        var delayedStoreLoad = repository.DelayAfterAdditionalLoads(2);
        var originalSynchronizationContext = SynchronizationContext.Current;
        using var uiContext = new PumpingSynchronizationContext();
        var uiThreadId = Environment.CurrentManagedThreadId;
        var storeLoadThreadId = 0;
        var storeLoaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SynchronizationContext.SetSynchronizationContext(uiContext);
        try
        {
            var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
            viewModel.StoreManagement.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(StoreManagementViewModel.ActiveStores) &&
                    viewModel.StoreManagement.ActiveStores.Any(candidate => candidate.Id == store.Id))
                {
                    Interlocked.CompareExchange(ref storeLoadThreadId, Environment.CurrentManagedThreadId, 0);
                    storeLoaded.TrySetResult();
                }
            };

            Assert.True(delayedStoreLoad.Started.IsCompleted);
            delayedStoreLoad.Release();
            uiContext.PumpUntil(storeLoaded.Task, TimeSpan.FromSeconds(5));
        }
        finally
        {
            delayedStoreLoad.Release();
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.Equal(uiThreadId, storeLoadThreadId);
    }

    [Fact]
    public async Task SelectingWorkspaceDoesNotWaitForStoreRefreshToComplete()
    {
        var now = DateTimeOffset.UtcNow;
        var personal = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Personal", null, false, now, now, "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Client", null, false, now, now, "{}");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var clientStore = new Store(Guid.NewGuid(), client.Id, "Client Store", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore, clientStore], [], [], [], [], [], [], [], []);
        var repository = new GateableWorkspaceRepository(snapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        var delayedStoreLoad = repository.DelayAfterAdditionalLoads(2);
        var clientWorkspace = viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id);
        var clientStoreApplied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.StoreManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StoreManagementViewModel.ActiveStores) &&
                viewModel.StoreManagement.ActiveStores.Any(store => store.Id == clientStore.Id))
            {
                clientStoreApplied.TrySetResult();
            }
        };

        var cancellationToken = TestContext.Current.CancellationToken;
        var selectionTask = Task.Run(
            () => viewModel.WorkspaceManagement.SelectWorkspaceAsync(clientWorkspace, cancellationToken),
            cancellationToken);
        var selectionReturnedBeforeStoreLoad = false;
        try
        {
            await delayedStoreLoad.Started.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            selectionReturnedBeforeStoreLoad = await Task.WhenAny(
                selectionTask,
                Task.Delay(TimeSpan.FromSeconds(2), cancellationToken)) == selectionTask;
        }
        finally
        {
            delayedStoreLoad.Release();
        }

        await selectionTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        await clientStoreApplied.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        Assert.True(selectionReturnedBeforeStoreLoad);
    }

    [Fact]
    public async Task StaleWorkspaceSwitchSnapshotDoesNotReplaceLatestNavigationContexts()
    {
        var now = DateTimeOffset.UtcNow;
        var personal = new FusionCanvas.Domain.Workspace.Workspace(
            FusionCanvas.Domain.Workspace.WorkspaceDefaults.DefaultWorkspaceId,
            "Personal",
            null,
            false,
            now,
            now,
            "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Client", null, false, now, now, "{}");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var clientStore = new Store(Guid.NewGuid(), client.Id, "Client Store", null, false, now, now, "{}");
        var personalNiche = new Niche(Guid.NewGuid(), personalStore.Id, "Personal Niche", null, false, now, now, "{}");
        var clientNiche = new Niche(Guid.NewGuid(), clientStore.Id, "Client Niche", null, false, now, now, "{}");
        var personalItem = new Item(Guid.NewGuid(), personalStore.Id, personalNiche.Id, null, "Initial Personal Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var clientItem = new Item(Guid.NewGuid(), clientStore.Id, clientNiche.Id, null, "Client Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var latestPersonalItem = personalItem with { Id = Guid.NewGuid(), Name = "Latest Personal Item" };
        var initialSnapshot = new WorkspaceSnapshot(
            [personal, client],
            [personalStore, clientStore],
            [personalNiche, clientNiche],
            [],
            [personalItem, clientItem],
            [],
            [],
            [],
            [],
            []);
        var staleSnapshot = new WorkspaceSnapshot(
            [personal, client],
            [personalStore, clientStore],
            [clientNiche],
            [],
            [clientItem],
            [],
            [],
            [],
            [],
            []);
        var latestSnapshot = new WorkspaceSnapshot(
            [personal, client],
            [personalStore, clientStore],
            [personalNiche],
            [],
            [latestPersonalItem],
            [],
            [],
            [],
            [],
            []);
        var serviceRepository = new InMemoryWorkspaceRepository(initialSnapshot);
        var snapshotRepository = new GateableWorkspaceRepository(initialSnapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(
            initialSnapshot,
            serviceRepository,
            workspaceSnapshotRepository: snapshotRepository);
        var clientWorkspace = viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id);
        var personalWorkspace = viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == personal.Id);
        var staleLoad = snapshotRepository.DelayAfterAdditionalLoads(1);
        var generationField = typeof(MainWindowViewModel).GetField(
            "_workspaceSwitchGeneration",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var switchMethod = typeof(MainWindowViewModel).GetMethod(
            "SwitchWorkspaceAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: [typeof(FusionCanvas.Application.Workspaces.WorkspaceSummary), typeof(long)],
            modifiers: null);
        Assert.NotNull(generationField);
        Assert.NotNull(switchMethod);

        Task? staleSwitch = null;
        Task? latestSwitch = null;
        try
        {
            snapshotRepository.SetSnapshot(staleSnapshot);
            generationField.SetValue(viewModel, 1L);
            staleSwitch = Assert.IsAssignableFrom<Task>(switchMethod.Invoke(viewModel, [clientWorkspace, 1L]));
            await staleLoad.Started.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            snapshotRepository.SetSnapshot(latestSnapshot);
            generationField.SetValue(viewModel, 2L);
            latestSwitch = Assert.IsAssignableFrom<Task>(switchMethod.Invoke(viewModel, [personalWorkspace, 2L]));
            await latestSwitch.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Contains(viewModel.NavigationContexts, context => context.Context.Title == "Latest Personal Item");

            staleLoad.Release();
            await staleSwitch.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.Contains(viewModel.NavigationContexts, context => context.Context.Title == "Latest Personal Item");
        }
        finally
        {
            staleLoad.Release();
            if (latestSwitch is not null)
            {
                await latestSwitch.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            }

            if (staleSwitch is not null)
            {
                await staleSwitch.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            }
        }
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
        var clientItem = new Item(Guid.NewGuid(), clientStore.Id, clientNiche.Id, null, "Client Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore, clientStore], [clientNiche], [], [clientItem], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);

        var clientWorkspace = viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id);
        await viewModel.WorkspaceManagement.SelectWorkspaceAsync(clientWorkspace, TestContext.Current.CancellationToken);

        Assert.Equal(client.Id, viewModel.WorkspaceManagement.SelectedWorkspace?.Id);
        Assert.DoesNotContain(viewModel.StoreManagement.ActiveStores, store => store.Id == personalStore.Id);
        Assert.Contains(viewModel.StoreManagement.ActiveStores, store => store.Id == clientStore.Id);
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Title == "Client Item");

        var clientItemNode = viewModel.WorkspaceTree.Roots
            .SelectMany(root => root.Children)
            .Single(node => node.EntityId == clientItem.Id);
        viewModel.WorkspaceTree.SelectNodeCommand.Execute(clientItemNode);

        Assert.Equal(clientItem.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(clientItem.Id, viewModel.ItemInspector.LoadedItemId);

        viewModel.WorkspaceTree.OpenInTabCommand.Execute(clientItemNode);

        Assert.Contains(viewModel.DocumentWindow.Tabs, tab => tab.Context.Id == clientItem.Id);
    }

    [Fact]
    public async Task WorkspaceManagementOpen_DefersFirstStorePromptUntilClosed()
    {
        var now = DateTimeOffset.UtcNow;
        var personal = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Alpha Personal", null, false, now, now, "{}");
        var client = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), "Zulu Client", null, false, now, now, "{}");
        var personalStore = new Store(Guid.NewGuid(), personal.Id, "Personal Store", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([personal, client], [personalStore], [], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);

        viewModel.WorkspaceManagement.OpenWorkspaceManagementCommand.Execute(null);
        await viewModel.WorkspaceManagement.SelectWorkspaceAsync(viewModel.WorkspaceManagement.ActiveWorkspaces.Single(workspace => workspace.Id == client.Id), TestContext.Current.CancellationToken);

        Assert.False(viewModel.ShouldShowFirstStorePrompt);

        viewModel.WorkspaceManagement.CloseWorkspaceManagementCommand.Execute(null);

        Assert.True(viewModel.ShouldShowFirstStorePrompt);
    }

    [Fact]
    public void GroupActions_FollowCanonicalSelectionCoordinatedFromTabsAndTree()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
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
        var viewModel = MainWindowViewModelFactory.CreateSample();
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
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        var nicheContext = Assert.Single(viewModel.NavigationContexts, context => context.Context.EntityKind == WorkspaceEntityKind.Niche);
        viewModel.OpenFromNavigation(nicheContext);
        await viewModel.WorkspaceTree.BeginCreateAsync();
        viewModel.WorkspaceTree.SelectedNode!.DraftName = "Campaign";
        await viewModel.WorkspaceTree.CommitEditAsync();

        var created = Assert.Single(repository.Snapshot.Groups);
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == created.Id);
        Assert.Equal(created.Id, viewModel.WorkspaceTree.SelectedNode?.EntityId);
        var groupContext = viewModel.NavigationContexts.Single(context => context.Context.Id == created.Id);
        viewModel.OpenFromNavigation(groupContext);
        var documentId = viewModel.DocumentWindow.ActiveContext?.Id;
        Assert.True(viewModel.GroupDetails.HasState);
        Assert.False(viewModel.GroupDetails.IsReadOnly);

        viewModel.GroupDetails.RequestArchiveCommand.Execute(null);
        viewModel.GroupDetails.ConfirmArchiveCommand.Execute(null);

        // Archived groups remain openable read-only but leave the active tree projection.
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == created.Id);
        Assert.DoesNotContain(viewModel.WorkspaceTree.Roots.SelectMany(root => root.Children), node => node.EntityId == created.Id);
        Assert.Equal(niche.Id, viewModel.NavigationState.SelectedNodeId);
        Assert.Equal(documentId, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.True(viewModel.GroupDetails.IsReadOnly);

        viewModel.GroupDetails.RestoreCommand.Execute(null);

        Assert.False(repository.Snapshot.Groups.Single().IsArchived);
        Assert.False(viewModel.GroupDetails.IsReadOnly);
        Assert.Equal(created.Id, viewModel.NavigationState.SelectedNodeId);
    }

    private static NavigationDocumentContext GroupContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Group &&
            context.Context.Title == "Dogs and coffee");

    private static NavigationDocumentContext DraftItemContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Item &&
            context.Context.Title == "Morning coffee idea");

    private static NavigationDocumentContext ReadyItemContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Item &&
            context.Context.Title == "Retro mug design");

    private static NavigationDocumentContext ActiveItemContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Item &&
            context.Context.Title == "Espresso listing draft");

    [Fact]
    public void DetailsPaneVisibility_FollowsActiveContextKind()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var niche = viewModel.NavigationContexts.Single(context => context.Context.EntityKind == WorkspaceEntityKind.Niche);
        var group = GroupContext(viewModel);
        var listing = ReadyItemContext(viewModel);

        viewModel.OpenFromNavigation(niche);

        Assert.True(viewModel.ShowSelectionSummary);
        Assert.True(viewModel.ShowStageToolHost);
        Assert.False(viewModel.ItemInspector.HasState);
        Assert.False(viewModel.GroupDetails.HasState);

        viewModel.OpenFromNavigation(group);

        Assert.False(viewModel.ShowSelectionSummary);
        Assert.False(viewModel.ShowStageToolHost);
        Assert.True(viewModel.GroupDetails.HasState);
        Assert.False(viewModel.ItemInspector.HasState);

        viewModel.OpenFromNavigation(listing);

        Assert.False(viewModel.ShowSelectionSummary);
        Assert.False(viewModel.ShowStageToolHost);
        Assert.True(viewModel.ItemInspector.HasState);
        Assert.False(viewModel.GroupDetails.HasState);
    }

    [Fact]
    public void ItemInspector_LoadsForActiveItemTabAndClearsForNonItemContext()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var listing = ReadyItemContext(viewModel);
        var group = GroupContext(viewModel);

        viewModel.OpenFromNavigation(listing);

        Assert.True(viewModel.ItemInspector.HasState);
        Assert.Equal(listing.Context.Id, viewModel.ItemInspector.LoadedItemId);

        viewModel.OpenFromNavigation(group);

        Assert.False(viewModel.ItemInspector.HasState);
    }

    [Fact]
    public async Task DirtyInspector_CommitsBeforeTabSwitchWithoutPrompt()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var first = viewModel.DocumentWindow.Open(ReadyItemContext(viewModel).Context);
        var secondTab = viewModel.DocumentWindow.Open(ActiveItemContext(viewModel).Context);
        viewModel.RequestSelectTabCommand.Execute(first);

        Assert.True(viewModel.ItemInspector.HasState);
        viewModel.ItemInspector.Title = "Persisted title";
        Assert.True(viewModel.ItemInspector.HasUnsavedChanges);

        viewModel.RequestSelectTabCommand.Execute(secondTab);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsStatusConfirmationVisible);
        Assert.Equal(secondTab.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.False(viewModel.ItemInspector.HasUnsavedChanges);

        viewModel.RequestSelectTabCommand.Execute(first);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal("Persisted title", viewModel.ItemInspector.Title);
    }

    [Fact]
    public async Task DirtyInspector_FailedCommitAbortsTransitionAndPreservesDraft()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var item = new Item(
            Guid.NewGuid(), store.Id, niche.Id, null, "Retro mug design", null,
            ItemStatus.Draft, WorkflowStage.Idea, false, now, now,
            "{\"notes\":\"Notes\",\"idea\":\"idea-value\"}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [item], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot) { FailSaves = true };
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        var itemContext = viewModel.NavigationContexts.Single(context => context.Context.Id == item.Id);
        viewModel.OpenFromNavigation(itemContext);
        viewModel.ItemInspector.Idea = "pending idea";

        viewModel.MoveStageForwardCommand.Execute(null);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.True(viewModel.ItemInspector.HasUnsavedChanges);
        Assert.Equal("pending idea", viewModel.ItemInspector.Idea);
        Assert.True(viewModel.ItemInspector.HasError);
        Assert.Equal(item.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(WorkflowStage.Idea, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
    }

    [Fact]
    public void StageMoveControls_ReflectActiveItemBoundaries()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();

        viewModel.OpenFromNavigation(DraftItemContext(viewModel));
        Assert.False(viewModel.CanMoveStageBack);
        Assert.True(viewModel.CanMoveStageForward);

        viewModel.OpenFromNavigation(ActiveItemContext(viewModel));
        Assert.True(viewModel.CanMoveStageBack);
        Assert.False(viewModel.CanMoveStageForward);
    }

    [Fact]
    public async Task MoveStageForward_AdvancesStageAndUpdatesView()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));

        viewModel.MoveStageForwardCommand.Execute(null);
        await Task.Delay(200);

        Assert.Null(viewModel.StageMoveError);
        Assert.Equal(WorkflowStage.Listing, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.True(viewModel.WorkflowNavigator.Stages.Single(stage => stage.Stage == WorkflowStage.Listing).IsCurrent);
        Assert.False(viewModel.CanMoveStageForward);
        Assert.True(viewModel.ItemInspector.CanEditStage);

        viewModel.SelectWorkflowStage(WorkflowStage.Design);

        Assert.False(viewModel.ItemInspector.CanEditStage);
    }

    [Fact]
    public async Task MoveStageBack_RegressesStage()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));

        viewModel.MoveStageBackCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(WorkflowStage.Concept, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.True(viewModel.WorkflowNavigator.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsCurrent);
    }

    [Fact]
    public async Task SetItemStatus_ChangesStatusWithoutMovingStage()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));

        viewModel.SetItemStatusCommand.Execute(ItemStatus.Rejected);
        Assert.True(viewModel.IsStatusConfirmationVisible);
        Assert.Equal(ItemStatus.Draft, viewModel.ActiveItemStatus);
        viewModel.ConfirmStatusChangeCommand.Execute(null);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(ItemStatus.Rejected, viewModel.ActiveItemStatus);
        Assert.Equal(WorkflowStage.Design, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
    }

    [Fact]
    public void StatusSelector_OffersOnlyCurrentAndAllowedDirectTargets()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));

        Assert.Equal([ItemStatus.Draft, ItemStatus.Rejected], viewModel.AvailableItemStatuses);
        Assert.Equal(["Draft", "Rejected"], viewModel.AvailableItemStatusLabels);
        Assert.Equal("Draft", viewModel.SelectedItemStatusLabel);
        Assert.Equal(["Draft", "Rejected"], viewModel.AvailableItemStatusOptions.Select(option => option.Label));
        Assert.Equal(ItemStatus.Draft, viewModel.SelectedItemStatusOption?.Status);
    }

    [Fact]
    public void StatusConfirmation_CancelKeepsAuthoritativeStatusAndSelection()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        var context = ReadyItemContext(viewModel);
        viewModel.OpenFromNavigation(context);

        viewModel.SetItemStatusCommand.Execute(ItemStatus.Rejected);
        Assert.True(viewModel.IsStatusConfirmationVisible);

        viewModel.CancelStatusChangeCommand.Execute(null);

        Assert.False(viewModel.IsStatusConfirmationVisible);
        Assert.Equal(ItemStatus.Draft, viewModel.ActiveItemStatus);
        Assert.Equal(context.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
    }

    [Fact]
    public void StageToolVisibility_FollowsActiveReviewStage()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));

        Assert.True(viewModel.ShowsDesignStageTool);
        Assert.False(viewModel.ShowsIdeaStageTool);

        viewModel.SelectWorkflowStage(WorkflowStage.Idea);

        Assert.True(viewModel.ShowsIdeaStageTool);
        Assert.False(viewModel.ShowsDesignStageTool);
        Assert.False(viewModel.ItemInspector.CanEditStage);
    }

    [Fact]
    public async Task StageMoveControls_DisabledForRejectedItem()
    {
        var viewModel = MainWindowViewModelFactory.CreateSample();
        viewModel.OpenFromNavigation(ReadyItemContext(viewModel));
        viewModel.SetItemStatusCommand.Execute(ItemStatus.Rejected);
        viewModel.ConfirmStatusChangeCommand.Execute(null);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.False(viewModel.CanMoveStageForward);
        Assert.False(viewModel.CanMoveStageBack);
    }

    private sealed class FailingIdeationAccessStatus : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() =>
            IdeationAccessAvailability.Unavailable("AI services are unavailable.");

        public Task RefreshAsync(CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("refresh failed"));
    }

    private sealed class RecordingTelemetryService : ITelemetryService
    {
        private readonly TaskCompletionSource<TelemetryEntry> _recorded =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler<TelemetryEntry>? EntryRecorded;

        public Task<TelemetryEntry> Recorded => _recorded.Task;

        public bool IsCaptureEnabled => true;

        public Task<WorkspaceTelemetrySettings> GetSettingsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceTelemetrySettings.Default);

        public Task SaveSettingsAsync(
            Guid workspaceId,
            WorkspaceTelemetrySettings settings,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RecordAsync(
            TelemetryEventRequest request,
            CancellationToken cancellationToken = default)
        {
            var entry = new TelemetryEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                request.Area,
                request.Name,
                request.Severity,
                request.Outcome,
                request.Message,
                request.MetadataJson,
                request.RequestBody,
                request.ResponseBody,
                request.RequestDetailsJson,
                request.ResponseDetailsJson,
                request.CorrelationId);
            _recorded.TrySetResult(entry);
            EntryRecorded?.Invoke(this, entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TelemetryEntry>> SearchAsync(
            Guid workspaceId,
            TelemetryQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>([]);

        public Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>([]);

        public Task<int> DeleteAllAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<string> ExportJsonAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("[]");

        public Task ExportAsync(
            Guid workspaceId,
            Stream destination,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CleanupExpiredAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class GateableWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private int _loadCount;
        private int _delayedLoadNumber = int.MaxValue;
        private TaskCompletionSource? _delayedLoadStarted;
        private TaskCompletionSource? _releaseDelayedLoad;
        private WorkspaceSnapshot _snapshot = snapshot;

        public DelayedLoad DelayAfterAdditionalLoads(int additionalLoads)
        {
            if (additionalLoads < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalLoads));
            }

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _delayedLoadStarted = started;
            _releaseDelayedLoad = release;
            Volatile.Write(ref _delayedLoadNumber, Volatile.Read(ref _loadCount) + additionalLoads);
            return new DelayedLoad(started.Task, () => release.TrySetResult());
        }

        public void SetSnapshot(WorkspaceSnapshot snapshot) =>
            Volatile.Write(ref _snapshot, snapshot);

        public Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default)
        {
            SetSnapshot(updated);
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = Volatile.Read(ref _snapshot);
            if (Interlocked.Increment(ref _loadCount) != Volatile.Read(ref _delayedLoadNumber))
            {
                return Task.FromResult(snapshot);
            }

            return WaitForDelayedLoadAsync(snapshot, cancellationToken);
        }

        private async Task<WorkspaceSnapshot> WaitForDelayedLoadAsync(
            WorkspaceSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            _delayedLoadStarted?.TrySetResult();
            await (_releaseDelayedLoad?.Task ?? throw new InvalidOperationException("A delayed load was not configured."))
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            return snapshot;
        }

        public sealed record DelayedLoad(Task Started, Action Release);
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public WorkspaceSnapshot Snapshot => _snapshot;

        public bool FailSaves { get; set; }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
