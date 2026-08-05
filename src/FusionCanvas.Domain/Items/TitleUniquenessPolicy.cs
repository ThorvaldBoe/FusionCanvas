namespace FusionCanvas.Domain.Items;

public static class TitleUniquenessPolicy
{
    public const int MaximumAttempts = 4;

    public const string IdeaKey = "idea";
    public const string ConceptIdeaKey = "concept.idea";
    public const string PhraseKey = "phrase";
    public const string GraphicDirectionKey = "graphicDirection";

    public static bool HasCreativeContent(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return !string.IsNullOrWhiteSpace(Value(metadata, IdeaKey)) ||
               !string.IsNullOrWhiteSpace(Value(metadata, ConceptIdeaKey)) ||
               !string.IsNullOrWhiteSpace(Value(metadata, PhraseKey)) ||
               !string.IsNullOrWhiteSpace(Value(metadata, GraphicDirectionKey));
    }

    public static ISet<string> DistinctTitles(
        IEnumerable<Item> items,
        Guid storeId,
        Guid activeItemId)
    {
        ArgumentNullException.ThrowIfNull(items);

        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item.StoreId != storeId || item.Id == activeItemId)
            {
                continue;
            }

            if (item.IsArchived || item.Status == ItemStatus.Rejected)
            {
                continue;
            }

            var name = item.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            titles.Add(name);
        }

        return titles;
    }

    public static bool IsUnique(string? candidate, ISet<string> existingTitles)
    {
        ArgumentNullException.ThrowIfNull(existingTitles);
        var normalized = candidate?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var existing in existingTitles)
        {
            if (string.Equals(existing.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public static string WithNumericSuffix(string? candidate, ISet<string> existingTitles)
    {
        ArgumentNullException.ThrowIfNull(existingTitles);
        var baseTitle = candidate?.Trim() ?? string.Empty;
        if (baseTitle.Length == 0)
        {
            return string.Empty;
        }

        var suffix = 2;
        while (existingTitles.Any(existing =>
                   string.Equals(existing.Trim(), $"{baseTitle} {suffix}", StringComparison.OrdinalIgnoreCase)))
        {
            suffix++;
        }

        return $"{baseTitle} {suffix}";
    }

    private static string? Value(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value : null;
}
