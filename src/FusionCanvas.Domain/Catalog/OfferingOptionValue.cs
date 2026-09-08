namespace FusionCanvas.Domain.Catalog;

public sealed record OfferingOptionValue
{
    public OfferingOptionValue(Guid id, Guid optionId, Guid offeringId, string value, int sortOrder, bool isArchived = false, string metadataJson = "{}")
    {
        Id = CatalogRecordValidation.Id(id, nameof(id));
        OptionId = CatalogRecordValidation.Id(optionId, nameof(optionId));
        OfferingId = CatalogRecordValidation.Id(offeringId, nameof(offeringId));
        Value = CatalogRecordValidation.Text(value, nameof(value));
        SortOrder = sortOrder;
        IsArchived = isArchived;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
    }

    public Guid Id { get; init; }
    public Guid OptionId { get; init; }
    public Guid OfferingId { get; init; }
    public string Value { get; init; }
    public int SortOrder { get; init; }
    public bool IsArchived { get; init; }
    public string MetadataJson { get; init; }
}
