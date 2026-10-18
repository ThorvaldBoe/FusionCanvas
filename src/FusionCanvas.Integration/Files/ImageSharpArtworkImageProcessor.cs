using FusionCanvas.Application.DesignFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FusionCanvas.Integration.Files;

public sealed class ImageSharpArtworkImageProcessor : IArtworkImageProcessor
{
    private const long MaximumEncodedBytes = 64L * 1024 * 1024;
    private const long MaximumDecodedPixels = 40L * 1000 * 1000;
    private const double MaximumTargetPixels = 40L * 1000 * 1000;

    public async Task<ArtworkImageInspection> InspectAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var image = await LoadAsync(source, cancellationToken).ConfigureAwait(false);
        var bounds = FindVisibleBounds(image, cancellationToken);
        return new ArtworkImageInspection(image.Width, image.Height, bounds);
    }

    public async Task<byte[]> TransformAsync(
        Stream source,
        ArtworkPixelRectangle sourceBounds,
        ArtworkPixelRectangle targetBounds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!sourceBounds.IsFinitePositive || !targetBounds.IsFinitePositive)
            throw new ArgumentOutOfRangeException(nameof(targetBounds), "Artwork bounds must be finite and have positive dimensions.");

        using var image = await LoadAsync(source, cancellationToken).ConfigureAwait(false);
        var crop = ToContainedPixelRectangle(sourceBounds, image.Width, image.Height);
        if (crop.Width <= 0 || crop.Height <= 0)
            throw new InvalidDataException("The visible artwork bounds are outside the image canvas.");

        var scale = Math.Min(targetBounds.Width / crop.Width, targetBounds.Height / crop.Height);
        var scaledWidth = crop.Width * scale;
        var scaledHeight = crop.Height * scale;
        if (!double.IsFinite(scaledWidth) || !double.IsFinite(scaledHeight)
            || scaledWidth * scaledHeight > MaximumTargetPixels
            || scaledWidth > int.MaxValue || scaledHeight > int.MaxValue)
            throw new InvalidDataException("The adjusted artwork is too large to render safely.");
        var targetWidth = Math.Max(1, (int)Math.Round(scaledWidth, MidpointRounding.AwayFromZero));
        var targetHeight = Math.Max(1, (int)Math.Round(scaledHeight, MidpointRounding.AwayFromZero));

        var x = checked((int)Math.Round(targetBounds.X, MidpointRounding.AwayFromZero));
        var y = checked((int)Math.Round(targetBounds.Y, MidpointRounding.AwayFromZero));
        var canvas = new Image<Rgba32>(Configuration.Default, image.Width, image.Height);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var cropped = image.Clone(context => context.Crop(crop));
            using var resized = cropped.Clone(context => context.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Lanczos3
            }));
            canvas.Mutate(context => context.DrawImage(resized, new Point(x, y), 1f));

            await using var output = new MemoryStream();
            await canvas.SaveAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }
        finally
        {
            canvas.Dispose();
        }
    }

    private static async Task<Image<Rgba32>> LoadAsync(Stream source, CancellationToken cancellationToken)
    {
        if (source.CanSeek && source.Length > MaximumEncodedBytes)
            throw new InvalidDataException("The image exceeds the maximum encoded size.");

        try
        {
            var image = await Image.LoadAsync<Rgba32>(source, cancellationToken).ConfigureAwait(false);
            if ((long)image.Width * image.Height > MaximumDecodedPixels)
            {
                image.Dispose();
                throw new InvalidDataException("The image exceeds the maximum decoded pixel count.");
            }

            return image;
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException exception)
        {
            throw new InvalidDataException("The managed artwork is not a supported raster image.", exception);
        }
    }

    private static ArtworkPixelRectangle? FindVisibleBounds(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        var minX = image.Width;
        var minY = image.Height;
        var maxX = -1;
        var maxY = -1;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A == 0) continue;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        });

        return maxX < minX
            ? null
            : ArtworkPixelRectangle.FromIntegerBounds(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static Rectangle ToContainedPixelRectangle(ArtworkPixelRectangle bounds, int canvasWidth, int canvasHeight)
    {
        var left = Math.Clamp((int)Math.Floor(bounds.X), 0, canvasWidth);
        var top = Math.Clamp((int)Math.Floor(bounds.Y), 0, canvasHeight);
        var right = Math.Clamp((int)Math.Ceiling(bounds.X + bounds.Width), 0, canvasWidth);
        var bottom = Math.Clamp((int)Math.Ceiling(bounds.Y + bounds.Height), 0, canvasHeight);
        return new Rectangle(left, top, right - left, bottom - top);
    }
}
