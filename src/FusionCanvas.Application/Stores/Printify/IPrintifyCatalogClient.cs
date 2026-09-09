namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCatalogClient
{
    Task<PrintifyCatalogResult> LoadBlueprintsAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<PrintifyCatalogResult> LoadSelectedAsync(
        string key,
        IReadOnlyCollection<int> blueprintIds,
        CancellationToken cancellationToken = default);
}
