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

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.NeedsFirstStore);
        Assert.True(viewModel.ShouldShowFirstStorePrompt);
        Assert.False(viewModel.HasActiveStores);
        Assert.Empty(viewModel.ActiveStores);
    }

    [Fact]
    public async Task FirstStorePrompt_CanOpenEditorOrBeDismissed()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.DeclineFirstStorePromptCommand.Execute(null);

        Assert.False(viewModel.ShouldShowFirstStorePrompt);

        var secondViewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
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

        Assert.Equal(storeId, viewModel.SelectedStore?.Id);
        Assert.Equal("North Star Studio", Assert.Single(viewModel.ActiveStores).Name);
        Assert.Equal("Soft humor", viewModel.SelectedStore?.Context.Notes);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task NewStoreDraft_RequestsStoreNameFocus()
    {
        var viewModel = new StoreManagementViewModel(new StoreManagementService(new InMemoryWorkspaceRepository()));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], []))));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
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
            new StoreManagementService(repository),
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []))));
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
            new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []))));
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
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectStoreAsync(viewModel.ActiveStores[0], TestContext.Current.CancellationToken);

        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        viewModel.CancelDeleteStoreCommand.Execute(null);

        Assert.False(viewModel.DeleteWarningVisible);
        Assert.Equal(store.Id, Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores).Id);
    }

    [Fact]
    public async Task StoreEditor_DeleteConfirmedEmptyStoreAndBlocksConnectedStore()
    {
        var empty = NewStore("Empty Studio");
        var connected = NewStore("Connected Studio");
        var niche = new Niche(Guid.NewGuid(), connected.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([empty, connected], [niche], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == connected.Id), TestContext.Current.CancellationToken);
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync(TestContext.Current.CancellationToken);

        Assert.Contains("connected data", viewModel.ErrorMessage);
        Assert.Contains((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores, store => store.Id == connected.Id);

        await viewModel.SelectStoreAsync(viewModel.ActiveStores.Single(store => store.Id == empty.Id), TestContext.Current.CancellationToken);
        viewModel.RequestDeleteSelectedStoreCommand.Execute(null);
        await viewModel.ConfirmDeleteStoreAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores, store => store.Id == empty.Id);
    }

    [Fact]
    public async Task StoreEditor_DeleteSelectsRemainingStoreByDefault()
    {
        var first = NewStore("First Studio");
        var second = NewStore("Second Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(new StoreManagementService(repository));
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
            new StoreManagementService(repository),
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
            new StoreManagementService(repository),
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
            new StoreManagementService(repository),
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
            new StoreContext("POD brand", "Soft humor")), TestContext.Current.CancellationToken);
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

        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(store => store.Id == second.Id), TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.NavigationContexts, context => context.Context.Id == secondListing.Id);
        Assert.DoesNotContain(viewModel.NavigationContexts, context => context.Context.Id == firstListing.Id);
    }

    [Fact]
    public async Task MainWindowViewModel_ShowsActiveNichesAsTopLevelSidebarContexts()
    {
        var store = NewStore("North Star Studio");
        var activeNiche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var archivedNiche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, true, Now, Now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, activeNiche.Id, null, "Espresso", null, ListingStatus.Draft, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [activeNiche, archivedNiche], [], [listing], [], [], [], [], []);
        var repository = new InMemoryWorkspaceRepository(snapshot);

        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot);
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
        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot);
        await viewModel.StoreManagement.SelectStoreAsync(viewModel.StoreManagement.ActiveStores.Single(), TestContext.Current.CancellationToken);

        viewModel.StoreManagement.SelectNichesTabCommand.Execute(null);
        viewModel.StoreManagement.StartCreateNiche();
        viewModel.StoreManagement.NicheName = "Coffee";
        await viewModel.StoreManagement.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);

        Assert.Contains(viewModel.NavigationContexts, context =>
            context.Context.Title == "Coffee" &&
            context.Context.EntityKind == WorkspaceEntityKind.Niche);
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
