namespace FusionCanvas.Application.Catalog;

public sealed class UnavailableProviderCatalogCandidateSource : IProviderCatalogCandidateSource
{
    public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default) => Task.FromResult(ProviderCatalogCandidateDescriptor.Unavailable(context, "Provider catalog data is not available. Confirmed local setup is unchanged."));
}
