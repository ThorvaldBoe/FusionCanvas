namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateColorVariant
{
    public MockupTemplateColorVariant(Guid id, Guid mockupTemplateId, Guid colorOptionValueId, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, Guid? sourceAssetId = null)
    {
        Id = Require(id, nameof(id));
        MockupTemplateId = Require(mockupTemplateId, nameof(mockupTemplateId));
        ColorOptionValueId = Require(colorOptionValueId, nameof(colorOptionValueId));
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        SourceAssetId = sourceAssetId == Guid.Empty ? throw new ArgumentException("Asset identifier must not be empty.", nameof(sourceAssetId)) : sourceAssetId;
    }

    public Guid Id { get; init; }
    public Guid MockupTemplateId { get; init; }
    public Guid ColorOptionValueId { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid? SourceAssetId { get; init; }

    private static Guid Require(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
