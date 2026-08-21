using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed class BulkSizeChoiceViewModel(OfferingOptionValue value) : SelectableCatalogRecord(value.Value)
{
    public OfferingOptionValue Value { get; } = value;
}
