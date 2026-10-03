using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.StageTools;

public sealed record MockupTemplateDiagnosticViewModel(
    string TemplateName,
    string Guidance,
    IReadOnlyList<MockupTemplateReadinessBlocker> Blockers);
