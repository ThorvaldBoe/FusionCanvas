using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.WorkflowNavigation;

public sealed class WorkflowStageNavigatorService : IWorkflowStageNavigatorService
{
    public WorkflowStageNavigatorState Build(ActiveItemWorkflowContext? context, WorkflowStage? activeViewStage = null)
    {
        if (context is null)
        {
            return WorkflowStageNavigatorState.Empty;
        }

        var availableStages = context.AvailableStages.ToHashSet();
        availableStages.Add(context.CurrentStage);

        var resolvedActiveViewStage = activeViewStage is WorkflowStage candidate && availableStages.Contains(candidate)
            ? candidate
            : context.CurrentStage;

        var stages = WorkflowStages.Ordered
            .Select(stage =>
            {
                var isAvailable = availableStages.Contains(stage);

                return new WorkflowStageNavigatorEntry(
                    stage,
                    WorkflowStages.GetDisplayName(stage),
                    stage == context.CurrentStage,
                    isAvailable,
                    isAvailable,
                    stage == resolvedActiveViewStage);
            })
            .ToArray();

        return new WorkflowStageNavigatorState(
            true,
            context.ItemId,
            context.CurrentStage,
            resolvedActiveViewStage,
            stages,
            context.IsInactive,
            ResolveInactiveLabel(context));
    }

    public WorkflowStageNavigatorState Navigate(
        ActiveItemWorkflowContext? context,
        WorkflowStage requestedStage,
        WorkflowStage? activeViewStage)
    {
        var currentState = Build(context, activeViewStage);
        var requestedEntry = currentState.Stages.SingleOrDefault(stage => stage.Stage == requestedStage);

        return requestedEntry?.CanNavigate == true
            ? Build(context, requestedStage)
            : currentState;
    }

    private static string? ResolveInactiveLabel(ActiveItemWorkflowContext context)
    {
        if (!context.IsInactive)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(context.InactiveLabel)
            ? "Archived"
            : context.InactiveLabel;
    }
}
