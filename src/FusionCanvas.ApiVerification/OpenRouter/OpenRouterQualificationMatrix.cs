namespace FusionCanvas.ApiVerification.OpenRouter;

internal sealed record OpenRouterQualificationCandidate(string Id, string Kind);

internal sealed record OpenRouterQualificationSelection(
    IReadOnlyList<OpenRouterQualificationCandidate> Candidates,
    IReadOnlyList<string> UnknownModelIds,
    bool RequestedAll,
    bool InvalidAllCombination);

internal static class OpenRouterQualificationMatrix
{
    public static OpenRouterQualificationSelection Select(
        IReadOnlyList<string> requestedModels,
        IEnumerable<string> textModelIds,
        IEnumerable<string> imageModelIds)
    {
        ArgumentNullException.ThrowIfNull(requestedModels);
        ArgumentNullException.ThrowIfNull(textModelIds);
        ArgumentNullException.ThrowIfNull(imageModelIds);

        var text = ToCanonicalMap(textModelIds);
        var image = ToCanonicalMap(imageModelIds);
        var requestAll = requestedModels.Contains("all", StringComparer.OrdinalIgnoreCase);
        if (requestAll && requestedModels.Count != 1)
        {
            return new([], [], true, true);
        }

        var candidates = new List<OpenRouterQualificationCandidate>();
        var unknown = new List<string>();
        if (requestAll)
        {
            candidates.AddRange(text.Values.Select(id => new OpenRouterQualificationCandidate(id, "text")));
            candidates.AddRange(image.Values.Select(id => new OpenRouterQualificationCandidate(id, "image")));
        }
        else
        {
            foreach (var requested in requestedModels.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var found = false;
                if (text.TryGetValue(requested, out var textId))
                {
                    candidates.Add(new(textId, "text"));
                    found = true;
                }
                if (image.TryGetValue(requested, out var imageId))
                {
                    candidates.Add(new(imageId, "image"));
                    found = true;
                }
                if (!found)
                    unknown.Add(requested);
            }
        }

        return new(candidates.DistinctBy(candidate => (candidate.Id, candidate.Kind), CandidateKeyComparer.Instance).ToArray(), unknown, requestAll, false);
    }

    private static Dictionary<string, string> ToCanonicalMap(IEnumerable<string> ids) => ids
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToDictionary(id => id, id => id, StringComparer.OrdinalIgnoreCase);

    private sealed class CandidateKeyComparer : IEqualityComparer<(string Id, string Kind)>
    {
        public static CandidateKeyComparer Instance { get; } = new();

        public bool Equals((string Id, string Kind) x, (string Id, string Kind) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Id, y.Id) && StringComparer.OrdinalIgnoreCase.Equals(x.Kind, y.Kind);

        public int GetHashCode((string Id, string Kind) value) => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Id),
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Kind));
    }
}
