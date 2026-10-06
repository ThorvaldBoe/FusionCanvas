using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Tests.Mockups;

public sealed class MockupOutputResolutionPolicyTests
{
    [Fact]
    public void Default_UsesTwoThousandPixels()
    {
        Assert.Equal(2000, MockupOutputResolutionPolicy.Default.MaximumLongEdgePixels);
    }

    [Fact]
    public void CalculateOutputDimensions_ScalesLongEdgeAndPreservesAspectRatio()
    {
        var policy = new MockupOutputResolutionPolicy(2000);

        Assert.Equal((2000, 1500), policy.CalculateOutputDimensions(4000, 3000));
        Assert.Equal((1500, 2000), policy.CalculateOutputDimensions(3000, 4000));
    }

    [Fact]
    public void CalculateOutputDimensions_DoesNotUpscale()
    {
        var policy = new MockupOutputResolutionPolicy(2000);

        Assert.Equal((1200, 800), policy.CalculateOutputDimensions(1200, 800));
    }

    [Fact]
    public void ScaleMapping_UsesScaledImageCoordinates()
    {
        var policy = new MockupOutputResolutionPolicy(2000);
        var mapping = new MockupImageSpaceMapping(4000, 3000, 1000, 600, 1600, 1200);

        var scaled = policy.ScaleMapping(mapping);

        Assert.Equal(new MockupImageSpaceMapping(2000, 1500, 500, 300, 800, 600), scaled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveValues(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MockupOutputResolutionPolicy(value));
    }
}
