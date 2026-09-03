using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Mockups;

public sealed record UpdateMockupTemplateRequest(Guid StoreId, Guid TemplateId, string? Name = null, string? Description = null, Guid? TargetPlaceholderId = null, string? PositionKey = null, bool ReplaceProviderImage = false, string? ProviderMockupReference = null, MockupImageSpaceMapping? ImageMapping = null, IReadOnlyList<Guid>? ReplaceColorOptionValueIds = null, bool ReplaceTargetPlaceholder = false);
