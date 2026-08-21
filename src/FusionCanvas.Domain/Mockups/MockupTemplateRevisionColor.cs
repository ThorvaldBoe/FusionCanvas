namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevisionColor
{
    public MockupTemplateRevisionColor(Guid id, Guid revisionId, Guid colorOptionValueId, Guid? sourceAssetId = null)
    {
        Id = Require(id, nameof(id));
        RevisionId = Require(revisionId, nameof(revisionId));
        ColorOptionValueId = Require(colorOptionValueId, nameof(colorOptionValueId));
        SourceAssetId = sourceAssetId == Guid.Empty ? throw new ArgumentException("Asset identifier must not be empty.", nameof(sourceAssetId)) : sourceAssetId;
    }

    public Guid Id { get; init; }
    public Guid RevisionId { get; init; }
    public Guid ColorOptionValueId { get; init; }
    public Guid? SourceAssetId { get; init; }

    private static Guid Require(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
