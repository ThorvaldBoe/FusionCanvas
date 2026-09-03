using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record CreateMockupTemplateRequest(Guid StoreId, Guid OfferingId, string Name, Guid? TargetPlaceholderId = null, string? Description = null, string? PositionKey = null, string? ProviderMockupReference = null, MockupImageSpaceMapping? ImageMapping = null, IReadOnlyList<Guid>? ColorOptionValueIds = null);
