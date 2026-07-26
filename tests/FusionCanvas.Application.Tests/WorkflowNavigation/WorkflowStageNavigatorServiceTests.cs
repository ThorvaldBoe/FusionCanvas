using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.Application.Tests;

public class WorkflowStageNavigatorServiceTests
{
    [Fact]
    public void Build_WithoutActiveItem_DoesNotExposeFakeWorkflowProgress()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();

        var state = service.Build(null);

        Assert.False(state.HasActiveItem);
        Assert.Empty(state.Stages);
        Assert.Null(state.CurrentStage);
    }

    [Fact]
    public void Build_ProducesOrderedStagesAndMarksCurrentStage()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();
        var context = NewContext(WorkflowStage.Concept, [WorkflowStage.Idea]);

        var state = service.Build(context);

        Assert.Equal(
            [WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design, WorkflowStage.Listing],
            state.Stages.Select(stage => stage.Stage).ToArray());
        Assert.Equal(WorkflowStage.Concept, state.CurrentStage);
        Assert.True(state.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsCurrent);
    }

    [Fact]
    public void Build_MarksAvailablePreviousAndUnavailableFutureStages()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();
        var context = NewContext(WorkflowStage.Concept, [WorkflowStage.Idea]);

        var state = service.Build(context);

        Assert.True(state.Stages.Single(stage => stage.Stage == WorkflowStage.Idea).IsAvailable);
        Assert.True(state.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsAvailable);
        Assert.False(state.Stages.Single(stage => stage.Stage == WorkflowStage.Design).IsAvailable);
        Assert.False(state.Stages.Single(stage => stage.Stage == WorkflowStage.Listing).IsAvailable);
    }

    [Fact]
    public void Navigate_ToAvailableStage_UpdatesActiveViewStage()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();
        var context = NewContext(WorkflowStage.Concept, [WorkflowStage.Idea]);

        var state = service.Navigate(context, WorkflowStage.Idea, WorkflowStage.Concept);

        Assert.Equal(WorkflowStage.Idea, state.ActiveViewStage);
        Assert.Equal(WorkflowStage.Concept, state.CurrentStage);
    }

    [Fact]
    public void Navigate_ToUnavailableStage_KeepsCurrentActiveViewStage()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();
        var context = NewContext(WorkflowStage.Concept, [WorkflowStage.Idea]);

        var state = service.Navigate(context, WorkflowStage.Design, WorkflowStage.Concept);

        Assert.Equal(WorkflowStage.Concept, state.ActiveViewStage);
    }

    [Fact]
    public void Build_ForInactiveItem_ExposesArchiveStateSeparately()
    {
        IWorkflowStageNavigatorService service = new WorkflowStageNavigatorService();
        var context = NewContext(WorkflowStage.Listing, WorkflowStages.Ordered, isInactive: true, inactiveLabel: "Archived");

        var state = service.Build(context);

        Assert.True(state.IsInactive);
        Assert.Equal("Archived", state.InactiveLabel);
        Assert.Equal(
            [WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design, WorkflowStage.Listing],
            state.Stages.Select(stage => stage.Stage).ToArray());
        Assert.DoesNotContain(state.Stages, stage => stage.Label == "Archived");
    }

    private static ActiveItemWorkflowContext NewContext(
        WorkflowStage currentStage,
        IReadOnlyCollection<WorkflowStage> availableStages,
        bool isInactive = false,
        string? inactiveLabel = null) =>
        new(Guid.NewGuid(), currentStage, availableStages, isInactive, inactiveLabel);
}
