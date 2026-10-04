namespace FusionCanvas.Domain.ContentRisk;

public sealed record ContentRiskReview
{
    private ContentRiskReview(
        ContentRiskReviewTarget target,
        string fingerprint,
        ContentRiskReviewState state,
        IReadOnlyList<ContentRiskFinding> findings,
        ContentRiskReviewProvenance? provenance)
    {
        Target = target;
        Fingerprint = fingerprint;
        State = state;
        Findings = findings;
        Provenance = provenance;
    }

    public ContentRiskReviewTarget Target { get; }

    public string Fingerprint { get; }

    public ContentRiskReviewState State { get; }

    public IReadOnlyList<ContentRiskFinding> Findings { get; }

    public ContentRiskReviewProvenance? Provenance { get; }

    public static ContentRiskReview Unreviewed(ContentRiskReviewTarget target, string fingerprint)
    {
        ValidateFingerprint(fingerprint);
        return new(target, fingerprint, ContentRiskReviewState.Unreviewed, [], null);
    }

    public static ContentRiskReview PotentialRisk(
        ContentRiskReviewTarget target,
        string fingerprint,
        IReadOnlyList<ContentRiskFinding> findings,
        ContentRiskReviewProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(provenance);
        ValidateFingerprint(fingerprint);
        if (findings.Count == 0)
        {
            throw new ArgumentException("Potential risk requires at least one finding.", nameof(findings));
        }

        return new(target, fingerprint, ContentRiskReviewState.PotentialRisk, findings.ToArray(), provenance);
    }

    public static ContentRiskReview NoObviousSignalDetected(
        ContentRiskReviewTarget target,
        string fingerprint,
        ContentRiskReviewProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        ValidateFingerprint(fingerprint);
        return new(target, fingerprint, ContentRiskReviewState.NoObviousSignalDetected, [], provenance);
    }

    public static ContentRiskReview ReviewUnavailable(
        ContentRiskReviewTarget target,
        string fingerprint,
        ContentRiskReviewProvenance? provenance = null)
    {
        ValidateFingerprint(fingerprint);
        return new(target, fingerprint, ContentRiskReviewState.ReviewUnavailable, [], provenance);
    }

    public bool IsCurrent(string fingerprint) =>
        string.Equals(Fingerprint, fingerprint, StringComparison.Ordinal);

    private static void ValidateFingerprint(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("A content fingerprint is required.", nameof(fingerprint));
        }
    }
}
