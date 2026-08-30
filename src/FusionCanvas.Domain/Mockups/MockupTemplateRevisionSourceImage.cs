namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevisionSourceImage
{
    public MockupTemplateRevisionSourceImage(Guid id, Guid revisionId, Guid sourceAssetId, MockupImageSpaceMapping? imageMapping, int imageWidth = 0, int imageHeight = 0)
    {
        Id = Required(id, nameof(id));
        RevisionId = Required(revisionId, nameof(revisionId));
        SourceAssetId = Required(sourceAssetId, nameof(sourceAssetId));
        ImageMapping = imageMapping;
        ImageWidth = imageMapping?.ImageWidth ?? (imageWidth > 0 ? imageWidth : throw new ArgumentOutOfRangeException(nameof(imageWidth)));
        ImageHeight = imageMapping?.ImageHeight ?? (imageHeight > 0 ? imageHeight : throw new ArgumentOutOfRangeException(nameof(imageHeight)));
    }

    public Guid Id { get; init; }
    public Guid RevisionId { get; init; }
    public Guid SourceAssetId { get; init; }
    public MockupImageSpaceMapping? ImageMapping { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }

    private static Guid Required(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
