namespace FusionCanvas.Domain.Catalog;

public sealed record PrintProvider
{
    public PrintProvider(Guid id, Guid storeId, string name, string? externalProviderId, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        StoreId = CatalogRecordValidation.Id(storeId, nameof(storeId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        ExternalProviderId = CatalogRecordValidation.Optional(externalProviderId);
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public string Name { get; init; }
    public string? ExternalProviderId { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }
}
