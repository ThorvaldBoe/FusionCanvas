using System.Text.Json.Serialization;

namespace FusionCanvas.Application.AI;

public readonly record struct AiImageSize
{
    [JsonConstructor]
    public AiImageSize(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be positive.");
        Width = width; Height = height;
    }
    public int Width { get; }
    public int Height { get; }
    public long PixelCount => (long)Width * Height;
}
