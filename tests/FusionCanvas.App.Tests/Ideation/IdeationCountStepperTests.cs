using FusionCanvas.App.Ideation;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public sealed class IdeationCountStepperTests
{
    private static readonly IdeationScope Scope = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Store / Dogs / Pugs",
        new ItemTopicReference(WorkspaceEntityKind.Group, Guid.NewGuid()));

    [Fact]
    public void IncrementFromInRangeStepsUpByOne()
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = "5";

        viewModel.IncrementCountCommand.Execute(null);

        Assert.Equal("6", viewModel.CountText);
        Assert.Null(viewModel.CountError);
    }

    [Fact]
    public void DecrementFromInRangeStepsDownByOne()
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = "5";

        viewModel.DecrementCountCommand.Execute(null);

        Assert.Equal("4", viewModel.CountText);
        Assert.Null(viewModel.CountError);
    }

    [Fact]
    public void IncrementIsDisabledAtMaximum()
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = "20";

        Assert.False(viewModel.CanIncrementCount);
        Assert.False(viewModel.IncrementCountCommand.CanExecute(null));

        viewModel.IncrementCountCommand.Execute(null);
        Assert.Equal("20", viewModel.CountText);
    }

    [Fact]
    public void DecrementIsDisabledAtMinimum()
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = "1";

        Assert.False(viewModel.CanDecrementCount);
        Assert.False(viewModel.DecrementCountCommand.CanExecute(null));

        viewModel.DecrementCountCommand.Execute(null);
        Assert.Equal("1", viewModel.CountText);
    }

    [Fact]
    public void IncrementRecoversInvalidTextToDefault()
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = "abc";

        Assert.True(viewModel.CanIncrementCount);
        viewModel.IncrementCountCommand.Execute(null);

        Assert.Equal("5", viewModel.CountText);
        Assert.Null(viewModel.CountError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    public void DecrementRecoversInvalidTextToMinimum(string invalid)
    {
        var viewModel = CreateViewModel();
        viewModel.CountText = invalid;

        Assert.True(viewModel.CanDecrementCount);
        viewModel.DecrementCountCommand.Execute(null);

        Assert.Equal("1", viewModel.CountText);
        Assert.Null(viewModel.CountError);
    }

    [Fact]
    public void IncrementClampsOutOfRangeParseableBeforeStepping()
    {
        var viewModel = CreateViewModel();

        viewModel.CountText = "25";
        viewModel.IncrementCountCommand.Execute(null);
        Assert.Equal("20", viewModel.CountText);

        viewModel.CountText = "-3";
        viewModel.IncrementCountCommand.Execute(null);
        Assert.Equal("2", viewModel.CountText);
    }

    [Fact]
    public void DecrementClampsOutOfRangeParseableBeforeStepping()
    {
        var viewModel = CreateViewModel();

        viewModel.CountText = "25";
        viewModel.DecrementCountCommand.Execute(null);
        Assert.Equal("19", viewModel.CountText);

        viewModel.CountText = "-3";
        viewModel.DecrementCountCommand.Execute(null);
        Assert.Equal("1", viewModel.CountText);
    }

    [Fact]
    public async Task BothCommandsAreDisabledWhileBusy()
    {
        var pending = new TaskCompletionSource<IdeationGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new StubService { Generate = _ => pending.Task };
        var viewModel = new IdeationViewModel(service, new AvailableAccess());
        viewModel.Open(Scope);
        viewModel.CountText = "5";

        var generation = viewModel.GenerateAsync();
        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.CanIncrementCount);
            Assert.False(viewModel.CanDecrementCount);
            Assert.False(viewModel.IncrementCountCommand.CanExecute(null));
            Assert.False(viewModel.DecrementCountCommand.CanExecute(null));
        }
        finally
        {
            pending.SetResult(new(true, false, [], 5, 5, 0, null));
            await generation;
        }
    }

    private static IdeationViewModel CreateViewModel() =>
        new(new StubService(), new AvailableAccess());

    private sealed class AvailableAccess : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() => IdeationAccessAvailability.Available;
    }

    private sealed class StubService : IIdeationService
    {
        public Func<CancellationToken, Task<IdeationGenerationResult>>? Generate { get; set; }

        public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
            IdeationScopeResult.Available(Scope);

        public Task<IdeationGenerationResult> GenerateAsync(
            IdeationGenerationRequest request,
            IProgress<IdeationGenerationProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            Generate?.Invoke(cancellationToken) ?? Task.FromResult(new IdeationGenerationResult(true, false, [], request.Count, request.Count, 0, null));

        public Task<IdeationDecisionResult> CreateAsync(IdeationScope scope, string candidateText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, EmptySnapshot));

        public Task<IdeationDecisionResult> RejectAsync(IdeationScope scope, string candidateText, string? reason, IdeationMode mode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, EmptySnapshot));
    }

    private static WorkspaceSnapshot EmptySnapshot { get; } = new([], [], [], [], [], [], [], [], []);
}
