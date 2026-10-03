namespace FusionCanvas.Application.AI;

/// <summary>One bounded raster image attachment for a multimodal text request.</summary>
public sealed record AiImageInput
{
    public const int MaximumBytes = 25_000_000;

    public AiImageInput(string mediaType, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("An image media type is required.", nameof(mediaType));
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0 || bytes.Length > MaximumBytes)
            throw new ArgumentOutOfRangeException(nameof(bytes), $"Image bytes must be between 1 and {MaximumBytes} bytes.");

        MediaType = mediaType.Trim().ToLowerInvariant();
        Bytes = bytes.ToArray();
    }

    public string MediaType { get; }

    public byte[] Bytes { get; }
}
