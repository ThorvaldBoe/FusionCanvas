using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

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

public sealed record WorkflowStageNavigatorEntry(
    WorkflowStage Stage,
    string Label,
    bool IsCurrent,
    bool IsAvailable,
    bool CanNavigate,
    bool IsActiveView);

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

public interface IWorkflowStageNavigatorService
{
    WorkflowStageNavigatorState Build(ActiveItemWorkflowContext? context, WorkflowStage? activeViewStage = null);

    WorkflowStageNavigatorState Navigate(
        ActiveItemWorkflowContext? context,
        WorkflowStage requestedStage,
        WorkflowStage? activeViewStage);
}

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
