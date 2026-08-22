namespace FusionCanvas.UiDescription.Validation;

internal static class UiVocabulary
{
    public const string TokenProfile = "fusioncanvas-wireframe-v1";

    public static readonly IReadOnlySet<string> ContainerKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "stack", "grid", "panel"
    };

    public static readonly IReadOnlySet<string> LeafKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "text", "field", "select", "list", "table", "button", "message", "divider"
    };

    public static readonly IReadOnlySet<string> SpacingTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "none", "tight", "compact", "control", "section", "region", "window"
    };

    public static readonly IReadOnlySet<string> Alignments = new HashSet<string>(StringComparer.Ordinal)
    {
        "start", "center", "end", "stretch"
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Variants =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["stack"] = Set("default", "action-group"),
            ["grid"] = Set("default"),
            ["panel"] = Set("standard", "elevated", "choice-card", "summary-card", "canvas"),
            ["text"] = Set("screen-heading", "section-heading", "subheading", "body", "supporting", "label", "emphasis", "link"),
            ["field"] = Set("single-line", "multiline"),
            ["select"] = Set("standard"),
            ["list"] = Set("standard"),
            ["table"] = Set("data-table"),
            ["button"] = Set("primary", "secondary", "danger", "link"),
            ["message"] = Set("info", "warning", "danger", "empty"),
            ["divider"] = Set("standard")
        };

    public static bool IsContainer(string kind) => ContainerKinds.Contains(kind);

    public static bool IsLeaf(string kind) => LeafKinds.Contains(kind);

    public static bool IsInteractive(string kind) => kind is "field" or "select" or "list" or "table" or "button";

    public static bool SupportsText(string kind) => kind is "text" or "field" or "select" or "button" or "message";

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
