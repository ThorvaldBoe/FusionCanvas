namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevisionSourceImageOptionValue(Guid RevisionSourceImageId, Guid OptionValueId)
{
    public Guid RevisionSourceImageId { get; } = RevisionSourceImageId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(RevisionSourceImageId)) : RevisionSourceImageId;
    public Guid OptionValueId { get; } = OptionValueId == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(OptionValueId)) : OptionValueId;
}
