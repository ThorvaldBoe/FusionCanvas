using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Integration.Mockups;

public sealed class ImageSharpMockupRasterCompositor : IMockupRasterCompositor
{
    private const long DefaultMaximumDecodedPixels = 100_000_000;

    private readonly long _maximumDecodedPixels;

    public ImageSharpMockupRasterCompositor(long maximumDecodedPixels = DefaultMaximumDecodedPixels)
    {
        if (maximumDecodedPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDecodedPixels), maximumDecodedPixels, "The maximum decoded pixel count must be positive.");

        _maximumDecodedPixels = maximumDecodedPixels;
    }

    public async Task<Stream> ComposeAsync(Stream template, Stream design, MockupImageSpaceMapping mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(design);

        await EnsureDecodedPixelLimitAsync(template, "template", cancellationToken).ConfigureAwait(false);
        using var templateImage = await Image.LoadAsync<Rgba32>(template, cancellationToken).ConfigureAwait(false);
        EnsureDecodedPixelLimit(templateImage.Width, templateImage.Height, "template");

        if (templateImage.Width != mapping.ImageWidth || templateImage.Height != mapping.ImageHeight)
            throw new InvalidOperationException("The template image dimensions do not match its saved mapping.");

        await EnsureDecodedPixelLimitAsync(design, "design", cancellationToken).ConfigureAwait(false);
        using var designImage = await Image.LoadAsync<Rgba32>(design, cancellationToken).ConfigureAwait(false);
        EnsureDecodedPixelLimit(designImage.Width, designImage.Height, "design");

        var widthScale = mapping.Width / (double)designImage.Width;
        var heightScale = mapping.Height / (double)designImage.Height;
        var scale = Math.Min(widthScale, heightScale);
        var width = Math.Max(1, (int)Math.Round(designImage.Width * scale));
        var height = Math.Max(1, (int)Math.Round(designImage.Height * scale));

        cancellationToken.ThrowIfCancellationRequested();
        designImage.Mutate(image => image.Resize(width, height));
        cancellationToken.ThrowIfCancellationRequested();
        var x = mapping.X + (mapping.Width - width) / 2;
        var y = mapping.Y + (mapping.Height - height) / 2;
        cancellationToken.ThrowIfCancellationRequested();
        templateImage.Mutate(image => image.DrawImage(designImage, new Point(x, y), 1f));
        cancellationToken.ThrowIfCancellationRequested();

        var output = new MemoryStream();
        await templateImage.SaveAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
        output.Position = 0;
        return output;
    }

    private async Task EnsureDecodedPixelLimitAsync(Stream source, string imageName, CancellationToken cancellationToken)
    {
        if (!source.CanSeek)
            return;

        var position = source.Position;
        try
        {
            var info = await Image.IdentifyAsync(source, cancellationToken).ConfigureAwait(false);
            EnsureDecodedPixelLimit(info.Width, info.Height, imageName);
        }
        finally
        {
            source.Position = position;
        }
    }

    private void EnsureDecodedPixelLimit(int width, int height, string imageName)
    {
        if ((long)width * height > _maximumDecodedPixels)
            throw new InvalidDataException($"The {imageName} image exceeds the maximum decoded pixel count.");
    }
}
