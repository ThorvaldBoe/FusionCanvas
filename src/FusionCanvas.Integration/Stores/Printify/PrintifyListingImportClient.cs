using System.Net;
using System.Text.Json;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Integration.Stores.Printify;

/// <summary>Read-only Printify shop listing and artwork access for listing import.</summary>
public sealed class PrintifyListingImportClient : IPrintifyListingImportClient
{
    private const int MaximumImageBytes = 20 * 1024 * 1024;
    private const int MaximumJsonBytes = 8 * 1024 * 1024;
    private readonly PrintifyHttpTransport _transport;
    private readonly HttpClient _imageClient;

    public static Uri ApiBaseUri { get; } = new("https://api.printify.com/v1/");

    public PrintifyListingImportClient(HttpClient apiClient, HttpClient imageClient, ITelemetryRecorder? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        _imageClient = imageClient ?? throw new ArgumentNullException(nameof(imageClient));
        _transport = new PrintifyHttpTransport(apiClient, telemetry: telemetry);
    }

    public static HttpClient CreateApiHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = ApiBaseUri,
        Timeout = Timeout.InfiniteTimeSpan
    };

    public static HttpClient CreateImageHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<IReadOnlyList<PrintifyListingProductSummary>> GetShopProductsAsync(string apiKey, int shopId, CancellationToken cancellationToken = default)
    {
        if (!PrintifyToken.IsValid(apiKey) || shopId <= 0) throw new InvalidOperationException("A valid Printify key and selected shop are required.");
        var products = new List<PrintifyListingProductSummary>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 1; page <= 1000; page++)
        {
            var response = await SendJsonAsync($"shops/{shopId}/products.json?limit=50&page={page}", apiKey, cancellationToken).ConfigureAwait(false);
            using var document = response;
            var root = document.RootElement;
            var list = root.ValueKind == JsonValueKind.Array ? root : RequiredArray(root, "data");
            foreach (var item in list.EnumerateArray())
            {
                var product = ParseSummary(item);
                if (!ids.Add(product.ProductId)) throw new InvalidOperationException("Printify returned a duplicate product identity.");
                products.Add(product);
            }

            var lastPage = root.ValueKind == JsonValueKind.Object ? OptionalInt(root, "last_page") ?? OptionalInt(root, "lastPage") : null;
            if (lastPage is int last ? page >= last : list.GetArrayLength() < 50) return products;
        }

        throw new InvalidOperationException("Printify shop pagination exceeded the safe page limit.");
    }

    public async Task<PrintifyListingProductDetail?> GetProductAsync(string apiKey, int shopId, string productId, CancellationToken cancellationToken = default)
    {
        if (!PrintifyToken.IsValid(apiKey) || shopId <= 0 || string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("A valid Printify key, shop, and product are required.");
        using var document = await SendJsonAsync($"shops/{shopId}/products/{Uri.EscapeDataString(productId)}.json", apiKey, cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Printify returned an invalid product response.");
        var summary = ParseSummary(root);
        if (!string.Equals(summary.ProductId, productId, StringComparison.Ordinal)) throw new InvalidOperationException("Printify product identity did not match the request.");

        var options = RequiredArray(root, "options").EnumerateArray().Select(option => new PrintifyListingOption(
            RequiredString(option, "name"), OptionalString(option, "type") ?? "other",
            RequiredArray(option, "values").EnumerateArray().Select(value => new PrintifyListingOptionValue(RequiredInt(value, "id"), RequiredString(value, "title"))).ToArray())).ToArray();
        var variants = RequiredArray(root, "variants").EnumerateArray().Select(variant => new PrintifyListingVariant(
            RequiredInt(variant, "id"), RequiredString(variant, "title"), OptionalInt(variant, "price") ?? 0,
            OptionalBool(variant, "is_enabled", true), OptionalBool(variant, "is_available", true),
            variant.TryGetProperty("options", out var optionIds) && optionIds.ValueKind == JsonValueKind.Array
                ? optionIds.EnumerateArray().Select(value => value.GetInt32()).ToArray() : [])).ToArray();
        var variantIds = variants.Select(variant => variant.Id).ToHashSet();
        var areas = new List<PrintifyListingPrintArea>();
        foreach (var area in RequiredArray(root, "print_areas").EnumerateArray())
        {
            var areaVariants = RequiredArray(area, "variant_ids").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            if (areaVariants.Any(id => !variantIds.Contains(id))) throw new InvalidOperationException("Printify returned an unknown variant in a print area.");
            var images = new List<PrintifyListingArtworkImage>();
            var unsupported = false;
            var areaPosition = "unknown";
            foreach (var placeholder in RequiredArray(area, "placeholders").EnumerateArray())
            {
                var position = OptionalString(placeholder, "position") ?? "front";
                areaPosition = areaPosition == "unknown" ? position : areaPosition;
                var layers = RequiredArray(placeholder, "images");
                if (layers.GetArrayLength() != 1 || placeholder.TryGetProperty("text", out _) || placeholder.TryGetProperty("layers", out _)) unsupported = true;
                foreach (var image in layers.EnumerateArray())
                {
                    var source = RequiredString(image, "src");
                    var uri = ValidateArtworkUri(source);
                    var imageId = OptionalString(image, "id") ?? uri.AbsolutePath.Split('/').LastOrDefault() ?? "artwork";
                    var imageVariants = image.TryGetProperty("variant_ids", out var variantValues) && variantValues.ValueKind == JsonValueKind.Array
                        ? variantValues.EnumerateArray().Select(value => value.GetInt32()).ToArray()
                        : areaVariants;
                    if (imageVariants.Any(id => !variantIds.Contains(id))) throw new InvalidOperationException("Printify returned artwork for an unknown variant.");
                    images.Add(new(imageId, uri.AbsoluteUri, OptionalString(image, "name"), OptionalString(image, "type"), position, imageVariants));
                }
            }
            areas.Add(new(areaPosition, areaVariants, images, unsupported));
        }

        var shipping = OptionalString(root, "shipping_template_id");
        return new(summary.ProductId, summary.Title, summary.Description, summary.IsVisible, summary.BlueprintId, summary.ProviderId, shipping, options, variants, areas);
    }

    public async Task<PrintifyArtworkDownload> DownloadArtworkAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        var uri = ValidateArtworkUri(sourceUrl);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _imageClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        var finalUri = response.RequestMessage?.RequestUri ?? uri;
        if (response.StatusCode != HttpStatusCode.OK || !IsAllowedArtworkUri(finalUri))
            throw new InvalidOperationException("Printify artwork could not be downloaded safely.");
        if (response.Content.Headers.ContentLength is long length && length > MaximumImageBytes)
            throw new InvalidOperationException("Printify artwork exceeds the supported file size.");
        var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
        var extension = mediaType switch { "image/png" => ".png", "image/jpeg" => ".jpg", "image/webp" => ".webp", _ => null };
        if (extension is null) throw new InvalidOperationException("Printify artwork must be a PNG, JPEG, or WebP image.");
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var count = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false);
            if (count == 0) break;
            if (buffer.Length + count > MaximumImageBytes) throw new InvalidOperationException("Printify artwork exceeds the supported file size.");
            buffer.Write(chunk, 0, count);
        }
        if (!HasValidImageSignature(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), mediaType!))
            throw new InvalidOperationException("Printify returned malformed artwork data.");
        return new(buffer.ToArray(), mediaType!, extension);
    }

    private async Task<JsonDocument> SendJsonAsync(string path, string key, CancellationToken cancellationToken)
    {
        var uri = new Uri(ApiBaseUri, path);
        var response = await _transport.SendAsync(HttpMethod.Get, uri, key, maximumResponseBytes: MaximumJsonBytes, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.Outcome != PrintifyTransportOutcome.Succeeded) throw new InvalidOperationException("Printify product data could not be retrieved.");
        try { return JsonDocument.Parse(response.Body); }
        catch (JsonException exception) { throw new InvalidOperationException("Printify returned malformed product data.", exception); }
    }

    private static PrintifyListingProductSummary ParseSummary(JsonElement item) => new(
        RequiredString(item, "id"), RequiredString(item, "title"), OptionalString(item, "description"),
        OptionalBool(item, "visible", false), RequiredInt(item, "blueprint_id"), RequiredInt(item, "print_provider_id"));

    private static Uri ValidateArtworkUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsAllowedArtworkUri(uri))
            throw new InvalidOperationException("Printify returned an unsafe artwork URL.");
        return uri;
    }

    private static bool IsAllowedArtworkUri(Uri uri) => uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0
        && (uri.Host.Equals("images.printify.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("printify-upload.s3.amazonaws.com", StringComparison.OrdinalIgnoreCase));

    private static JsonElement RequiredArray(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value : throw new JsonException();
    private static string RequiredString(JsonElement item, string name) => OptionalString(item, name) ?? throw new JsonException();
    private static string? OptionalString(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int RequiredInt(JsonElement item, string name) => OptionalInt(item, name) is int value && value > 0 ? value : throw new JsonException();
    private static int? OptionalInt(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : null;
    private static bool OptionalBool(JsonElement item, string name, bool fallback) => item.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;

    private static bool HasValidImageSignature(ReadOnlySpan<byte> data, string mediaType) => mediaType switch
    {
        "image/png" => data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF,
        "image/webp" => data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
