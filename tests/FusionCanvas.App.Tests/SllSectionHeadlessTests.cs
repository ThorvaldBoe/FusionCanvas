using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public sealed class SllSectionHeadlessTests
{
    [AvaloniaFact]
    public void SectionNotVisible_ForNonConceptStage()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Idea);
        fixture.PumpLayout();

        var generate = fixture.FindControlOrDefault<Button>(b => b.IsVisible
            && AutomationProperties.GetName(b) == "Generate SLL sketch");
        Assert.Null(generate);
    }

    [AvaloniaFact]
    public void SectionVisible_ForConceptStage()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        var header = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text == "Generate SLL sketch" && tb.IsVisible);
        Assert.NotNull(header);

        var generate = fixture.FindControlOrDefault<Button>(b => b.IsVisible
            && AutomationProperties.GetName(b) == "Generate SLL sketch");
        Assert.NotNull(generate);

        var regenerate = fixture.FindControlOrDefault<Button>(b => b.IsVisible
            && AutomationProperties.GetName(b) == "Regenerate SLL sketch");
        Assert.NotNull(regenerate);

        // Keyboard order: Generate before Regenerate (declared after the refinement actions).
        Assert.True(generate.Bounds.Y <= regenerate.Bounds.Y);
    }

    [AvaloniaFact]
    public void BusyIndicator_VisibleWhenBusy()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        fixture.ViewModel.SllGeneration.SetBusyForTest(true);
        fixture.PumpLayout();

        var busy = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.IsVisible && tb.Text == "SLL generation in progress…");
        Assert.NotNull(busy);
    }

    [AvaloniaFact]
    public void ErrorMessage_VisibleWhenSet()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        fixture.ViewModel.SllGeneration.SetErrorForTest("SLL failed for test");
        fixture.PumpLayout();

        var error = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.IsVisible && tb.Text == "SLL failed for test");
        Assert.NotNull(error);
    }

    [AvaloniaFact]
    public void ActionsDisabled_WhenSllAiUnavailable()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // In the test environment SLL AI services are not supplied.
        Assert.False(fixture.ViewModel.SllGeneration.IsAvailable);
        Assert.False(fixture.ViewModel.SllGeneration.CanGenerate);

        var generate = fixture.FindControlOrDefault<Button>(b => b.IsVisible
            && AutomationProperties.GetName(b) == "Generate SLL sketch");
        Assert.NotNull(generate);
        Assert.False(generate.IsEnabled);

        var expectedReason = fixture.ViewModel.SllGeneration.GenerateDisabledReason;
        Assert.NotNull(expectedReason);
        var toolTip = ToolTip.GetTip(generate);
        Assert.NotNull(toolTip);
        Assert.Equal(expectedReason, toolTip);
    }

    [AvaloniaFact]
    public void StaleMarker_HiddenWhenNoCurrentSll()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.False(fixture.ViewModel.SllGeneration.HasCurrentSll);
        Assert.False(fixture.ViewModel.SllGeneration.IsStale);

        var staleBlock = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text?.Contains("stale", System.StringComparison.OrdinalIgnoreCase) == true && tb.IsVisible);
        Assert.Null(staleBlock);
    }
}
