using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record UpdateLocalMockupTemplateSourceRequest(Guid StoreId, Guid TemplateId, Guid SourceImageId, IReadOnlyList<Guid> OptionValueIds, MockupImageSpaceMapping? ImageMapping = null, bool Archive = false);
