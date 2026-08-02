namespace FusionCanvas.Domain.Concepts;

public static class DesignTriangleScore
{
    public static int FromValues(string? conceptIdea, string? phrase, string? graphicDirection)
    {
        var sum = CornerContribution(conceptIdea)
                + CornerContribution(phrase)
                + CornerContribution(graphicDirection);

        return (int)Math.Round(100.0 * sum / 3.0, MidpointRounding.AwayFromZero);
    }

    private static double CornerContribution(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0.0;
        }

        var trimmed = value.Trim();
        return trimmed.Length < 8 ? 0.5 : 1.0;
    }
}