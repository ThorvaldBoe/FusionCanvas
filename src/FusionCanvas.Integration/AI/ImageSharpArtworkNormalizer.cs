using FusionCanvas.Application.AI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FusionCanvas.Integration.AI;

public sealed class ImageSharpArtworkNormalizer : IRasterArtworkNormalizer
{
    public async Task<RasterArtworkNormalizationResult> NormalizeAsync(
        Stream source,
        RasterArtworkNormalizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (request.MaximumBytes <= 0 || request.MaximumPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Raster limits must be positive.");

        if (source.CanSeek && source.Length > request.MaximumBytes)
            throw new InvalidDataException("The provider image exceeds the maximum encoded size.");

        Image<Rgba32> image;
        try
        {
            image = await Image.LoadAsync<Rgba32>(source, cancellationToken).ConfigureAwait(false);
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException exception)
        {
            throw new InvalidDataException("The provider returned an unsupported or malformed raster image.", exception);
        }
        using (image)
        {
        if ((long)image.Width * image.Height > request.MaximumPixels)
            throw new InvalidDataException("The provider image exceeds the maximum decoded pixel count.");
        if (!HasVisiblePixels(image))
            throw new InvalidDataException("The provider image contains no visible artwork.");
        var sourceHasTransparency = HasTransparentPixels(image);

        var horizontalInset = Math.Min(Math.Max(1, (int)Math.Ceiling(request.TargetSize.Width * 0.02)), Math.Max(0, (request.TargetSize.Width - 1) / 2));
        var topInset = Math.Min(Math.Max(1, (int)Math.Ceiling(request.TargetSize.Height * 0.02)), Math.Max(0, request.TargetSize.Height - 1));
        var safeWidth = Math.Max(1, request.TargetSize.Width - horizontalInset * 2);
        var safeHeight = Math.Max(1, request.TargetSize.Height - topInset);
        var scale = Math.Min(safeWidth / (double)image.Width, safeHeight / (double)image.Height);
        var fittedWidth = Math.Max(1, Math.Min(safeWidth, (int)Math.Round(image.Width * scale, MidpointRounding.AwayFromZero)));
        var fittedHeight = Math.Max(1, Math.Min(safeHeight, (int)Math.Round(image.Height * scale, MidpointRounding.AwayFromZero)));
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(fittedWidth, fittedHeight),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Lanczos3
        }));

        using var canvas = new Image<Rgba32>(request.TargetSize.Width, request.TargetSize.Height, Color.Transparent);
        canvas.Mutate(context => context.DrawImage(image, new Point((request.TargetSize.Width - fittedWidth) / 2, topInset), 1f));
        var hasTransparency = HasTransparentPixels(canvas);
        var warnings = request.TransparencyRequested && !sourceHasTransparency
            ? new[] { "The provider returned opaque artwork although transparent background was requested." }
            : Array.Empty<string>();

        await using var output = new MemoryStream();
        await canvas.SaveAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
        if (output.Length > request.MaximumBytes)
        {
            output.SetLength(0);
            output.Position = 0;
            await canvas.SaveAsync(output, new PngEncoder { CompressionLevel = PngCompressionLevel.Level7 }, cancellationToken).ConfigureAwait(false);
        }
        if (output.Length > request.MaximumBytes)
        {
            output.SetLength(0);
            output.Position = 0;
            await canvas.SaveAsync(output, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }, cancellationToken).ConfigureAwait(false);
        }
        if (output.Length > request.MaximumBytes)
            throw new InvalidDataException("The normalized PNG exceeds the maximum encoded size.");
        return new RasterArtworkNormalizationResult(output.ToArray(), request.TargetSize, hasTransparency, warnings);
        }
    }

    private static bool HasVisiblePixels(Image<Rgba32> image)
    {
        var visible = false;
        image.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height && !visible; row++)
            {
                var pixels = accessor.GetRowSpan(row);
                for (var column = 0; column < pixels.Length && !visible; column++)
                    visible = pixels[column].A > 0;
            }
        });
        return visible;
    }

    private static bool HasTransparentPixels(Image<Rgba32> image)
    {
        var transparent = false;
        image.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height && !transparent; row++)
            {
                var pixels = accessor.GetRowSpan(row);
                for (var column = 0; column < pixels.Length && !transparent; column++)
                    transparent = pixels[column].A < byte.MaxValue;
            }
        });
        return transparent;
    }
}
