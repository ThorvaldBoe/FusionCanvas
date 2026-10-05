using FusionCanvas.Application.DesignFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FusionCanvas.Integration.Files;

public sealed class ImageSharpGlobalColorRemovalProcessor : IGlobalColorRemovalProcessor
{
    private const long MaximumEncodedBytes = 64L * 1024 * 1024;
    private const long MaximumDecodedPixels = 40L * 1000 * 1000;
    private const double MaximumRgbDistance = 441.6729559300637;

    public async Task<GlobalColorRemovalRasterPreview> PreviewAsync(
        Stream source,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(parameters);

        using var image = await LoadAsync(source, cancellationToken).ConfigureAwait(false);
        var mask = BuildMask(image, parameters, cancellationToken, out var matchedPixelCount, out var visiblePixelCount);
        using var overlay = image.Clone();
        ApplyOverlay(overlay, mask, cancellationToken);
        var bytes = await EncodePngAsync(overlay, cancellationToken).ConfigureAwait(false);
        return new GlobalColorRemovalRasterPreview(
            bytes,
            image.Width,
            image.Height,
            matchedPixelCount,
            visiblePixelCount);
    }

    public async Task<GlobalColorRemovalRasterResult> ApplyAsync(
        Stream source,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(parameters);

        using var image = await LoadAsync(source, cancellationToken).ConfigureAwait(false);
        var mask = BuildMask(image, parameters, cancellationToken, out var matchedPixelCount, out var visiblePixelCount);
        ApplyTransparency(image, mask, cancellationToken);
        var bytes = await EncodePngAsync(image, cancellationToken).ConfigureAwait(false);
        return new GlobalColorRemovalRasterResult(
            bytes,
            image.Width,
            image.Height,
            matchedPixelCount,
            visiblePixelCount);
    }

    public async Task<GlobalColorRemovalColor?> SampleAsync(
        Stream source,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (x < 0 || y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The image coordinate must not be negative.");
        }

        using var image = await LoadAsync(source, cancellationToken).ConfigureAwait(false);
        if (x >= image.Width || y >= image.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The image coordinate is outside the source image.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var pixel = image[x, y];
        return pixel.A == 0
            ? null
            : new GlobalColorRemovalColor(pixel.R, pixel.G, pixel.B);
    }

    private static async Task<Image<Rgba32>> LoadAsync(Stream source, CancellationToken cancellationToken)
    {
        if (source.CanSeek && source.Length > MaximumEncodedBytes)
        {
            throw new InvalidDataException("The image exceeds the maximum encoded size.");
        }

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
            throw new InvalidDataException("The selected file is not a supported raster image.", exception);
        }
    }

    private static bool[,] BuildMask(
        Image<Rgba32> image,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken,
        out int matchedPixelCount,
        out int visiblePixelCount)
    {
        var mask = new bool[image.Height, image.Width];
        var matched = 0;
        var visible = 0;
        var target = parameters.Color;
        var threshold = parameters.Tolerance * MaximumRgbDistance;
        var thresholdSquared = threshold * threshold;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    if (pixel.A == 0)
                    {
                        continue;
                    }

                    visible++;
                    var red = pixel.R - target.Red;
                    var green = pixel.G - target.Green;
                    var blue = pixel.B - target.Blue;
                    var distanceSquared = red * red + green * green + blue * blue;
                    if (distanceSquared <= thresholdSquared)
                    {
                        mask[y, x] = true;
                        matched++;
                    }
                }
            }
        });

        matchedPixelCount = matched;
        visiblePixelCount = visible;
        return mask;
    }

    private static void ApplyTransparency(Image<Rgba32> image, bool[,] mask, CancellationToken cancellationToken)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (mask[y, x])
                    {
                        row[x].A = 0;
                    }
                }
            }
        });
    }

    private static void ApplyOverlay(Image<Rgba32> image, bool[,] mask, CancellationToken cancellationToken)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (!mask[y, x])
                    {
                        continue;
                    }

                    var source = row[x];
                    row[x] = new Rgba32(
                        Blend(source.R, 255, 0.55),
                        Blend(source.G, 80, 0.55),
                        Blend(source.B, 80, 0.55),
                        source.A);
                }
            }
        });
    }

    private static async Task<byte[]> EncodePngAsync(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        await using var output = new MemoryStream();
        await image.SaveAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
        if (output.Length > MaximumEncodedBytes)
        {
            throw new InvalidDataException("The resulting PNG exceeds the maximum encoded size.");
        }

        return output.ToArray();
    }

    private static byte Blend(byte source, byte overlay, double opacity) =>
        (byte)Math.Clamp(Math.Round(source * (1 - opacity) + overlay * opacity), 0, 255);
}
