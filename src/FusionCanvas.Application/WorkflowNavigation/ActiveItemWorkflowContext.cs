using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.WorkflowNavigation;

public sealed record ActiveItemWorkflowContext(
    Guid ItemId,
    WorkflowStage CurrentStage,
    IReadOnlyCollection<WorkflowStage> AvailableStages,
    bool IsInactive = false,
    string? InactiveLabel = null)
{
    public Guid ItemId { get; } = ItemId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(ItemId))
        : ItemId;
}
