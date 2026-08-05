using FusionCanvas.App.Items;
using FusionCanvas.App.SllGeneration;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.Tests;

public sealed class SllGenerationSessionViewModelTests
{
    [Fact]
    public async Task Generate_DisabledWhenTriangleIncomplete()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "A short idea", phrase: "", graphicDirection: "");

        Assert.False(vm.CanGenerate);
        Assert.Contains("Complete", vm.GenerateDisabledReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_EnabledWhenTriangleCompleteAndAvailableAndEditable()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        Assert.True(vm.CanGenerate);
        Assert.Null(vm.GenerateDisabledReason);
    }

    [Fact]
    public async Task Generate_DisabledWhenAiUnavailable()
    {
        var inspector = CreateInspector();
        var acc = new StubSllAccess(false);
        var svc = new StubSllService();
        var vm = new SllGenerationSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        Assert.False(vm.IsAvailable);
        Assert.False(vm.CanGenerate);
        Assert.NotNull(vm.GenerateDisabledReason);
    }

    [Fact]
    public async Task GenerateSuccess_AppliesSllToInspectorAndCommits()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        Assert.True(vm.CanGenerate);
        vm.GenerateCommand.Execute(null);
        await Task.Delay(100);

        Assert.True(vm.HasCurrentSll);
        Assert.Contains("PRODUCT TEXT", vm.AsciiSketch);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsStale);
    }

    [Fact]
    public async Task Regenerate_WhenCurrentSllExists_ReplacesIt()
    {
        var inspector = CreateInspector();
        var svc = new StubSllService();
        var vm = new SllGenerationSessionViewModel(svc, new StubSllAccess(true), inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        vm.GenerateCommand.Execute(null);
        await Task.Delay(100);
        Assert.True(vm.HasCurrentSll);
        Assert.True(vm.CanRegenerate);
        Assert.Null(vm.RegenerateDisabledReason);

        svc.Result = SllGenerationResult.Success(SampleDocument("SECOND VERSION"));
        vm.RegenerateCommand.Execute(null);
        await Task.Delay(100);

        Assert.Contains("SECOND VERSION", vm.AsciiSketch);
    }

    [Fact]
    public async Task GenerateFailure_KeepsExistingSllAndShowsError()
    {
        var inspector = CreateInspector();
        var svc = new StubSllService();
        var vm = new SllGenerationSessionViewModel(svc, new StubSllAccess(true), inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        svc.Result = SllGenerationResult.Success(SampleDocument("FIRST VERSION"));
        vm.GenerateCommand.Execute(null);
        await Task.Delay(100);
        Assert.True(vm.HasCurrentSll);

        svc.Result = SllGenerationResult.Failure(AiTextFailureKind.ProviderFailure, "Provider failed.");
        vm.RegenerateCommand.Execute(null);
        await Task.Delay(100);

        Assert.Contains("FIRST VERSION", vm.AsciiSketch);
        Assert.Equal("Provider failed.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Busy_DisablesActions()
    {
        var inspector = CreateInspector();
        var svc = new StubSllService();
        var vm = new SllGenerationSessionViewModel(svc, new StubSllAccess(true), inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        var tcs = new TaskCompletionSource<SllGenerationResult>();
        svc.Func = _ => tcs.Task;

        vm.GenerateCommand.Execute(null);
        Assert.True(vm.IsBusy);
        Assert.False(vm.CanGenerate);
        Assert.False(vm.CanRegenerate);

        tcs.TrySetResult(SllGenerationResult.Success(SampleDocument("X")));
        await Task.Delay(100);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ItemSwitch_CancelsInFlightAndDoesNotApplyLateResult()
    {
        var inspector = CreateInspector();
        var svc = new StubSllService();
        var vm = new SllGenerationSessionViewModel(svc, new StubSllAccess(true), inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        var tcs = new TaskCompletionSource<SllGenerationResult>();
        svc.Func = async ct =>
        {
            await Task.Delay(500, ct);
            ct.ThrowIfCancellationRequested();
            return SllGenerationResult.Success(SampleDocument("LATE"));
        };

        vm.GenerateCommand.Execute(null);
        Assert.True(vm.IsBusy);

        vm.ResetSession();
        await Task.Delay(50);
        Assert.False(vm.IsBusy);

        tcs.TrySetResult(SllGenerationResult.Success(SampleDocument("LATE")));
        await Task.Delay(200);
        Assert.False(vm.HasCurrentSll);
        Assert.Empty(inspector.Sll);
    }

    [Fact]
    public async Task StaleMarker_ShowsWhenScoreDropsAfterGeneration()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic");

        vm.GenerateCommand.Execute(null);
        await Task.Delay(100);
        Assert.True(vm.HasCurrentSll);
        Assert.False(vm.IsStale);

        inspector.GraphicDirection = "short";
        Assert.False(vm.CanGenerate);
        Assert.False(vm.CanRegenerate);
        Assert.True(vm.IsStale);
    }

    [Fact]
    public async Task CommitFailure_RetainsSllDraftAndSurfacesRecoverableError()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic",
            failSaves: true);

        vm.GenerateCommand.Execute(null);
        await Task.Delay(100);

        // The generated SLL is retained in the inspector draft despite the failed commit.
        Assert.True(vm.HasCurrentSll);
        Assert.False(string.IsNullOrEmpty(inspector.Sll));
        // A recoverable error surfaces through the inspector.
        Assert.True(inspector.HasError);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ReadOnlyReview_DisablesGenerateWithStageReason()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "A substantive concept idea",
            phrase: "A substantive phrase",
            graphicDirection: "A substantive graphic",
            stage: WorkflowStage.Design);
        inspector.ApplyStage(WorkflowStage.Concept);

        Assert.False(inspector.CanEditStage);
        Assert.False(vm.CanGenerate);
        Assert.Equal(inspector.StageReadOnlyReason, vm.GenerateDisabledReason);
    }

    // --- Helpers ---

    private static SllDocument SampleDocument(string sketchText) =>
        new(
            ["Assumption"],
            new SllCommunication("signal", "inference", "emotion", "context"),
            new SllTriangle("idea", "phrase", "graphic", "completion", null),
            sketchText,
            new SllNotes("composition", null, null, null, null, null, null),
            new SllValidation("order", null, null, null));

    private static ItemInspectorViewModel CreateInspector()
    {
        var svc = new StubItemInspectorService();
        return new ItemInspectorViewModel(svc, new StubItemManagementService());
    }

    private static SllGenerationSessionViewModel CreateSessionViewModel(
        ItemInspectorViewModel? inspector = null)
    {
        inspector ??= CreateInspector();
        return new SllGenerationSessionViewModel(
            new StubSllService(),
            new StubSllAccess(true),
            inspector);
    }

    private static async Task SetupLoadedInspectorAsync(ItemInspectorViewModel inspector,
        string? conceptIdea = null, string? phrase = null, string? graphicDirection = null,
        string? sll = null, bool failSaves = false, WorkflowStage stage = WorkflowStage.Concept)
    {
        var state = CreateValidState(conceptIdea: conceptIdea, phrase: phrase, graphicDirection: graphicDirection, sll: sll, stage: stage);
        var svcField = typeof(ItemInspectorViewModel).GetField(
            "_service",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (svcField?.GetValue(inspector) is StubItemInspectorService stub)
        {
            stub.StateToReturn = state;
            stub.FailSaves = failSaves;
        }

        await inspector.LoadAsync(state.Id);
    }

    private static ItemInspectorState CreateValidState(Guid? id = null,
        string? conceptIdea = null, string? phrase = null, string? graphicDirection = null,
        string? sll = null, WorkflowStage stage = WorkflowStage.Concept) =>
        new(
            id ?? Guid.NewGuid(),
            "Test Item",
            "Description",
            new ItemInspectorCreativeFields(
                Idea: "test-idea",
                Audience: null,
                ConceptIdea: conceptIdea ?? "",
                Phrase: phrase ?? "",
                GraphicDirection: graphicDirection ?? ""),
            "Notes",
            ItemStatus.Draft,
            stage,
            IsArchived: false,
            IsEffectivelyActive: true,
            "Store / Niche / Test",
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            Sll: sll);

    private sealed class StubSllService : ISllGenerationService
    {
        public SllGenerationResult Result { get; set; } =
            SllGenerationResult.Success(new SllDocument(
                ["A"], new SllCommunication("s", "i", "e", "c"),
                new SllTriangle("PRODUCT TEXT", "phrase", "graphic", "completion", null),
                "+---+\n|PRODUCT TEXT|\n+---+",
                new SllNotes(null, null, null, null, null, null, null),
                new SllValidation(null, null, null, null)));
        public Func<CancellationToken, Task<SllGenerationResult>>? Func { get; set; }

        public Task<SllGenerationResult> GenerateAsync(
            Guid itemId, ConceptRefinementTriangle triangle, string originalIdea,
            CancellationToken cancellationToken = default) =>
            Func?.Invoke(cancellationToken) ?? Task.FromResult(Result);
    }

    private sealed class StubSllAccess(bool available) : ISllAccessStatus
    {
        private readonly bool _available = available;

        public event EventHandler? AvailabilityChanged;

        public SllAccessAvailability GetAvailability() =>
            _available
                ? SllAccessAvailability.Available
                : SllAccessAvailability.Unavailable("API key required.");

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubItemInspectorService : IItemInspectorService
    {
        public ItemInspectorState? StateToReturn { get; set; }
        public bool FailSaves { get; set; }

        public Task<ItemInspectorState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StateToReturn);

        public Task<ItemInspectorSaveResult> SaveAsync(ItemInspectorSaveRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(StateToReturn is { } s
                ? ItemInspectorSaveResult.Success(s)
                : ItemInspectorSaveResult.Failure("No state"));

        public Task<ItemInspectorSaveResult> SaveStageAsync(ItemStageAwareSaveRequest request, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                return Task.FromResult(ItemInspectorSaveResult.Failure("SaveStage failed"));
            }

            var updated = StateToReturn is { } s
                ? s with
                {
                    Sll = request.StagePayload.Sll ?? s.Sll,
                    Creative = s.Creative with
                    {
                        ConceptIdea = request.StagePayload.ConceptIdea ?? s.Creative.ConceptIdea,
                        Phrase = request.StagePayload.Phrase ?? s.Creative.Phrase,
                        GraphicDirection = request.StagePayload.GraphicDirection ?? s.Creative.GraphicDirection,
                    }
                }
                : null;
            return Task.FromResult(updated is not null
                ? ItemInspectorSaveResult.Success(updated)
                : ItemInspectorSaveResult.Failure("No state"));
        }
    }

    private sealed class StubItemManagementService : IItemManagementService
    {
        public Guid? ActiveWorkspaceId => null;
        public Guid? ActiveStoreId => null;
        public Guid? ActiveItemId => null;

        public void SetActiveWorkspace(Guid? workspaceId) { }

        public Task<ItemManagementState> LoadAsync(Guid? storeId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementState(null, null, null, [], [], [], false));

        public Task<ItemCreationDestinationResult> ResolveCreateTopicAsync(
            Guid storeId, WorkspaceTreeSelection? selection, CancellationToken ct = default) =>
            Task.FromResult(new ItemCreationDestinationResult(null, null));

        public Task<ItemManagementResult> CreateItemAsync(ItemManagementCreateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> UpdateItemAsync(ItemManagementUpdateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> MoveItemAsync(ItemManagementMoveRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> DuplicateItemAsync(ItemManagementDuplicateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> ArchiveItemAsync(Guid itemId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> RestoreItemAsync(ItemManagementRestoreRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> DeleteItemAsync(ItemManagementDeleteRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> SetItemStatusAsync(ItemManagementSetStatusRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> MoveItemStageAsync(ItemManagementMoveStageRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> SelectItemAsync(Guid itemId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));
    }
}
