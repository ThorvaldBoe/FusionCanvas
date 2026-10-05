namespace FusionCanvas.Application.DesignFiles;

public sealed record GlobalColorRemovalRasterPreview(
    byte[] OverlayPng,
    int Width,
    int Height,
    int MatchedPixelCount,
    int VisiblePixelCount)
{
    public int RemainingVisiblePixelCount => VisiblePixelCount - MatchedPixelCount;

    public bool HasMatches => MatchedPixelCount > 0;

    public bool LeavesVisibleArtwork => RemainingVisiblePixelCount > 0;
}

public sealed record GlobalColorRemovalRasterResult(
    byte[] Png,
    int Width,
    int Height,
    int MatchedPixelCount,
    int VisiblePixelCount)
{
    public int RemainingVisiblePixelCount => VisiblePixelCount - MatchedPixelCount;
}
