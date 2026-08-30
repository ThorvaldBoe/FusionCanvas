using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateFocusedMockupTemplateRequest(OfferingContext Context, string Name, string? ProviderMockupReference = null, Guid? TargetDesignAreaId = null, IReadOnlyList<Guid>? ColorOptionValueIds = null, MockupImageSpaceMapping? ImageMapping = null, string? Description = null);
