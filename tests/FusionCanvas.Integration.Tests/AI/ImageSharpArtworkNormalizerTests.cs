using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace FusionCanvas.Integration.Tests.AI;

public sealed class ImageSharpArtworkNormalizerTests
{
    [Fact]
    public async Task NormalizeAsync_FitsArtworkOnTransparentTargetCanvas()
    {
        using var sourceImage = new Image<Rgba32>(20, 10, new Rgba32(255, 0, 0, 255));
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        source.Position = 0;

        var normalizer = new ImageSharpArtworkNormalizer();
        var result = await normalizer.NormalizeAsync(
            source,
            new RasterArtworkNormalizationRequest(new AiImageSize(100, 80), TransparencyRequested: true));

        Assert.Equal(new AiImageSize(100, 80), result.FinalSize);
        Assert.True(result.HasTransparency);
        await using var resultStream = new MemoryStream(result.PngBytes);
        using var output = await Image.LoadAsync<Rgba32>(resultStream);
        Assert.Equal(100, output.Width);
        Assert.Equal(80, output.Height);
        Assert.Equal(0, output[0, 0].A);
        Assert.True(output[50, 10].A > 0);
    }

    [Fact]
    public async Task NormalizeAsync_RejectsFullyTransparentArtwork()
    {
        using var sourceImage = new Image<Rgba32>(4, 4, Color.Transparent);
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        source.Position = 0;

        var normalizer = new ImageSharpArtworkNormalizer();

        await Assert.ThrowsAsync<InvalidDataException>(() => normalizer.NormalizeAsync(
            source,
            new RasterArtworkNormalizationRequest(new AiImageSize(40, 40), TransparencyRequested: true)));
    }

    [Fact]
    public async Task NormalizeAsync_EnforcesDecodedPixelLimit()
    {
        using var sourceImage = new Image<Rgba32>(3, 2, Color.White);
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        source.Position = 0;

        var normalizer = new ImageSharpArtworkNormalizer();

        await Assert.ThrowsAsync<InvalidDataException>(() => normalizer.NormalizeAsync(
            source,
            new RasterArtworkNormalizationRequest(new AiImageSize(40, 40), TransparencyRequested: false, MaximumPixels: 5)));
    }

    [Fact]
    public async Task NormalizeAsync_AcceptsJpegAndReportsOpaqueTransparencyShortfall()
    {
        using var sourceImage = new Image<Rgba32>(10, 20, new Rgba32(0, 120, 255, 255));
        await using var source = new MemoryStream();
        await sourceImage.SaveAsJpegAsync(source, new JpegEncoder { Quality = 90 });
        source.Position = 0;

        var result = await new ImageSharpArtworkNormalizer().NormalizeAsync(
            source,
            new RasterArtworkNormalizationRequest(new AiImageSize(80, 80), TransparencyRequested: true));

        Assert.Equal(new AiImageSize(80, 80), result.FinalSize);
        Assert.True(result.HasTransparency);
        Assert.Contains(result.Warnings, warning => warning.Contains("opaque", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NormalizeAsync_PreservesSourceAlphaAndCentersOnTopInset()
    {
        using var sourceImage = new Image<Rgba32>(2, 1);
        sourceImage[0, 0] = new Rgba32(255, 0, 0, 255);
        sourceImage[1, 0] = new Rgba32(255, 0, 0, 0);
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        source.Position = 0;

        var result = await new ImageSharpArtworkNormalizer().NormalizeAsync(
            source,
            new RasterArtworkNormalizationRequest(new AiImageSize(100, 100), TransparencyRequested: true));

        await using var outputStream = new MemoryStream(result.PngBytes);
        using var output = await Image.LoadAsync<Rgba32>(outputStream);
        Assert.Equal(100, output.Width);
        Assert.True(output[50, 2].A > 0);
        Assert.Equal(0, output[0, 99].A);
    }

    [Fact]
    public async Task NormalizeAsync_RejectsMalformedPayload()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => new ImageSharpArtworkNormalizer().NormalizeAsync(
            new MemoryStream([1, 2, 3, 4]),
            new RasterArtworkNormalizationRequest(new AiImageSize(40, 40), TransparencyRequested: false)));
    }
}
