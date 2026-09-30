using FusionCanvas.App.Stores;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class TagEditorScopeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LateTagCreateDoesNotClobberDraftInNewScope()
    {
        var oldStore = NewStore("Old scope store");
        var newStore = NewStore("New scope store");
        var repository = new SaveGatedWorkspaceRepository(
            new WorkspaceSnapshot([oldStore, newStore], [], [], [], [], [], [], [], []));
        var service = new TagManagementService(repository, () => Now, Guid.NewGuid);
        var editor = new TagEditorViewModel(service);
        var workspaceId = Guid.NewGuid();
        editor.SetScope(new StoreManagementScope(workspaceId, oldStore.Id));
        editor.StartCreateTag();
        editor.TagName = "Old scope tag";

        var save = editor.SaveSelectedTagAsync(TestContext.Current.CancellationToken);
        await repository.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        editor.SetScope(new StoreManagementScope(workspaceId, newStore.Id));
        editor.StartCreateTag();
        editor.TagName = "New scope draft";
        repository.ReleaseSave();
        await save;

        Assert.Equal(newStore.Id, editor.Scope.StoreId);
        Assert.True(editor.IsCreatingDraft);
        Assert.Equal("New scope draft", editor.TagName);
    }

    [Fact]
    public async Task LateTagCreateDoesNotClobberReplacementDraftInSameScope()
    {
        var store = NewStore("Current store");
        var repository = new SaveGatedWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new TagManagementService(repository, () => Now, Guid.NewGuid);
        var workspaceChanged = false;
        var editor = new TagEditorViewModel(service, workspaceChanged: () => workspaceChanged = true);
        editor.SetScope(new StoreManagementScope(Guid.NewGuid(), store.Id));
        editor.StartCreateTag();
        editor.TagName = "Old draft";

        var save = editor.SaveSelectedTagAsync(TestContext.Current.CancellationToken);
        await repository.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        editor.DiscardUnsavedChanges();
        editor.StartCreateTag();
        editor.TagName = "Replacement draft";
        repository.ReleaseSave();
        await save;

        Assert.True(editor.IsCreatingDraft);
        Assert.Equal("Replacement draft", editor.TagName);
        Assert.True(workspaceChanged);
    }

    [Fact]
    public async Task FailedDeleteCountIsShownAsUnknownInsteadOfZero()
    {
        var store = NewStore("Current store");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Evergreen", null, false, Now, Now, "{}", null);
        var repository = new FailsSecondLoadWorkspaceRepository(
            new WorkspaceSnapshot([store], [], [], [], [], [], [tag], [], []));
        Func<CancellationToken, Task>? pendingOperation = null;
        string? errorMessage = null;
        var editor = new TagEditorViewModel(
            new TagManagementService(repository, () => Now, Guid.NewGuid),
            operation => pendingOperation = operation,
            reportError: message => errorMessage = message);

        editor.SetScope(new StoreManagementScope(store.WorkspaceId, store.Id));
        await editor.LoadAsync(TestContext.Current.CancellationToken);
        editor.SelectTagForEditing(Assert.Single(editor.ActiveTags));

        editor.RequestDeleteSelectedTag();
        Assert.Contains("Checking", editor.DeleteWarningMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(editor.CanConfirmDeleteTag);
        await editor.ConfirmDeleteTagAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Wait", errorMessage, StringComparison.OrdinalIgnoreCase);
        var countOperation = Assert.IsType<Func<CancellationToken, Task>>(pendingOperation);
        await Assert.ThrowsAsync<IOException>(() => countOperation(TestContext.Current.CancellationToken));

        Assert.Contains("unknown", editor.DeleteWarningMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0 item", editor.DeleteWarningMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(editor.CanConfirmDeleteTag);
    }

    private static FusionCanvas.Domain.Stores.Store NewStore(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private sealed class SaveGatedWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;
        private readonly TaskCompletionSource _releaseSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseSave() => _releaseSave.TrySetResult();

        public async Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default)
        {
            SaveStarted.TrySetResult();
            await _releaseSave.Task.WaitAsync(cancellationToken);
            _snapshot = updated;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class FailsSecondLoadWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;
        private int _loadCount;

        public Task SaveAsync(WorkspaceSnapshot updated, CancellationToken cancellationToken = default)
        {
            _snapshot = updated;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _loadCount) == 2)
                throw new IOException("Unable to load the workspace snapshot.");

            return Task.FromResult(_snapshot);
        }
    }
}
