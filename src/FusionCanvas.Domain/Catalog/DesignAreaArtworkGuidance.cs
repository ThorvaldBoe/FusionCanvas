namespace FusionCanvas.Domain.Catalog;

public sealed record DesignAreaArtworkGuidance
{
    public DesignAreaArtworkGuidance(
        int? recommendedWidthPixels = null,
        int? recommendedHeightPixels = null,
        int? dotsPerInch = null,
        string? fileFormat = null,
        string? background = null)
    {
        if (recommendedWidthPixels.HasValue != recommendedHeightPixels.HasValue)
            throw new ArgumentException("Recommended artwork width and height must be provided together.");
        if (recommendedWidthPixels is <= 0)
            throw new ArgumentOutOfRangeException(nameof(recommendedWidthPixels), recommendedWidthPixels, "Recommended artwork width must be positive.");
        if (recommendedHeightPixels is <= 0)
            throw new ArgumentOutOfRangeException(nameof(recommendedHeightPixels), recommendedHeightPixels, "Recommended artwork height must be positive.");
        if (dotsPerInch is <= 0)
            throw new ArgumentOutOfRangeException(nameof(dotsPerInch), dotsPerInch, "Artwork DPI must be positive.");

        RecommendedWidthPixels = recommendedWidthPixels;
        RecommendedHeightPixels = recommendedHeightPixels;
        DotsPerInch = dotsPerInch;
        FileFormat = Optional(fileFormat);
        Background = Optional(background);
    }

    public int? RecommendedWidthPixels { get; }
    public int? RecommendedHeightPixels { get; }
    public int? DotsPerInch { get; }
    public string? FileFormat { get; }
    public string? Background { get; }

    public DesignAreaPhysicalSize? PhysicalSizeFor(int widthPixels, int heightPixels)
    {
        if (DotsPerInch is not int dpi)
            return null;
        if (widthPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthPixels));
        if (heightPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightPixels));

        return new DesignAreaPhysicalSize(
            widthPixels / (double)dpi,
            heightPixels / (double)dpi);
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
