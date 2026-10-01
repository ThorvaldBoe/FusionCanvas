using FusionCanvas.Application.Ideation;

namespace FusionCanvas.Application.Tests.Ideation;

public sealed class IdeationCountPolicyTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void IsValid_AcceptsInclusiveBounds(int count)
    {
        Assert.True(IdeationCountPolicy.IsValid(count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void IsValid_RejectsValuesOutsideBounds(int count)
    {
        Assert.False(IdeationCountPolicy.IsValid(count));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("20", 20)]
    public void TryParse_ReturnsValidWholeNumbers(string text, int expected)
    {
        Assert.True(IdeationCountPolicy.TryParse(text, out var count));
        Assert.Equal(expected, count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("21")]
    public void TryParse_RejectsInvalidOrOutOfRangeText(string text)
    {
        Assert.False(IdeationCountPolicy.TryParse(text, out _));
    }
}
