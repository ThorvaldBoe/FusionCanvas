using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.StageTools;

public sealed record StageToolSelectionKey(WorkflowStage Stage, StageToolContextKind ContextKind);
