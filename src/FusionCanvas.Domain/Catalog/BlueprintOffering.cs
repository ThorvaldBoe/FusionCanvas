namespace FusionCanvas.Domain.Catalog;

public enum BlueprintOfferingKind
{
    FixedPrintProvider = 0,
    ProviderNetwork = 1
}

public sealed record BlueprintOffering
{
    public BlueprintOffering(Guid id, Guid blueprintId, Guid storeId, string name, string? description, BlueprintOfferingKind kind, Guid? printProviderId, string? providerNetworkCode, Guid? defaultPlaceholderId, string? externalOfferingId, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        BlueprintId = CatalogRecordValidation.Id(blueprintId, nameof(blueprintId));
        StoreId = CatalogRecordValidation.Id(storeId, nameof(storeId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        Description = CatalogRecordValidation.Optional(description);
        Kind = Enum.IsDefined(kind) ? kind : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Offering kind is not supported.");
        PrintProviderId = printProviderId == Guid.Empty ? throw new ArgumentException("Provider identifier must not be empty.", nameof(printProviderId)) : printProviderId;
        ProviderNetworkCode = CatalogRecordValidation.Optional(providerNetworkCode)?.ToLowerInvariant();
        DefaultPlaceholderId = defaultPlaceholderId == Guid.Empty ? throw new ArgumentException("Placeholder identifier must not be empty.", nameof(defaultPlaceholderId)) : defaultPlaceholderId;
        ExternalOfferingId = CatalogRecordValidation.Optional(externalOfferingId);
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid BlueprintId { get; init; }
    public Guid StoreId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public BlueprintOfferingKind Kind { get; init; }
    public Guid? PrintProviderId { get; init; }
    public string? ProviderNetworkCode { get; init; }
    public Guid? DefaultPlaceholderId { get; init; }
    public string? ExternalOfferingId { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }

    public bool IsProviderNetwork => Kind == BlueprintOfferingKind.ProviderNetwork;
}
