namespace FusionCanvas.Application.Listings;

public static class ListingPlacementCalculator
{
    public static ArtworkPlacement Calculate(ArtworkDimensions artwork, ArtworkDimensions target)
    {
        artwork.Normalize();
        target.Normalize();

        var scale = (double)target.Width / artwork.Width;
        var renderedHeight = artwork.Height * scale;

        return new ArtworkPlacement(
            Scale: scale,
            X: 0.5d,
            Y: renderedHeight / (2d * target.Height),
            Angle: 0d);
    }
}
