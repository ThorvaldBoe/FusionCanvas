using FusionCanvas.Application.Groups;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public static class WorkspaceTreeMoveValidator
{
    public static WorkspaceTreeMoveValidation Validate(
        WorkspaceSnapshot snapshot,
        IReadOnlyList<WorkspaceTreeSelection> sources,
        WorkspaceEntityKind targetKind,
        Guid targetId,
        GroupPlacement placement,
        bool isFiltering)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(placement);

        var effectiveSources = WorkspaceTreeSelectionRules.Normalize(snapshot, sources);
        if (effectiveSources.Count == 0)
        {
            return WorkspaceTreeMoveValidation.Invalid("Select an active Item or group before dragging.");
        }

        if (effectiveSources.Any(source => source.Id == targetId))
        {
            return InvalidHierarchy();
        }

        if (effectiveSources.Any(source =>
                source.Kind == WorkspaceEntityKind.Group &&
                targetKind == WorkspaceEntityKind.Group &&
                GroupHierarchy.IsDescendant(snapshot, targetId, source.Id)))
        {
            return InvalidHierarchy();
        }

        if (effectiveSources.Any(source => source.Kind == WorkspaceEntityKind.Item) &&
            placement.Kind != GroupPlacementKind.Append)
        {
            return WorkspaceTreeMoveValidation.Invalid(
                "Items can only be moved inside a niche or group.", effectiveSources);
        }

        foreach (var source in effectiveSources)
        {
            var validation = ValidateSource(
                snapshot, source, targetKind, targetId, placement, isFiltering, effectiveSources);
            if (!validation.IsValid)
            {
                return validation;
            }
        }

        return WorkspaceTreeMoveValidation.Valid(effectiveSources);
    }

    public static WorkspaceTreeMoveValidation ValidateSingle(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind sourceKind,
        Guid sourceId,
        WorkspaceEntityKind targetKind,
        Guid targetId,
        GroupPlacement placement,
        bool isFiltering)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(placement);

        var source = new WorkspaceTreeSelection(sourceKind, sourceId);
        return ValidateSource(snapshot, source, targetKind, targetId, placement, isFiltering, [source]);
    }

    private static WorkspaceTreeMoveValidation ValidateSource(
        WorkspaceSnapshot snapshot,
        WorkspaceTreeSelection source,
        WorkspaceEntityKind targetKind,
        Guid targetId,
        GroupPlacement placement,
        bool isFiltering,
        IReadOnlyList<WorkspaceTreeSelection> effectiveSources)
    {
        if (source.Kind == WorkspaceEntityKind.Item)
        {
            var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == source.Id);
            if (item is null || !ItemHierarchy.IsEffectivelyActive(snapshot, item))
            {
                return WorkspaceTreeMoveValidation.Invalid("Only an active item can be moved.", effectiveSources);
            }

            if (targetKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group))
            {
                return WorkspaceTreeMoveValidation.Invalid(
                    "Drop the item onto an active niche or group.", effectiveSources);
            }

            var itemTargetStoreId = ResolveTargetStoreId(snapshot, targetKind, targetId);
            return itemTargetStoreId == item.StoreId
                ? WorkspaceTreeMoveValidation.Valid(effectiveSources)
                : WorkspaceTreeMoveValidation.Invalid(
                    "The destination must be active and belong to the same store.", effectiveSources);
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == source.Id);
        if (group is null || !GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            return WorkspaceTreeMoveValidation.Invalid("Only an active group can be moved.", effectiveSources);
        }

        if (targetKind is not (WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group))
        {
            return WorkspaceTreeMoveValidation.Invalid(
                "Drop the group onto an active niche or group.", effectiveSources);
        }

        if (isFiltering && placement.Kind != GroupPlacementKind.Append)
        {
            return WorkspaceTreeMoveValidation.Invalid(
                "Clear filtering before positioning a group between siblings.", effectiveSources);
        }

        if (targetKind == WorkspaceEntityKind.Group &&
            (targetId == group.Id || GroupHierarchy.IsDescendant(snapshot, targetId, group.Id)))
        {
            return WorkspaceTreeMoveValidation.Invalid(
                "A group cannot be moved beneath itself or one of its descendants.", effectiveSources);
        }

        var groupTargetStoreId = ResolveTargetStoreId(snapshot, targetKind, targetId);
        return groupTargetStoreId == group.StoreId
            ? WorkspaceTreeMoveValidation.Valid(effectiveSources)
            : WorkspaceTreeMoveValidation.Invalid(
                "The destination must be active and belong to the same store.", effectiveSources);
    }

    private static WorkspaceTreeMoveValidation InvalidHierarchy() =>
        WorkspaceTreeMoveValidation.Invalid("The destination must be outside the selected hierarchy.");

    private static Guid? ResolveTargetStoreId(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind targetKind,
        Guid targetId) => targetKind switch
        {
            WorkspaceEntityKind.Niche => snapshot.Niches
                .SingleOrDefault(niche => niche.Id == targetId && !niche.IsArchived)?.StoreId,
            WorkspaceEntityKind.Group => snapshot.Groups
                .SingleOrDefault(group => group.Id == targetId && GroupHierarchy.IsEffectivelyActive(snapshot, group))?.StoreId,
            _ => null
        };
}
