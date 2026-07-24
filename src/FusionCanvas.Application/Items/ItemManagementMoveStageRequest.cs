using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Items;

public sealed record ItemManagementMoveStageRequest(Guid ItemId, WorkflowStage Stage);
