using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.WorkflowNavigation;

public interface IWorkflowStageNavigatorService
{
    WorkflowStageNavigatorState Build(ActiveItemWorkflowContext? context, WorkflowStage? activeViewStage = null);

    WorkflowStageNavigatorState Navigate(
        ActiveItemWorkflowContext? context,
        WorkflowStage requestedStage,
        WorkflowStage? activeViewStage);
}
