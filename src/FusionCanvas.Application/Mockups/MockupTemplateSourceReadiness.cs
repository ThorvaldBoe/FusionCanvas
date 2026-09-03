using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupTemplateSourceReadiness(Guid VariantId, MockupTemplateSourceResolutionKind Kind, IReadOnlyList<Guid> SourceImageIds);
