using FusionCanvas.App.Ideation;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public sealed class IdeationViewModelTests
{
    private static readonly IdeationScope Scope = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Store / Dogs / Pugs",
        new ItemTopicReference(WorkspaceEntityKind.Group, Guid.NewGuid()));

    [Fact]
    public void Open_UsesSafeDefaultsAndValidatesBoundedCount()
    {
        var viewModel = new IdeationViewModel(new StubService(), new StubAccess(true));

        viewModel.Open(Scope);

        Assert.True(viewModel.IsOpen);
        Assert.Equal(IdeationMode.Basic, viewModel.SelectedMode);
        Assert.Equal(string.Empty, viewModel.Guidance);
        Assert.Equal("5", viewModel.CountText);
        Assert.Equal(Scope.DisplayPath, viewModel.ScopeLabel);
        Assert.True(viewModel.CanGenerate);

        viewModel.CountText = "21";

        Assert.NotNull(viewModel.CountError);
        Assert.False(viewModel.CanGenerate);
        Assert.False(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public async Task Generate_AppendsOrderedCandidatesAndSuppressesNormalizedDuplicates()
    {
        var service = new StubService
        {
            GenerationResult = new(
                true,
                false,
                [new(1, "Second idea"), new(0, "  First   idea  "), new(2, "first idea")],
                3,
                3,
                0,
                null)
        };
        var viewModel = new IdeationViewModel(service, new StubAccess(true));
        viewModel.Open(Scope);
        viewModel.CountText = "3";

        await viewModel.GenerateAsync();

        Assert.Equal(["First idea", "Second idea"], viewModel.Candidates.Select(candidate => candidate.Text));
        Assert.False(viewModel.IsBusy);
        Assert.Equal(3, viewModel.Completed);
    }

    [Fact]
    public async Task CreateFailureKeepsRowAndRejectSuccessRemovesIt()
    {
        var service = new StubService
        {
            GenerationResult = new(true, false, [new(0, "Pug phrase")], 1, 1, 0, null),
            CreateResult = new(false, "Save failed", EmptySnapshot),
            RejectResult = new(true, null, EmptySnapshot)
        };
        var viewModel = new IdeationViewModel(service, new StubAccess(true));
        viewModel.Open(Scope);
        viewModel.CountText = "1";
        await viewModel.GenerateAsync();
        var candidate = Assert.Single(viewModel.Candidates);

        await viewModel.CreateCandidateAsync(candidate);

        Assert.Same(candidate, Assert.Single(viewModel.Candidates));
        Assert.Equal("Save failed", candidate.Error);

        viewModel.RejectCandidateCommand.Execute(candidate);
        viewModel.RejectionReason = "Too generic";
        await viewModel.ConfirmRejectAsync();

        Assert.Empty(viewModel.Candidates);
        Assert.False(viewModel.IsRejectionVisible);
        Assert.Equal("Too generic", service.LastRejectionReason);
    }

    [Fact]
    public async Task DeclinedClosePreservesStateAndConfirmedCloseIgnoresLateGeneration()
    {
        var pending = new TaskCompletionSource<IdeationGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { Generate = _ => pending.Task };
        var viewModel = new IdeationViewModel(service, new StubAccess(true));
        viewModel.Open(Scope);

        var generation = viewModel.GenerateAsync();
        viewModel.RequestClose();

        Assert.True(viewModel.IsDiscardConfirmationVisible);
        viewModel.CancelDiscard();
        Assert.True(viewModel.IsOpen);
        Assert.True(viewModel.IsBusy);

        viewModel.RequestClose();
        viewModel.ConfirmDiscard();
        pending.SetResult(new(true, false, [new(0, "Late idea")], 5, 5, 0, null));
        await generation;

        Assert.False(viewModel.IsOpen);
        Assert.Empty(viewModel.Candidates);
    }

    [Fact]
    public async Task ClearAllRemovesPriorCandidatesWithoutCancellingActiveBatch()
    {
        var pending = new TaskCompletionSource<IdeationGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { Generate = _ => pending.Task };
        var viewModel = new IdeationViewModel(service, new StubAccess(true));
        viewModel.Open(Scope);
        viewModel.Candidates.Add(new IdeaCandidateViewModel("Prior idea", IdeationMode.Basic));

        var generation = viewModel.GenerateAsync();
        viewModel.RequestClear();
        viewModel.ConfirmDiscard();

        Assert.Empty(viewModel.Candidates);
        Assert.True(viewModel.IsBusy);

        pending.SetResult(new(true, false, [new(0, "New idea")], 5, 5, 0, null));
        await generation;

        Assert.Equal("New idea", Assert.Single(viewModel.Candidates).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MainActionIsIdeaStageOnlyAndAccessControlsEnabledState(bool accessAvailable)
    {
        var snapshot = SampleWorkspace.Create();
        var repository = new InMemoryWorkspaceRepository(snapshot);
        var access = new StubAccess(accessAvailable);
        var service = new StubService();
        var viewModel = new MainWindowViewModel(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            repository,
            snapshot,
            ideationService: service,
            ideationAccessStatus: access);
        var ideaContext = viewModel.NavigationContexts.Single(context => context.Context.Id == SampleWorkspace.IdeaNodeId);

        viewModel.OpenFromNavigation(ideaContext);

        Assert.True(viewModel.IsIdeationActionVisible);
        Assert.Equal(accessAvailable, viewModel.CanOpenIdeation);
        var activeContext = viewModel.DocumentWindow.ActiveContext;
        if (accessAvailable)
        {
            viewModel.OpenIdeationCommand.Execute(null);
            Assert.True(viewModel.Ideation.IsOpen);
            Assert.Same(activeContext, viewModel.DocumentWindow.ActiveContext);
            viewModel.Ideation.RequestClose();
        }
        else
        {
            Assert.NotNull(viewModel.IdeationUnavailableMessage);
        }

        viewModel.SelectWorkflowStage(FusionCanvas.Domain.Workflow.WorkflowStage.Concept);
        Assert.False(viewModel.IsIdeationActionVisible);
    }

    private static WorkspaceSnapshot EmptySnapshot { get; } = new([], [], [], [], [], [], [], [], []);

    private sealed class StubAccess(bool available) : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() =>
            available ? IdeationAccessAvailability.Available : IdeationAccessAvailability.Unavailable("API key required.");
    }

    private sealed class StubService : IIdeationService
    {
        public IdeationGenerationResult GenerationResult { get; set; } =
            new(true, false, [], 0, 0, 0, null);

        public IdeationDecisionResult CreateResult { get; set; } = new(true, null, EmptySnapshot);

        public IdeationDecisionResult RejectResult { get; set; } = new(true, null, EmptySnapshot);

        public Func<CancellationToken, Task<IdeationGenerationResult>>? Generate { get; set; }

        public string? LastRejectionReason { get; private set; }

        public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
            IdeationScopeResult.Available(Scope);

        public Task<IdeationGenerationResult> GenerateAsync(
            IdeationGenerationRequest request,
            IProgress<IdeationGenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new IdeationGenerationProgress(request.Count, request.Count));
            return Generate?.Invoke(cancellationToken) ?? Task.FromResult(GenerationResult);
        }

        public Task<IdeationDecisionResult> CreateAsync(
            IdeationScope scope,
            string candidateText,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);

        public Task<IdeationDecisionResult> RejectAsync(
            IdeationScope scope,
            string candidateText,
            string? reason,
            IdeationMode mode,
            CancellationToken cancellationToken = default)
        {
            LastRejectionReason = reason;
            return Task.FromResult(RejectResult);
        }
    }
}
