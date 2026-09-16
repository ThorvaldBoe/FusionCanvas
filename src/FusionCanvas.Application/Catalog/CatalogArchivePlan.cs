namespace FusionCanvas.Application.Catalog;

public sealed record CatalogArchivePlan(
    Guid StoreId,
    Guid OfferingId,
    string OfferingName,
    IReadOnlyList<CatalogArchiveDependency> CatalogDependents,
    IReadOnlyList<CatalogArchiveDependency> ExternalBlockers)
{
    public bool CanConfirm => ExternalBlockers.Count == 0;
    public bool HasExternalBlockers => ExternalBlockers.Count > 0;
}
