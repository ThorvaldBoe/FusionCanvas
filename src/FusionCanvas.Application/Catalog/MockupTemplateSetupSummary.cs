using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public sealed record MockupTemplateSetupSummary(Guid Id, string Name, Guid? TargetDesignAreaId, string? TargetDesignAreaName, IReadOnlyList<Guid> ColorOptionValueIds, IReadOnlyList<Guid> CompatibleVariantIds, string? ProviderMockupReference, int CurrentRevision, bool IsArchived, MockupTemplateLifecycle Lifecycle = MockupTemplateLifecycle.Draft, IReadOnlyList<MockupTemplateReadinessBlocker>? ReadinessBlockers = null);
