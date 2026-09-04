namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplate
{
    public MockupTemplate(Guid id, Guid blueprintOfferingId, Guid? targetPlaceholderId, string name, string? description, int currentRevision, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string? positionKey = null, string? futureAssetState = null, string metadataJson = "{}")
    {
        Id = Require(id, nameof(id));
        BlueprintOfferingId = Require(blueprintOfferingId, nameof(blueprintOfferingId));
        TargetPlaceholderId = OptionalId(targetPlaceholderId, nameof(targetPlaceholderId));
        Name = Text(name, nameof(name));
        Description = Optional(description);
        CurrentRevision = currentRevision > 0 ? currentRevision : throw new ArgumentOutOfRangeException(nameof(currentRevision));
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        PositionKey = Optional(positionKey);
        FutureAssetState = Optional(futureAssetState);
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid BlueprintOfferingId { get; init; }
    public Guid? TargetPlaceholderId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public int CurrentRevision { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? PositionKey { get; init; }
    public string? FutureAssetState { get; init; }
    public string MetadataJson { get; init; }

    private static Guid Require(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
    private static Guid? OptionalId(Guid? value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;
    private static string Text(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
