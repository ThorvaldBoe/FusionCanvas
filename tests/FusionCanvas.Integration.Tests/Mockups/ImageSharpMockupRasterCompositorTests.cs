using FusionCanvas.Domain.Mockups;
using FusionCanvas.Integration.Mockups;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FusionCanvas.Integration.Tests.Mockups;

public sealed class ImageSharpMockupRasterCompositorTests
{
    [Fact]
    public async Task ComposeAsync_PreservesTemplateDimensionsAndFitsDesignInMapping()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var template = new Image<Rgba32>(100, 80, Color.DarkBlue);
        using var design = new Image<Rgba32>(20, 10, Color.White);
        await using var templateStream = new MemoryStream();
        await using var designStream = new MemoryStream();
        await template.SaveAsPngAsync(templateStream, cancellationToken);
        await design.SaveAsPngAsync(designStream, cancellationToken);
        templateStream.Position = 0;
        designStream.Position = 0;

        var compositor = new ImageSharpMockupRasterCompositor();
        await using var result = await compositor.ComposeAsync(
            templateStream,
            designStream,
            new MockupImageSpaceMapping(100, 80, 10, 10, 40, 20),
            cancellationToken);

        using var output = await Image.LoadAsync<Rgba32>(result, cancellationToken);
        Assert.Equal(100, output.Width);
        Assert.Equal(80, output.Height);
        Assert.Equal(new Rgba32(255, 255, 255, 255), output[20, 15]);
    }

    [Fact]
    public async Task ComposeAsync_RejectsTemplateThatExceedsDecodedPixelLimit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var template = new Image<Rgba32>(3, 2, Color.DarkBlue);
        using var design = new Image<Rgba32>(1, 1, Color.White);
        await using var templateStream = new MemoryStream();
        await using var designStream = new MemoryStream();
        await template.SaveAsPngAsync(templateStream, cancellationToken);
        await design.SaveAsPngAsync(designStream, cancellationToken);
        templateStream.Position = 0;
        designStream.Position = 0;

        var compositor = new ImageSharpMockupRasterCompositor(maximumDecodedPixels: 5);

        await Assert.ThrowsAsync<InvalidDataException>(() => compositor.ComposeAsync(
            templateStream,
            designStream,
            new MockupImageSpaceMapping(3, 2, 0, 0, 1, 1),
            cancellationToken));
    }

    [Fact]
    public async Task ComposeAsync_RejectsDesignThatExceedsDecodedPixelLimit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var template = new Image<Rgba32>(2, 2, Color.DarkBlue);
        using var design = new Image<Rgba32>(3, 2, Color.White);
        await using var templateStream = new MemoryStream();
        await using var designStream = new MemoryStream();
        await template.SaveAsPngAsync(templateStream, cancellationToken);
        await design.SaveAsPngAsync(designStream, cancellationToken);
        templateStream.Position = 0;
        designStream.Position = 0;

        var compositor = new ImageSharpMockupRasterCompositor(maximumDecodedPixels: 5);

        await Assert.ThrowsAsync<InvalidDataException>(() => compositor.ComposeAsync(
            templateStream,
            designStream,
            new MockupImageSpaceMapping(2, 2, 0, 0, 1, 1),
            cancellationToken));
    }
}
