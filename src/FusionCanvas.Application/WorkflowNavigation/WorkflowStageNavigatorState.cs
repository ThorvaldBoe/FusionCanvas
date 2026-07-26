using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.WorkflowNavigation;

public sealed record WorkflowStageNavigatorState(
    bool HasActiveItem,
    Guid? ItemId,
    WorkflowStage? CurrentStage,
    WorkflowStage? ActiveViewStage,
    IReadOnlyList<WorkflowStageNavigatorEntry> Stages,
    bool IsInactive,
    string? InactiveLabel)
{
    public static WorkflowStageNavigatorState Empty { get; } =
        new(false, null, null, null, [], false, null);
}
