namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateSourceImage
{
    public MockupTemplateSourceImage(Guid id, Guid mockupTemplateId, Guid sourceAssetId, MockupImageSpaceMapping? imageMapping, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, int imageWidth = 0, int imageHeight = 0)
    {
        Id = Required(id, nameof(id));
        MockupTemplateId = Required(mockupTemplateId, nameof(mockupTemplateId));
        SourceAssetId = Required(sourceAssetId, nameof(sourceAssetId));
        ImageMapping = imageMapping;
        ImageWidth = imageMapping?.ImageWidth ?? (imageWidth > 0 ? imageWidth : throw new ArgumentOutOfRangeException(nameof(imageWidth)));
        ImageHeight = imageMapping?.ImageHeight ?? (imageHeight > 0 ? imageHeight : throw new ArgumentOutOfRangeException(nameof(imageHeight)));
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; init; }
    public Guid MockupTemplateId { get; init; }
    public Guid SourceAssetId { get; init; }
    public MockupImageSpaceMapping? ImageMapping { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    private static Guid Required(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
