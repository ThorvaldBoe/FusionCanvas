namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateSourceResolution(Guid VariantId, MockupTemplateSourceResolutionKind Kind, IReadOnlyList<Guid> SourceImageIds)
{
    public Guid VariantId { get; } = VariantId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(VariantId)) : VariantId;
    public IReadOnlyList<Guid> SourceImageIds { get; } = SourceImageIds ?? throw new ArgumentNullException(nameof(SourceImageIds));
}
