using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.Tests.ContentRisk;

public sealed class ContentRiskWarningProjectionTests
{
    [Fact]
    public void ProjectionKeepsDefaultWarningForMissingOrUnavailableReview()
    {
        var missing = ContentRiskWarningProjection.For([]);
        var unavailable = ContentRiskWarningProjection.For([
            ContentRiskReview.ReviewUnavailable(Target, "fingerprint")]);

        Assert.True(missing.IsVisible);
        Assert.Contains("not legal clearance", missing.Summary);
        Assert.Contains("needs an advisory review", missing.Details);
        Assert.Contains("could not be completed", unavailable.Details);
    }

    [Fact]
    public void ProjectionIncludesFindingEvidenceAndBoundedProvenance()
    {
        var checkedAt = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var review = ContentRiskReview.PotentialRisk(
            Target,
            "fingerprint",
            [new(ContentRiskCategory.IpRisk, ContentRiskSeverity.Medium, "Possible brand reference.", "brand-like wording")],
            new ContentRiskReviewProvenance("fake-analyzer", checkedAt));

        var projection = ContentRiskWarningProjection.For([review]);

        Assert.Contains("IpRisk", projection.Details);
        Assert.Contains("brand-like wording", projection.Details);
        Assert.Contains("fake-analyzer", projection.Details);
        Assert.Contains("Checked:", projection.Details);
    }

    private static ContentRiskReviewTarget Target { get; } = new(
        Guid.NewGuid(), ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");
}
