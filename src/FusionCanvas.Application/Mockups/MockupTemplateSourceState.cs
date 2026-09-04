using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSourceState(IReadOnlyList<MockupTemplateSourceImageSummary> Images, IReadOnlyList<MockupTemplateSourceReadiness> Readiness, bool IsReady, string? Error = null);
