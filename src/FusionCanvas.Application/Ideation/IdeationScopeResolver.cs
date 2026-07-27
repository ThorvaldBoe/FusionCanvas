using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Ideation;

public sealed class IdeationScopeResolver
{
    public IdeationScopeResult Resolve(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (entityId == Guid.Empty)
        {
            return IdeationScopeResult.Unavailable("An active niche context is required.");
        }

        return entityKind switch
        {
            WorkspaceEntityKind.Niche => ResolveNiche(snapshot, entityId),
            WorkspaceEntityKind.Group => ResolveGroup(snapshot, entityId),
            WorkspaceEntityKind.Item => ResolveItem(snapshot, entityId),
            _ => IdeationScopeResult.Unavailable("An active niche context is required.")
        };
    }

    private static IdeationScopeResult ResolveNiche(WorkspaceSnapshot snapshot, Guid nicheId)
    {
        var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId);
        var store = niche is null ? null : snapshot.Stores.SingleOrDefault(candidate => candidate.Id == niche.StoreId);
        if (niche is null || niche.IsArchived || store is null || store.IsArchived)
        {
            return IdeationScopeResult.Unavailable("An active niche context is required.");
        }

        return IdeationScopeResult.Available(new IdeationScope(
            store.Id,
            niche.Id,
            null,
            $"{store.Name} / {niche.Name}",
            new ItemTopicReference(WorkspaceEntityKind.Niche, niche.Id)));
    }

    private static IdeationScopeResult ResolveGroup(WorkspaceSnapshot snapshot, Guid groupId)
    {
        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId);
        if (group is null)
        {
            return IdeationScopeResult.Unavailable("The selected group is no longer available.");
        }

        try
        {
            if (!GroupHierarchy.IsEffectivelyActive(snapshot, group))
            {
                return IdeationScopeResult.Unavailable("An active niche context is required.");
            }

            var niche = GroupHierarchy.GetEffectiveNiche(snapshot, group);
            var store = snapshot.Stores.Single(candidate => candidate.Id == group.StoreId);
            var path = GroupHierarchy.GetAncestors(snapshot, group).Select(candidate => candidate.Name).Append(group.Name);
            return IdeationScopeResult.Available(new IdeationScope(
                store.Id,
                niche.Id,
                group.Id,
                $"{store.Name} / {niche.Name} / {string.Join(" / ", path)}",
                new ItemTopicReference(WorkspaceEntityKind.Group, group.Id)));
        }
        catch (InvalidOperationException)
        {
            return IdeationScopeResult.Unavailable("The selected group has an invalid niche context.");
        }
    }

    private static IdeationScopeResult ResolveItem(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return IdeationScopeResult.Unavailable("The selected Item is no longer available.");
        }

        if (item.GroupId is Guid groupId)
        {
            return ResolveGroup(snapshot, groupId);
        }

        return item.NicheId is Guid nicheId
            ? ResolveNiche(snapshot, nicheId)
            : IdeationScopeResult.Unavailable("The selected Item has no active niche context.");
    }
}
