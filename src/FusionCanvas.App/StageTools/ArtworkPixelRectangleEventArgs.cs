using FusionCanvas.Application.DesignFiles;

namespace FusionCanvas.App.StageTools;

public sealed class ArtworkPixelRectangleEventArgs(ArtworkPixelRectangle bounds) : EventArgs
{
    public ArtworkPixelRectangle Bounds { get; } = bounds;
}
