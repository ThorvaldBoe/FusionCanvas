namespace FusionCanvas.Domain.Catalog;

public sealed record OfferingPlaceholder
{
    private readonly Guid[] _variantIds;

    public OfferingPlaceholder(Guid id, Guid offeringId, string name, string? description, string position, string decorationMethod, int width, int height, IReadOnlyList<Guid> variantIds, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}", string? providerReference = null, DesignAreaArtworkGuidance? artworkGuidance = null)
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        OfferingId = CatalogRecordValidation.Id(offeringId, nameof(offeringId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        Description = CatalogRecordValidation.Optional(description);
        Position = CatalogRecordValidation.Text(position, nameof(position));
        DecorationMethod = CatalogRecordValidation.Text(decorationMethod, nameof(decorationMethod));
        Width = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width), width, "Placeholder width must be positive.");
        Height = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height), height, "Placeholder height must be positive.");
        _variantIds = variantIds?.Where(value => value != Guid.Empty).Distinct().ToArray() ?? throw new ArgumentNullException(nameof(variantIds));
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
        ProviderReference = CatalogRecordValidation.Optional(providerReference);
        ArtworkGuidance = artworkGuidance;
    }

    public Guid Id { get; init; }
    public Guid OfferingId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string Position { get; init; }
    public string DecorationMethod { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public IReadOnlyList<Guid> VariantIds => _variantIds;
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }
    public string? ProviderReference { get; init; }
    public DesignAreaArtworkGuidance? ArtworkGuidance { get; init; }

    public DesignAreaPhysicalSize? MaximumPhysicalSize => ArtworkGuidance?.PhysicalSizeFor(Width, Height);

    public bool Equals(OfferingPlaceholder? other) => other is not null
        && Id == other.Id && OfferingId == other.OfferingId && Name == other.Name && Description == other.Description
        && Position == other.Position && DecorationMethod == other.DecorationMethod && Width == other.Width && Height == other.Height
        && IsArchived == other.IsArchived && CreatedAt == other.CreatedAt && UpdatedAt == other.UpdatedAt
        && MetadataJson == other.MetadataJson && ProviderReference == other.ProviderReference
        && ArtworkGuidance == other.ArtworkGuidance && _variantIds.SequenceEqual(other._variantIds);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id); hash.Add(OfferingId); hash.Add(Name); hash.Add(Description); hash.Add(Position); hash.Add(DecorationMethod); hash.Add(Width); hash.Add(Height); hash.Add(IsArchived); hash.Add(CreatedAt); hash.Add(UpdatedAt); hash.Add(MetadataJson); hash.Add(ProviderReference); hash.Add(ArtworkGuidance);
        foreach (var value in _variantIds) hash.Add(value);
        return hash.ToHashCode();
    }
}
