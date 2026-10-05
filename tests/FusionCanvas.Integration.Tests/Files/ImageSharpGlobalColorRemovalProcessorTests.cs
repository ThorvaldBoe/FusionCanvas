using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Integration.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FusionCanvas.Integration.Tests.Files;

public sealed class ImageSharpGlobalColorRemovalProcessorTests
{
    [Fact]
    public async Task PreviewAndApply_MatchEveryVisiblePixelGlobally()
    {
        using var sourceImage = new Image<Rgba32>(6, 1);
        sourceImage[0, 0] = new Rgba32(0, 0, 0, 255);
        sourceImage[1, 0] = new Rgba32(255, 255, 255, 255);
        sourceImage[2, 0] = new Rgba32(4, 4, 4, 255);
        sourceImage[3, 0] = new Rgba32(255, 255, 255, 255);
        sourceImage[4, 0] = new Rgba32(0, 0, 0, 255);
        sourceImage[5, 0] = new Rgba32(0, 0, 0, 0);
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        var parameters = new GlobalColorRemovalParameters(new(0, 0, 0), 0.02);
        var processor = new ImageSharpGlobalColorRemovalProcessor();

        source.Position = 0;
        var preview = await processor.PreviewAsync(source, parameters, TestContext.Current.CancellationToken);

        Assert.Equal(3, preview.MatchedPixelCount);
        Assert.Equal(5, preview.VisiblePixelCount);
        Assert.True(preview.LeavesVisibleArtwork);

        source.Position = 0;
        var result = await processor.ApplyAsync(source, parameters, TestContext.Current.CancellationToken);

        await using var resultStream = new MemoryStream(result.Png);
        using var output = await Image.LoadAsync<Rgba32>(resultStream);
        Assert.Equal(0, output[0, 0].A);
        Assert.Equal(255, output[1, 0].A);
        Assert.Equal(0, output[2, 0].A);
        Assert.Equal(255, output[3, 0].A);
        Assert.Equal(0, output[4, 0].A);
        Assert.Equal(0, output[5, 0].A);
    }

    [Fact]
    public async Task SampleAsync_ReturnsVisibleColorAndIgnoresTransparentPixel()
    {
        using var sourceImage = new Image<Rgba32>(2, 1);
        sourceImage[0, 0] = new Rgba32(10, 20, 30, 255);
        sourceImage[1, 0] = new Rgba32(10, 20, 30, 0);
        await using var source = new MemoryStream();
        await sourceImage.SaveAsPngAsync(source);
        var processor = new ImageSharpGlobalColorRemovalProcessor();

        source.Position = 0;
        var visible = await processor.SampleAsync(source, 0, 0, TestContext.Current.CancellationToken);
        Assert.Equal(new GlobalColorRemovalColor(10, 20, 30), visible);

        source.Position = 0;
        var transparent = await processor.SampleAsync(source, 1, 0, TestContext.Current.CancellationToken);
        Assert.Null(transparent);
    }

    [Fact]
    public async Task PreviewAsync_RejectsMalformedRasterPayload()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => new ImageSharpGlobalColorRemovalProcessor().PreviewAsync(
            new MemoryStream([1, 2, 3]),
            new GlobalColorRemovalParameters(new(0, 0, 0), 0),
            TestContext.Current.CancellationToken));
    }
}
