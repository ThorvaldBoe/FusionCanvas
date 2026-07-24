using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.ToolContexts;

namespace FusionCanvas.Application.StageTools;

public sealed record StageToolHostState(
    WorkflowStage WorkflowStage,
    StageToolContextKind ContextKind,
    IReadOnlyList<StageToolAvailability> ToolStates,
    IReadOnlyList<StageToolAvailability> AvailableTools,
    StageToolAvailability? SelectedTool,
    ToolContextResolution? ActiveToolContext)
{
    public bool HasMultipleAvailableTools => AvailableTools.Count > 1;
}
