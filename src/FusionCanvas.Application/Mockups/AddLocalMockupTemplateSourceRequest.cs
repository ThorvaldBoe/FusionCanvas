using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record AddLocalMockupTemplateSourceRequest(Guid StoreId, Guid TemplateId, string SourcePath, IReadOnlyList<Guid> OptionValueIds, MockupImageSpaceMapping? ImageMapping = null);
