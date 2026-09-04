using FusionCanvas.Domain.Mockups;
namespace FusionCanvas.Application.Mockups;
public sealed record MockupGenerationState(
    Guid ItemId,
    Guid? OfferingId,
    bool IsReadOnly,
    string ReadOnlyReason,
    IReadOnlyList<MockupTemplate> Templates,
    Guid? SelectedTemplateId,
    IReadOnlyList<MockupGenerationOutput> Outputs,
    IReadOnlyList<string> SelectedColors,
    string? BlockedReason,
    string? Error,
    IReadOnlyList<MockupTemplateEligibilityDiagnostic>? TemplateDiagnostics = null)
{
    public IReadOnlyList<MockupTemplateEligibilityDiagnostic> CandidateDiagnostics => TemplateDiagnostics ?? [];
}
