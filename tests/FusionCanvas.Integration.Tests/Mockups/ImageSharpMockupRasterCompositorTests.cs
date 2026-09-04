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
        using var template = new Image<Rgba32>(100, 80, Color.DarkBlue);
        using var design = new Image<Rgba32>(20, 10, Color.White);
        await using var templateStream = new MemoryStream();
        await using var designStream = new MemoryStream();
        await template.SaveAsPngAsync(templateStream);
        await design.SaveAsPngAsync(designStream);
        templateStream.Position = 0;
        designStream.Position = 0;

        var compositor = new ImageSharpMockupRasterCompositor();
        await using var result = await compositor.ComposeAsync(
            templateStream,
            designStream,
            new MockupImageSpaceMapping(100, 80, 10, 10, 40, 20));

        using var output = await Image.LoadAsync<Rgba32>(result);
        Assert.Equal(100, output.Width);
        Assert.Equal(80, output.Height);
        Assert.Equal(new Rgba32(255, 255, 255, 255), output[20, 15]);
    }
}
