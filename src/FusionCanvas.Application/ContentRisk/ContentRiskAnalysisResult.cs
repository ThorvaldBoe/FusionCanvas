using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.ContentRisk;

public sealed record ContentRiskAnalysisResult(
    bool Succeeded,
    IReadOnlyList<ContentRiskFinding> Findings,
    string? AnalyzerId,
    long? InputUnits,
    decimal? ReportedCost,
    ContentRiskAnalyzerFailureKind? FailureKind,
    string? Message)
{
    public static ContentRiskAnalysisResult Success(
        IReadOnlyList<ContentRiskFinding> findings,
        string analyzerId,
        long? inputUnits = null,
        decimal? reportedCost = null)
    {
        ArgumentNullException.ThrowIfNull(findings);
        if (string.IsNullOrWhiteSpace(analyzerId))
        {
            throw new ArgumentException("An analyzer identity is required.", nameof(analyzerId));
        }

        return new(true, findings.ToArray(), analyzerId.Trim(), inputUnits, reportedCost, null, null);
    }

    public static ContentRiskAnalysisResult Unavailable(
        ContentRiskAnalyzerFailureKind failureKind,
        string message) =>
        new(false, [], null, null, null, failureKind, message);
}
