using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class StoreManagementViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_ShowsFirstStoreEmptyState()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));

        await viewModel.LoadAsync();

        Assert.True(viewModel.NeedsFirstStore);
        Assert.True(viewModel.ShouldShowFirstStorePrompt);
        Assert.False(viewModel.HasActiveStores);
        Assert.Empty(viewModel.ActiveStores);
    }

    [Fact]
    public async Task FirstStorePrompt_CanOpenEditorOrBeDismissed()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
        await viewModel.LoadAsync();

        viewModel.DeclineFirstStorePromptCommand.Execute(null);

        Assert.False(viewModel.ShouldShowFirstStorePrompt);

        var secondViewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
        await secondViewModel.LoadAsync();

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

        await viewModel.CreateStoreAsync();

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
            () => Now,
            () => storeId));
        await viewModel.LoadAsync();

        viewModel.StartCreateStore();

        var draft = Assert.Single(viewModel.EditorActiveStores);
        Assert.Equal("New store", draft.Name);
        Assert.True(viewModel.HasSelectedStore);

        viewModel.NewStoreName = "North Star Studio";
        viewModel.Notes = "Soft humor";
        viewModel.SelectStoreForEditing(viewModel.EditorActiveStores.Single());
        await viewModel.SaveSelectedStoreAsync();

        Assert.Equal(storeId, viewModel.SelectedStore?.Id);
        Assert.Equal("North Star Studio", Assert.Single(viewModel.ActiveStores).Name);
        Assert.Equal("Soft humor", viewModel.SelectedStore?.Context.Notes);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task NewStoreDraft_RequestsStoreNameFocus()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
        await viewModel.LoadAsync();
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();

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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0]);
        viewModel.NewStoreName = "North Star Gifts";
        viewModel.Notes = "Sharper positioning";

        await viewModel.SaveSelectedStoreAsync();

        Assert.Equal("North Star Gifts", viewModel.SelectedStore?.Name);
        Assert.Equal("Sharper positioning", viewModel.SelectedStore?.Context.Notes);
    }

    [Fact]
    public async Task SelectingAnotherStore_WithUnsavedChangesRequiresDiscardConfirmation()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == second.Id));

        viewModel.OpenStoreEditorCommand.Execute(null);

        Assert.True(viewModel.IsStoreEditorOpen);
        Assert.Equal(second.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Second Studio", viewModel.NewStoreName);
    }

    [Fact]
    public async Task ArchiveAndRestoreFlows_ShowStoresSeparately()
    {
        var store = NewStore("North Star Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0]);

        await viewModel.ArchiveSelectedStoreAsync();
        var archived = Assert.Single(viewModel.ArchivedStores);
        await viewModel.RestoreStoreAsync(archived);

        Assert.Empty(viewModel.ArchivedStores);
        Assert.Equal(store.Id, Assert.Single(viewModel.ActiveStores).Id);
    }

    [Fact]
    public async Task Selector_TogglesCompactExpandedAndHighlightsSelectedStore()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var viewModel = new StoreManagementViewModel(new StoreManagementService(
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == first.Id));

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

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == second.Id));

        Assert.Contains(viewModel.SelectorStores, entry => entry.Id == second.Id && entry.IsSelected);
        Assert.DoesNotContain(viewModel.SelectorStores, entry => entry.Id == first.Id && entry.IsSelected);
    }

    [Fact]
    public async Task StoreEditor_DeleteWarningCanBeCanceled()
    {
        var store = NewStore("Empty Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        await viewModel.LoadAsync();
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0]);

        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        viewModel.CancelDeleteStoreCommand.Execute(null);

        Assert.False(viewModel.DeleteWarningVisible);
        Assert.Equal(store.Id, Assert.Single((await repository.LoadAsync()).Stores).Id);
    }

    [Fact]
    public async Task StoreEditor_DeleteConfirmedEmptyStoreAndBlocksConnectedStore()
    {
        var empty = NewStore("Empty Studio");
        var connected = NewStore("Connected Studio");
        var niche = new Niche(Guid.NewGuid(), connected.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([empty, connected], [niche], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        await viewModel.LoadAsync();

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == connected.Id));
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync();

        Assert.Contains("connected data", viewModel.ErrorMessage);
        Assert.Contains((await repository.LoadAsync()).Stores, store => store.Id == connected.Id);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == empty.Id));
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync();

        Assert.DoesNotContain((await repository.LoadAsync()).Stores, store => store.Id == empty.Id);
    }

    [Fact]
    public async Task StoreEditor_DeleteSelectsRemainingStoreByDefault()
    {
        var first = NewStore("First Studio");
        var second = NewStore("Second Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        await viewModel.LoadAsync();
        viewModel.SelectStoreForEditing(viewModel.ActiveStores.Single(store => store.Id == first.Id));

        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync();

        Assert.Equal(second.Id, viewModel.SelectedStore?.Id);
        Assert.Equal("Second Studio", viewModel.NewStoreName);
        Assert.True(viewModel.HasSelectedStore);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public void OpenStoreEditorCommand_ProvidesMenuFriendlyManagementEntry()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));

        viewModel.OpenStoreEditorCommand.Execute(null);

        Assert.True(viewModel.IsStoreEditorOpen);
    }

    [Fact]
    public async Task AppWorkspaceFactory_UsesSqliteRepositoryForPersistentStores()
    {
        using var tempDirectory = new TemporaryDirectory();
        var runtime = AppWorkspaceFactory.Create(tempDirectory.GetPath("workspace.db"));
        var service = new StoreManagementService(runtime.Repository, () => Now, () => Guid.NewGuid());

        var created = await service.CreateStoreAsync(new StoreManagementCreateRequest(
            "North Star Studio",
            new StoreContext("POD brand", "Soft humor")));
        var reloaded = AppWorkspaceFactory.Create(tempDirectory.GetPath("workspace.db"));

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
        var firstListing = new Listing(Guid.NewGuid(), first.Id, firstNiche.Id, null, "Espresso", null, ListingStatus.Draft, false, Now, Now, "{}");
        var secondListing = new Listing(Guid.NewGuid(), second.Id, secondNiche.Id, null, "Whiskers", null, ListingStatus.Draft, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([first, second], [firstNiche, secondNiche], [], [firstListing, secondListing], [], [], [], [], []);
        var storeService = new StoreManagementService(new InMemoryWorkspaceRepository(snapshot));

        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            storeService,
            snapshot);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == firstListing.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == secondListing.Id);

        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(store => store.Id == second.Id));

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == secondListing.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == firstListing.Id);
    }

    private static Store NewStore(string name, bool isArchived = false) =>
        new(Guid.NewGuid(), name, null, isArchived, Now, Now, "{}");

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
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
}
