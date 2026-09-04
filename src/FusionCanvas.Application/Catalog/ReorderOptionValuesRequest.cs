namespace FusionCanvas.Application.Catalog;

public sealed record ReorderOptionValuesRequest(Guid StoreId, Guid OptionId, IReadOnlyList<Guid> OrderedValueIds);
