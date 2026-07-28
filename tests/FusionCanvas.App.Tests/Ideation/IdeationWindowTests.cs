using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Ideation;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.App.Tests;

public sealed class IdeationWindowTests
{
    [AvaloniaFact]
    public void WindowConstructsWithScopeInputModeCountAndAccessibleCandidateList()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess());
        viewModel.Open(Scope);
        var window = new IdeationWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();

            Assert.Equal(560, window.MinWidth);
            Assert.Equal(500, window.MinHeight);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == Scope.DisplayPath);
            Assert.Contains(window.GetVisualDescendants().OfType<ComboBox>(), combo => AutomationProperties.GetName(combo) == "Ideation mode");
            Assert.Contains(window.GetVisualDescendants().OfType<TextBox>(), box => AutomationProperties.GetName(box) == "Number of ideas");
            Assert.Contains(window.GetVisualDescendants().OfType<ListBox>(), list => AutomationProperties.GetName(list) == "Ideas candidate list");
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), button => AutomationProperties.GetName(button) == "Generate ideas");
            Assert.Contains(window.GetVisualDescendants().OfType<SpinningWheel>(), wheel => AutomationProperties.GetName(wheel) == "Generating ideas");
        }
        finally
        {
            viewModel.RequestClose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NestedDialogsConstructWithCancelAsTheSafeDestructiveChoice()
    {
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess());
        viewModel.Open(Scope);
        viewModel.Candidates.Add(new IdeaCandidateViewModel("A grumpy pug", IdeationMode.Basic));
        viewModel.RejectCandidateCommand.Execute(viewModel.Candidates[0]);

        var reject = new RejectIdeaWindow { DataContext = viewModel };
        var discard = new IdeationDiscardConfirmationWindow { DataContext = viewModel };
        try
        {
            reject.Show();
            discard.Show();
            reject.UpdateLayout();
            discard.UpdateLayout();

            Assert.Contains(reject.GetVisualDescendants().OfType<TextBox>(), box => AutomationProperties.GetName(box) == "Optional rejection reason");
            Assert.Contains(reject.GetVisualDescendants().OfType<Button>(), button => AutomationProperties.GetName(button) == "Cancel rejection");
            Assert.Contains(discard.GetVisualDescendants().OfType<Button>(), button => AutomationProperties.GetName(button) == "Cancel discard");
        }
        finally
        {
            discard.Close();
            reject.Close();
        }
    }

    [AvaloniaFact]
    public async Task SnowcloneManagementIsProgressivelyDisclosedAndBlocksGenerationWhileOpen()
    {
        var library = new StubLibrary();
        var viewModel = new IdeationViewModel(new NoOpService(), new AvailableAccess(), library);
        viewModel.Open(Scope);
        var window = new IdeationWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();
            var manage = window.GetVisualDescendants().OfType<Button>()
                .Single(button => AutomationProperties.GetName(button) == "Manage Snowclones");
            Assert.False(manage.IsEffectivelyVisible);

            viewModel.SelectedMode = IdeationMode.Snowclones;
            await viewModel.CompleteSnowcloneLibraryAsync();
            window.UpdateLayout();
            Assert.True(manage.IsEffectivelyVisible);
            Assert.True(viewModel.HasSnowclones);
            Assert.True(viewModel.CanGenerate);

            viewModel.OpenSnowcloneLibrary();
            Assert.True(viewModel.IsSnowcloneLibraryOpen);
            Assert.False(viewModel.CanGenerate);
            viewModel.OpenSnowcloneLibrary();
            Assert.True(viewModel.IsSnowcloneLibraryOpen);

            await viewModel.CompleteSnowcloneLibraryAsync();
            Assert.False(viewModel.IsSnowcloneLibraryOpen);
            Assert.True(viewModel.CanGenerate);
        }
        finally
        {
            viewModel.RequestClose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task CountStepperButtonsRenderBesideCountFieldWithLimitAndBusyDisabledStates()
    {
        var pending = new TaskCompletionSource<IdeationGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new PendingService(_ => pending.Task);
        var viewModel = new IdeationViewModel(service, new AvailableAccess());
        viewModel.Open(Scope);
        var window = new IdeationWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var descendants = window.GetVisualDescendants().ToList();
            var countBox = descendants.OfType<TextBox>().Single(box => AutomationProperties.GetName(box) == "Number of ideas");
            var increment = descendants.OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Increment idea count");
            var decrement = descendants.OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Decrement idea count");
            var generate = descendants.OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Generate ideas");

            Assert.Same(countBox, descendants.Single(d => ReferenceEquals(d, countBox)));
            Assert.True(descendants.IndexOf(countBox) < descendants.IndexOf(increment));
            Assert.True(descendants.IndexOf(increment) < descendants.IndexOf(decrement));
            Assert.True(descendants.IndexOf(decrement) < descendants.IndexOf(generate));

            Assert.True(increment.IsEnabled);
            Assert.True(decrement.IsEnabled);

            viewModel.CountText = "20";
            window.UpdateLayout();
            Assert.False(increment.IsEnabled);
            Assert.True(decrement.IsEnabled);

            viewModel.CountText = "1";
            window.UpdateLayout();
            Assert.True(increment.IsEnabled);
            Assert.False(decrement.IsEnabled);

            viewModel.CountText = "5";
            window.UpdateLayout();
            Assert.True(increment.IsEnabled);
            Assert.True(decrement.IsEnabled);

            var generation = viewModel.GenerateAsync();
            window.UpdateLayout();
            Assert.True(viewModel.IsBusy);
            Assert.False(increment.IsEnabled);
            Assert.False(decrement.IsEnabled);

            pending.SetResult(new(true, false, [new(0, "Idea")], 5, 5, 0, null));
            await generation;
            window.UpdateLayout();
            Assert.True(increment.IsEnabled);
            Assert.True(decrement.IsEnabled);
        }
        finally
        {
            viewModel.RequestClose();
            window.Close();
        }
    }

    private static readonly IdeationScope Scope = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        "Store / Dogs",
        new ItemTopicReference(WorkspaceEntityKind.Niche, Guid.NewGuid()));

    private sealed class AvailableAccess : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() => IdeationAccessAvailability.Available;
    }

    private sealed class NoOpService : IIdeationService
    {
        private static readonly WorkspaceSnapshot Empty = new([], [], [], [], [], [], [], [], []);

        public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
            IdeationScopeResult.Available(Scope);

        public Task<IdeationGenerationResult> GenerateAsync(IdeationGenerationRequest request, IProgress<IdeationGenerationProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationGenerationResult(true, false, [], request.Count, request.Count, 0, null));

        public Task<IdeationDecisionResult> CreateAsync(IdeationScope scope, string candidateText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, Empty));

        public Task<IdeationDecisionResult> RejectAsync(IdeationScope scope, string candidateText, string? reason, IdeationMode mode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, Empty));
    }

    private sealed class PendingService : IIdeationService
    {
        private readonly Func<CancellationToken, Task<IdeationGenerationResult>> _generate;

        public PendingService(Func<CancellationToken, Task<IdeationGenerationResult>> generate) => _generate = generate;

        public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
            IdeationScopeResult.Available(Scope);

        public Task<IdeationGenerationResult> GenerateAsync(IdeationGenerationRequest request, IProgress<IdeationGenerationProgress>? progress = null, CancellationToken cancellationToken = default) =>
            _generate(cancellationToken);

        public Task<IdeationDecisionResult> CreateAsync(IdeationScope scope, string candidateText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, new([], [], [], [], [], [], [], [], [])));

        public Task<IdeationDecisionResult> RejectAsync(IdeationScope scope, string candidateText, string? reason, IdeationMode mode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdeationDecisionResult(true, null, new([], [], [], [], [], [], [], [], [])));
    }

    private sealed class StubLibrary : ISnowcloneLibraryService
    {
        private static readonly SnowcloneSummary Summary = new(
            Guid.NewGuid(),
            "Talk to me about {X}",
            "Fill {X}.",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        private static readonly SnowcloneLibraryResult Result = SnowcloneLibraryResult.Success(
            new([Summary], [Summary], true, string.Empty));

        public Task<SnowcloneLibraryResult> LoadAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
        public Task<SnowcloneLibraryResult> InitializeAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
        public Task<SnowcloneLibraryResult> CreateAsync(SnowcloneCreateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> UpdateAsync(SnowcloneUpdateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> DeleteAsync(Guid id, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ImportAsync(Stream stream, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ImportBundledAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ExportAsync(Stream stream, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
