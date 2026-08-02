using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.Domain.Tests.Concepts;

public sealed class DesignTriangleScoreTests
{
    [Fact]
    public void FromValues_AllEmpty_ReturnsZero()
    {
        var score = DesignTriangleScore.FromValues(null, null, null);
        Assert.Equal(0, score);
    }

    [Fact]
    public void FromValues_AllWhitespace_ReturnsZero()
    {
        var score = DesignTriangleScore.FromValues("   ", "\t", "");
        Assert.Equal(0, score);
    }

    [Fact]
    public void FromValues_AllSubstantive_ReturnsOneHundred()
    {
        var score = DesignTriangleScore.FromValues(
            "A warm sunset on the beach",
            "Live every moment",
            "Watercolor sunset tones");
        Assert.Equal(100, score);
    }

    [Fact]
    public void FromValues_OneShortCorner_GivesHalfCredit()
    {
        // "Short" has trimmed length 5 (< 8), so contributes 0.5 instead of 1.0
        var score = DesignTriangleScore.FromValues(
            "A long enough concept idea",
            "Short",
            "Also long enough for graphic");
        // (1.0 + 0.5 + 1.0) = 2.5 / 3 = 83.33 -> Round = 83
        Assert.Equal(83, score);
    }

    [Fact]
    public void FromValues_OneWhitespaceAndTwoSubstantive_ReturnsSixtySeven()
    {
        var score = DesignTriangleScore.FromValues(
            "A warm sunset on the beach",
            "",
            "Watercolor sunset tones");
        // (1.0 + 0 + 1.0) = 2.0 / 3 = 66.66 -> Round = 67
        Assert.Equal(67, score);
    }

    [Fact]
    public void FromValues_OneShortAndTwoSubstantive_ReturnsEightyThree()
    {
        var score = DesignTriangleScore.FromValues(
            "A warm sunset on the beach",
            "Short",
            "Watercolor sunset tones");
        // (1.0 + 0.5 + 1.0) = 2.5 / 3 = 83.33 -> Round = 83
        Assert.Equal(83, score);
    }

    [Fact]
    public void FromValues_Monotonic_GrowsAsCornersGainContent()
    {
        var allEmpty = DesignTriangleScore.FromValues(null, null, null);
        var oneSubstantive = DesignTriangleScore.FromValues("A warm sunset on the beach", null, null);
        var twoSubstantive = DesignTriangleScore.FromValues("A warm sunset on the beach", "Live every moment", null);
        var threeSubstantive = DesignTriangleScore.FromValues(
            "A warm sunset on the beach",
            "Live every moment",
            "Watercolor sunset tones");

        Assert.True(oneSubstantive >= allEmpty);
        Assert.True(twoSubstantive >= oneSubstantive);
        Assert.True(threeSubstantive >= twoSubstantive);
    }
}