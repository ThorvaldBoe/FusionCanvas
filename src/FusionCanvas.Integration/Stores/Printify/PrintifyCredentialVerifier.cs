using System.Text.Json;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyCredentialVerifier : IPrintifyCredentialVerifier
{
    public static Uri VerificationUri { get; } = new("https://api.printify.com/v1/shops.json");
    private const int MaximumResponseBytes = 1024 * 1024;
    private readonly PrintifyHttpTransport _transport;

    public PrintifyCredentialVerifier(HttpClient client, ITelemetryRecorder? telemetry = null)
        : this(new PrintifyHttpTransport(client, telemetry: telemetry))
    {
    }

    public PrintifyCredentialVerifier(PrintifyHttpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public static HttpClient CreateHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<PrintifyConfigurationResult> VerifyAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!PrintifyToken.IsValid(key))
        {
            return new(PrintifyConfigurationKind.InvalidKey, "The Printify key is invalid.");
        }

        var response = await _transport.SendAsync(
            HttpMethod.Get,
            VerificationUri,
            key,
            maximumResponseBytes: MaximumResponseBytes,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        PrintifyConfigurationResult? classified = response.Outcome switch
        {
            PrintifyTransportOutcome.InvalidCredential => new(PrintifyConfigurationKind.InvalidKey, "The Printify key is invalid or expired. Use Manage to replace it."),
            PrintifyTransportOutcome.PermissionDenied => new(PrintifyConfigurationKind.PermissionDenied, "The Printify key lacks permission. Provide a key with shops.read access."),
            PrintifyTransportOutcome.RateLimited => new(PrintifyConfigurationKind.RateLimited, "Printify is rate limiting requests. Try Verify again later."),
            PrintifyTransportOutcome.NetworkFailure => new(PrintifyConfigurationKind.NetworkFailure, "Printify could not be reached. Try Verify again."),
            PrintifyTransportOutcome.UnexpectedResponse when response.StatusCode is not System.Net.HttpStatusCode.OK => new(PrintifyConfigurationKind.UnexpectedResponse, "Printify returned an unexpected response. Try Verify again later."),
            _ => null
        };
        if (classified is not null)
        {
            return classified;
        }

        try
        {
            using var json = JsonDocument.Parse(response.Body);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Unexpected();
            }

            var shops = new List<PrintifyShopOption>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var id)
                    || !id.TryGetInt32(out var shopId)
                    || !item.TryGetProperty("title", out var title)
                    || title.ValueKind != JsonValueKind.String)
                {
                    return Unexpected();
                }

                shops.Add(new(shopId, title.GetString()!));
            }

            return new(PrintifyConfigurationKind.Verified, "Printify api key verified. Shopify connectivity and publishing permissions were not tested.", shops);
        }
        catch (JsonException)
        {
            return Unexpected();
        }
    }

    private static PrintifyConfigurationResult Unexpected() =>
        new(PrintifyConfigurationKind.UnexpectedResponse, "Printify returned an unexpected response. Try Verify again later.");
}
