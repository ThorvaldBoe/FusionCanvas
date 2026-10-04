namespace FusionCanvas.Domain.ContentRisk;

public sealed record ContentRiskFinding
{
    public ContentRiskFinding(
        ContentRiskCategory category,
        ContentRiskSeverity severity,
        string explanation,
        string? evidence = null)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new ArgumentException("A content-risk finding explanation is required.", nameof(explanation));
        }

        Explanation = Limit(explanation.Trim(), 500);
        Evidence = string.IsNullOrWhiteSpace(evidence) ? null : Limit(evidence.Trim(), 240);
        Category = category;
        Severity = severity;
    }

    public ContentRiskCategory Category { get; }

    public ContentRiskSeverity Severity { get; }

    public string Explanation { get; }

    public string? Evidence { get; }

    private static string Limit(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum];
}
