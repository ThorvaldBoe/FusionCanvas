namespace FusionCanvas.Domain.Workspace;

public static class WorkspaceNamePolicy
{
    public static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim();
    }

    public static bool IsTaken(string candidateName, IEnumerable<string> existingNames)
    {
        ArgumentNullException.ThrowIfNull(existingNames);

        var normalizedCandidate = Normalize(candidateName);
        return existingNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(Normalize)
            .Any(name => string.Equals(name, normalizedCandidate, StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveUniqueName(string name, IEnumerable<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name must not be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(existingNames);
        var normalizedName = Normalize(name);
        var usedNames = existingNames
            .Where(existingName => !string.IsNullOrWhiteSpace(existingName))
            .Select(Normalize)
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
