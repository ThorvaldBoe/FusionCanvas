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
}
