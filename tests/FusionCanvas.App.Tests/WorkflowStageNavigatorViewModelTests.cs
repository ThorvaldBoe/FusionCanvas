using FusionCanvas.App.Workflow;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.App.Tests;

public class WorkflowStageNavigatorViewModelTests
{
    [Fact]
    public void SetActiveItem_UpdatesNavigatorState()
    {
        var viewModel = NewViewModel();
        var context = NewContext(WorkflowStage.Idea, [WorkflowStage.Idea]);

        viewModel.SetActiveItem(context);

        Assert.True(viewModel.HasActiveItem);
        Assert.Equal(WorkflowStage.Idea, viewModel.ActiveViewStage);
        Assert.Equal(["Idea", "Concept", "Design", "Listing"], viewModel.Stages.Select(stage => stage.Label).ToArray());
    }

    [Fact]
    public void SetActiveTab_RebuildsNavigatorForTabItem()
    {
        var viewModel = NewViewModel();
        var firstTab = new DocumentTabWorkflowContext(Guid.NewGuid(), NewContext(WorkflowStage.Idea, [WorkflowStage.Idea]));
        var secondTab = new DocumentTabWorkflowContext(Guid.NewGuid(), NewContext(WorkflowStage.Design, [WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design]));

        viewModel.SetActiveTab(firstTab);
        viewModel.SetActiveTab(secondTab);

        Assert.Equal(secondTab.TabId, viewModel.ActiveTabId);
        Assert.Equal(WorkflowStage.Design, viewModel.ActiveViewStage);
        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Design).IsCurrent);
    }

    [Fact]
    public void ChangeActiveStage_RefreshesCurrentStage()
    {
        var viewModel = NewViewModel();
        viewModel.SetActiveItem(NewContext(WorkflowStage.Idea, [WorkflowStage.Idea]));

        viewModel.ChangeActiveStage(WorkflowStage.Concept);

        Assert.Equal(WorkflowStage.Concept, viewModel.ActiveViewStage);
        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsCurrent);
        Assert.False(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Idea).IsCurrent);
    }

    [Fact]
    public void SelectStageCommand_NavigatesOnlyToAvailableStages()
    {
        var viewModel = NewViewModel();
        viewModel.SetActiveItem(NewContext(WorkflowStage.Concept, [WorkflowStage.Idea]));

        viewModel.SelectStageCommand.Execute(WorkflowStage.Idea);

        Assert.Equal(WorkflowStage.Idea, viewModel.ActiveViewStage);

        viewModel.SelectStageCommand.Execute(WorkflowStage.Design);

        Assert.Equal(WorkflowStage.Idea, viewModel.ActiveViewStage);
    }

    [Fact]
    public void ArchivedItem_ShowsInactiveStateWithoutAddingArchiveStage()
    {
        var viewModel = NewViewModel();

        viewModel.SetActiveItem(NewContext(WorkflowStage.Listing, WorkflowStages.Ordered, isInactive: true, inactiveLabel: "Rejected"));

        Assert.True(viewModel.ShowsInactiveState);
        Assert.Equal("Rejected", viewModel.InactiveLabel);
        Assert.DoesNotContain(viewModel.Stages, stage => stage.Label == "Rejected");
    }

    [Fact]
    public void SelectStage_MarksActiveViewWithoutMovingCurrentStage()
    {
        var viewModel = NewViewModel();
        viewModel.SetActiveItem(NewContext(WorkflowStage.Design, [WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design]));

        viewModel.SelectStageCommand.Execute(WorkflowStage.Idea);

        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Design).IsCurrent);
        Assert.False(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Idea).IsCurrent);
        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Idea).IsActiveView);
        Assert.False(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Design).IsActiveView);
        Assert.Equal(WorkflowStage.Idea, viewModel.ActiveViewStage);
    }

    [Fact]
    public void SetActiveItem_InitializesActiveViewToCurrentStage()
    {
        var viewModel = NewViewModel();
        viewModel.SetActiveItem(NewContext(WorkflowStage.Concept, [WorkflowStage.Idea, WorkflowStage.Concept]));

        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsCurrent);
        Assert.True(viewModel.Stages.Single(stage => stage.Stage == WorkflowStage.Concept).IsActiveView);
        Assert.Equal(WorkflowStage.Concept, viewModel.ActiveViewStage);
    }

    private static WorkflowStageNavigatorViewModel NewViewModel() =>
        new(new WorkflowStageNavigatorService());

    private static ActiveItemWorkflowContext NewContext(
        WorkflowStage currentStage,
        IReadOnlyCollection<WorkflowStage> availableStages,
        bool isInactive = false,
        string? inactiveLabel = null) =>
        new(Guid.NewGuid(), currentStage, availableStages, isInactive, inactiveLabel);
}
