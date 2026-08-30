namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevisionSourceImage
{
    public MockupTemplateRevisionSourceImage(Guid id, Guid revisionId, Guid sourceAssetId, MockupImageSpaceMapping imageMapping)
    {
        Id = Required(id, nameof(id));
        RevisionId = Required(revisionId, nameof(revisionId));
        SourceAssetId = Required(sourceAssetId, nameof(sourceAssetId));
        ImageMapping = imageMapping ?? throw new ArgumentNullException(nameof(imageMapping));
    }

    public Guid Id { get; init; }
    public Guid RevisionId { get; init; }
    public Guid SourceAssetId { get; init; }
    public MockupImageSpaceMapping ImageMapping { get; init; }

    private static Guid Required(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
