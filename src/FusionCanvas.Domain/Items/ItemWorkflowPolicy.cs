using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Domain.Items;

public static class ItemWorkflowPolicy
{
    public static IReadOnlyList<WorkflowStage> OrderedStages { get; } = WorkflowStages.Ordered;

    public static IReadOnlyList<WorkflowStage> ViewableStages { get; } = WorkflowStages.Ordered;

    public static bool CanMoveAdjacent(WorkflowStage current, WorkflowStage destination)
    {
        var currentIndex = IndexOf(current);
        var destinationIndex = IndexOf(destination);
        return Math.Abs(destinationIndex - currentIndex) == 1;
    }

    public static ItemEditDecision CanEditStage(Item item, WorkflowStage activeViewStage)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsArchived)
        {
            return new ItemEditDecision(false, "Restore the item before editing its stage content.");
        }

        if (item.Status is ItemStatus.Published)
        {
            return new ItemEditDecision(false, "Pause the published item before editing protected content.");
        }

        if (item.Status is ItemStatus.Rejected)
        {
            return new ItemEditDecision(false, "Recover the rejected item to Draft before editing its content.");
        }

        if (activeViewStage != item.Stage)
        {
            return new ItemEditDecision(false, "Move the active stage to the current stage before editing reviewed content.");
        }

        return new ItemEditDecision(true, "Stage content is editable.");
    }

    public static ItemEditDecision CanPerformOperation(Item item, ItemOperationKind operation)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsArchived)
        {
            return operation switch
            {
                ItemOperationKind.Archive => new ItemEditDecision(true, "The archived item can be restored."),
                ItemOperationKind.StatusRecovery => new ItemEditDecision(true, "The archived item keeps its allowed status recovery path."),
                _ => new ItemEditDecision(false, "Restore the archived item before changing its content or relationships.")
            };
        }

        if (item.Status is ItemStatus.Published or ItemStatus.Rejected)
        {
            return operation switch
            {
                ItemOperationKind.StageContent => new ItemEditDecision(false, item.Status is ItemStatus.Published
                    ? "Pause the published item before editing protected stage content."
                    : "Recover the rejected item to Draft before editing its content."),
                ItemOperationKind.DesignFile => new ItemEditDecision(false, item.Status is ItemStatus.Published
                    ? "Pause the published item before changing Design files."
                    : "Recover the rejected item to Draft before changing Design files."),
                ItemOperationKind.RelatedAssetLink => new ItemEditDecision(false, item.Status is ItemStatus.Published
                    ? "Pause the published item before changing related asset links."
                    : "Recover the rejected item to Draft before changing related asset links."),
                ItemOperationKind.StageMovement => new ItemEditDecision(false, item.Status is ItemStatus.Published
                    ? "Pause the published item before moving stages."
                    : "Recover the rejected item to Draft before moving stages."),
                ItemOperationKind.WorkingTitle => new ItemEditDecision(true, "Working title remains available for this item."),
                ItemOperationKind.Notes => new ItemEditDecision(true, "Notes remain available for this item."),
                ItemOperationKind.TagLink => new ItemEditDecision(true, "Tags remain available for this item."),
                ItemOperationKind.TopicPlacement => new ItemEditDecision(true, "Topic placement remains available for this item."),
                ItemOperationKind.Archive => new ItemEditDecision(true, "The item can be archived."),
                ItemOperationKind.StatusRecovery => new ItemEditDecision(true, "The allowed status recovery path remains available."),
                _ => new ItemEditDecision(false, "Unsupported operation for this item state.")
            };
        }

        return operation switch
        {
            ItemOperationKind.StageContent => new ItemEditDecision(true, "Stage content is editable."),
            ItemOperationKind.WorkingTitle => new ItemEditDecision(true, "Working title is editable."),
            ItemOperationKind.Notes => new ItemEditDecision(true, "Notes are editable."),
            ItemOperationKind.TagLink => new ItemEditDecision(true, "Tag links are editable."),
            ItemOperationKind.TopicPlacement => new ItemEditDecision(true, "Topic placement is editable."),
            ItemOperationKind.Archive => new ItemEditDecision(true, "The item can be archived."),
            ItemOperationKind.StatusRecovery => new ItemEditDecision(true, "Status recovery is available."),
            ItemOperationKind.DesignFile => new ItemEditDecision(true, "Design files are editable."),
            ItemOperationKind.RelatedAssetLink => new ItemEditDecision(true, "Related asset links are editable."),
            ItemOperationKind.StageMovement => new ItemEditDecision(true, "Adjacent stage movement is available."),
            _ => new ItemEditDecision(false, "Unsupported operation for this item state.")
        };
    }

    public static ItemStatusTransitionDecision DecideTransition(ItemStatus current, WorkflowStage currentStage, ItemStatus target)
    {
        var entersPublished = target is ItemStatus.Published;
        var entersRejected = target is ItemStatus.Rejected;
        var leavesPublished = current is ItemStatus.Published;

        return (current, target) switch
        {
            (ItemStatus.Draft, ItemStatus.Published) when currentStage is WorkflowStage.Listing =>
                new ItemStatusTransitionDecision(true, true, "Publish the item at the Listing stage."),
            (ItemStatus.Draft, ItemStatus.Published) =>
                new ItemStatusTransitionDecision(false, false, "Move the item to the Listing stage before publishing."),
            (ItemStatus.Draft, ItemStatus.Rejected) =>
                new ItemStatusTransitionDecision(true, true, "Reject the draft item."),
            (ItemStatus.Published, ItemStatus.Paused) =>
                new ItemStatusTransitionDecision(true, true, "Pause the published item to edit protected content."),
            (ItemStatus.Published, ItemStatus.Rejected) =>
                new ItemStatusTransitionDecision(true, true, "Reject the published item."),
            (ItemStatus.Paused, ItemStatus.Published) when currentStage is WorkflowStage.Listing =>
                new ItemStatusTransitionDecision(true, true, "Resume publishing the item at the Listing stage."),
            (ItemStatus.Paused, ItemStatus.Published) =>
                new ItemStatusTransitionDecision(false, false, "Move the item to the Listing stage before resuming publication."),
            (ItemStatus.Paused, ItemStatus.Draft) =>
                new ItemStatusTransitionDecision(true, false, "Return the paused item to Draft."),
            (ItemStatus.Paused, ItemStatus.Rejected) =>
                new ItemStatusTransitionDecision(true, true, "Reject the paused item."),
            (ItemStatus.Rejected, ItemStatus.Draft) =>
                new ItemStatusTransitionDecision(true, false, "Recover the rejected item to Draft."),
            _ => new ItemStatusTransitionDecision(false, false, "This status transition is not allowed.")
        };
    }

    private static int IndexOf(WorkflowStage stage) =>
        stage switch
        {
            WorkflowStage.Idea => 0,
            WorkflowStage.Concept => 1,
            WorkflowStage.Design => 2,
            WorkflowStage.Listing => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage.")
        };
}
