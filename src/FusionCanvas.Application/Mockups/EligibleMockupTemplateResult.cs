using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record EligibleMockupTemplateResult(
    bool Succeeded,
    string? Error,
    IReadOnlyList<MockupTemplate> Templates,
    IReadOnlyList<MockupTemplateReadinessBlocker> Blockers,
    IReadOnlyList<MockupTemplateEligibilityDiagnostic>? Diagnostics = null)
{
    public IReadOnlyList<MockupTemplateEligibilityDiagnostic> CandidateDiagnostics => Diagnostics ?? [];
}
