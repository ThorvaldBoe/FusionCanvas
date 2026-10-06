namespace FusionCanvas.Domain.Mockups;

public sealed record MockupOutputResolutionPolicy
{
    public const int DefaultMaximumLongEdgePixels = 2000;

    public MockupOutputResolutionPolicy(int maximumLongEdgePixels)
    {
        MaximumLongEdgePixels = maximumLongEdgePixels > 0
            ? maximumLongEdgePixels
            : throw new ArgumentOutOfRangeException(nameof(maximumLongEdgePixels), maximumLongEdgePixels, "The maximum long edge must be a positive whole-pixel value.");
    }

    public int MaximumLongEdgePixels { get; }

    public static MockupOutputResolutionPolicy Default { get; } = new(DefaultMaximumLongEdgePixels);

    public (int Width, int Height) CalculateOutputDimensions(int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0) throw new ArgumentOutOfRangeException(nameof(imageWidth), imageWidth, "The image width must be positive.");
        if (imageHeight <= 0) throw new ArgumentOutOfRangeException(nameof(imageHeight), imageHeight, "The image height must be positive.");

        var sourceLongEdge = Math.Max(imageWidth, imageHeight);
        if (sourceLongEdge <= MaximumLongEdgePixels)
            return (imageWidth, imageHeight);

        var scale = MaximumLongEdgePixels / (double)sourceLongEdge;
        return (
            Math.Max(1, (int)Math.Round(imageWidth * scale, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)Math.Round(imageHeight * scale, MidpointRounding.AwayFromZero)));
    }

    public MockupImageSpaceMapping ScaleMapping(MockupImageSpaceMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        var output = CalculateOutputDimensions(mapping.ImageWidth, mapping.ImageHeight);
        if (output.Width == mapping.ImageWidth && output.Height == mapping.ImageHeight)
            return mapping;

        var widthScale = output.Width / (double)mapping.ImageWidth;
        var heightScale = output.Height / (double)mapping.ImageHeight;
        var (x, width) = ScaleAxis(mapping.X, mapping.Width, widthScale, output.Width);
        var (y, height) = ScaleAxis(mapping.Y, mapping.Height, heightScale, output.Height);
        return new MockupImageSpaceMapping(output.Width, output.Height, x, y, width, height);
    }

    private static (int Start, int Length) ScaleAxis(int start, int length, double scale, int outputLength)
    {
        var scaledStart = Math.Clamp((int)Math.Round(start * scale, MidpointRounding.AwayFromZero), 0, outputLength - 1);
        var scaledEnd = Math.Clamp((int)Math.Round((start + length) * scale, MidpointRounding.AwayFromZero), scaledStart + 1, outputLength);
        return (scaledStart, scaledEnd - scaledStart);
    }
}
