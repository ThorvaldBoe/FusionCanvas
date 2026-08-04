namespace FusionCanvas.Domain.Products;

/// <summary>
/// A fulfillment offering joins a product blueprint to either a named fixed
/// provider or the Printify Choice network, and owns provider-specific facts.
/// </summary>
public sealed record FulfillmentOffering
{
    public FulfillmentOffering(
        Guid id,
        Guid storeProductId,
        string name,
        string? description,
        FulfillmentKind kind,
        string? providerName,
        string? externalOfferingId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        StoreProductId = ProductRecordValidation.RequireId(storeProductId, nameof(storeProductId));
        Name = ProductRecordValidation.RequireText(name, nameof(name));
        Description = ProductRecordValidation.NormalizeOptional(description);
        Kind = Enum.IsDefined(kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Fulfillment kind is not supported.");
        ExternalOfferingId = ProductRecordValidation.NormalizeOptional(externalOfferingId);

        providerName = ProductRecordValidation.NormalizeOptional(providerName);
        if (Kind == FulfillmentKind.FixedProvider && providerName is null)
        {
            throw new ArgumentException("A fixed-provider offering requires a provider name.", nameof(providerName));
        }

        if (Kind == FulfillmentKind.PrintifyChoiceNetwork && providerName is not null)
        {
            throw new ArgumentException("A Printify Choice offering must not specify a fixed provider name.", nameof(providerName));
        }

        ProviderName = providerName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }

    public Guid StoreProductId { get; init; }

    public string Name { get; init; }

    public string? Description { get; init; }

    public FulfillmentKind Kind { get; init; }

    /// <summary>Present only for <see cref="FulfillmentKind.FixedProvider"/>.</summary>
    public string? ProviderName { get; init; }

    public string? ExternalOfferingId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string MetadataJson { get; init; }
}
