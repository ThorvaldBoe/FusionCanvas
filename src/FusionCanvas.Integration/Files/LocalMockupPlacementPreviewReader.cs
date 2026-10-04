using FusionCanvas.Application.Mockups;

namespace FusionCanvas.Integration.Files;

public sealed class LocalMockupPlacementPreviewReader : IMockupPlacementPreviewReader
{
    public Stream OpenRead(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("An image path is required.", nameof(sourcePath));

        return File.OpenRead(sourcePath);
    }
}
