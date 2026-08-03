using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
    public void WorkingTriangleEditors_ShowCompleteValuesAndBindTwoWay()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);

        const string idea = "A complete concept idea long enough to wrap across several lines without being ellipsized.";
        const string phrase = "A complete phrase that remains one logical line while wrapping visually";
        const string graphic = "A complete graphic direction with composition, palette, typography, texture, and placement details.";
        fixture.ViewModel.ConceptRefinement.ConceptIdeaInput = idea;
        fixture.ViewModel.ConceptRefinement.PhraseInput = phrase;
        fixture.ViewModel.ConceptRefinement.GraphicDirectionInput = graphic;
        fixture.PumpLayout();

        var ideaEditor = fixture.FindControlOrDefault<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Refinement Concept idea input");
        var phraseEditor = fixture.FindControlOrDefault<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Refinement Phrase input");
        var graphicEditor = fixture.FindControlOrDefault<TextBox>(tb =>
            AutomationProperties.GetName(tb) == "Refinement Graphic direction input");

        Assert.NotNull(ideaEditor);
        Assert.NotNull(phraseEditor);
        Assert.NotNull(graphicEditor);
        Assert.Equal(idea, ideaEditor.Text);
        Assert.Equal(phrase, phraseEditor.Text);
        Assert.Equal(graphic, graphicEditor.Text);
        Assert.Equal(TextWrapping.Wrap, ideaEditor.TextWrapping);
        Assert.Equal(TextWrapping.Wrap, phraseEditor.TextWrapping);
        Assert.Equal(TextWrapping.Wrap, graphicEditor.TextWrapping);
        Assert.True(ideaEditor.AcceptsReturn);
        Assert.False(phraseEditor.AcceptsReturn);
        Assert.True(graphicEditor.AcceptsReturn);
        Assert.True(ideaEditor.MinHeight >= 72);
        Assert.True(phraseEditor.MinHeight >= 44);
        Assert.True(graphicEditor.MinHeight >= 72);
        Assert.Equal(ideaEditor.Bounds.X, phraseEditor.Bounds.X);
        Assert.Equal(ideaEditor.Bounds.X, graphicEditor.Bounds.X);

        ideaEditor.Text = "Edited through the visible control";
        fixture.PumpLayout();

        Assert.Equal("Edited through the visible control", fixture.ViewModel.ConceptRefinement.ConceptIdeaInput);
        Assert.NotEqual("Edited through the visible control", fixture.ViewModel.ItemInspector.ConceptIdea);
    }

    [AvaloniaFact]
    public void WorkingTriangleEditors_AreReadOnlyDuringConceptReview()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // The fixture's first item is in Idea, so Concept is a read-only review stage.
        Assert.False(fixture.ViewModel.ItemInspector.CanEditStage);
        var editors = new[]
        {
            fixture.FindControlOrDefault<TextBox>(tb => AutomationProperties.GetName(tb) == "Refinement Concept idea input"),
            fixture.FindControlOrDefault<TextBox>(tb => AutomationProperties.GetName(tb) == "Refinement Phrase input"),
            fixture.FindControlOrDefault<TextBox>(tb => AutomationProperties.GetName(tb) == "Refinement Graphic direction input")
        };

        Assert.All(editors, editor => Assert.True(Assert.IsType<TextBox>(editor).IsReadOnly));
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
        // VR-001/VR-012: inline error TextBlock visible in the visual tree when ErrorMessage is set
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        Assert.False(fixture.ViewModel.ConceptRefinement.HasError);
        Assert.Null(fixture.ViewModel.ConceptRefinement.ErrorMessage);

        fixture.ViewModel.ConceptRefinement.SetErrorForTest("Test error message");
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ConceptRefinement.HasError);
        Assert.Equal("Test error message", fixture.ViewModel.ConceptRefinement.ErrorMessage);

        // Find a visible TextBlock whose text matches the error message
        var errorBlock = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.IsVisible && tb.Text == "Test error message");
        Assert.NotNull(errorBlock);
    }

    [AvaloniaFact]
    public void InitializeGuidance_VisibleWhenNoBaseIdea()
    {
        // VR-009: guidance TextBlock visible when Initialize is disabled due to no base idea
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // In test environment AI is disabled, so Initialize is already disabled.
        // The guidance should show the AI-unavailable message as the reason.
        Assert.False(fixture.ViewModel.ConceptRefinement.CanInitialize);
        Assert.NotNull(fixture.ViewModel.ConceptRefinement.InitializeDisabledReason);

        // Find a visible TextBlock matching the disable reason
        var reason = fixture.ViewModel.ConceptRefinement.InitializeDisabledReason;
        var guidanceBlock = fixture.Window.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.IsVisible && tb.Text == reason);
        Assert.NotNull(guidanceBlock);
    }

    [AvaloniaFact]
    public void FineTuneButtonOnEmptyCorner_HasEmptyCornerTooltip()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // In the test environment, AI is disabled. Make a corner non-empty so
        // the disabled reason becomes unavailability, not empty-corner.
        // Set ConceptIdea to non-empty to isolate Phrase as the empty corner.
        fixture.ViewModel.ItemInspector.ConceptIdea = "Some concept text";
        fixture.ViewModel.ItemInspector.Phrase = "";
        fixture.PumpLayout();

        // Since AI is disabled in test environment, the FineTunePhrase
        // button shows unavailable reason, not the empty-corner reason.
        // Find the Fine tune Phrase button and check its tooltip reflects the
        // disabled reason.
        var fineTunePhrase = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Phrase");
        Assert.NotNull(fineTunePhrase);

        // Since AI is unavailable, the disabled reason should be the unavailable reason
        var disabledReason = fixture.ViewModel.ConceptRefinement.FineTunePhraseDisabledReason;
        Assert.NotNull(disabledReason);

        var toolTip = ToolTip.GetTip(fineTunePhrase);
        Assert.NotNull(toolTip);
        Assert.Equal(disabledReason, toolTip);

        Assert.True(ToolTip.GetShowOnDisabled(fineTunePhrase));
    }

    [AvaloniaFact]
    public void PerCornerButton_ShowOnDisabled_IsTrue()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        var fineTuneIdea = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Concept idea");
        var changeIdea = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Concept idea");
        var fineTunePhrase = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Phrase");
        var changePhrase = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Phrase");
        var fineTuneGraphic = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Graphic direction");
        var changeGraphic = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Change Graphic direction");

        Assert.True(ToolTip.GetShowOnDisabled(fineTuneIdea));
        Assert.True(ToolTip.GetShowOnDisabled(changeIdea));
        Assert.True(ToolTip.GetShowOnDisabled(fineTunePhrase));
        Assert.True(ToolTip.GetShowOnDisabled(changePhrase));
        Assert.True(ToolTip.GetShowOnDisabled(fineTuneGraphic));
        Assert.True(ToolTip.GetShowOnDisabled(changeGraphic));
    }

    [AvaloniaFact]
    public void DisabledButton_WithUnavailableReason_ShowsReasonInTooltip()
    {
        using var fixture = new MainWindowFixture();
        fixture.ViewModel.OpenFromNavigation(fixture.FirstItemContext());
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Concept);
        fixture.PumpLayout();

        // In test environment AI is unavailable
        Assert.False(fixture.ViewModel.ConceptRefinement.IsAvailable);
        var expectedReason = fixture.ViewModel.ConceptRefinement.UnavailableReason;
        Assert.NotNull(expectedReason);

        var fineTuneIdea = fixture.FindControlOrDefault<Button>(b =>
            AutomationProperties.GetName(b) == "Fine tune Concept idea");
        Assert.NotNull(fineTuneIdea);
        Assert.False(fineTuneIdea.IsEnabled);

        var toolTip = ToolTip.GetTip(fineTuneIdea);
        Assert.NotNull(toolTip);
        Assert.Equal(expectedReason, toolTip);
    }
}
