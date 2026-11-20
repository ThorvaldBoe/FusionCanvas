namespace FusionCanvas.Integration.Updates;

/// <summary>
/// Follows GitHub's release download redirect to its release asset CDN without
/// allowing the update client to follow arbitrary redirects.
/// </summary>
public sealed class GitHubReleaseAssetRedirectHandler : DelegatingHandler
{
    private const string GitHubHost = "github.com";
    private const string ReleaseAssetHost = "release-assets.githubusercontent.com";
    private const string RepositoryPath = "/ThorvaldBoe/FusionCanvas/releases/download/";

    public GitHubReleaseAssetRedirectHandler(HttpMessageHandler innerHandler)
        : base(innerHandler ?? throw new ArgumentNullException(nameof(innerHandler)))
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (request.Method != HttpMethod.Get || !IsRedirect(response.StatusCode)) return response;

        var source = request.RequestUri;
        var location = response.Headers.Location;
        if (!IsReleaseDownloadUri(source) || location is null)
        {
            return response;
        }

        var target = location.IsAbsoluteUri ? location : new Uri(source!, location);
        if (!IsReleaseAssetUri(target)) return response;

        response.Dispose();
        using var redirectedRequest = new HttpRequestMessage(HttpMethod.Get, target);
        foreach (var productInfo in request.Headers.UserAgent)
        {
            redirectedRequest.Headers.UserAgent.Add(productInfo);
        }

        return await base.SendAsync(redirectedRequest, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsRedirect(System.Net.HttpStatusCode statusCode) =>
        statusCode is System.Net.HttpStatusCode.MovedPermanently
            or System.Net.HttpStatusCode.Redirect
            or System.Net.HttpStatusCode.RedirectMethod
            or System.Net.HttpStatusCode.TemporaryRedirect
            or System.Net.HttpStatusCode.PermanentRedirect;

    private static bool IsReleaseDownloadUri(Uri? uri) =>
        uri is { IsAbsoluteUri: true, Scheme: "https" }
        && uri.IdnHost.Equals(GitHubHost, StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrEmpty(uri.UserInfo)
        && uri.AbsolutePath.StartsWith(RepositoryPath, StringComparison.Ordinal);

    private static bool IsReleaseAssetUri(Uri uri) =>
        uri.IsAbsoluteUri
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.IdnHost.Equals(ReleaseAssetHost, StringComparison.OrdinalIgnoreCase)
        && string.IsNullOrEmpty(uri.UserInfo);
}

