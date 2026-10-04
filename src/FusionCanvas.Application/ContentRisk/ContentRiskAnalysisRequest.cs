using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public sealed record ContentRiskAnalysisRequest
{
    public const int MaximumTextCharacters = 20_000;
    public const int MaximumImageBytes = 8_000_000;

    public ContentRiskAnalysisRequest(
        ContentRiskReviewTarget target,
        string? text = null,
        string? mediaType = null,
        byte[]? imageBytes = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.ContentKind == ContentRiskContentKind.Text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumTextCharacters)
            {
                throw new ArgumentOutOfRangeException(nameof(text), $"Text must contain 1 to {MaximumTextCharacters} characters.");
            }

            if (imageBytes is not null || mediaType is not null)
            {
                throw new ArgumentException("Text analysis cannot include image data.");
            }
        }
        else
        {
            if (imageBytes is null || imageBytes.Length == 0 || imageBytes.Length > MaximumImageBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(imageBytes), $"Image bytes must contain 1 to {MaximumImageBytes} bytes.");
            }

            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw new ArgumentException("An image media type is required.", nameof(mediaType));
            }

            if (text is not null)
            {
                throw new ArgumentException("Image analysis cannot include text content.");
            }
        }

        Target = target;
        Text = text?.Trim();
        MediaType = mediaType?.Trim().ToLowerInvariant();
        ImageBytes = imageBytes?.ToArray();
    }

    public ContentRiskReviewTarget Target { get; }

    public string? Text { get; }

    public string? MediaType { get; }

    public byte[]? ImageBytes { get; }
}
