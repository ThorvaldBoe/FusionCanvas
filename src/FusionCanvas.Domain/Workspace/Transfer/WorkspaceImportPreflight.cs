namespace FusionCanvas.Domain.Workspace.Transfer;

public sealed record WorkspaceIdentityCollision(string EntityType, string Identity);

public static class WorkspaceImportPreflight
{
    public static IReadOnlyList<WorkspaceIdentityCollision> FindIdentityCollisions(
        WorkspaceSnapshot live,
        WorkspaceSnapshot package)
    {
        ArgumentNullException.ThrowIfNull(live);
        ArgumentNullException.ThrowIfNull(package);

        var collisions = new List<WorkspaceIdentityCollision>();
        AddGuidCollisions(collisions, "Workspace", live.Workspaces.Select(x => x.Id), package.Workspaces.Select(x => x.Id));
        AddGuidCollisions(collisions, "Store", live.Stores.Select(x => x.Id), package.Stores.Select(x => x.Id));
        AddGuidCollisions(collisions, "Niche", live.Niches.Select(x => x.Id), package.Niches.Select(x => x.Id));
        AddGuidCollisions(collisions, "Group", live.Groups.Select(x => x.Id), package.Groups.Select(x => x.Id));
        AddGuidCollisions(collisions, "Item", live.Items.Select(x => x.Id), package.Items.Select(x => x.Id));
        AddGuidCollisions(collisions, "Asset", live.Assets.Select(x => x.Id), package.Assets.Select(x => x.Id));
        AddGuidCollisions(collisions, "Prompt", live.Prompts.Select(x => x.Id), package.Prompts.Select(x => x.Id));
        AddGuidCollisions(collisions, "Tag", live.Tags.Select(x => x.Id), package.Tags.Select(x => x.Id));
        AddCompositeCollisions(
            collisions,
            "ItemTag",
            live.ItemTags.Select(x => $"{x.ItemId:N}:{x.TagId:N}"),
            package.ItemTags.Select(x => $"{x.ItemId:N}:{x.TagId:N}"));
        AddCompositeCollisions(
            collisions,
            "AssetLink",
            live.AssetLinks.Select(x => $"{x.AssetId:N}:{(int)x.EntityKind}:{x.EntityId:N}"),
            package.AssetLinks.Select(x => $"{x.AssetId:N}:{(int)x.EntityKind}:{x.EntityId:N}"));
        return collisions;
    }

    public static string ResolveImportName(string packageName, IEnumerable<string> activeWorkspaceNames)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Workspace name must not be empty.", nameof(packageName));
        }

        ArgumentNullException.ThrowIfNull(activeWorkspaceNames);
        var normalizedName = packageName.Trim();
        var usedNames = activeWorkspaceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!usedNames.Contains(normalizedName))
        {
            return normalizedName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{normalizedName} ({suffix})";
            if (!usedNames.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private static void AddGuidCollisions(
        ICollection<WorkspaceIdentityCollision> collisions,
        string entityType,
        IEnumerable<Guid> live,
        IEnumerable<Guid> package) =>
        AddCompositeCollisions(
            collisions,
            entityType,
            live.Select(id => id.ToString("N")),
            package.Select(id => id.ToString("N")));

    private static void AddCompositeCollisions(
        ICollection<WorkspaceIdentityCollision> collisions,
        string entityType,
        IEnumerable<string> live,
        IEnumerable<string> package)
    {
        var liveIdentities = live.ToHashSet(StringComparer.Ordinal);
        foreach (var identity in package.Where(liveIdentities.Contains).Distinct(StringComparer.Ordinal))
        {
            collisions.Add(new WorkspaceIdentityCollision(entityType, identity));
        }
    }
}
