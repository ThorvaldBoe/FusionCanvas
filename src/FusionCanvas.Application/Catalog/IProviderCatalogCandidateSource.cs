namespace FusionCanvas.Application.Catalog;

public interface IProviderCatalogCandidateSource
{
    Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default);
}
