namespace FusionCanvas.Application.Listings;

public sealed record ArtworkDimensions(int Width, int Height)
{
    public ArtworkDimensions Normalize()
    {
        if (Width <= 0) throw new ArgumentOutOfRangeException(nameof(Width), Width, "Artwork width must be positive.");
        if (Height <= 0) throw new ArgumentOutOfRangeException(nameof(Height), Height, "Artwork height must be positive.");
        return this;
    }
}
