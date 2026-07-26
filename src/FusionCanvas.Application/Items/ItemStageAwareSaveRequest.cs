using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Items;

public sealed record ItemStageAwareSaveRequest(
    Guid ItemId,
    WorkflowStage ExpectedCurrentStage,
    string Title,
    string? Notes,
    ItemStageSavePayload StagePayload,
    IReadOnlyList<string> TagNames);
