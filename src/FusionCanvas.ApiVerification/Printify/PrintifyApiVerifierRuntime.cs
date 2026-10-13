using FusionCanvas.Integration.Stores.Printify;
using FusionCanvas.ApiVerification.Sessions;

namespace FusionCanvas.ApiVerification.Printify;

internal sealed record PrintifyApiVerifierRuntime(
    Func<HttpClient> CredentialClient,
    Func<HttpClient> CatalogClient,
    Func<HttpClient> ImportApiClient,
    Func<HttpClient> ImportImageClient,
    Func<HttpClient> ListingClient,
    Func<string, string, CancellationToken, Task<bool>> Confirm)
{
    public static PrintifyApiVerifierRuntime Production { get; } = new(
        PrintifyCredentialVerifier.CreateHttpClient,
        PrintifyCatalogClient.CreateHttpClient,
        PrintifyListingImportClient.CreateApiHttpClient,
        PrintifyListingImportClient.CreateImageHttpClient,
        PrintifyListingClient.CreateHttpClient,
        SessionConfirmation.RequirePhraseAsync);
}
