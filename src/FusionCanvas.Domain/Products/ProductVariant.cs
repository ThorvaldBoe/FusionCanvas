namespace FusionCanvas.Domain.Products;

/// <summary>
/// A concrete available option combination offered on an offering.
/// </summary>
public sealed record ProductVariant
{
    private readonly VariantOption[] _options;

    public ProductVariant(
        Guid id,
        Guid fulfillmentOfferingId,
        IReadOnlyList<VariantOption> options,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        FulfillmentOfferingId = ProductRecordValidation.RequireId(fulfillmentOfferingId, nameof(fulfillmentOfferingId));
        if (options is null || options.Count == 0)
        {
            throw new ArgumentException("A product variant must define at least one option.", nameof(options));
        }

        _options = options.ToArray();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; init; }

    public Guid FulfillmentOfferingId { get; init; }

    public IReadOnlyList<VariantOption> Options => _options;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public bool Equals(ProductVariant? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id
            && FulfillmentOfferingId == other.FulfillmentOfferingId
            && CreatedAt == other.CreatedAt
            && UpdatedAt == other.UpdatedAt
            && _options.SequenceEqual(other._options);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(FulfillmentOfferingId);
        hash.Add(CreatedAt);
        hash.Add(UpdatedAt);
        foreach (var option in _options)
        {
            hash.Add(option);
        }

        return hash.ToHashCode();
    }
}
