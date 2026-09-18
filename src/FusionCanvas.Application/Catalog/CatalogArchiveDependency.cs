namespace FusionCanvas.Application.Catalog;

public sealed record CatalogArchiveDependency(string RecordType, Guid RecordId, string Name)
{
    public string DisplayText => $"{RecordType}: {Name}";
}
