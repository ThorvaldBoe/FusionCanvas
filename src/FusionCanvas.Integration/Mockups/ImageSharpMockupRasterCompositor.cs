using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Integration.Mockups;

public sealed class ImageSharpMockupRasterCompositor : IMockupRasterCompositor
{
    public async Task<Stream> ComposeAsync(Stream template, Stream design, MockupImageSpaceMapping mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(design);
        using var templateImage = await Image.LoadAsync<Rgba32>(template, cancellationToken).ConfigureAwait(false);
        using var designImage = await Image.LoadAsync<Rgba32>(design, cancellationToken).ConfigureAwait(false);
        if (templateImage.Width != mapping.ImageWidth || templateImage.Height != mapping.ImageHeight)
            throw new InvalidOperationException("The template image dimensions do not match its saved mapping.");
        var widthScale = mapping.Width / (double)designImage.Width;
        var heightScale = mapping.Height / (double)designImage.Height;
        var scale = Math.Min(widthScale, heightScale);
        var width = Math.Max(1, (int)Math.Round(designImage.Width * scale));
        var height = Math.Max(1, (int)Math.Round(designImage.Height * scale));
        if (mapping.X < 0 || mapping.Y < 0 || mapping.Width <= 0 || mapping.Height <= 0 || mapping.X + mapping.Width > templateImage.Width || mapping.Y + mapping.Height > templateImage.Height)
            throw new InvalidOperationException("The template placement is outside the source image bounds.");
        designImage.Mutate(image => image.Resize(width, height));
        var x = mapping.X + (mapping.Width - width) / 2;
        var y = mapping.Y + (mapping.Height - height) / 2;
        templateImage.Mutate(image => image.DrawImage(designImage, new Point(x, y), 1f));
        var output = new MemoryStream();
        await templateImage.SaveAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
        output.Position = 0;
        return output;
    }
}
