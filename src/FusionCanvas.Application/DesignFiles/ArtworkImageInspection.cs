namespace FusionCanvas.Application.DesignFiles;

public sealed record ArtworkImageInspection(
    int CanvasWidth,
    int CanvasHeight,
    ArtworkPixelRectangle? VisibleBounds)
{
    public bool HasVisibleArtwork => VisibleBounds is not null;
}
