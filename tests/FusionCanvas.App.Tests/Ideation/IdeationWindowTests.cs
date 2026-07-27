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
}
