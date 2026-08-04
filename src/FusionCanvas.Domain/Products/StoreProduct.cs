namespace FusionCanvas.Domain.Products;

/// <summary>
/// A product blueprint: the underlying blank product, identified locally and
/// optionally carrying an external (provider) platform identity.
/// </summary>
public sealed record StoreProduct
{
    public StoreProduct(
        Guid id,
        Guid storeId,
        string name,
        string? description,
        string? externalProductId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        StoreId = ProductRecordValidation.RequireId(storeId, nameof(storeId));
        Name = ProductRecordValidation.RequireText(name, nameof(name));
        Description = ProductRecordValidation.NormalizeOptional(description);
        ExternalProductId = ProductRecordValidation.NormalizeOptional(externalProductId);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public string Name { get; init; }

    public string? Description { get; init; }

    public string? ExternalProductId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string MetadataJson { get; init; }
}
