using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public sealed record ContentRiskWarningProjection(
    bool IsVisible,
    string Summary,
    string Details)
{
    public static ContentRiskWarningProjection For(IReadOnlyList<ContentRiskReview>? reviews)
    {
        var current = reviews ?? [];
        if (current.Count == 0 || current.Any(review => review.State == ContentRiskReviewState.Unreviewed))
        {
            return new(
                true,
                "Check customer-visible content for possible IP infringement and harmful or inappropriate content. Automated review is advisory and is not legal clearance.",
                "This content still needs an advisory review. A missing review is not a negative result. Retry the review when an analyzer is available.");
        }

        if (current.Any(review => review.State == ContentRiskReviewState.ReviewUnavailable))
        {
            return new(
                true,
                "Check customer-visible content for possible IP infringement and harmful or inappropriate content. Automated review is advisory and is not legal clearance.",
                "The advisory review could not be completed. This is not a negative result; retry when the analyzer is available. The content remains usable.");
        }

        var findings = current.SelectMany(review => review.Findings).Count();
        return findings > 0
            ? new(
                true,
                "Check customer-visible content for possible IP infringement and harmful or inappropriate content. Automated review is advisory and is not legal clearance.",
                BuildFindingDetails(current, findings))
            : new(
                true,
                "Check customer-visible content for possible IP infringement and harmful or inappropriate content. Automated review is advisory and is not legal clearance.",
                $"No obvious signal was detected. This is not a clearance or approval; check the content yourself before publishing. {BuildProvenanceDetails(current)}");
    }

    private static string BuildFindingDetails(IReadOnlyList<ContentRiskReview> reviews, int findingCount)
    {
        var findings = reviews
            .SelectMany(review => review.Findings)
            .Select(finding => $"• {finding.Category} ({finding.Severity}): {finding.Explanation}"
                + (finding.Evidence is null ? string.Empty : $" Signal: {finding.Evidence}"));
        return $"{findingCount} advisory finding(s) need your review. Automated checks do not determine legal or marketplace compliance.\n{string.Join("\n", findings)}\n{BuildProvenanceDetails(reviews)}";
    }

    private static string BuildProvenanceDetails(IReadOnlyList<ContentRiskReview> reviews)
    {
        var provenance = reviews.Select(review => review.Provenance).FirstOrDefault(value => value is not null);
        return provenance is null
            ? string.Empty
            : $"Method: {provenance.AnalyzerId}. Checked: {provenance.CheckedAt.ToLocalTime():g}.";
    }
}
