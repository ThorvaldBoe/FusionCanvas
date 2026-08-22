namespace FusionCanvas.UiDescription.Layout;

public sealed class WireframeTokenProfile
{
    private static readonly IReadOnlyDictionary<string, decimal> Spacing =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["none"] = 0,
            ["tight"] = 4,
            ["compact"] = 8,
            ["control"] = 12,
            ["section"] = 16,
            ["region"] = 24,
            ["window"] = 48
        };

    private static readonly IReadOnlyDictionary<string, TextMetric> TextMetrics =
        new Dictionary<string, TextMetric>(StringComparer.Ordinal)
        {
            ["screen-heading"] = new(26, 32, 0.58m, true),
            ["section-heading"] = new(20, 26, 0.57m, true),
            ["subheading"] = new(16, 22, 0.56m, true),
            ["body"] = new(14, 20, 0.55m, false),
            ["supporting"] = new(14, 20, 0.55m, false),
            ["label"] = new(13, 18, 0.54m, false),
            ["emphasis"] = new(14, 20, 0.56m, true),
            ["link"] = new(14, 20, 0.55m, false)
        };

    public decimal ResolveSpacing(string? token) => Spacing[token ?? "none"];

    public TextMetric ResolveText(string? variant) => TextMetrics[variant ?? "body"];

    public UiSize MeasureText(string text, string? variant)
    {
        var metric = ResolveText(variant);
        var scalarCount = text.EnumerateRunes().Count();
        var width = decimal.Ceiling(scalarCount * metric.FontSize * metric.WidthFactor);
        return new UiSize(width, metric.LineHeight);
    }

    public sealed record TextMetric(decimal FontSize, decimal LineHeight, decimal WidthFactor, bool Bold);
}
