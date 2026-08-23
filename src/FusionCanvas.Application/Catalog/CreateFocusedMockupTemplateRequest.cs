using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateFocusedMockupTemplateRequest(OfferingContext Context, string Name, string ProviderMockupReference, Guid TargetDesignAreaId, IReadOnlyList<Guid> ColorOptionValueIds, MockupImageSpaceMapping ImageMapping, string? Description = null);
