using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyCredentialVerifier(HttpClient client) : IPrintifyCredentialVerifier
{
    public static Uri VerificationUri { get; } = new("https://api.printify.com/v1/shops.json");
    private const int MaximumResponseBytes = 1024 * 1024;

    public static HttpClient CreateHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<PrintifyConfigurationResult> VerifyAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!PrintifyToken.IsValid(key)) return new(PrintifyConfigurationKind.InvalidKey, "The Printify key is invalid.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, VerificationUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
            request.Headers.UserAgent.ParseAdd("FusionCanvas");
            request.Headers.Accept.Add(new("application/json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(PrintifyConfigurationKind.InvalidKey, "The Printify key is invalid or expired. Use Manage to replace it.");
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return new(PrintifyConfigurationKind.PermissionDenied, "The Printify key lacks permission. Provide a key with shops.read access.");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return new(PrintifyConfigurationKind.RateLimited, "Printify is rate limiting requests. Try Verify again later.");
            if ((int)response.StatusCode >= 500)
                return new(PrintifyConfigurationKind.NetworkFailure, "Printify is temporarily unavailable. Try Verify again later.");
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentLength > MaximumResponseBytes)
                return Unexpected();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > MaximumResponseBytes) return Unexpected();
                buffer.Write(chunk, 0, count);
            }
            using var json = JsonDocument.Parse(buffer.ToArray());
            if (json.RootElement.ValueKind != JsonValueKind.Array) return Unexpected();
            var shops = new List<PrintifyShopOption>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var id) || !id.TryGetInt32(out var shopId) ||
                    !item.TryGetProperty("title", out var title) || title.ValueKind != JsonValueKind.String)
                    return Unexpected();
                shops.Add(new(shopId, title.GetString()!));
            }
            return new(PrintifyConfigurationKind.Verified, "Printify api key verified. Shopify connectivity and publishing permissions were not tested.", shops);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new(PrintifyConfigurationKind.NetworkFailure, "Printify verification timed out. Try Verify again."); }
        catch (HttpRequestException) { return new(PrintifyConfigurationKind.NetworkFailure, "Printify could not be reached. Try Verify again."); }
        catch (IOException) { return new(PrintifyConfigurationKind.NetworkFailure, "Printify response could not be read. Try Verify again."); }
        catch (JsonException) { return Unexpected(); }
    }

    private static PrintifyConfigurationResult Unexpected() =>
        new(PrintifyConfigurationKind.UnexpectedResponse, "Printify returned an unexpected response. Try Verify again later.");
}
