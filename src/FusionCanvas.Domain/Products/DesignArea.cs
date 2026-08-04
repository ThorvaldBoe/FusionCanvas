namespace FusionCanvas.Domain.Products;

/// <summary>
/// A printable area within an offering, defined by its position, decoration
/// method, positive pixel dimensions, and an explicit set of applicable
/// variants. An area with no variant restriction applies to all offering variants.
/// </summary>
public sealed record DesignArea
{
    private readonly Guid[] _variantIds;

    public DesignArea(
        Guid id,
        Guid fulfillmentOfferingId,
        string name,
        string? description,
        string position,
        string decorationMethod,
        int width,
        int height,
        IReadOnlyList<Guid>? variantIds,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        FulfillmentOfferingId = ProductRecordValidation.RequireId(fulfillmentOfferingId, nameof(fulfillmentOfferingId));
        Name = ProductRecordValidation.RequireText(name, nameof(name));
        Description = ProductRecordValidation.NormalizeOptional(description);
        Position = ProductRecordValidation.RequireText(position, nameof(position));
        DecorationMethod = ProductRecordValidation.RequireText(decorationMethod, nameof(decorationMethod));
        Width = width > 0
            ? width
            : throw new ArgumentOutOfRangeException(nameof(width), width, "Design area width must be positive.");
        Height = height > 0
            ? height
            : throw new ArgumentOutOfRangeException(nameof(height), height, "Design area height must be positive.");
        _variantIds = variantIds?.Distinct().ToArray() ?? [];
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }

    public Guid FulfillmentOfferingId { get; init; }

    public string Name { get; init; }

    public string? Description { get; init; }

    public string Position { get; init; }

    public string DecorationMethod { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public IReadOnlyList<Guid> VariantIds => _variantIds;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string MetadataJson { get; init; }

    public bool Equals(DesignArea? other)
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
            && Name == other.Name
            && Description == other.Description
            && Position == other.Position
            && DecorationMethod == other.DecorationMethod
            && Width == other.Width
            && Height == other.Height
            && CreatedAt == other.CreatedAt
            && UpdatedAt == other.UpdatedAt
            && MetadataJson == other.MetadataJson
            && _variantIds.SequenceEqual(other._variantIds);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(FulfillmentOfferingId);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Position);
        hash.Add(DecorationMethod);
        hash.Add(Width);
        hash.Add(Height);
        hash.Add(CreatedAt);
        hash.Add(UpdatedAt);
        hash.Add(MetadataJson);
        foreach (var variantId in _variantIds)
        {
            hash.Add(variantId);
        }

        return hash.ToHashCode();
    }
}
