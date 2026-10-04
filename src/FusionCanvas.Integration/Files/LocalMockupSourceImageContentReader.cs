using FusionCanvas.Application.AI;
using FusionCanvas.Application.Mockups;

namespace FusionCanvas.Integration.Files;

public sealed class LocalMockupSourceImageContentReader : IMockupSourceImageContentReader
{
    private static readonly IReadOnlyDictionary<string, string> MediaTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp"
    };

    public async Task<MockupSourceImageContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("An image path is required.", nameof(sourcePath));

        var file = new FileInfo(sourcePath);
        if (!file.Exists)
            throw new FileNotFoundException("The selected image was not found.", sourcePath);
        if (file.Length is <= 0 or > AiImageInput.MaximumBytes)
            throw new InvalidDataException($"The selected image must be between 1 and {AiImageInput.MaximumBytes} bytes.");
        if (!MediaTypes.TryGetValue(file.Extension, out var mediaType))
            throw new InvalidDataException("The selected image format is not supported for visual AI review.");

        return new MockupSourceImageContent(mediaType, await File.ReadAllBytesAsync(file.FullName, cancellationToken).ConfigureAwait(false));
    }
}
