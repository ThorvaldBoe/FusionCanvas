using FusionCanvas.App.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Stores;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Tests;

public class StoreEditorScopeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LateStoreCreateDoesNotClobberDraftInNewScope()
    {
        var oldWorkspaceId = Guid.NewGuid();
        var newWorkspaceId = Guid.NewGuid();
        var createdStore = new StoreSummary(Guid.NewGuid(), oldWorkspaceId, "Old scope store", new StoreContext(), false, Now, Now);
        var state = new StoreManagementState(oldWorkspaceId, [createdStore], [], createdStore.Id, createdStore, false);
        var service = new DelayedStoreManagementService();
        var editor = new StoreNicheConfigurationViewModel(service, null, null, _ => { }, _ => { }, _ => { });
        editor.SetScope(new StoreManagementScope(oldWorkspaceId, null));
        editor.StartCreateStoreDraft();
        editor.NewStoreName = "Old scope store";

        var save = editor.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);
        await service.CreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(newWorkspaceId, null, IsCreatingNewStore: true));
        editor.StartCreateStoreDraft();
        editor.NewStoreName = "New scope draft";
        service.CompleteCreate(StoreManagementResult.Success(createdStore, state));
        await save;

        Assert.Equal(newWorkspaceId, editor.Scope.WorkspaceId);
        Assert.Null(editor.Scope.StoreId);
        Assert.True(editor.IsCreatingNewStore);
        Assert.Equal("New scope draft", editor.NewStoreName);
    }

    [Fact]
    public async Task LateNicheCreateDoesNotClobberDraftInNewScope()
    {
        var workspaceId = Guid.NewGuid();
        var oldStoreId = Guid.NewGuid();
        var newStoreId = Guid.NewGuid();
        var stores = new DelayedStoreManagementService();
        var niches = new DelayedNicheManagementService();
        var editor = new StoreNicheConfigurationViewModel(stores, niches, null, _ => { }, _ => { }, _ => { });
        editor.SetScope(new StoreManagementScope(workspaceId, oldStoreId));
        editor.StartCreateNicheDraft();
        editor.NicheName = "Old scope niche";

        var save = editor.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);
        await niches.CreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(workspaceId, newStoreId));
        editor.StartCreateNicheDraft();
        editor.NicheName = "New scope draft";
        niches.CompleteCreate(NicheManagementResult.Success(
            null,
            new NicheManagementState(oldStoreId, [], [], null, null, true)));
        await save;

        Assert.Equal(newStoreId, editor.Scope.StoreId);
        Assert.True(editor.IsCreatingNewNiche);
        Assert.Equal("New scope draft", editor.NicheName);
    }

    [Fact]
    public async Task LateStoreCreateDoesNotClobberReplacementDraftInSameWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var createdStore = new StoreSummary(Guid.NewGuid(), workspaceId, "Old draft", new StoreContext(), false, Now, Now);
        var state = new StoreManagementState(workspaceId, [createdStore], [], createdStore.Id, createdStore, false);
        var service = new DelayedStoreManagementService();
        var workspaceChanged = false;
        var editor = new StoreNicheConfigurationViewModel(service, null, null, _ => { }, _ => { }, _ => { }, workspaceChanged: () => workspaceChanged = true);
        editor.SetScope(new StoreManagementScope(workspaceId, null));
        editor.StartCreateStoreDraft();
        editor.NewStoreName = "Old draft";

        var save = editor.SaveSelectedStoreAsync(TestContext.Current.CancellationToken);
        await service.CreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.DiscardStoreDraft();
        editor.StartCreateStoreDraft();
        editor.NewStoreName = "Replacement draft";
        service.CompleteCreate(StoreManagementResult.Success(createdStore, state));
        await save;

        Assert.Null(editor.Scope.StoreId);
        Assert.True(editor.IsCreatingNewStore);
        Assert.Equal("Replacement draft", editor.NewStoreName);
        Assert.True(workspaceChanged);
    }

    [Fact]
    public async Task LateNicheCreateDoesNotClobberReplacementDraftInSameScope()
    {
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var stores = new DelayedStoreManagementService();
        var niches = new DelayedNicheManagementService();
        var workspaceChanged = false;
        var editor = new StoreNicheConfigurationViewModel(stores, niches, null, _ => { }, _ => { }, _ => { }, workspaceChanged: () => workspaceChanged = true);
        editor.SetScope(new StoreManagementScope(workspaceId, storeId));
        editor.StartCreateNicheDraft();
        editor.NicheName = "Old draft";

        var save = editor.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);
        await niches.CreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.DiscardNicheDraft();
        editor.StartCreateNicheDraft();
        editor.NicheName = "Replacement draft";
        niches.CompleteCreate(NicheManagementResult.Success(
            null,
            new NicheManagementState(storeId, [], [], null, null, true)));
        await save;

        Assert.True(editor.IsCreatingNewNiche);
        Assert.Equal("Replacement draft", editor.NicheName);
        Assert.True(workspaceChanged);
    }

    [Fact]
    public async Task StoreDeleteResultIsIgnoredWhenTheWorkspaceScopeChanged()
    {
        var oldWorkspaceId = Guid.NewGuid();
        var newWorkspaceId = Guid.NewGuid();
        var store = new StoreSummary(Guid.NewGuid(), oldWorkspaceId, "Store", new StoreContext(), false, Now, Now);
        var state = new StoreManagementState(oldWorkspaceId, [], [], null, null, false);
        var service = new DelayedStoreManagementService();
        var applied = false;
        var editor = new StoreNicheConfigurationViewModel(service, null, null, _ => { }, _ => applied = true, _ => { });
        editor.SetScope(new StoreManagementScope(oldWorkspaceId, store.Id));

        var deletion = editor.DeleteStoreAsync(store, TestContext.Current.CancellationToken);
        await service.DeleteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.SetScope(new StoreManagementScope(newWorkspaceId, store.Id));
        service.CompleteDelete(StoreManagementResult.Success(null, state));

        Assert.False(await deletion);
        Assert.False(applied);
        Assert.Equal(newWorkspaceId, editor.Scope.WorkspaceId);
    }

    private sealed class DelayedStoreManagementService : IStoreManagementService
    {
        private readonly TaskCompletionSource<StoreManagementResult> _createCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<StoreManagementResult> _deleteCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CreateStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DeleteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Guid? ActiveWorkspaceId { get; private set; }
        public Guid? ActiveStoreId => null;
        public void SetActiveWorkspace(Guid? workspaceId) => ActiveWorkspaceId = workspaceId;
        public void CompleteCreate(StoreManagementResult result) => _createCompletion.TrySetResult(result);
        public void CompleteDelete(StoreManagementResult result) => _deleteCompletion.TrySetResult(result);
        public Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateStarted.TrySetResult();
            return _createCompletion.Task;
        }
        public Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default)
        {
            DeleteStarted.TrySetResult();
            return _deleteCompletion.Task;
        }
        public Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DelayedNicheManagementService : INicheManagementService
    {
        private readonly TaskCompletionSource<NicheManagementResult> _createCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CreateStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Guid? ActiveWorkspaceId { get; private set; }
        public Guid? ActiveStoreId => null;
        public Guid? ActiveNicheId => null;
        public void SetActiveWorkspace(Guid? workspaceId) => ActiveWorkspaceId = workspaceId;
        public void CompleteCreate(NicheManagementResult result) => _createCompletion.TrySetResult(result);
        public Task<NicheManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NicheManagementResult> CreateNicheAsync(NicheManagementCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateStarted.TrySetResult();
            return _createCompletion.Task;
        }
        public Task<NicheManagementResult> UpdateNicheAsync(NicheManagementUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NicheManagementResult> ArchiveNicheAsync(Guid nicheId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NicheManagementResult> RestoreNicheAsync(Guid nicheId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NicheManagementResult> DeleteNicheAsync(NicheManagementDeleteRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NicheManagementResult> SelectNicheAsync(Guid nicheId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
