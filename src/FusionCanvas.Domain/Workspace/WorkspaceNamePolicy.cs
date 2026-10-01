namespace FusionCanvas.Domain.Workspace;

public static class WorkspaceNamePolicy
{
    public static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim();
    }

    public static bool Conflicts(string candidateName, IEnumerable<string> existingNames)
    {
        ArgumentNullException.ThrowIfNull(existingNames);
        var normalizedCandidate = NormalizeName(candidateName);

        return existingNames.Any(existingName =>
            !string.IsNullOrWhiteSpace(existingName) &&
            string.Equals(NormalizeName(existingName), normalizedCandidate, StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveImportName(string packageName, IEnumerable<string> activeWorkspaceNames)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Workspace name must not be empty.", nameof(packageName));
        }

        ArgumentNullException.ThrowIfNull(activeWorkspaceNames);
        var normalizedName = NormalizeName(packageName);
        var usedNames = activeWorkspaceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(NormalizeName)
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
}
