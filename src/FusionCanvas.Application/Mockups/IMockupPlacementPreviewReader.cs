namespace FusionCanvas.Application.Mockups;

/// <summary>Opens mockup source image content for UI preview without exposing filesystem access to the control.</summary>
public interface IMockupPlacementPreviewReader
{
    Stream OpenRead(string sourcePath);
}
