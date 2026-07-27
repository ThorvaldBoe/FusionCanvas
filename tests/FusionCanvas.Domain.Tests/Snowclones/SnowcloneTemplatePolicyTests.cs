using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.Domain.Tests.Snowclones;

public sealed class SnowcloneTemplatePolicyTests
{
    [Theory]
    [InlineData("Easily distracted by {X}")]
    [InlineData("You are the {person} to my {counterpart}")]
    [InlineData("{X} and {X} forever")]
    [InlineData("The { thing with spaces } of {Y}")]
    public void Validate_AcceptsSupportedPlaceholderForms(string phrase)
    {
        var result = SnowcloneTemplatePolicy.Validate(phrase, "Helpful guidance");

        Assert.True(result.IsValid);
        Assert.Equal(phrase, result.Phrase);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("No placeholder")]
    [InlineData("Empty {}")]
    [InlineData("Whitespace {   }")]
    [InlineData("Unmatched {X")]
    [InlineData("Unmatched X}")]
    [InlineData("Nested {{X}}")]
    [InlineData("Line {X}\nbreak")]
    [InlineData("Line {X}\rbreak")]
    public void Validate_RejectsInvalidPhraseStructure(string phrase)
    {
        var result = SnowcloneTemplatePolicy.Validate(phrase, "Guidance");

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_RejectsBlankGuidance(string guidance)
    {
        var result = SnowcloneTemplatePolicy.Validate("Valid {X}", guidance);

        Assert.False(result.IsValid);
        Assert.Equal("Guidance is required.", result.Error);
        Assert.Equal("Valid {X}", result.Phrase);
    }

    [Fact]
    public void Validate_TrimsOnlyOuterDisplayWhitespace()
    {
        var result = SnowcloneTemplatePolicy.Validate("  The  {Chosen One}  returns  ", "  Preserve detail.  ");

        Assert.True(result.IsValid);
        Assert.Equal("The  {Chosen One}  returns", result.Phrase);
        Assert.Equal("Preserve detail.", result.Guidance);
    }

    [Fact]
    public void Validate_ExposesDistinctCompleteNamedAndRepeatedTokens()
    {
        var result = SnowcloneTemplatePolicy.Validate(
            "My {Audience} knows {Product}, and {Audience} agrees",
            "Fill every placeholder.");

        Assert.True(result.IsValid);
        Assert.Equal(["{Audience}", "{Product}"], result.PlaceholderTokens);
    }

    [Theory]
    [InlineData(" Easily distracted by {X} ", "easily distracted by {x}")]
    [InlineData("The\t{Hero}\r\nreturns", "the {hero} returns")]
    [InlineData("A  {PERSON}   thing", "a {person} thing")]
    public void CreateDuplicateKey_CollapsesWhitespaceAndFoldsCase(string first, string second)
    {
        Assert.Equal(
            SnowcloneTemplatePolicy.CreateDuplicateKey(first),
            SnowcloneTemplatePolicy.CreateDuplicateKey(second));
    }

    [Fact]
    public void Snowclone_HasOnlyApprovedDataShape()
    {
        var createdAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        var updatedAt = createdAt.AddMinutes(1);
        var snowclone = new Snowclone(Guid.NewGuid(), "Phrase {X}", "Guidance", createdAt, updatedAt);

        Assert.Equal("Phrase {X}", snowclone.Phrase);
        Assert.Equal("Guidance", snowclone.Guidance);
        Assert.Equal(createdAt, snowclone.CreatedAt);
        Assert.Equal(updatedAt, snowclone.UpdatedAt);
    }
}
