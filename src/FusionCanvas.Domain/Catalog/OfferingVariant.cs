namespace FusionCanvas.Domain.Catalog;

public sealed record OfferingVariant
{
    private readonly Guid[] _optionValueIds;

    public OfferingVariant(Guid id, Guid offeringId, string name, IReadOnlyList<Guid> optionValueIds, bool isArchived, DateTimeOffset createdAt, DateTimeOffset updatedAt, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        OfferingId = CatalogRecordValidation.Id(offeringId, nameof(offeringId));
        Name = CatalogRecordValidation.Text(name, nameof(name));
        if (optionValueIds is null || optionValueIds.Count == 0 || optionValueIds.Any(value => value == Guid.Empty))
        {
            throw new ArgumentException("A concrete Variant must reference at least one Option Value.", nameof(optionValueIds));
        }

        _optionValueIds = optionValueIds.Distinct().ToArray();
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid OfferingId { get; init; }
    public string Name { get; init; }
    public IReadOnlyList<Guid> OptionValueIds => _optionValueIds;
    public bool IsArchived { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string MetadataJson { get; init; }

    public bool Equals(OfferingVariant? other) => other is not null
        && Id == other.Id && OfferingId == other.OfferingId && Name == other.Name
        && IsArchived == other.IsArchived && CreatedAt == other.CreatedAt && UpdatedAt == other.UpdatedAt
        && MetadataJson == other.MetadataJson && _optionValueIds.SequenceEqual(other._optionValueIds);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id); hash.Add(OfferingId); hash.Add(Name); hash.Add(IsArchived); hash.Add(CreatedAt); hash.Add(UpdatedAt); hash.Add(MetadataJson);
        foreach (var value in _optionValueIds) hash.Add(value);
        return hash.ToHashCode();
    }
}
