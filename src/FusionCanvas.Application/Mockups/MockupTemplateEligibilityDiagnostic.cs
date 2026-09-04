using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateEligibilityDiagnostic(
    Guid TemplateId,
    string TemplateName,
    IReadOnlyList<MockupTemplateReadinessBlocker> Blockers);
