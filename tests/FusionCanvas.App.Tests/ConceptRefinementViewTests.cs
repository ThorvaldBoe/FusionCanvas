using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public sealed class ConceptRefinementViewTests
{
    [AvaloniaFact]
    public void SectionNotVisible_ForNonConceptStage()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Idea);
        fixture.PumpLayout();

        var conceptBorders = fixture.Window.GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Child is StackPanel panel
                && panel.Children.OfType<TextBlock>().Any(tb => tb.Text == "Refine with AI"))
            .ToList();

        Assert.All(conceptBorders, b => Assert.False(b.IsVisible));
    }

    [AvaloniaFact]
    public void SectionVisible_ForConceptStage()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        var header = fixture.FindControlOrDefault<TextBlock>(tb =>
            tb.Text == "Refine with AI" && tb.IsVisible);
        Assert.NotNull(header);

        var initializeButton = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Initialize from base idea" && b.IsVisible);
        Assert.NotNull(initializeButton);
    }

    [AvaloniaFact]
    public void PerCornerActions_HaveDisambiguatedAccessibleNames()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Concept idea"));
        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Concept idea"));
        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Phrase"));
        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Phrase"));
        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Graphic direction"));
        Assert.NotNull(fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Graphic direction"));
    }

    [AvaloniaFact]
    public void UnavailableGuidance_ShowsWhenAIDisabled()
    {
        // In the test environment, MainWindowViewModelFactory uses
        // DisabledConceptRefinementAccessStatus, so AI is unavailable.
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.False(fixture.ViewModel.ConceptRefinement.IsAvailable);
        Assert.NotNull(fixture.ViewModel.ConceptRefinement.UnavailableReason);
    }

    [AvaloniaFact]
    public void ScoreText_VisibleForConceptStage()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // The score TextBlock uses <Run> children, so tb.Text is empty.
        // Find by matching the first Inline Run text.
        var scoreBlock = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.IsVisible
                && tb.Inlines is { Count: > 0 }
                && tb.Inlines[0] is Avalonia.Controls.Documents.Run run
                && run.Text == "Triangle completeness: ");
        Assert.NotNull(scoreBlock);
    }

    [AvaloniaFact]
    public void HistoryList_HiddenWhenEmpty()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.Empty(fixture.ViewModel.ConceptRefinement.History);
        Assert.False(fixture.ViewModel.ConceptRefinement.HasHistory);

        var listBox = fixture.FindControlOrDefault<ListBox>(lb =>
            lb.ItemsSource == fixture.ViewModel.ConceptRefinement.History);
        Assert.NotNull(listBox);
        Assert.False(listBox.IsVisible);
    }

    [AvaloniaFact]
    public void ReadOnlyReview_DisablesAllActions()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // The first item has Stage=Idea, so when viewing at Concept stage,
        // CanEditStage returns false.
        Assert.False(fixture.ViewModel.ItemInspector.CanEditStage);

        Assert.False(fixture.ViewModel.ConceptRefinement.CanInitialize);
        Assert.False(fixture.ViewModel.ConceptRefinement.CanFineTuneConceptIdea);
        Assert.False(fixture.ViewModel.ConceptRefinement.CanChangeConceptIdea);
    }

    [AvaloniaFact]
    public void ScoreUpdates_WhenInspectorDraftsChange()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        fixture.ViewModel.ItemInspector.ConceptIdea = "Substantive concept idea";
        fixture.ViewModel.ItemInspector.Phrase = "Great phrase text";
        fixture.ViewModel.ItemInspector.GraphicDirection = "Bold visual direction";

        Assert.Equal(100, fixture.ViewModel.ConceptRefinement.Score);
    }

    [AvaloniaFact]
    public void ErrorMessage_ShowsWhenSet()
    {
        // VR-001: inline error renders when ErrorMessage is set
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.False(fixture.ViewModel.ConceptRefinement.HasError);
        Assert.Null(fixture.ViewModel.ConceptRefinement.ErrorMessage);

        // Set an error on the session VM
        fixture.ViewModel.ConceptRefinement.SetErrorForTest("Test error message");
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ConceptRefinement.HasError);
        Assert.Equal("Test error message", fixture.ViewModel.ConceptRefinement.ErrorMessage);
    }
}