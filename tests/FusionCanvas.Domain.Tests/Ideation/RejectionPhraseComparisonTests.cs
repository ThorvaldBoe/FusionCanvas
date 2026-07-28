using FusionCanvas.Domain.Ideation;

namespace FusionCanvas.Domain.Tests.Ideation;

public sealed class RejectionPhraseComparisonTests
{
    private static readonly DateTimeOffset CreatedAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z");

    private static IdeationRejection Rejection(
        Guid storeId, Guid nicheId, Guid? groupId, string text, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), storeId, nicheId, groupId, text, null, IdeationMode.Basic, CreatedAt);

    [Theory]
    [InlineData("A grumpy pug", "A grumpy pug")]
    [InlineData("  A grumpy pug  ", "A grumpy pug")]
    [InlineData("a grumpy PUG", "A Grumpy pug")]
    [InlineData("A   grumpy\tpug", "A grumpy\npug")]
    public void NormalizeKey_CollapsesWhitespaceAndFoldsCase(string first, string second)
    {
        Assert.Equal(
            RejectionPhraseComparison.NormalizeKey(first),
            RejectionPhraseComparison.NormalizeKey(second));
    }

    [Fact]
    public void NormalizeKey_ReturnsDistinctForDifferentPhrases()
    {
        Assert.NotEqual(
            RejectionPhraseComparison.NormalizeKey("Talk to me about pugs"),
            RejectionPhraseComparison.NormalizeKey("Talk to me about cats"));
    }

    [Fact]
    public void NormalizeKey_EmptyPhraseProducesEmptyKey()
    {
        Assert.Equal(string.Empty, RejectionPhraseComparison.NormalizeKey("   "));
    }

    [Fact]
    public void SameScope_CollidesWithinIdenticalScope()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, groupId, "Talk to me about pugs");
        var second = Rejection(storeId, nicheId, groupId, "TALK to me about pugs");

        Assert.True(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_CollidesWhenGroupIsNullForBoth()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, null, " Talk to me about pugs ");
        var second = Rejection(storeId, nicheId, null, "talk to me about pugs");

        Assert.True(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideAcrossDifferentStores()
    {
        var nicheId = Guid.NewGuid();

        var first = Rejection(Guid.NewGuid(), nicheId, null, "Talk to me about pugs");
        var second = Rejection(Guid.NewGuid(), nicheId, null, "Talk to me about pugs");

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideAcrossDifferentNiches()
    {
        var storeId = Guid.NewGuid();

        var first = Rejection(storeId, Guid.NewGuid(), null, "Talk to me about pugs");
        var second = Rejection(storeId, Guid.NewGuid(), null, "Talk to me about pugs");

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideWhenOneHasGroupAndOtherDoesNot()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, Guid.NewGuid(), "Talk to me about pugs");
        var second = Rejection(storeId, nicheId, null, "Talk to me about pugs");

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideAcrossDifferentGroups()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, Guid.NewGuid(), "Talk to me about pugs");
        var second = Rejection(storeId, nicheId, Guid.NewGuid(), "Talk to me about pugs");

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideAcrossDifferentPhrases()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, null, "Talk to me about pugs");
        var second = Rejection(storeId, nicheId, null, "Talk to me about cats");

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }

    [Fact]
    public void SameScope_DoesNotCollideWhenSameRecordIdentity()
    {
        var id = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();

        var first = Rejection(storeId, nicheId, null, "Talk to me about pugs", id);
        var second = Rejection(storeId, nicheId, null, "Talk to me about cats", id);

        Assert.False(RejectionPhraseComparison.IsWithinScopeDuplicate(first, second));
    }
}
