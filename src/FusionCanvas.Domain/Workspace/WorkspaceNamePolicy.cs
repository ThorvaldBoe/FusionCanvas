namespace FusionCanvas.Domain.Workspace;

public static class WorkspaceNamePolicy
{
    public static bool HasActiveNameCollision(
        string candidateName,
        IEnumerable<Workspace> workspaces,
        Guid? excludedWorkspaceId = null)
    {
        ArgumentNullException.ThrowIfNull(workspaces);

        var normalizedCandidate = Normalize(candidateName);
        return workspaces.Any(workspace =>
            workspace.Id != excludedWorkspaceId &&
            !workspace.IsArchived &&
            !string.IsNullOrWhiteSpace(workspace.Name) &&
            string.Equals(workspace.Name.Trim(), normalizedCandidate, StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveImportName(string packageName, IEnumerable<Workspace> workspaces)
    {
        ArgumentNullException.ThrowIfNull(workspaces);

        var normalizedName = Normalize(packageName);
        var usedNames = workspaces
            .Where(workspace => !workspace.IsArchived)
            .Where(workspace => !string.IsNullOrWhiteSpace(workspace.Name))
            .Select(workspace => workspace.Name.Trim())
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

    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name must not be empty.", nameof(name));
        }

        return name.Trim();
    }
}
