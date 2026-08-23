namespace FusionCanvas.Domain.Catalog;

public sealed record Blueprint
{
    public Blueprint(Guid id, Guid storeId, string name, string? description, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        StoreId = CatalogRecordValidation.Id(storeId, nameof(storeId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        Description = CatalogRecordValidation.Optional(description);
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }
}
