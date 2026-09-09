namespace FusionCanvas.Domain.Catalog;

public sealed record OfferingOption
{
    public OfferingOption(Guid id, Guid offeringId, OptionKind optionKind, string name, int sortOrder, bool isArchived = false, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        OfferingId = CatalogRecordValidation.Id(offeringId, nameof(offeringId));
        OptionKind = Enum.IsDefined(optionKind) ? optionKind : throw new ArgumentOutOfRangeException(nameof(optionKind), optionKind, "Option kind is not supported.");
        Name = CatalogRecordValidation.Text(name, nameof(name));
        SortOrder = sortOrder;
        IsArchived = isArchived;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid OfferingId { get; init; }
    public OptionKind OptionKind { get; init; }
    public string Name { get; init; }
    public int SortOrder { get; init; }
    public bool IsArchived { get; init; }
    public string MetadataJson { get; init; }
}
