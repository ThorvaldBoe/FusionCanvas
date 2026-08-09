namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateRevision
{
    public MockupTemplateRevision(Guid id, Guid mockupTemplateId, int revisionNumber, Guid targetPlaceholderId, DateTimeOffset createdAt, string? note = null)
    {
        Id = Require(id, nameof(id));
        MockupTemplateId = Require(mockupTemplateId, nameof(mockupTemplateId));
        RevisionNumber = revisionNumber > 0 ? revisionNumber : throw new ArgumentOutOfRangeException(nameof(revisionNumber));
        TargetPlaceholderId = Require(targetPlaceholderId, nameof(targetPlaceholderId));
        CreatedAt = createdAt;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid Id { get; init; }
    public Guid MockupTemplateId { get; init; }
    public int RevisionNumber { get; init; }
    public Guid TargetPlaceholderId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Note { get; init; }

    private static Guid Require(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
}

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

public static class MockupTemplatePolicy
{
    public static bool IsOutputAffectingChange(Guid oldPlaceholderId, Guid newPlaceholderId, IReadOnlySet<Guid> oldColors, IReadOnlySet<Guid> newColors) =>
        oldPlaceholderId != newPlaceholderId || !oldColors.SetEquals(newColors);

    public static void EnsureUniqueActiveColor(IEnumerable<MockupTemplateColorVariant> bindings)
    {
        var duplicate = bindings.Where(binding => !binding.IsArchived)
            .GroupBy(binding => (binding.MockupTemplateId, binding.ColorOptionValueId))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException("Only one active template-color record is allowed per template and Color Option Value.");
    }
}
