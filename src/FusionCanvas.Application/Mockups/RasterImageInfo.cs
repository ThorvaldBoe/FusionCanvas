namespace FusionCanvas.Application.Mockups;

public sealed record RasterImageInfo
{
    public RasterImageInfo(int width, int height)
    {
        Width = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width));
        Height = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height));
    }

    public int Width { get; }
    public int Height { get; }
}
