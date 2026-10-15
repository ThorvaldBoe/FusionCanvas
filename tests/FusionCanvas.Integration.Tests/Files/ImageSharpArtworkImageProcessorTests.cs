using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Integration.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FusionCanvas.Integration.Tests.Files;

public sealed class ImageSharpArtworkImageProcessorTests
{
    [Fact]
    public async Task InspectAsync_FindsAlphaBoundsIncludingSemiTransparentPixels()
    {
        using var image = new Image<Rgba32>(8, 6, Color.Transparent);
        image[2, 1] = new Rgba32(200, 30, 40, 1);
        image[5, 4] = new Rgba32(20, 100, 220, 128);
        await using var source = new MemoryStream();
        await image.SaveAsPngAsync(source);
        source.Position = 0;

        var result = await new ImageSharpArtworkImageProcessor().InspectAsync(source, TestContext.Current.CancellationToken);

        Assert.Equal(8, result.CanvasWidth);
        Assert.Equal(6, result.CanvasHeight);
        Assert.Equal(new ArtworkPixelRectangle(2, 1, 4, 4), result.VisibleBounds);
    }

    [Fact]
    public async Task InspectAsync_OpaqueCanvasUsesFullBoundsAndTransparentCanvasHasNoBounds()
    {
        using var opaque = new Image<Rgba32>(3, 2, Color.White);
        await using var opaqueSource = new MemoryStream();
        await opaque.SaveAsPngAsync(opaqueSource);
        opaqueSource.Position = 0;
        var processor = new ImageSharpArtworkImageProcessor();

        var opaqueResult = await processor.InspectAsync(opaqueSource, TestContext.Current.CancellationToken);

        Assert.Equal(new ArtworkPixelRectangle(0, 0, 3, 2), opaqueResult.VisibleBounds);

        using var transparent = new Image<Rgba32>(3, 2, Color.Transparent);
        await using var transparentSource = new MemoryStream();
        await transparent.SaveAsPngAsync(transparentSource);
        transparentSource.Position = 0;
        var transparentResult = await processor.InspectAsync(transparentSource, TestContext.Current.CancellationToken);

        Assert.Null(transparentResult.VisibleBounds);
    }

    [Fact]
    public async Task TransformAsync_ResizesProportionallyMovesAndClipsOnOriginalCanvas()
    {
        using var image = new Image<Rgba32>(6, 6, Color.Transparent);
        image[1, 1] = new Rgba32(255, 0, 0, 255);
        image[2, 1] = new Rgba32(255, 0, 0, 255);
        image[1, 2] = new Rgba32(255, 0, 0, 255);
        image[2, 2] = new Rgba32(255, 0, 0, 255);
        await using var source = new MemoryStream();
        await image.SaveAsPngAsync(source);
        source.Position = 0;

        var bytes = await new ImageSharpArtworkImageProcessor().TransformAsync(
            source,
            new ArtworkPixelRectangle(1, 1, 2, 2),
            new ArtworkPixelRectangle(-1, 2, 4, 4),
            TestContext.Current.CancellationToken);

        await using var outputStream = new MemoryStream(bytes);
        using var output = await Image.LoadAsync<Rgba32>(outputStream, TestContext.Current.CancellationToken);
        Assert.Equal(6, output.Width);
        Assert.Equal(6, output.Height);
        Assert.Equal(0, output[5, 5].A);
        Assert.True(output[0, 3].A > 0);
        Assert.Equal(0, output[3, 3].A);
    }
}
