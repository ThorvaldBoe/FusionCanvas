using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.WorkflowNavigation;

public sealed record WorkflowStageNavigatorEntry(
    WorkflowStage Stage,
    string Label,
    bool IsCurrent,
    bool IsAvailable,
    bool CanNavigate,
    bool IsActiveView);
