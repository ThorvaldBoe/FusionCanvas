using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class StableProductVersionTests
{
    [Theory]
    [InlineData("0.2.0", 0, 2, 0)]
    [InlineData("10.20.300", 10, 20, 300)]
    public void TryParse_AcceptsStrictThreePartVersions(string value, int major, int minor, int build)
    {
        Assert.True(StableProductVersion.TryParse(value, out var version));
        Assert.Equal(new StableProductVersion(major, minor, build), version);
    }

    [Theory]
    [InlineData("v0.2.0")]
    [InlineData("0.2")]
    [InlineData("0.2.0+abc")]
    [InlineData("0.2.0.1")]
    [InlineData("0.a.0")]
    public void TryParse_RejectsNonStableProductVersions(string value)
    {
        Assert.False(StableProductVersion.TryParse(value, out _));
    }

    [Fact]
    public void Comparison_OrdersMajorMinorAndBuild()
    {
        Assert.True(new StableProductVersion(0, 3, 0).CompareTo(new StableProductVersion(0, 2, 99)) > 0);
        Assert.True(new StableProductVersion(1, 0, 0).CompareTo(new StableProductVersion(0, 99, 99)) > 0);
        Assert.Equal(0, new StableProductVersion(1, 2, 3).CompareTo(new StableProductVersion(1, 2, 3)));
    }
}
