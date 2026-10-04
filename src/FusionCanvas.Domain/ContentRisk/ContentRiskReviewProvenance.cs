namespace FusionCanvas.Domain.ContentRisk;

public sealed record ContentRiskReviewProvenance
{
    public ContentRiskReviewProvenance(
        string analyzerId,
        DateTimeOffset checkedAt,
        long? inputUnits = null,
        decimal? reportedCost = null)
    {
        if (string.IsNullOrWhiteSpace(analyzerId))
        {
            throw new ArgumentException("An analyzer identity is required.", nameof(analyzerId));
        }

        if (inputUnits is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputUnits));
        }

        if (reportedCost is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reportedCost));
        }

        AnalyzerId = analyzerId.Trim();
        CheckedAt = checkedAt;
        InputUnits = inputUnits;
        ReportedCost = reportedCost;
    }

    public string AnalyzerId { get; }

    public DateTimeOffset CheckedAt { get; }

    public long? InputUnits { get; }

    public decimal? ReportedCost { get; }
}
