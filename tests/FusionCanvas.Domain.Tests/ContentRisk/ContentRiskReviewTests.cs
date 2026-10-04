using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Domain.Tests.ContentRisk;

public sealed class ContentRiskReviewTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset CheckedAt = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnreviewedReviewRetainsWarningAndFingerprint()
    {
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var fingerprint = ContentRiskFingerprint.ForText(target, "  Powered   by coffee ");

        var review = ContentRiskReview.Unreviewed(target, fingerprint);

        Assert.Equal(ContentRiskReviewState.Unreviewed, review.State);
        Assert.True(review.IsCurrent(fingerprint));
        Assert.Empty(review.Findings);
        Assert.Null(review.Provenance);
    }

    [Fact]
    public void SuccessfulNoSignalReviewDoesNotBecomeClearance()
    {
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");
        var provenance = new ContentRiskReviewProvenance("test-analyzer", CheckedAt);

        var review = ContentRiskReview.NoObviousSignalDetected(target, "abc", provenance);

        Assert.Equal(ContentRiskReviewState.NoObviousSignalDetected, review.State);
        Assert.Equal("test-analyzer", review.Provenance!.AnalyzerId);
    }

    [Fact]
    public void PotentialRiskSeparatesCategoryAndBoundsEvidence()
    {
        var finding = new ContentRiskFinding(
            ContentRiskCategory.IpRisk,
            ContentRiskSeverity.Medium,
            "Possible brand or character reference.",
            new string('x', 500));

        Assert.Equal(ContentRiskCategory.IpRisk, finding.Category);
        Assert.Equal(240, finding.Evidence!.Length);
    }

    [Fact]
    public void TextFingerprintNormalizesWhitespaceButIncludesRole()
    {
        var phrase = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var title = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");

        Assert.Equal(ContentRiskFingerprint.ForText(phrase, "A   phrase"), ContentRiskFingerprint.ForText(phrase, " A phrase "));
        Assert.NotEqual(ContentRiskFingerprint.ForText(phrase, "A phrase"), ContentRiskFingerprint.ForText(title, "A phrase"));
    }

    [Fact]
    public void ImageFingerprintChangesWhenBytesChange()
    {
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");

        Assert.NotEqual(ContentRiskFingerprint.ForImage(target, [1, 2, 3]), ContentRiskFingerprint.ForImage(target, [1, 2, 4]));
    }

    [Fact]
    public void ReviewUnavailableRemainsDistinctFromNoSignal()
    {
        var target = new ContentRiskReviewTarget(OwnerId, ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");

        var review = ContentRiskReview.ReviewUnavailable(target, "abc");

        Assert.Equal(ContentRiskReviewState.ReviewUnavailable, review.State);
        Assert.NotEqual(ContentRiskReviewState.NoObviousSignalDetected, review.State);
    }
}
