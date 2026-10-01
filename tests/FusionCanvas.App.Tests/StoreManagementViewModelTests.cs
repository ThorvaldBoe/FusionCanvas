using Avalonia.Headless.XUnit;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Tests;

public class StoreManagementViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LoadAsyncStartsCatalogChildOnCapturedSynchronizationContext()
    {
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var repository = new DelayedFirstLoadWorkspaceRepository(snapshot);
        var delayedTagLoadStarted = repository.DelayNextLoad();
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper(),
                initialActiveWorkspaceId: store.WorkspaceId,
                initialActiveStoreId: store.Id),
            tagService: new TagManagementService(repository),
            catalogService: new CatalogSetupService(repository),
            mockupService: new MockupTemplateSetupService(repository));

        var catalogBusyThreadId = 0;
        viewModel.CatalogSetup!.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CatalogSetupViewModel.IsBusy) && viewModel.CatalogSetup.IsBusy)
            {
                Interlocked.CompareExchange(ref catalogBusyThreadId, Environment.CurrentManagedThreadId, 0);
            }
        };

        var uiThreadId = Environment.CurrentManagedThreadId;
        var originalSynchronizationContext = SynchronizationContext.Current;
        using var uiContext = new PumpingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(uiContext);
        try
        {
            var loadTask = viewModel.LoadAsync(TestContext.Current.CancellationToken);
            repository.ReleaseFirstLoad();
            uiContext.PumpUntil(delayedTagLoadStarted.Task, TimeSpan.FromSeconds(5));
            repository.ReleaseNextLoad();
            uiContext.PumpUntil(loadTask, TimeSpan.FromSeconds(5));
        }
        finally
        {
            repository.ReleaseFirstLoad();
            repository.ReleaseNextLoad();
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.Equal(store.Id, viewModel.SelectedStore?.Id);
        Assert.Equal(uiThreadId, catalogBusyThreadId);
    }

    [Fact]
    public void SetActiveWorkspaceAsyncStartsCatalogCommandOnCapturedSynchronizationContext()
    {
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var repository = new DelayedFirstLoadWorkspaceRepository(snapshot);
        var delayedTagLoadStarted = repository.DelayNextLoad();
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper(),
                initialActiveWorkspaceId: store.WorkspaceId,
                initialActiveStoreId: store.Id),
            tagService: new TagManagementService(repository),
            catalogService: new CatalogSetupService(repository),
            mockupService: new MockupTemplateSetupService(repository));

        var busyThreadId = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StoreManagementViewModel.IsBusy) && viewModel.IsBusy)
            {
                Interlocked.CompareExchange(ref busyThreadId, Environment.CurrentManagedThreadId, 0);
            }
        };

        var uiThreadId = Environment.CurrentManagedThreadId;
        var originalSynchronizationContext = SynchronizationContext.Current;
        using var uiContext = new PumpingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(uiContext);
        try
        {
            var switchTask = viewModel.SetActiveWorkspaceAsync(store.WorkspaceId, TestContext.Current.CancellationToken);
            repository.ReleaseFirstLoad();
            uiContext.PumpUntil(delayedTagLoadStarted.Task, TimeSpan.FromSeconds(5));
            repository.ReleaseNextLoad();
            uiContext.PumpUntil(switchTask, TimeSpan.FromSeconds(5));
        }
        finally
        {
            repository.ReleaseFirstLoad();
            repository.ReleaseNextLoad();
            SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
        }

        Assert.Equal(store.Id, viewModel.SelectedStore?.Id);
        Assert.Equal(uiThreadId, busyThreadId);
    }

    [Fact]
    public async Task StoreNicheConfiguration_OwnsStoreSaveWorkflowAndAppliesResult()
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper(), () => Now, Guid.NewGuid);
        StoreManagementState? applied = null;
        var editor = new StoreNicheConfigurationViewModel(
            service, null, null, state => applied = state, result => applied = result.State, _ => { });
        await editor.LoadStoresAsync(TestContext.Current.CancellationToken);
        var workspaceId = applied?.ActiveWorkspaceId;
        editor.SetScope(new StoreManagementScope(workspaceId, null, IsCreatingNewStore: true));
        editor.NewStoreName = "Owned by child";

        await editor.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(applied);
        Assert.Contains(applied!.ActiveStores, store => store.Name == "Owned by child");
        Assert.False(editor.IsCreatingNewStore);
    }

    [Fact]
    public void StoreNicheConfiguration_OwnsStoreAndNicheDirtyBaselines()
    {
        var service = new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper());
        var editor = new StoreNicheConfigurationViewModel(service, null, null, _ => { }, _ => { }, _ => { });
        editor.CaptureStoreDraft();
        editor.CaptureNicheDraft();

        editor.NewStoreName = "Unsaved store";
        editor.NicheNotes = "Unsaved niche";

        Assert.True(editor.HasUnsavedStoreChanges);
        Assert.True(editor.HasUnsavedNicheChanges);
        editor.CaptureStoreDraft();
        editor.CaptureNicheDraft();
        Assert.False(editor.HasUnsavedStoreChanges);
        Assert.False(editor.HasUnsavedNicheChanges);
    }

    [Fact]
    public async Task LoadAsync_ShowsFirstStoreEmptyState()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper()));

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.NeedsFirstStore);
        Assert.True(viewModel.ShouldShowFirstStorePrompt);
        Assert.False(viewModel.HasActiveStores);
        Assert.Empty(viewModel.ActiveStores);
    }

    [Fact]
    public async Task FirstStorePrompt_CanOpenEditorOrBeDismissed()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.DeclineFirstStorePromptCommand.Execute(null);

        Assert.False(viewModel.ShouldShowFirstStorePrompt);

        var secondViewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await secondViewModel.LoadAsync(TestContext.Current.CancellationToken);

        secondViewModel.AcceptFirstStorePromptCommand.Execute(null);

        Assert.False(secondViewModel.ShouldShowFirstStorePrompt);
        Assert.True(secondViewModel.IsStoreEditorOpen);
    }

    [Fact]
    public async Task CreateStoreAsync_SelectsNewStoreAndPopulatesEditFields()
    {
        var storeId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(),
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            () => Now,
            () => storeId))
        {
            NewStoreName = "North Star Studio",
            Description = "POD brand",
            Notes = "Soft humor",
            TargetMarket = "Coffee fans",
            BrandDirection = "Warm vintage",
            PlanningContext = "Fall launch"
        };

        await viewModel.CreateStoreAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.NeedsFirstStore);
        Assert.Equal(storeId, viewModel.SelectedStore?.Id);
        Assert.Equal("North Star Studio", viewModel.NewStoreName);
        Assert.Equal("Soft humor", viewModel.Notes);
        Assert.True(viewModel.HasSelectedStore);
        Assert.Contains(viewModel.SelectorStores, entry => entry.Id == storeId && entry.IsSelected);
    }

    [Fact]
    public async Task NewStoreDraft_AppearsInEditorAndSaveCreatesStore()
    {
        var storeId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(),
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            () => Now,
            () => storeId));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.StartCreateStore();

        var draft = Assert.Single(viewModel.EditorActiveStores);
        Assert.Equal("New store", draft.Name);
        Assert.True(viewModel.HasSelectedStore);

        viewModel.NewStoreName = "North Star Studio";
        viewModel.Notes = "Soft humor";
        viewModel.SelectStoreForEditing(viewModel.EditorActiveStores.Single());
        await viewModel.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.ActiveStores.Count > 0, viewModel.ErrorMessage);
        Assert.Equal(storeId, Assert.Single(viewModel.ActiveStores).Id);
        Assert.Equal(storeId, viewModel.SelectedStore?.Id);
        Assert.Equal("North Star Studio", Assert.Single(viewModel.ActiveStores).Name);
        Assert.Equal("Soft humor", viewModel.SelectedStore?.Context.Notes);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task NewStoreDraft_RequestsStoreNameFocus()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        var focusRequests = 0;
        viewModel.StoreNameFocusRequested += (_, _) => focusRequests++;

        viewModel.StartCreateStore();

        Assert.Equal(1, focusRequests);
    }

    [Fact]
    public async Task StoreEditor_EnablesActionsOnlyWhenRelevant()
    {
        var active = NewStore("North Star Studio");
        var archived = NewStore("Archived Studio", isArchived: true);
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == active.Id));

        Assert.False(viewModel.CanSaveSelectedStore);
        Assert.True(viewModel.CanArchiveSelectedStore);
        Assert.True(viewModel.CanDeleteSelectedStore);

        viewModel.NewStoreName = "North Star Gifts";

        Assert.True(viewModel.CanSaveSelectedStore);
        Assert.True(viewModel.CanArchiveSelectedStore);
        Assert.True(viewModel.CanDeleteSelectedStore);

        viewModel.StartCreateStore();
        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.True(viewModel.CanSaveSelectedStore);
        Assert.False(viewModel.CanArchiveSelectedStore);
        Assert.False(viewModel.CanDeleteSelectedStore);

        viewModel.SelectStoreForEditing(viewModel.ArchivedStores.Single(store => store.Id == archived.Id));
        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.False(viewModel.CanSaveSelectedStore);
        Assert.False(viewModel.CanArchiveSelectedStore);
        Assert.True(viewModel.CanDeleteSelectedStore);
    }

    [Fact]
    public async Task SaveSelectedStoreAsync_RenamesAndUpdatesContext()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0], TestContext.Current.CancellationToken);
        viewModel.NewStoreName = "North Star Gifts";
        viewModel.Notes = "Sharper positioning";

        await viewModel.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.Equal("North Star Gifts", viewModel.SelectedStore?.Name);
        Assert.Equal("Sharper positioning", viewModel.SelectedStore?.Context.Notes);
    }

    [Fact]
    public async Task SelectingAnotherStore_WithUnsavedChangesRequiresDiscardConfirmation()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == first.Id));
        viewModel.NewStoreName = "Unsaved name";

        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == second.Id));

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(first.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Unsaved name", viewModel.NewStoreName);

        viewModel.KeepEditingCommand.Execute(null);

        Assert.False(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(first.Id, viewModel.SelectedStore?.Id);

        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == second.Id));
        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.False(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(second.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Second Studio", viewModel.NewStoreName);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task ClosingStoreEditor_WithUnsavedChangesRequiresDiscardConfirmation()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenStoreEditorCommand.Execute(null);
        viewModel.NewStoreName = "Unsaved name";

        var canClose = viewModel.TryCloseStoreEditor();

        Assert.False(canClose);
        Assert.True(viewModel.IsStoreEditorOpen);
        Assert.True(viewModel.DiscardChangesPromptVisible);

        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.False(viewModel.IsStoreEditorOpen);
        Assert.False(viewModel.DiscardChangesPromptVisible);
    }

    [Fact]
    public async Task OpenStoreEditorCommand_PreselectsActiveStore()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == second.Id), TestContext.Current.CancellationToken);

        viewModel.OpenStoreEditorCommand.Execute(null);

        Assert.True(viewModel.IsStoreEditorOpen);
        Assert.Equal(second.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Second Studio", viewModel.NewStoreName);
        Assert.True(viewModel.IsBasicInfoTabSelected);
    }

    [Fact]
    public async Task OpenNichesTabCommand_OpensStoreEditorOnNichesTab()
    {
        var store = NewStore("North Star Studio");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            new NicheManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.OpenNichesTabCommand.Execute(null);

        Assert.True(viewModel.IsStoreEditorOpen);
        Assert.True(viewModel.IsNichesTabSelected);
        Assert.Equal(niche.Id, viewModel.SelectedNiche?.Id);
    }

    [Fact]
    public async Task ArchiveAndRestoreFlows_ShowStoresSeparately()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0], TestContext.Current.CancellationToken);

        await viewModel.ArchiveSelectedStoreAsync(TestContext.Current.CancellationToken);
        var archived = Assert.Single(viewModel.ArchivedStores);
        await viewModel.RestoreStoreAsync(archived, TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.ArchivedStores);
        Assert.Equal(store.Id, Assert.Single(viewModel.ActiveStores).Id);
    }

    [Fact]
    public async Task Selector_TogglesCompactExpandedAndHighlightsSelectedStore()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == first.Id), TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsSelectorCompact);
        Assert.False(viewModel.IsSelectorExpanded);
        Assert.Equal("▼", viewModel.SelectorToggleGlyph);
        Assert.Equal("Expand stores", viewModel.SelectorToggleTooltip);
        Assert.Contains(viewModel.SelectorStores, entry => entry.Id == first.Id && entry.IsSelected);

        viewModel.ToggleStoreSelectorCommand.Execute(null);

        Assert.True(viewModel.IsSelectorExpanded);
        Assert.False(viewModel.IsSelectorCompact);
        Assert.Equal("▲", viewModel.SelectorToggleGlyph);
        Assert.Equal("Collapse stores", viewModel.SelectorToggleTooltip);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == second.Id), TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.SelectorStores, entry => entry.Id == second.Id && entry.IsSelected);
        Assert.DoesNotContain(viewModel.SelectorStores, entry => entry.Id == first.Id && entry.IsSelected);
    }

    [Fact]
    public async Task StoreEditor_DeleteWarningCanBeCanceled()
    {
        var store = NewStore("Empty Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0], TestContext.Current.CancellationToken);

        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        viewModel.CancelDeleteStoreCommand.Execute(null);

        Assert.False(viewModel.DeleteWarningVisible);
        Assert.Equal(store.Id, Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores).Id);
    }

    [Fact]
    public async Task SelectStoreCommand_TracksPendingTaskAndShowsTheFailure()
    {
        var failure = new InvalidOperationException("The store could not be selected.");
        var serviceCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serviceCompletion = new TaskCompletionSource<StoreManagementResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FaultingStoreManagementService
        {
            SelectStore = (_, _) =>
            {
                serviceCalled.TrySetResult();
                return serviceCompletion.Task;
            }
        };
        var viewModel = new StoreManagementViewModel(service);
        var busyStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var busyStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.IsBusy))
            {
                if (viewModel.IsBusy)
                {
                    busyStarted.TrySetResult();
                }
                else
                {
                    busyStopped.TrySetResult();
                }
            }

            if (args.PropertyName == nameof(viewModel.ErrorMessage))
            {
                errorChanged.TrySetResult();
            }
        };

        var store = NewStoreSummary("North Star Studio");
        Assert.True(viewModel.SelectStoreCommand.CanExecute(store));
        viewModel.SelectStoreCommand.Execute(store);
        Assert.True(viewModel.IsBusy);

        await serviceCalled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await busyStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        serviceCompletion.SetException(failure);
        await errorChanged.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await busyStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Contains(failure.Message, viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task DisposeAsync_CancelsAndWaitsForPendingCommandTasks()
    {
        var serviceCalled = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FaultingStoreManagementService
        {
            SelectStore = async (_, cancellationToken) =>
            {
                serviceCalled.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The cancelled selection unexpectedly completed.");
            }
        };
        var viewModel = new StoreManagementViewModel(service);

        var lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(viewModel);
        viewModel.SelectStoreCommand.Execute(NewStoreSummary("North Star Studio"));
        var operationToken = await serviceCalled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await lifetime.DisposeAsync();

        Assert.True(operationToken.IsCancellationRequested);
        Assert.False(viewModel.IsBusy);
    }

    [AvaloniaFact]
    public async Task DisposeAsync_DisposesOwnedPrintifyImportAndCredentialsSessions()
    {
        var service = new PendingPrintifyCatalogImportService();
        var session = new PrintifyCatalogImportViewModel(
            service,
            () => new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid()));
        var credentialsService = new PendingPrintifyCredentialConfigurationService();
        var credentialsSession = new StorePrintifyCredentialsViewModel(credentialsService);
        credentialsSession.SetContext(
            NewStoreSummary("Printify") with { FulfillmentStrategy = FulfillmentStrategy.ShopifyPrintify },
            FulfillmentStrategy.ShopifyPrintify,
            isDraft: false,
            editorOpen: true);
        var owner = new StoreManagementViewModel(new FaultingStoreManagementService());
        typeof(StoreManagementViewModel)
            .GetProperty(nameof(StoreManagementViewModel.PrintifyCatalogImportSession))!
            .SetValue(owner, session);
        typeof(StoreManagementViewModel)
            .GetProperty(nameof(StoreManagementViewModel.PrintifyCredentials))!
            .SetValue(owner, credentialsSession);

        session.Open();
        var token = await service.LoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var credentialsToken = await credentialsService.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var catalogWaitHandle = service.LoadWaitHandle!;
        var credentialWaitHandle = credentialsService.ReadWaitHandle!;
        Task? disposalTask = null;
        try
        {
            disposalTask = owner.DisposeAsync().AsTask();
            Assert.True(token.IsCancellationRequested);
            Assert.False(catalogWaitHandle.SafeWaitHandle.IsClosed);
            Assert.True(credentialsToken.IsCancellationRequested);
            Assert.False(credentialWaitHandle.SafeWaitHandle.IsClosed);
            Assert.False(disposalTask.IsCompleted);

            service.ReleaseLoad.TrySetResult();
            credentialsService.ReleaseRead.TrySetResult();
            await disposalTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.True(catalogWaitHandle.SafeWaitHandle.IsClosed);
            Assert.True(credentialWaitHandle.SafeWaitHandle.IsClosed);
        }
        finally
        {
            service.ReleaseLoad.TrySetResult();
            credentialsService.ReleaseRead.TrySetResult();
            await owner.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            session.Dispose();
            credentialsSession.Dispose();
        }
    }

    [Fact]
    public async Task DisposeAsyncTimeoutKeepsParentBusyUntilPrintifyChildDrains()
    {
        var service = new PendingPrintifyCatalogImportService();
        var session = new PrintifyCatalogImportViewModel(
            service,
            () => new StoreCredentialScope(Guid.NewGuid(), Guid.NewGuid()));
        var owner = new StoreManagementViewModel(new FaultingStoreManagementService());
        typeof(StoreManagementViewModel)
            .GetProperty(nameof(StoreManagementViewModel.PrintifyCatalogImportSession))!
            .SetValue(owner, session);
        session.Open();
        var token = await service.LoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        try
        {
            await owner.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.True(token.IsCancellationRequested);
            Assert.True(owner.IsBusy);
            Assert.True(session.HasPendingOperations);

            service.ReleaseLoad.TrySetResult();
            await session.WaitForPendingOperationsAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(owner.IsBusy);
        }
        finally
        {
            service.ReleaseLoad.TrySetResult();
            await owner.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            session.Dispose();
        }
    }

    [Fact]
    public async Task SelectStoreCommand_CompletesAndClearsBusyStateAfterSuccess()
    {
        var serviceCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serviceCompletion = new TaskCompletionSource<StoreManagementResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FaultingStoreManagementService
        {
            SelectStore = (_, _) =>
            {
                serviceCalled.TrySetResult();
                return serviceCompletion.Task;
            }
        };
        var viewModel = new StoreManagementViewModel(service);
        var busyStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var busyStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.IsBusy))
            {
                if (viewModel.IsBusy)
                {
                    busyStarted.TrySetResult();
                }
                else
                {
                    busyStopped.TrySetResult();
                }
            }
        };

        var store = NewStoreSummary("North Star Studio");
        Assert.True(viewModel.SelectStoreCommand.CanExecute(store));
        viewModel.SelectStoreCommand.Execute(store);
        Assert.True(viewModel.IsBusy);

        await serviceCalled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await busyStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        serviceCompletion.SetResult(StoreManagementResult.Success(
            null,
            new StoreManagementState(null, [], [], null, null, NeedsFirstStore: true)));
        await busyStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task DisposeAsync_CancelsAndBoundsInFlightOperations()
    {
        var serviceCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serviceCompletion = new TaskCompletionSource<StoreManagementResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken operationToken = default;
        var service = new FaultingStoreManagementService
        {
            SelectStore = (_, cancellationToken) =>
            {
                operationToken = cancellationToken;
                serviceCalled.TrySetResult();
                return serviceCompletion.Task;
            }
        };
        var viewModel = new StoreManagementViewModel(service);
        var busyStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.IsBusy) && !viewModel.IsBusy)
            {
                busyStopped.TrySetResult();
            }
        };

        viewModel.SelectStoreCommand.Execute(NewStoreSummary("North Star Studio"));
        await serviceCalled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await viewModel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        Assert.True(operationToken.IsCancellationRequested);
        Assert.True(viewModel.IsBusy);

        serviceCompletion.SetResult(StoreManagementResult.Success(
            null,
            new StoreManagementState(null, [], [], null, null, NeedsFirstStore: true)));
        await busyStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task StoreEditor_DeleteConfirmedEmptyStoreAndBlocksConnectedStore()
    {
        var empty = NewStore("Empty Studio");
        var connected = NewStore("Zulu Connected Studio");
        var niche = new Niche(Guid.NewGuid(), connected.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([empty, connected], [niche], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == connected.Id), TestContext.Current.CancellationToken);
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync(TestContext.Current.CancellationToken);

        Assert.Contains("connected data", viewModel.ErrorMessage);
        Assert.Contains((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores, store => store.Id == connected.Id);
        Assert.Equal(connected.Id, viewModel.SelectedStore?.Id);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == empty.Id), TestContext.Current.CancellationToken);
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores, store => store.Id == empty.Id);
    }

    [Fact]
    public async Task NicheEditor_DeleteFailureKeepsSelectedNiche()
    {
        var store = NewStore("North Star Studio");
        var first = new Niche(Guid.NewGuid(), store.Id, "Alpha Niche", null, false, Now, Now, "{}");
        var selected = new Niche(Guid.NewGuid(), store.Id, "Zulu Niche", null, false, Now, Now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, selected.Id, null, "Espresso", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [first, selected], [], [listing], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()), new NicheManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(), TestContext.Current.CancellationToken);
        await viewModel.SelectNicheAsync(viewModel.ActiveNiches.Single(niche => niche.Id == selected.Id), TestContext.Current.CancellationToken);

        viewModel.RequestDeleteSelectedNiche();
        await viewModel.ConfirmDeleteNicheAsync(TestContext.Current.CancellationToken);

        Assert.Contains("connected data", viewModel.ErrorMessage);
        Assert.Contains((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches, niche => niche.Id == selected.Id);
        Assert.Equal(selected.Id, viewModel.SelectedNiche?.Id);
    }

    [Fact]
    public async Task StoreEditor_DeleteSelectsRemainingStoreByDefault()
    {
        var first = NewStore("First Studio");
        var second = NewStore("Second Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == first.Id));

        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync(TestContext.Current.CancellationToken);

        Assert.Equal(second.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Second Studio", viewModel.NewStoreName);
        Assert.True(viewModel.HasSelectedStore);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task StoreEditor_UsesBasicInfoAndNichesTabs()
    {
        var store = NewStore("North Star Studio");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            new NicheManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsBasicInfoTabSelected);

        viewModel.SelectNichesTabCommand.Execute(null);

        Assert.True(viewModel.IsNichesTabSelected);
        Assert.Equal(niche.Id, Assert.Single(viewModel.ActiveNiches).Id);
        Assert.Equal(niche.Id, viewModel.SelectedNiche?.Id);

        viewModel.SelectBasicInfoTabCommand.Execute(null);

        Assert.True(viewModel.IsBasicInfoTabSelected);
        Assert.Equal(store.Id, viewModel.SelectedStore?.Id);
    }

    [Fact]
    public async Task NichesTab_CreatesArchivesRestoresAndDeletesNiches()
    {
        var store = NewStore("North Star Studio");
        var nicheId = Guid.NewGuid();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            new NicheManagementService(repository, () => Now, () => nicheId));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectNichesTabCommand.Execute(null);

        Assert.True(viewModel.CanSaveSelectedNiche);
        Assert.Equal("New niche", Assert.Single(viewModel.EditorActiveNiches).Name);

        viewModel.NicheName = "Coffee";
        viewModel.NicheAudience = "Coffee fans";
        await viewModel.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);

        Assert.Equal(nicheId, viewModel.SelectedNiche?.Id);
        Assert.Equal("Coffee fans", viewModel.SelectedNiche?.Context.Audience);
        Assert.Single(viewModel.ActiveNiches);

        await viewModel.ArchiveSelectedNicheAsync(TestContext.Current.CancellationToken);

        var archived = Assert.Single(viewModel.ArchivedNiches);
        Assert.Empty(viewModel.ActiveNiches);
        Assert.True(viewModel.CanRestoreSelectedNiche);

        await viewModel.RestoreNicheAsync(archived, TestContext.Current.CancellationToken);

        Assert.Single(viewModel.ActiveNiches);
        Assert.Empty(viewModel.ArchivedNiches);

        viewModel.RequestDeleteSelectedNicheCommand.Execute(null);
        viewModel.CancelDeleteNicheCommand.Execute(null);

        Assert.False(viewModel.NicheDeleteWarningVisible);
        Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches);

        viewModel.RequestDeleteSelectedNicheCommand.Execute(null);
        await viewModel.ConfirmDeleteNicheAsync(TestContext.Current.CancellationToken);

        Assert.Empty((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches);
    }

    [Fact]
    public async Task NichesTab_DiscardPromptProtectsUnsavedNicheEdits()
    {
        var store = NewStore("North Star Studio");
        var first = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var second = new Niche(Guid.NewGuid(), store.Id, "Cats", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [first, second], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            new NicheManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectNichesTabCommand.Execute(null);
        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single(niche => niche.Id == first.Id));
        viewModel.NicheName = "Unsaved";

        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single(niche => niche.Id == second.Id));

        Assert.True(viewModel.DiscardChangesPromptVisible);
        Assert.Equal(first.Id, viewModel.SelectedNiche?.Id);

        viewModel.ConfirmDiscardChangesCommand.Execute(null);

        Assert.Equal(second.Id, viewModel.SelectedNiche?.Id);
        Assert.False(viewModel.HasUnsavedNicheChanges);
    }

    [Fact]
    public void OpenStoreEditorCommand_ProvidesMenuFriendlyManagementEntry()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository(), new FusionCanvas.Integration.Stores.StoreContextMapper()));

        viewModel.OpenStoreEditorCommand.Execute(null);

        Assert.True(viewModel.IsStoreEditorOpen);
    }

    [Fact]
    public async Task AppWorkspaceFactory_UsesSqliteRepositoryForPersistentStores()
    {
        using var tempDirectory = new TemporaryDirectory();
        var runtime = AppWorkspaceFactory.Create(tempDirectory.GetPath("workspace.db"), new UnavailableAi());
        var service = new StoreManagementService(runtime.Repository, new FusionCanvas.Integration.Stores.StoreContextMapper(), () => Now, () => Guid.NewGuid());

        var created = await service.CreateStoreAsync(new StoreManagementCreateRequest(
            "North Star Studio",
            new StoreContext("POD brand", "Soft humor")), TestContext.Current.CancellationToken);
        var reloaded = AppWorkspaceFactory.Create(tempDirectory.GetPath("workspace.db"), new UnavailableAi());

        var store = Assert.Single(reloaded.Snapshot.Stores);
        Assert.True(created.Succeeded);
        Assert.Equal("North Star Studio", store.Name);
        Assert.Equal("POD brand", store.Description);
        Assert.Contains("Soft humor", store.MetadataJson);
    }

    [Fact]
    public async Task MainWindowViewModel_UsesSelectedStoreToFilterNavigationContexts()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var firstNiche = new Niche(Guid.NewGuid(), first.Id, "Coffee", null, false, Now, Now, "{}");
        var secondNiche = new Niche(Guid.NewGuid(), second.Id, "Cats", null, false, Now, Now, "{}");
        var firstItem = new Item(Guid.NewGuid(), first.Id, firstNiche.Id, null, "Espresso", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var secondItem = new Item(Guid.NewGuid(), second.Id, secondNiche.Id, null, "Whiskers", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([first, second], [firstNiche, secondNiche], [], [firstItem, secondItem], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);

        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == firstItem.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == secondItem.Id);

        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(store => store.Id == second.Id), TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == secondItem.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == firstItem.Id);
    }

    [Fact]
    public async Task MainWindowViewModel_ShowsActiveNichesAsTopLevelSidebarContexts()
    {
        var store = NewStore("North Star Studio");
        var activeNiche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var archivedNiche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, true, Now, Now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, activeNiche.Id, null, "Espresso", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [activeNiche, archivedNiche], [], [listing], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);

        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(), TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.NavigationContexts, context =>
            context.Context.Id == activeNiche.Id &&
            context.Context.Kind == DocumentContextKind.Topic &&
            context.Context.EntityKind == WorkspaceEntityKind.Niche &&
            context.Context.NavigationLocation?.NodePath.SequenceEqual([store.Id, activeNiche.Id]) == true);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == archivedNiche.Id);
        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == listing.Id);
    }

    [Fact]
    public async Task MainWindowViewModel_RefreshesSidebarAfterNicheCreation()
    {
        var store = NewStore("North Star Studio");
        var snapshot = new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = MainWindowViewModelFactory.CreateFromSnapshot(snapshot, repository);
        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(), TestContext.Current.CancellationToken);

        viewModel.StoreManagement.SelectNichesTabCommand.Execute(null);
        viewModel.StoreManagement.StartCreateNiche();
        viewModel.StoreManagement.NicheName = "Coffee";
        await viewModel.StoreManagement.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.NavigationContexts, context =>
            context.Context.Title == "Coffee" &&
            context.Context.EntityKind == WorkspaceEntityKind.Niche);
    }

    [Fact]
    public async Task TagsTab_CreatesRenamesArchivesRestoresAndDeletesTags()
    {
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, "Studio", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var tagId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            nicheService: null,
            new TagManagementService(repository, () => Now.AddMinutes(1), () => tagId));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.OpenTagsTabCommand.Execute(null);
        Assert.True(viewModel.IsTagsTabSelected);
        Assert.True(viewModel.NeedsFirstTag);

        viewModel.StartCreateTagCommand.Execute(null);
        Assert.True(viewModel.IsTagsTabSelected);
        viewModel.TagName = "Evergreen";
        viewModel.TagColor = "#1ab";
        viewModel.TagDescription = "Always relevant";
        await viewModel.SaveSelectedTagAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsStoreEditorOpen == false);
        Assert.True(viewModel.HasActiveTags);
        Assert.Equal("Evergreen", viewModel.SelectedTag!.Name);
        Assert.Equal("#11AABB", viewModel.SelectedTag.Color);

        viewModel.TagName = "EvergreenUpdated";
        await viewModel.SaveSelectedTagAsync(TestContext.Current.CancellationToken);
        Assert.Equal("EvergreenUpdated", viewModel.SelectedTag!.Name);

        viewModel.ArchiveSelectedTagCommand.Execute(null);
        await Task.Yield();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.HasArchivedTags);

        viewModel.RestoreTagCommand.Execute(viewModel.ArchivedTags.Single());
        await Task.Yield();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.HasActiveTags);

        viewModel.RequestDeleteSelectedTagCommand.Execute(null);
        Assert.True(viewModel.TagDeleteWarningVisible);
        viewModel.CancelDeleteTagCommand.Execute(null);
        Assert.False(viewModel.TagDeleteWarningVisible);
        Assert.True(viewModel.HasSelectedTag);

        viewModel.RequestDeleteSelectedTagCommand.Execute(null);
        await Task.Yield();
        await viewModel.ConfirmDeleteTagAsync(TestContext.Current.CancellationToken);
        Assert.False(viewModel.HasSelectedTag);
        Assert.Empty(repository.Snapshot.Tags);
    }

    [Fact]
    public async Task TagsTab_DeleteWarningReportsItemCountFromAppliedTag()
    {
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, "Studio", null, false, Now, Now, "{}");
        var niche = new Niche(Guid.NewGuid(), storeId, "Niche", null, false, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), storeId, niche.Id, null, "Shirt", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var tag = new Tag(Guid.NewGuid(), storeId, "Evergreen", null, false, Now, Now, "{}", null);
        var link = new ItemTag(item.Id, tag.Id);
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [item], [], [], [tag], [link], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            nicheService: null,
            new TagManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenTagsTabCommand.Execute(null);
        viewModel.EditTagCommand.Execute(viewModel.EditorActiveTags.Single());

        viewModel.RequestDeleteSelectedTagCommand.Execute(null);
        Assert.True(viewModel.TagDeleteWarningVisible);
        await Task.Yield();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Contains("1 item", viewModel.TagDeleteWarningMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(viewModel.CanConfirmDeleteTag);
    }

    [Fact]
    public async Task TagsTab_DiscardsUnsavedChangesWhenSwitchingTagsWithConfirmation()
    {
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, "Studio", null, false, Now, Now, "{}");
        var first = new Tag(Guid.NewGuid(), storeId, "Alpha", null, false, Now, Now, "{}", null);
        var second = new Tag(Guid.NewGuid(), storeId, "Beta", null, false, Now, Now, "{}", null);
        var snapshot = new WorkspaceSnapshot([store], [], [], [], [], [], [first, second], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            nicheService: null,
            new TagManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.OpenTagsTabCommand.Execute(null);
        viewModel.EditTagCommand.Execute(viewModel.EditorActiveTags.Single(tag => tag.Id == first.Id));

        viewModel.TagName = "Renamed";
        Assert.True(viewModel.HasUnsavedTagChanges);
        viewModel.EditTagCommand.Execute(viewModel.EditorActiveTags.Single(tag => tag.Id == second.Id));
        Assert.True(viewModel.DiscardChangesPromptVisible);

        viewModel.ConfirmDiscardChangesCommand.Execute(null);
        Assert.False(viewModel.DiscardChangesPromptVisible);
        Assert.Equal("Beta", viewModel.SelectedTag!.Name);
        Assert.False(viewModel.HasUnsavedTagChanges);
    }

    [Fact]
    public async Task TagEditor_IgnoresLoadResultFromPreviousStoreScope()
    {
        var firstStore = NewStore("First studio");
        var secondStore = NewStore("Second studio");
        var firstStoreId = firstStore.Id;
        var secondStoreId = secondStore.Id;
        var firstTag = new Tag(Guid.NewGuid(), firstStoreId, "First store tag", null, false, Now, Now, "{}", null);
        var secondTag = new Tag(Guid.NewGuid(), secondStoreId, "Second store tag", null, false, Now, Now, "{}", null);
        var repository = new DelayedFirstLoadWorkspaceRepository(new WorkspaceSnapshot(
            [firstStore, secondStore], [], [], [], [], [], [firstTag, secondTag], [], []));
        var service = new TagManagementService(repository);
        var editor = new TagEditorViewModel(service);

        editor.SetScope(new StoreManagementScope(firstStore.WorkspaceId, firstStoreId));
        var firstLoad = editor.LoadAsync(TestContext.Current.CancellationToken);
        await repository.FirstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(secondStore.WorkspaceId, secondStoreId));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Second store tag", Assert.Single(editor.ActiveTags).Name);

        repository.ReleaseFirstLoad();
        await firstLoad;

        Assert.Equal(secondStoreId, editor.Scope.StoreId);
        Assert.Equal(secondStoreId, service.ActiveStoreId);
        Assert.Equal("Second store tag", Assert.Single(editor.ActiveTags).Name);
    }

    [Fact]
    public async Task TagEditor_IgnoresDeleteCountFromPreviousStoreScope()
    {
        var firstStore = NewStore("First studio");
        var secondStore = NewStore("Second studio");
        var niche = new Niche(Guid.NewGuid(), firstStore.Id, "Coffee", null, false, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), firstStore.Id, niche.Id, null, "Mug", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var firstTag = new Tag(Guid.NewGuid(), firstStore.Id, "Old tag", null, false, Now, Now, "{}", null);
        var secondTag = new Tag(Guid.NewGuid(), secondStore.Id, "Current tag", null, false, Now, Now, "{}", null);
        var repository = new DelayedFirstLoadWorkspaceRepository(new WorkspaceSnapshot(
            [firstStore, secondStore], [niche], [], [item], [], [], [firstTag, secondTag], [new ItemTag(item.Id, firstTag.Id)], []));
        Func<CancellationToken, Task>? queuedOperation = null;
        var editor = new TagEditorViewModel(new TagManagementService(repository), operation => queuedOperation = operation);
        editor.SetScope(new StoreManagementScope(firstStore.WorkspaceId, firstStore.Id));
        var initialLoad = editor.LoadAsync(TestContext.Current.CancellationToken);
        await repository.FirstLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        repository.ReleaseFirstLoad();
        await initialLoad;
        editor.SelectTagForEditing(Assert.Single(editor.ActiveTags));

        var delayedCountStarted = repository.DelayNextLoad();
        editor.RequestDeleteSelectedTag();
        var oldCountTask = queuedOperation!(TestContext.Current.CancellationToken);
        await delayedCountStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(secondStore.WorkspaceId, secondStore.Id));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        editor.SelectTagForEditing(Assert.Single(editor.ActiveTags));
        editor.RequestDeleteSelectedTag();
        await queuedOperation!(TestContext.Current.CancellationToken);
        repository.ReleaseNextLoad();
        await oldCountTask;

        Assert.True(editor.DeleteWarningVisible);
        Assert.Contains("Current tag", editor.DeleteWarningMessage);
        Assert.DoesNotContain("1 Item", editor.DeleteWarningMessage);
    }

    [Fact]
    public async Task StoreEditor_UrlFieldIsBoundAndEditingMarksUnsavedChanges()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == store.Id));

        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(string.Empty, viewModel.Url);

        viewModel.Url = "https://mystore.example.com";

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.CanSaveSelectedStore);
        Assert.Equal("https://mystore.example.com", viewModel.Url);
    }

    [Fact]
    public async Task StoreEditor_UrlIncludedInCreatePayload()
    {
        var storeId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(),
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            () => Now,
            () => storeId));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.StartCreateStore();
        viewModel.NewStoreName = "North Star Studio";
        viewModel.Url = "https://mystore.example.com";

        await viewModel.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.SelectedStore?.Id != Guid.Empty);
        Assert.Equal("https://mystore.example.com", viewModel.SelectedStore?.Context.Url);
    }

    [Fact]
    public async Task StoreEditor_UrlIncludedInUpdatePayload()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0], TestContext.Current.CancellationToken);
        viewModel.Url = "https://updated.example.com";

        await viewModel.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.Equal("https://updated.example.com", viewModel.SelectedStore?.Context.Url);
    }

    [Fact]
    public async Task StoreEditor_ApplyingStoreRestoresUrlIntoField()
    {
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, "North Star Studio", null, false, Now, Now, """{"url":"https://mystore.example.com"}""");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(s => s.Id == storeId));

        Assert.Equal("https://mystore.example.com", viewModel.Url);
    }

    [Fact]
    public async Task StoreEditor_ClearingEditorStateBlanksUrl()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(s => s.Id == store.Id));

        viewModel.StartCreateStore();

        Assert.Equal(string.Empty, viewModel.Url);
    }

    [Fact]
    public async Task StoreEditor_UnchangedExistingStoreWithEmptyUrl_KeepsSaveDisabled()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(s => s.Id == store.Id));

        Assert.Equal(string.Empty, viewModel.Url);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.CanSaveSelectedStore);
    }

    [Fact]
    public async Task StoreEditor_NewStoreDraftSavesSuccessfullyWithEmptyUrl()
    {
        var storeId = Guid.NewGuid();
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(),
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            () => Now,
            () => storeId));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.StartCreateStore();
        viewModel.NewStoreName = "North Star Studio";

        await viewModel.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.SelectedStore?.Id != Guid.Empty);
        Assert.Null(viewModel.SelectedStore?.Context.Url);
    }

    [Fact]
    public async Task EditorClusters_PreserveFacadeDraftsAndShareCanonicalStoreScope()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single());

        viewModel.NewStoreName = "North Star Gifts";
        viewModel.TagName = "Seasonal";
        viewModel.ProductName = "Coffee mug";

        Assert.Equal(viewModel.NewStoreName, viewModel.StoreConfiguration.NewStoreName);
        Assert.Equal(viewModel.TagName, viewModel.TagEditor.TagName);
        Assert.Equal(viewModel.ProductName, viewModel.ProductCatalogEditor.ProductName);
        Assert.Equal(store.Id, viewModel.StoreConfiguration.Scope.StoreId);
        Assert.Equal(store.Id, viewModel.TagEditor.Scope.StoreId);
        Assert.Equal(store.Id, viewModel.ProductCatalogEditor.Scope.StoreId);

        viewModel.StoreConfiguration.NewStoreName = "Owner-edited name";
        viewModel.TagEditor.TagName = "Owner-edited tag";
        viewModel.ProductCatalogEditor.ProductName = "Owner-edited product";

        Assert.Equal("Owner-edited name", viewModel.NewStoreName);
        Assert.Equal("Owner-edited tag", viewModel.TagName);
        Assert.Equal("Owner-edited product", viewModel.ProductName);
        Assert.True(viewModel.TagEditor.HasUnsavedChanges);
        Assert.True(viewModel.ProductCatalogEditor.HasUnsavedProductDraft);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task TagEditorScope_TracksArchivedStoreAndNewStoreDraftContext()
    {
        var active = NewStore("Active studio");
        var archived = NewStore("Archived studio", isArchived: true);
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], [])), new FusionCanvas.Integration.Stores.StoreContextMapper()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(active.Id, viewModel.TagEditor.Scope.StoreId);
        Assert.False(viewModel.TagEditor.Scope.IsStoreArchived);
        Assert.False(viewModel.TagEditor.Scope.IsCreatingNewStore);

        viewModel.StartCreateStore();

        Assert.True(viewModel.TagEditor.Scope.IsCreatingNewStore);
        Assert.NotEqual(active.Id, viewModel.TagEditor.Scope.StoreId);

        viewModel.SelectStoreForEditing(viewModel.ArchivedStores.Single());

        Assert.Equal(archived.Id, viewModel.TagEditor.Scope.StoreId);
        Assert.True(viewModel.TagEditor.Scope.IsStoreArchived);
        Assert.False(viewModel.TagEditor.Scope.IsCreatingNewStore);
    }

    private static StoreSummary NewStoreSummary(string name)
    {
        var store = NewStore(name);
        return new StoreSummary(
            store.Id,
            store.WorkspaceId,
            store.Name,
            new StoreContext(),
            store.IsArchived,
            store.CreatedAt,
            store.UpdatedAt,
            store.FulfillmentStrategy);
    }

    private static Store NewStore(string name, bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, isArchived, Now, Now, "{}");

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public WorkspaceSnapshot Snapshot => _snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class UnavailableAi : IAiTextGenerationService
    {
        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiAvailabilityResult(
                AiAvailabilityKind.MissingCredential,
                "No test credential."));

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiTextResult.Failure(
                AiTextFailureKind.NotConfigured,
                "No test credential."));
    }

    private sealed class PendingPrintifyCatalogImportService : IPrintifyCatalogImportService
    {
        public TaskCompletionSource<CancellationToken> LoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource LoadFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseLoad { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public WaitHandle? LoadWaitHandle { get; private set; }

        public async Task<PrintifyCatalogResult> LoadBlueprintsAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            LoadWaitHandle = cancellationToken.WaitHandle;
            LoadStarted.TrySetResult(cancellationToken);
            try
            {
                await ReleaseLoad.Task.WaitAsync(TestContext.Current.CancellationToken);
                LoadWaitHandle.WaitOne();
                cancellationToken.ThrowIfCancellationRequested();
                return new(PrintifyCatalogResultKind.Succeeded, "Late result.", []);
            }
            finally
            {
                LoadFinished.TrySetResult();
            }
        }

        public Task<PrintifyCatalogResult> LoadSelectedAsync(
            StoreCredentialScope scope,
            IReadOnlyCollection<int> blueprintIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "No items selected."));
    }

    private sealed class PendingPrintifyCredentialConfigurationService : IStorePrintifyConfigurationService
    {
        public TaskCompletionSource<CancellationToken> ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReadFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public WaitHandle? ReadWaitHandle { get; private set; }

        public async Task<PrintifyConfigurationResult> ReadStatusAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
        {
            ReadWaitHandle = cancellationToken.WaitHandle;
            ReadStarted.TrySetResult(cancellationToken);
            try
            {
                await ReleaseRead.Task.WaitAsync(TestContext.Current.CancellationToken);
                ReadWaitHandle.WaitOne();
                cancellationToken.ThrowIfCancellationRequested();
                return new(PrintifyConfigurationKind.Missing, "Late result.");
            }
            finally
            {
                ReadFinished.TrySetResult();
            }
        }

        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Saved, "Saved."));

        public Task<PrintifyConfigurationResult> VerifyAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyConfigurationResult(PrintifyConfigurationKind.Verified, "Verified."));
    }

    private sealed class FaultingStoreManagementService : IStoreManagementService
    {
        public Func<Guid, CancellationToken, Task<StoreManagementResult>> SelectStore { get; init; } =
            (_, _) => throw new NotSupportedException();

        public Guid? ActiveWorkspaceId => null;

        public Guid? ActiveStoreId => null;

        public void SetActiveWorkspace(Guid? workspaceId) { }

        public Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default) =>
            SelectStore(storeId, cancellationToken);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string fileName) => Path.Combine(_directory.FullName, fileName);

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            _directory.Delete(recursive: true);
        }
    }

    private sealed class DelayedFirstLoadWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private int _loadCount;
        private readonly TaskCompletionSource _releaseFirstLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource? _nextLoadStarted;
        private TaskCompletionSource? _releaseNextLoad;
        public TaskCompletionSource FirstLoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseFirstLoad() => _releaseFirstLoad.TrySetResult();

        public TaskCompletionSource DelayNextLoad()
        {
            _releaseNextLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return _nextLoadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void ReleaseNextLoad() => _releaseNextLoad?.TrySetResult();

        public Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _loadCount) == 1)
            {
                FirstLoadStarted.TrySetResult();
                await _releaseFirstLoad.Task.WaitAsync(cancellationToken);
            }
            else if (Interlocked.Exchange(ref _nextLoadStarted, null) is { } started)
            {
                started.TrySetResult();
                await _releaseNextLoad!.Task.WaitAsync(cancellationToken);
                _releaseNextLoad = null;
            }

            return snapshot;
        }
    }
}
