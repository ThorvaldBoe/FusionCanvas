namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateSourceImageOptionValue(Guid SourceImageId, Guid OptionValueId)
{
    public Guid SourceImageId { get; } = SourceImageId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(SourceImageId)) : SourceImageId;
    public Guid OptionValueId { get; } = OptionValueId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(OptionValueId)) : OptionValueId;
}
