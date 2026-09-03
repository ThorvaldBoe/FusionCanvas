using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateReadinessResult(IReadOnlyList<MockupTemplateReadinessBlocker> Blockers)
{
    public MockupTemplateLifecycle Lifecycle => Blockers.Count == 0
        ? MockupTemplateLifecycle.ReadyForUse
        : MockupTemplateLifecycle.Draft;

    public bool IsReadyForUse => Lifecycle == MockupTemplateLifecycle.ReadyForUse;
}
