using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.StageTools;

public sealed record MockupTemplateDiagnosticViewModel(
    string TemplateName,
    string Guidance,
    IReadOnlyList<MockupTemplateReadinessBlocker> Blockers,
    MockupTemplateCoveragePlan? CoveragePlan = null)
{
    public string CoverageSummary => CoveragePlan?.Summary ?? string.Empty;
    public bool HasCoverageSummary => CoveragePlan is not null;
    public string CoverageAffectedVariantGuidance => CoveragePlan is null
        ? string.Empty
        : string.Join(Environment.NewLine, CoveragePlan.Requirements
            .Where(value => value.Status != MockupTemplateCoverageStatus.Resolved)
            .Select(value => $"{value.VariantSummary}: {value.Explanation}"));
    public bool HasCoverageAffectedVariantGuidance => !string.IsNullOrWhiteSpace(CoverageAffectedVariantGuidance);
}
