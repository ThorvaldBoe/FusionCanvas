namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevision
{
    public MockupTemplateRevision(Guid id, Guid mockupTemplateId, int revisionNumber, Guid targetPlaceholderId, DateTimeOffset createdAt, string? note = null, string? providerMockupReference = null, MockupImageSpaceMapping? imageMapping = null)
    {
        Id = Require(id, nameof(id));
        MockupTemplateId = Require(mockupTemplateId, nameof(mockupTemplateId));
        RevisionNumber = revisionNumber > 0 ? revisionNumber : throw new ArgumentOutOfRangeException(nameof(revisionNumber));
        TargetPlaceholderId = Require(targetPlaceholderId, nameof(targetPlaceholderId));
        CreatedAt = createdAt;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ProviderMockupReference = string.IsNullOrWhiteSpace(providerMockupReference) ? null : providerMockupReference.Trim();
        ImageMapping = imageMapping;
        if ((ProviderMockupReference is null) != (ImageMapping is null))
            throw new ArgumentException("A configured mockup image requires both a provider reference and an image-space mapping.");
    }

    public Guid Id { get; init; }
    public Guid MockupTemplateId { get; init; }
    public int RevisionNumber { get; init; }
    public Guid TargetPlaceholderId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Note { get; init; }
    public string? ProviderMockupReference { get; init; }
    public MockupImageSpaceMapping? ImageMapping { get; init; }

    private static Guid Require(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}
