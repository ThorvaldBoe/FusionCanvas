using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Tests.Updates;

public sealed class UpdateProductVersionTests
{
    [Theory]
    [InlineData("0.3.0", false)]
    [InlineData("2.14.7-preview.1", true)]
    public void TryParse_AcceptsStableAndPreviewVersions(string value, bool isPreview)
    {
        Assert.True(UpdateProductVersion.TryParse(value, out var version));
        Assert.Equal(isPreview, version.IsPreview);
        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("v0.3.0")]
    [InlineData("0.3.0-preview.0")]
    [InlineData("0.3")]
    [InlineData("0.3.0-rc.1")]
    [InlineData("01.3.0")]
    public void TryParse_RejectsMalformedVersions(string value) => Assert.False(UpdateProductVersion.TryParse(value, out _));

    [Fact]
    public void CompareTo_OrdersPrereleasesBelowStableAndUsesSequenceNumber()
    {
        Assert.True(Parse("1.2.0-preview.1").CompareTo(Parse("1.2.0-preview.2")) < 0);
        Assert.True(Parse("1.2.0-preview.99").CompareTo(Parse("1.2.0")) < 0);
        Assert.True(Parse("1.2.1-preview.1").CompareTo(Parse("1.2.0")) > 0);
    }

    private static UpdateProductVersion Parse(string value)
    {
        Assert.True(UpdateProductVersion.TryParse(value, out var version));
        return version;
    }
}
