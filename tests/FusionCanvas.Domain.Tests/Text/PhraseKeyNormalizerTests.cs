using FusionCanvas.Domain.Text;

namespace FusionCanvas.Domain.Tests.Text;

public sealed class PhraseKeyNormalizerTests
{
    [Theory]
    [InlineData("A grumpy pug", "a grumpy PUG")]
    [InlineData("  A   grumpy\tpug  ", "A grumpy\npug")]
    [InlineData("Phrase {X}", "phrase {x}")]
    public void Normalize_CollapsesWhitespaceAndFoldsCase(string first, string second)
    {
        Assert.Equal(PhraseKeyNormalizer.Normalize(first), PhraseKeyNormalizer.Normalize(second));
    }

    [Fact]
    public void Normalize_ProducesEmptyKeyForWhitespaceOnlyPhrase()
    {
        Assert.Equal(string.Empty, PhraseKeyNormalizer.Normalize(" \t\r\n "));
    }
}
