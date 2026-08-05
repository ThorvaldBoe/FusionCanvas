using FusionCanvas.App.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.TitleOptimization;

namespace FusionCanvas.App.Tests;

public class ItemInspectorViewModelTests
{
    [Fact]
    public async Task Load_PopulatesFieldsFromStateAndClearsDirty()
    {
        var sample = Sample.Create(withRelationships: true);
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(sample.Item.Id);

        Assert.True(viewModel.HasState);
        Assert.Equal(sample.Item.Name, viewModel.Title);
        Assert.Equal("Description", viewModel.Description);
        Assert.Equal("idea-value", viewModel.Idea);
        Assert.Equal("audience-value", viewModel.Audience);
        Assert.Equal("phrase-value", viewModel.Phrase);
        Assert.Equal("graphic-value", viewModel.GraphicDirection);
        Assert.Equal("Notes", viewModel.Notes);
        Assert.Equal(sample.Tag.Name, viewModel.TagDraft.Single());
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.False(viewModel.IsReadOnly);
    }

    [Fact]
    public async Task Load_MissingItemClearsState()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasState);
        Assert.Empty(viewModel.Title);
        Assert.Empty(viewModel.TagDraft);
    }

    [Fact]
    public async Task Load_ArchivedItemIsReadOnlyWithNoticeAndRestore()
    {
        var sample = Sample.Create();
        var archived = sample.Item with { IsArchived = true };
        sample.Repository.Set(sample.Snapshot with { Items = [archived] });
        var viewModel = sample.CreateViewModel();

        await viewModel.LoadAsync(archived.Id);

        Assert.True(viewModel.IsReadOnly);
        Assert.NotEmpty(viewModel.InactiveNotice);
        Assert.False(viewModel.CanEdit);
        Assert.True(viewModel.CanRestore);
        Assert.False(viewModel.CanArchive);
    }

    [Fact]
    public async Task EditingFields_MarksDirty()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "Changed";
        viewModel.Idea = "new idea";
        viewModel.Audience = "new audience";
        viewModel.Description = "new description";

        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Commit_NoOpWhenClean()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        await viewModel.CommitEditsAsync();

        Assert.Equal(0, sample.Repository.SaveCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Commit_DesignStagePersistsSharedFieldsWithoutChangingUpstreamMetadata()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "Renamed";
        viewModel.Idea = "new idea";
        viewModel.Audience = "coffee lovers";
        viewModel.Description = "new description";
        viewModel.Notes = "new notes";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("Renamed", viewModel.Title);
        Assert.Equal("idea-value", viewModel.Idea);
        Assert.Equal("audience-value", viewModel.Audience);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal("Description", persisted.Description);
        Assert.Contains("\"idea\":\"idea-value\"", persisted.MetadataJson);
        Assert.Contains("\"idea.audience\":\"audience-value\"", persisted.MetadataJson);
        Assert.Contains("\"notes\":\"new notes\"", persisted.MetadataJson);
        Assert.Contains("\"unknown\":\"kept\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_BlankOptionalTitleSavesWithOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "   ";
        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal(string.Empty, persisted.Name);
        Assert.Contains("\"notes\":\"changed notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Commit_MultiLineTitleRevertsTitleAndPersistsOtherEdits()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        viewModel.Notes = "kept notes";

        viewModel.Title = "line one\nline two";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Item.Name, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal(sample.Item.Name, persisted.Name);
        Assert.Contains("\"notes\":\"kept notes\"", persisted.MetadataJson);
        Assert.Equal(1, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_MultiLineOnlyTitleSkipsSaveButReportsRevert()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Title = "line one\nline two";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(sample.Item.Name, viewModel.Title);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(0, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_SerializedDrainPersistsLatestEditAndNoStaleOverwrite()
    {
        var sample = Sample.Create();
        sample.Repository.SaveDelay = TimeSpan.FromMilliseconds(150);
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "first";
        var firstCommit = viewModel.CommitEditsAsync();
        viewModel.Notes = "second";
        var secondCommit = viewModel.CommitEditsAsync();

        await Task.WhenAll(firstCommit, secondCommit);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal("second", viewModel.Notes);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Contains("\"notes\":\"second\"", persisted.MetadataJson);
        Assert.Equal(2, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task Commit_MidFlightEditIsPreservedAcrossSave()
    {
        var sample = Sample.Create();
        sample.Repository.SaveDelay = TimeSpan.FromMilliseconds(150);
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "first";
        var commit = viewModel.CommitEditsAsync();
        viewModel.Notes = "second";
        await commit;

        Assert.Equal("second", viewModel.Notes);
        Assert.True(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Commit_PersistenceFailureKeepsDraftAndReportsError()
    {
        var sample = Sample.Create();
        sample.Repository.FailSaves = true;
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.Notes = "changed notes";
        await viewModel.CommitEditsAsync();

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("changed notes", viewModel.Notes);
        Assert.Equal(sample.Snapshot, sample.Repository.Snapshot);
    }

    [Fact]
    public async Task TagChange_PersistsImmediatelyWithoutReplacingTextDraft()
    {
        var sample = Sample.Create();
        var viewModel = new ItemInspectorViewModel(
            new ItemInspectorService(sample.Repository),
            new ItemManagementService(sample.Repository),
            new TagManagementService(sample.Repository));
        await viewModel.LoadAsync(sample.Item.Id, TestContext.Current.CancellationToken);
        viewModel.Title = "pending title";
        viewModel.TagInput = "Immediate";

        viewModel.AddTagCommand.Execute(null);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Equal("pending title", viewModel.Title);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Contains("Immediate", viewModel.TagDraft);
        Assert.Contains(sample.Repository.Snapshot.ItemTags, link =>
            link.ItemId == sample.Item.Id
            && sample.Repository.Snapshot.Tags.Single(tag => tag.Id == link.TagId).Name == "Immediate");
    }

    [Fact]
    public async Task Commit_RaisesSavedEvent()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        var raised = 0;
        viewModel.Saved += (_, _) => raised++;

        viewModel.Title = "Renamed";
        await viewModel.CommitEditsAsync();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task AddTag_CommitsImmediately()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.TagInput = "New Tag";
        viewModel.AddTagCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Contains("New Tag", viewModel.TagDraft);
        Assert.Contains(sample.Repository.Snapshot.Tags, tag => tag.Name == "New Tag");
        Assert.Equal(1, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task AddTag_RejectsInvalidInputWithoutSaving()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.TagInput = "   ";
        viewModel.AddTagCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(0, sample.Repository.SaveCount);
    }

    [Fact]
    public async Task RemoveTag_CommitsImmediately()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.RemoveTagCommand.Execute(sample.Tag.Name);

        Assert.False(viewModel.HasError);
        Assert.Empty(viewModel.TagDraft);
        Assert.Empty(sample.Repository.Snapshot.ItemTags.Where(link => link.ItemId == sample.Item.Id));
        Assert.Contains(sample.Repository.Snapshot.Tags, tag => tag.Id == sample.Tag.Id);
    }

    [Fact]
    public async Task ApplyStage_UpdatesEmphasisAndKeepsAllSectionsAccessible()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);

        viewModel.ApplyStage(WorkflowStage.Concept);

        Assert.True(viewModel.EmphasizesConcept);
        Assert.False(viewModel.EmphasizesIdea);
        Assert.True(viewModel.HasState);
    }

    [Fact]
    public async Task SllOnlyChange_MarksDirtyAndCommitsThroughStagePayload()
    {
        var sample = Sample.Create();
        var conceptItem = sample.Item with { Stage = WorkflowStage.Concept };
        sample.Repository.Set(sample.Snapshot with { Items = [conceptItem] });
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(conceptItem.Id);
        viewModel.ApplyStage(WorkflowStage.Concept);

        // An SLL-only change with no other dirty field must still register as unsaved
        // (regression guard for the SR-001 dirty-check fix).
        viewModel.Sll = """{"AsciiSketch":"+---+","Triangle":{"Phrase":"X"}}""";

        Assert.True(viewModel.HasUnsavedChanges);

        await viewModel.CommitEditsAsync();

        Assert.False(viewModel.HasError);
        var persisted = sample.Repository.Snapshot.Items.Single(listing => listing.Id == conceptItem.Id);
        Assert.Contains("\"sll\":", persisted.MetadataJson);
        Assert.Contains("AsciiSketch", persisted.MetadataJson);
    }

    [Fact]
    public async Task Archive_ConfirmedArchivesAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestArchiveCommand.Execute(null);
        Assert.True(viewModel.ArchiveConfirmationVisible);
        viewModel.ConfirmArchiveCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.ArchiveConfirmationVisible);
        Assert.NotNull(raised);
        Assert.False(raised!.Deleted);
        Assert.True(sample.Repository.Snapshot.Items.Single().IsArchived);
    }

    [Fact]
    public async Task Restore_RestoresArchivedItemAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        viewModel.RequestArchiveCommand.Execute(null);
        viewModel.ConfirmArchiveCommand.Execute(null);
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RestoreCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.False(sample.Repository.Snapshot.Items.Single().IsArchived);
    }

    [Fact]
    public async Task Delete_ConfirmedDeletesAndRaisesLifecycleChanged()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        ItemInspectorLifecycleEventArgs? raised = null;
        viewModel.LifecycleChanged += (_, args) => raised = args;

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.DeleteConfirmationVisible);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.NotNull(raised);
        Assert.True(raised!.Deleted);
        Assert.Empty(sample.Repository.Snapshot.Items);
    }

    [Fact]
    public async Task Delete_PersistenceFailureReportsErrorAndKeepsItem()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel();
        await viewModel.LoadAsync(sample.Item.Id);
        sample.Repository.FailSaves = true;

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Single(sample.Repository.Snapshot.Items);
    }

    [Fact]
    public async Task Optimize_DisabledWhenNoOptimizationServiceIsSupplied()
    {
        var sample = Sample.Create();
        var viewModel = sample.CreateViewModel(); // no TitleOptimization service
        await viewModel.LoadAsync(sample.Item.Id);

        Assert.False(viewModel.CanOptimize);
    }

    [Fact]
    public async Task Optimize_DisabledWithSettingsGuidanceWhenAiUnavailable()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization
        {
            Availability = new AiAvailabilityResult(AiAvailabilityKind.MissingCredential, "Add an OpenRouter API key in AI settings.")
        };
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        Assert.False(viewModel.CanOptimize);
        Assert.Contains("AI settings", viewModel.OptimizeGuidance);
    }

    [Fact]
    public async Task Optimize_DisabledWithContentGuidanceWhenNoCreativeContent()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization();
        var viewModel = sample.CreateViewModel(optimization: optimization);
        sample.Repository.Set(sample.Snapshot with { Items = [sample.Item with { MetadataJson = "{}" }] });
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        Assert.False(viewModel.CanOptimize);
        Assert.Contains("creative content", viewModel.OptimizeGuidance);
    }

    [Fact]
    public async Task Optimize_DisabledWithRestoreGuidanceWhenReadOnlyArchived()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization();
        var viewModel = sample.CreateViewModel(optimization: optimization);
        sample.Repository.Set(sample.Snapshot with { Items = [sample.Item with { IsArchived = true }] });
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        Assert.False(viewModel.CanOptimize);
        Assert.Contains("Restore", viewModel.OptimizeGuidance);
    }

    [Fact]
    public async Task Optimize_EnabledWhenAiReadyAndCreativeContentPresent()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization();
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        Assert.True(viewModel.CanOptimize);
    }

    [Fact]
    public async Task OptimizeCommand_CanExecuteTracksReadyAiAfterRefresh()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization
        {
            Availability = new AiAvailabilityResult(AiAvailabilityKind.MissingCredential, "Add an OpenRouter API key in AI settings.")
        };
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);

        Assert.False(viewModel.OptimizeCommand.CanExecute(null));

        optimization.Availability = AiAvailabilityResult.Ready;
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        Assert.True(viewModel.CanOptimize);
        Assert.True(viewModel.OptimizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Optimize_SuccessOverwritesAndPersists()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization
        {
            Result = TitleOptimizationResult.Success("Pug coach hostage")
        };
        var viewModel = sample.CreateViewModel(optimization: optimization, clock: () => sample.Now.AddMinutes(1));
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        viewModel.OptimizeCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.Title == "Pug coach hostage" && sample.Repository.SaveCount > 0);

        Assert.False(viewModel.HasError);
        Assert.Equal("Pug coach hostage", viewModel.Title);
        var persisted = sample.Repository.Snapshot.Items.Single(item => item.Id == sample.Item.Id);
        Assert.Equal("Pug coach hostage", persisted.Name);
    }

    [Fact]
    public async Task Optimize_LocksFieldWhileRunning()
    {
        var sample = Sample.Create();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var optimization = new FakeTitleOptimization
        {
            Handler = async (_, _) =>
            {
                await gate.Task;
                return TitleOptimizationResult.Success("Pug coach hostage");
            }
        };
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();
        Assert.True(viewModel.CanEditShared);

        viewModel.OptimizeCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.IsOptimizing);

        Assert.True(viewModel.IsOptimizing);
        Assert.False(viewModel.CanEditShared);
        Assert.False(viewModel.CanOptimize);

        gate.SetResult();
        await WaitUntilAsync(() => !viewModel.IsOptimizing);
    }

    [Fact]
    public async Task Optimize_FailureLeavesTitleUnchanged()
    {
        var sample = Sample.Create();
        var optimization = new FakeTitleOptimization
        {
            Result = TitleOptimizationResult.Failure("Provider boom.")
        };
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();
        var original = viewModel.Title;

        viewModel.OptimizeCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.HasError);

        Assert.Equal(original, viewModel.Title);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public async Task Optimize_ItemSwitchCancelsInFlightOperation()
    {
        var sample = Sample.Create();
        var other = sample.Item with { Id = Guid.NewGuid(), Name = "Other item" };
        sample.Repository.Set(sample.Snapshot with { Items = [sample.Item, other] });
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var optimization = new FakeTitleOptimization
        {
            Handler = async (_, token) =>
            {
                using var registration = token.Register(() => gate.TrySetCanceled());
                await gate.Task;
                return TitleOptimizationResult.Success("Late title");
            }
        };
        var viewModel = sample.CreateViewModel(optimization: optimization);
        await viewModel.LoadAsync(sample.Item.Id);
        await viewModel.RefreshTitleOptimizationAvailabilityAsync();

        viewModel.OptimizeCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.IsOptimizing);
        await viewModel.LoadAsync(other.Id);

        await WaitUntilAsync(() => viewModel.LoadedItemId == other.Id && !viewModel.IsOptimizing);
        Assert.NotEqual("Late title", viewModel.Title);
        Assert.False(viewModel.IsOptimizing);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.ElapsedMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeTitleOptimization : ITitleOptimizationService
    {
        public AiAvailabilityResult Availability { get; set; } = AiAvailabilityResult.Ready;
        public TitleOptimizationResult Result { get; set; } = TitleOptimizationResult.Success("Optimized title");
        public Func<TitleOptimizationRequest, CancellationToken, Task<TitleOptimizationResult>>? Handler { get; set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Availability);

        public async Task<TitleOptimizationResult> OptimizeAsync(
            TitleOptimizationRequest request,
            CancellationToken cancellationToken = default) =>
            Handler is null ? Result : await Handler(request, cancellationToken);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; set; }
        public TimeSpan? SaveDelay { get; set; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public async Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (SaveDelay is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            Snapshot = value;
            SaveCount++;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Item Item, Tag Tag, TestRepository Repository)
    {
        public ItemInspectorViewModel CreateViewModel(
            Func<DateTimeOffset>? clock = null,
            ITitleOptimizationService? optimization = null) =>
            new(
                new ItemInspectorService(Repository, clock: clock, newId: Guid.NewGuid),
                new ItemManagementService(Repository, clock: clock, newId: Guid.NewGuid),
                optimization: optimization);

        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Item(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ItemStatus.Draft, WorkflowStage.Design, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"idea.audience\":\"audience-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [], [], [tag],
                [new ItemTag(listing.Id, tag.Id)], []);
            return new(snapshot, now, store, niche, root, child, listing, tag, new TestRepository(snapshot));
        }
    }
}
