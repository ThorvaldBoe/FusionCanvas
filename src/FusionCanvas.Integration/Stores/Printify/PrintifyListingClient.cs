using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Integration.Stores.Printify;

/// <summary>
/// Printify implementation of the provider-neutral listing ports. The instance is
/// scoped to one Store so credential resolution never crosses Store boundaries.
/// </summary>
public sealed class PrintifyListingClient : IListingConnectionPort, IListingImagePort, IListingProductPort, IListingPublicationPort
{
    private const int MaximumResponseBytes = 4 * 1024 * 1024;
    private readonly Guid _storeId;
    private readonly Func<CancellationToken, Task<string?>> _credentialResolver;
    private readonly IWorkspaceFileReader? _fileReader;
    private readonly PrintifyHttpTransport _transport;

    public PrintifyListingClient(
        Guid storeId,
        HttpClient client,
        Func<CancellationToken, Task<string?>> credentialResolver,
        IWorkspaceFileReader? fileReader = null,
        ITelemetryRecorder? telemetry = null)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("A Store identifier is required.", nameof(storeId));
        _storeId = storeId;
        _transport = new PrintifyHttpTransport(client ?? throw new ArgumentNullException(nameof(client)), telemetry: telemetry);
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _fileReader = fileReader;
    }

    public static HttpClient CreateHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = new Uri("https://api.printify.com/v1/"),
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<ListingReadiness> CheckAsync(ListingConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.StoreId != _storeId)
            return Unavailable("The Printify client is scoped to a different Store.");

        var secret = await _credentialResolver(cancellationToken).ConfigureAwait(false);
        if (!PrintifyToken.IsValid(secret))
            return Unavailable("Printify is not connected. Verify the Store's API key before using this tool.");

        var response = await SendAsync(HttpMethod.Get, "shops.json", secret!, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!response.Succeeded)
            return Unavailable(MessageFor(response.Outcome));

        try
        {
            using var document = JsonDocument.Parse(response.Body);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return Unavailable("Printify returned an invalid shop list.");

            var selectedShopFound = false;
            foreach (var shop in document.RootElement.EnumerateArray())
            {
                if (shop.ValueKind != JsonValueKind.Object
                    || !shop.TryGetProperty("id", out var id)
                    || !id.TryGetInt32(out var numericId))
                {
                    return Unavailable("Printify returned an invalid shop list.");
                }

                if (string.Equals(numericId.ToString(CultureInfo.InvariantCulture), request.ShopId, StringComparison.Ordinal))
                    selectedShopFound = true;
            }

            if (!selectedShopFound)
                return Unavailable("The selected Printify shop is not available for this account. Refresh Store settings and select an available shop.");

            return new(
                ListingConnectionState.Ready,
                ProductOperationsAvailable: true,
                PublicationOperationsAvailable: request.RequirePublication,
                []);
        }
        catch (JsonException)
        {
            return Unavailable("Printify returned an invalid shop response.");
        }
    }

    public async Task<ListingImageReference> UploadAsync(ListingImageUpload request, CancellationToken cancellationToken = default)
    {
        if (request.AssetId == Guid.Empty || string.IsNullOrWhiteSpace(request.WorkspaceRelativePath))
            throw new ArgumentException("A valid artwork asset is required.", nameof(request));

        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        if (_fileReader is null)
            throw new InvalidOperationException("A workspace file reader is required before artwork can be uploaded.");
        await using var stream = await _fileReader.OpenReadAsync(request.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bytes = buffer.ToArray();
        var payload = JsonSerializer.Serialize(new
        {
            file_name = Path.GetFileName(request.WorkspaceRelativePath),
            contents = Convert.ToBase64String(bytes)
        });
        var response = await SendJsonAsync(HttpMethod.Post, "uploads/images.json", secret, payload, cancellationToken).ConfigureAwait(false);
        if (!response.Succeeded)
            throw new InvalidOperationException(MessageFor(response.Outcome));

        using var document = JsonDocument.Parse(response.Body);
        var id = ReadString(document.RootElement, "id");
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("Printify returned no image identity.");
        return new(id, request.Fingerprint);
    }

    public async Task<IReadOnlyList<ListingRemoteProductSummary>> ListShopProductsAsync(string shopId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("A Printify shop is required.", nameof(shopId));
        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var products = new List<ListingRemoteProductSummary>();
        var productIds = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 1; page <= 1000; page++)
        {
            var path = $"shops/{Uri.EscapeDataString(shopId)}/products.json?limit=50&page={page}";
            var response = await SendAsync(HttpMethod.Get, path, secret, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!response.Succeeded) throw new InvalidOperationException(MessageFor(response.Outcome));

            using var document = JsonDocument.Parse(response.Body);
            var root = document.RootElement;
            var data = root.ValueKind == JsonValueKind.Array
                ? root
                : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataValue) && dataValue.ValueKind == JsonValueKind.Array
                    ? dataValue
                    : throw new InvalidOperationException("Printify returned an invalid shop product list.");
            foreach (var product in data.EnumerateArray())
            {
                var productId = ReadString(product, "id");
                var title = ReadString(product, "title");
                if (string.IsNullOrWhiteSpace(productId)
                    || string.IsNullOrWhiteSpace(title)
                    || !product.TryGetProperty("blueprint_id", out var blueprintValue)
                    || !blueprintValue.TryGetInt32(out var blueprintId)
                    || !product.TryGetProperty("print_provider_id", out var providerValue)
                    || !providerValue.TryGetInt32(out var providerId)
                    || !productIds.Add(productId))
                {
                    throw new InvalidOperationException("Printify returned an incomplete or duplicate product identity in the shop list.");
                }
                products.Add(new(productId, title, blueprintId, providerId));
            }

            var lastPage = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("last_page", out var lastPageValue) && lastPageValue.TryGetInt32(out var parsedLastPage)
                ? parsedLastPage
                : (int?)null;
            if (lastPage is int last ? page >= last : data.GetArrayLength() < 50)
                return products;
        }

        throw new InvalidOperationException("Printify shop pagination exceeded the safe page limit.");
    }

    public async Task<ListingRemoteProduct?> GetAsync(string shopId, string productId, CancellationToken cancellationToken = default)
    {
        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var response = await SendAsync(HttpMethod.Get, $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}.json", secret, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.Outcome == PrintifyTransportOutcome.NotFound)
            return null;
        if (!response.Succeeded)
            throw new InvalidOperationException(MessageFor(response.Outcome));
        return ParseRemoteProduct(response.Body, productId, ListingPublicationState.Unpublished);
    }

    public Task<ListingMutationResult> CreateAsync(string shopId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default) =>
        MutateProductAsync(HttpMethod.Post, $"shops/{Uri.EscapeDataString(shopId)}/products.json", null, projection, images, cancellationToken);

    public Task<ListingMutationResult> UpdateAsync(string shopId, string productId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, IReadOnlySet<string>? changedFields = null, CancellationToken cancellationToken = default) =>
        MutateProductAsync(HttpMethod.Put, $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}.json", productId, projection, images, cancellationToken, changedFields);

    public async Task<ListingMutationResult> DeleteAsync(string shopId, string productId, CancellationToken cancellationToken = default)
    {
        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var response = await SendAsync(HttpMethod.Delete, $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}.json", secret, cancellationToken: cancellationToken).ConfigureAwait(false);
        return MutationResult(response, productId, ListingPublicationState.Unpublished);
    }

    public Task<ListingMutationResult> PublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
        MutatePublicationAsync(shopId, productId, publish: true, cancellationToken);

    public Task<ListingMutationResult> UnpublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
        MutatePublicationAsync(shopId, productId, publish: false, cancellationToken);

    public async Task<ListingMutationResult> PublishingFailedAsync(string shopId, string productId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A failure reason is required.", nameof(reason));

        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var path = $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}/publishing_failed.json";
        var response = await SendJsonAsync(HttpMethod.Post, path, secret, JsonSerializer.Serialize(new { reason }), cancellationToken).ConfigureAwait(false);
        return MutationResult(response, productId, ListingPublicationState.Unpublished);
    }

    public async Task<ListingMutationResult> PublishingSucceededAsync(string shopId, string productId, string externalId, string externalHandle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("An external channel identity is required.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(externalHandle))
            throw new ArgumentException("An external channel handle is required.", nameof(externalHandle));

        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var path = $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}/publishing_succeeded.json";
        var payload = JsonSerializer.Serialize(new { external = new { id = externalId, handle = externalHandle } });
        var response = await SendJsonAsync(HttpMethod.Post, path, secret, payload, cancellationToken).ConfigureAwait(false);
        return MutationResult(response, productId, ListingPublicationState.Published);
    }

    private async Task<ListingMutationResult> MutateProductAsync(
        HttpMethod method,
        string path,
        string? knownProductId,
        ListingProductProjection projection,
        IReadOnlyDictionary<Guid, ListingImageReference> images,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? changedFields = null)
    {
        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var payload = BuildProductPayload(projection, images, changedFields);
        var response = await SendJsonAsync(method, path, secret, payload, cancellationToken).ConfigureAwait(false);
        return MutationResult(response, knownProductId ?? TryReadProductId(response.Body), ListingPublicationState.Unpublished);
    }

    private async Task<ListingMutationResult> MutatePublicationAsync(string shopId, string productId, bool publish, CancellationToken cancellationToken)
    {
        var secret = await RequireCredentialAsync(cancellationToken).ConfigureAwait(false);
        var path = publish
            ? $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}/publish.json"
            : $"shops/{Uri.EscapeDataString(shopId)}/products/{Uri.EscapeDataString(productId)}/unpublish.json";
        var response = publish
            ? await SendJsonAsync(HttpMethod.Post, path, secret, JsonSerializer.Serialize(new
            {
                title = true,
                description = true,
                images = true,
                variants = true,
                tags = true,
                keyFeatures = true,
                shipping_template = true
            }), cancellationToken).ConfigureAwait(false)
            : await SendAsync(HttpMethod.Post, path, secret, cancellationToken: cancellationToken).ConfigureAwait(false);
        return MutationResult(response, productId, publish ? ListingPublicationState.Published : ListingPublicationState.Unpublished);
    }

    private async Task<PrintifyTransportResponse> SendJsonAsync(HttpMethod method, string path, string secret, string payload, CancellationToken cancellationToken)
    {
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await SendAsync(method, path, secret, content, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PrintifyTransportResponse> SendAsync(HttpMethod method, string path, string secret, CancellationToken cancellationToken = default) =>
        await SendAsync(method, path, secret, null, cancellationToken).ConfigureAwait(false);

    private async Task<PrintifyTransportResponse> SendAsync(HttpMethod method, string path, string secret, HttpContent? content, CancellationToken cancellationToken)
    {
        var uri = new Uri(new Uri("https://api.printify.com/v1/"), path);
        return await _transport.SendAsync(method, uri, secret, content, MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> RequireCredentialAsync(CancellationToken cancellationToken)
    {
        var secret = await _credentialResolver(cancellationToken).ConfigureAwait(false);
        if (!PrintifyToken.IsValid(secret))
            throw new InvalidOperationException("Printify is not connected.");
        return secret!;
    }

    private static string BuildProductPayload(ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, IReadOnlySet<string>? changedFields = null)
    {
        if (projection.ExternalBlueprintId is null || projection.ExternalProviderId is null)
            throw new InvalidOperationException("The selected Blueprint and Print Provider have no Printify identities.");

        var variants = projection.Variants
            .Where(value => value.ExternalVariantId is not null)
            .Select(value => new
            {
                id = value.ExternalVariantId!.Value,
                price = ToMinorUnits(value.Price.RetailPrice),
                is_enabled = true
            })
            .ToArray();
        if (variants.Length != projection.Variants.Count)
            throw new InvalidOperationException("One or more selected variants has no Printify identity.");

        var variantIds = variants.Select(value => value.id).ToArray();
        var areas = projection.Artwork.Select(area => new
        {
            variant_ids = variantIds,
            placeholders = new[]
            {
                new
                {
                    position = area.Position,
                    images = new[]
                    {
                        new
                        {
                            id = images.TryGetValue(area.AssetId, out var image) ? image.ReferenceId : throw new InvalidOperationException($"No uploaded Printify image exists for artwork asset '{area.AssetId}'."),
                            x = area.Placement.X,
                            y = area.Placement.Y,
                            scale = area.Placement.Scale,
                            angle = area.Placement.Angle
                        }
                    }
                }
            }
        }).ToArray();

        var shippingTemplate = string.IsNullOrWhiteSpace(projection.ShippingProfile)
            ? null
            : new { shipping_template_id = projection.ShippingProfile };

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        bool Include(string field) => changedFields is null || changedFields.Count == 0 || changedFields.Contains(field);
        if (changedFields is null || changedFields.Count == 0)
        {
            payload["blueprint_id"] = projection.ExternalBlueprintId.Value;
            payload["print_provider_id"] = projection.ExternalProviderId.Value;
        }
        if (Include("title")) payload["title"] = projection.Title;
        if (Include("description")) payload["description"] = projection.Description ?? string.Empty;
        if (Include("variants")) payload["variants"] = variants;
        if (Include("variants")) payload["print_areas"] = areas;
        if (Include("shippingProfile") && shippingTemplate is not null) payload["external"] = shippingTemplate;
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
    }

    private static ListingMutationResult MutationResult(PrintifyTransportResponse response, string? productId, ListingPublicationState publicationState)
    {
        if (!response.Succeeded)
        {
            var definitive = response.Outcome is not (PrintifyTransportOutcome.NetworkFailure);
            return new(productId, definitive, ErrorCode: response.Outcome.ToString(), ErrorMessage: MessageFor(response.Outcome, response.StatusCode));
        }

        ListingRemoteProduct? product = null;
        if (response.Body.Length > 0 && !string.IsNullOrWhiteSpace(productId))
        {
            try { product = ParseRemoteProduct(response.Body, productId, publicationState); }
            catch (JsonException) { }
        }
        return new(productId, true, product);
    }

    private static ListingRemoteProduct ParseRemoteProduct(byte[] body, string productId, ListingPublicationState fallbackPublication)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var returnedId = ReadString(root, "id");
        var id = returnedId ?? productId;
        var shopId = root.TryGetProperty("shop_id", out var shopValue)
            ? shopValue.ValueKind switch
            {
                JsonValueKind.Number when shopValue.TryGetInt32(out var numericShopId) => numericShopId.ToString(CultureInfo.InvariantCulture),
                JsonValueKind.String => shopValue.GetString(),
                _ => null
            }
            : null;
        var publication = ReadPublicationState(root, fallbackPublication);
        var snapshot = new ListingSnapshot(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["title"] = ReadString(root, "title"),
            ["description"] = ReadString(root, "description"),
            ["shippingProfile"] = ReadExternalValue(root, "shipping_template_id") ?? ReadScalar(root, "shipping_template"),
            ["variants"] = ReadVariantPrices(root)
        });
        return new(id, root.TryGetProperty("is_locked", out var lockedProperty) && lockedProperty.ValueKind == JsonValueKind.True, publication, snapshot,
            ReadExternalValue(root, "id") ?? ReadString(root, "external_id"), ReadExternalValue(root, "handle") ?? ReadString(root, "handle"),
            shopId, !string.IsNullOrWhiteSpace(returnedId) && string.Equals(returnedId, productId, StringComparison.Ordinal));
    }

    private static ListingPublicationState ReadPublicationState(JsonElement root, ListingPublicationState fallbackPublication)
    {
        // Printify defaults `visible` to true; external channel identity is the publish-success marker.
        if (!root.TryGetProperty("external", out var external))
            return fallbackPublication;

        if (external.ValueKind == JsonValueKind.Array)
        {
            if (external.EnumerateArray().Any(HasExternalIdentity))
                return ListingPublicationState.Published;
            return external.GetArrayLength() == 0 ? fallbackPublication : ListingPublicationState.Unverified;
        }

        if (external.ValueKind == JsonValueKind.Object)
        {
            if (HasExternalIdentity(external))
                return ListingPublicationState.Published;
            return external.EnumerateObject().Any() ? ListingPublicationState.Unverified : fallbackPublication;
        }

        return ListingPublicationState.Unverified;
    }

    private static bool HasExternalIdentity(JsonElement external) =>
        external.ValueKind == JsonValueKind.Object
        && (!string.IsNullOrWhiteSpace(ReadString(external, "id"))
            || !string.IsNullOrWhiteSpace(ReadString(external, "handle")));

    private static string? TryReadProductId(byte[] body)
    {
        if (body.Length == 0) return null;
        try { using var document = JsonDocument.Parse(body); return ReadString(document.RootElement, "id"); }
        catch (JsonException) { return null; }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? ReadScalar(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static string? ReadExternalValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty("external", out var external))
            return null;

        if (external.ValueKind == JsonValueKind.Object)
            return ReadString(external, name);

        if (external.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var entry in external.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object)
            {
                var value = ReadString(entry, name);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private static string? ReadVariantPrices(JsonElement root)
    {
        if (!root.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Array)
            return null;

        var values = new List<string>();
        foreach (var variant in variants.EnumerateArray())
        {
            var id = ReadScalar(variant, "id");
            var price = ReadScalar(variant, "price");
            if (!variant.TryGetProperty("is_enabled", out var enabled) || enabled.ValueKind != JsonValueKind.True)
                continue;
            if (id is null || price is null)
                continue;

            if (decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out var minorUnits))
                values.Add($"{id}:{(minorUnits / 100m).ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join("|", values);
    }

    private static int ToMinorUnits(decimal amount) => checked((int)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static ListingReadiness Unavailable(string message) => new(ListingConnectionState.Unavailable, false, false, [new("printify-unavailable", message)]);

    private static string MessageFor(PrintifyTransportOutcome outcome, HttpStatusCode? statusCode = null) => outcome switch
    {
        PrintifyTransportOutcome.InvalidCredential => "Printify rejected the saved API key.",
        PrintifyTransportOutcome.PermissionDenied => "The Printify API key lacks permission for this operation.",
        PrintifyTransportOutcome.NotFound => "Printify could not find the selected shop or product.",
        PrintifyTransportOutcome.RateLimited => "Printify is rate limiting requests. Try again later.",
        PrintifyTransportOutcome.Locked => "Printify has locked the product. Refresh before trying again.",
        PrintifyTransportOutcome.NetworkFailure => "Printify is unavailable. Try again when the connection is restored.",
        PrintifyTransportOutcome.UnexpectedResponse when statusCode is { } status => $"Printify returned an unexpected response (HTTP {(int)status}).",
        _ => "Printify returned an unexpected response."
    };
}

