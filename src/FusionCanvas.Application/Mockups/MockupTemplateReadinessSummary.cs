using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateReadinessSummary(Guid TemplateId, MockupTemplateLifecycle Lifecycle, IReadOnlyList<MockupTemplateReadinessBlocker> Blockers);
