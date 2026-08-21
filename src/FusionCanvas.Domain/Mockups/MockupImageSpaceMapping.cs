namespace FusionCanvas.Domain.Mockups;

public sealed record MockupImageSpaceMapping
{
    public MockupImageSpaceMapping(
        int imageWidth,
        int imageHeight,
        int x,
        int y,
        int width,
        int height)
    {
        ImageWidth = Positive(imageWidth, nameof(imageWidth));
        ImageHeight = Positive(imageHeight, nameof(imageHeight));
        X = NonNegative(x, nameof(x));
        Y = NonNegative(y, nameof(y));
        Width = Positive(width, nameof(width));
        Height = Positive(height, nameof(height));

        if ((long)X + Width > ImageWidth)
            throw new ArgumentOutOfRangeException(nameof(width), width, "The mapped area must remain within the image width.");
        if ((long)Y + Height > ImageHeight)
            throw new ArgumentOutOfRangeException(nameof(height), height, "The mapped area must remain within the image height.");
    }

    public int ImageWidth { get; }
    public int ImageHeight { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    private static int Positive(int value, string name) => value > 0
        ? value
        : throw new ArgumentOutOfRangeException(name, value, "The value must be positive.");

    private static int NonNegative(int value, string name) => value >= 0
        ? value
        : throw new ArgumentOutOfRangeException(name, value, "The value must not be negative.");
}
