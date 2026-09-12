using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyCatalogClient(HttpClient client) : IPrintifyCatalogClient
{
    private sealed record ProductPage(IReadOnlyList<PrintifyShopProductSummary> Products, IReadOnlyList<PrintifyCatalogBlueprint> Details, int LastPage);
    public static Uri CatalogBaseUri { get; } = new("https://api.printify.com/v1/catalog/");
    public static Uri ApiBaseUri { get; } = new("https://api.printify.com/v1/");
    private const int MaximumResponseBytes = 4 * 1024 * 1024;

    public static HttpClient CreateHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = CatalogBaseUri,
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<PrintifyCatalogResult> LoadShopProductsAsync(string key, int shopId, CancellationToken cancellationToken = default)
    {
        if (!PrintifyToken.IsValid(key)) return new(PrintifyCatalogResultKind.InvalidKey, "Enter a valid Printify key before loading shop products.");
        if (shopId <= 0) return new(PrintifyCatalogResultKind.InvalidRequest, "Select a valid Printify shop before loading products.");

        var products = new List<PrintifyShopProductSummary>();
        for (var page = 1; ; page++)
        {
            var response = await SendJsonAsync($"{ApiBaseUri}shops/{shopId}/products.json?limit=50&page={page}", key, cancellationToken).ConfigureAwait(false);
            if (response.Error is not null) return response.Error;
            using var document = response.Json!;
            try
            {
                var pageResult = ParseProductPage(document);
                products.AddRange(pageResult.Products);
                if (pageResult.LastPage <= page) break;
            }
            catch (JsonException) { return Unexpected(); }
            catch (InvalidOperationException) { return Unexpected(); }
        }

        return products.Count == 0
            ? new(PrintifyCatalogResultKind.Empty, "The selected Printify shop has no products to import.", Products: products)
            : new(PrintifyCatalogResultKind.Succeeded, "Printify shop products loaded.", Products: products);
    }

    public async Task<PrintifyCatalogResult> LoadSelectedProductsAsync(string key, int shopId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds is null || productIds.Count == 0 || productIds.Any(string.IsNullOrWhiteSpace))
            return new(PrintifyCatalogResultKind.InvalidRequest, "Select at least one valid Printify product.");
        var loaded = await LoadAllProductsAsync(key, shopId, cancellationToken).ConfigureAwait(false);
        if (!loaded.Succeeded) return loaded;
        var selectedIds = productIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var selected = loaded.SelectedProducts!.Where(product => product.ProductId is { } id && selectedIds.Contains(id)).ToList();
        if (selected.Count != selectedIds.Count)
            return new(PrintifyCatalogResultKind.InvalidRequest, "One or more selected Printify products is no longer available.");
        return new(PrintifyCatalogResultKind.Succeeded, "Selected Printify shop products loaded.", Products: loaded.Products, SelectedProducts: selected);
    }

    private async Task<PrintifyCatalogResult> LoadAllProductsAsync(string key, int shopId, CancellationToken cancellationToken)
    {
        if (!PrintifyToken.IsValid(key)) return new(PrintifyCatalogResultKind.InvalidKey, "Enter a valid Printify key before loading shop products.");
        if (shopId <= 0) return new(PrintifyCatalogResultKind.InvalidRequest, "Select a valid Printify shop before loading products.");
        var summaries = new List<PrintifyShopProductSummary>();
        var details = new List<PrintifyCatalogBlueprint>();
        for (var page = 1; ; page++)
        {
            var response = await SendJsonAsync($"{ApiBaseUri}shops/{shopId}/products.json?limit=50&page={page}", key, cancellationToken).ConfigureAwait(false);
            if (response.Error is not null) return response.Error;
            using var document = response.Json!;
            try
            {
                var pageResult = ParseProductPage(document);
                summaries.AddRange(pageResult.Products);
                details.AddRange(pageResult.Details);
                if (pageResult.LastPage <= page) break;
            }
            catch (JsonException) { return Unexpected(); }
            catch (InvalidOperationException) { return Unexpected(); }
        }
        return summaries.Count == 0
            ? new(PrintifyCatalogResultKind.Empty, "The selected Printify shop has no products to import.", Products: summaries, SelectedProducts: details)
            : new(PrintifyCatalogResultKind.Succeeded, "Printify shop products loaded.", Products: summaries, SelectedProducts: details);
    }

    private static ProductPage ParseProductPage(JsonDocument document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new JsonException();
        var lastPage = root.TryGetProperty("last_page", out var last) && last.TryGetInt32(out var value) && value > 0 ? value : 1;
        var summaries = new List<PrintifyShopProductSummary>();
        var details = new List<PrintifyCatalogBlueprint>();
        foreach (var item in data.EnumerateArray())
        {
            var productId = RequiredString(item, "id");
            var blueprintId = RequiredInt(item, "blueprint_id");
            var providerId = RequiredInt(item, "print_provider_id");
            var title = RequiredString(item, "title");
            summaries.Add(new(productId, title, OptionalString(item, "description"), blueprintId, providerId));
            var options = ParseProductOptions(item);
            var variants = ParseProductVariants(item);
            details.Add(new(new PrintifyCatalogBlueprintSummary(blueprintId, title, OptionalString(item, "description"), null, null),
                [new PrintifyCatalogProvider(providerId, $"Printify provider {providerId}", options, variants)]) { ProductId = productId });
        }
        return new(summaries, details, lastPage);
    }

    private static List<PrintifyCatalogOption> ParseProductOptions(JsonElement item)
    {
        if (!item.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array) return [];
        return options.EnumerateArray().Select(option => new PrintifyCatalogOption(
            RequiredString(option, "name"), OptionalString(option, "type") ?? "other",
            option.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array
                ? values.EnumerateArray().Select(value => new PrintifyCatalogOptionValue(RequiredInt(value, "id"), RequiredString(value, "title"))).ToList()
                : throw new JsonException())).ToList();
    }

    private static List<PrintifyCatalogVariant> ParseProductVariants(JsonElement item)
    {
        if (!item.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Array) throw new JsonException();
        var areas = item.TryGetProperty("print_areas", out var printAreas) && printAreas.ValueKind == JsonValueKind.Array
            ? printAreas.EnumerateArray().ToList() : [];
        return variants.EnumerateArray().Select(variant =>
        {
            var id = RequiredInt(variant, "id");
            var placeholders = new List<PrintifyCatalogPlaceholder>();
            foreach (var area in areas)
            {
                if (!area.TryGetProperty("variant_ids", out var ids) || ids.ValueKind != JsonValueKind.Array || !ids.EnumerateArray().Any(value => value.TryGetInt32(out var areaId) && areaId == id)) continue;
                if (!area.TryGetProperty("placeholders", out var areaPlaceholders) || areaPlaceholders.ValueKind != JsonValueKind.Array) throw new JsonException();
                foreach (var placeholder in areaPlaceholders.EnumerateArray())
                {
                    var images = placeholder.TryGetProperty("images", out var imageList) && imageList.ValueKind == JsonValueKind.Array ? imageList.EnumerateArray().ToList() : [];
                    var image = images.FirstOrDefault();
                    var width = image.ValueKind == JsonValueKind.Object ? RequiredInt(image, "width") : 0;
                    var height = image.ValueKind == JsonValueKind.Object ? RequiredInt(image, "height") : 0;
                    if (width <= 0 || height <= 0) throw new JsonException();
                    placeholders.Add(new(RequiredString(placeholder, "position"), OptionalString(placeholder, "decoration_method") ?? "unknown", width, height));
                }
            }
            return new PrintifyCatalogVariant(id, RequiredString(variant, "title"), OptionalBool(variant, "is_enabled", true), OptionalBool(variant, "is_available", true), ParseOptionIds(variant), placeholders);
        }).ToList();
    }

    public async Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!PrintifyToken.IsValid(key))
            return new(PrintifyCatalogResultKind.InvalidKey, "Enter a valid Printify key before loading the catalog.");

        var response = await SendJsonAsync("blueprints.json", key, cancellationToken).ConfigureAwait(false);
        if (response.Error is not null) return response.Error;
        using var document = response.Json!;
        try
        {
            var summaries = ParseBlueprintSummaries(document);
            return summaries.Count == 0
                ? new(PrintifyCatalogResultKind.Empty, "Printify returned no Blueprints.", summaries)
                : new(PrintifyCatalogResultKind.Succeeded, "Printify Blueprints loaded.", summaries);
        }
        catch (JsonException) { return Unexpected(); }
        catch (InvalidOperationException) { return Unexpected(); }
    }

    public async Task<PrintifyCatalogResult> LoadSelectedAsync(
        string key,
        IReadOnlyCollection<int> blueprintIds,
        CancellationToken cancellationToken = default)
    {
        if (!PrintifyToken.IsValid(key))
            return new(PrintifyCatalogResultKind.InvalidKey, "Enter a valid Printify key before loading the catalog.");
        if (blueprintIds is null || blueprintIds.Count == 0 || blueprintIds.Any(id => id <= 0))
            return new(PrintifyCatalogResultKind.InvalidRequest, "Select at least one valid Blueprint.");

        var summariesResult = await LoadBlueprintsAsync(key, cancellationToken).ConfigureAwait(false);
        if (!summariesResult.Succeeded) return summariesResult;
        var summaries = summariesResult.Blueprints!.Where(summary => blueprintIds.Contains(summary.Id)).ToDictionary(summary => summary.Id);
        if (summaries.Count != blueprintIds.Distinct().Count())
            return new(PrintifyCatalogResultKind.InvalidRequest, "One or more selected Blueprints is no longer available.");

        var selected = new List<PrintifyCatalogBlueprint>();
        foreach (var id in blueprintIds.Distinct())
        {
            var providersResponse = await SendJsonAsync($"blueprints/{id}/print_providers.json", key, cancellationToken).ConfigureAwait(false);
            if (providersResponse.Error is not null) return providersResponse.Error;
            using var providersDocument = providersResponse.Json!;
            var providers = ParseProviders(providersDocument);
            var resolved = new List<PrintifyCatalogProvider>();
            foreach (var provider in providers)
            {
                var variantsResponse = await SendJsonAsync($"blueprints/{id}/print_providers/{provider.Id}/variants.json", key, cancellationToken).ConfigureAwait(false);
                if (variantsResponse.Error is not null) return variantsResponse.Error;
                using var variantsDocument = variantsResponse.Json!;
                resolved.Add(provider with { Variants = ParseVariants(variantsDocument) });
            }
            selected.Add(new(summaries[id], resolved));
        }

        return new(PrintifyCatalogResultKind.Succeeded, "Selected Printify catalog data loaded.", summariesResult.Blueprints, selected);
    }

    private async Task<(JsonDocument? Json, PrintifyCatalogResult? Error)> SendJsonAsync(
        string relativePath,
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
            request.Headers.UserAgent.ParseAdd("FusionCanvas");
            request.Headers.Accept.Add(new("application/json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var failure = Classify(response.StatusCode);
            if (failure is not null) return (null, failure);
            if (response.Content.Headers.ContentLength > MaximumResponseBytes) return (null, Unexpected());

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > MaximumResponseBytes) return (null, Unexpected());
                buffer.Write(chunk, 0, count);
            }
            return (JsonDocument.Parse(buffer.ToArray()), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return (null, new(PrintifyCatalogResultKind.NetworkFailure, "Printify catalog retrieval timed out. Try again.")); }
        catch (HttpRequestException) { return (null, new(PrintifyCatalogResultKind.NetworkFailure, "Printify could not be reached. Try again.")); }
        catch (IOException) { return (null, new(PrintifyCatalogResultKind.NetworkFailure, "Printify response could not be read. Try again.")); }
        catch (JsonException) { return (null, Unexpected()); }
    }

    private static PrintifyCatalogResult? Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => new(PrintifyCatalogResultKind.InvalidKey, "The Printify key is invalid or expired."),
        HttpStatusCode.Forbidden => new(PrintifyCatalogResultKind.PermissionDenied, "The Printify key lacks catalog permission."),
        HttpStatusCode.TooManyRequests => new(PrintifyCatalogResultKind.RateLimited, "Printify is rate limiting requests. Try again later."),
        _ when (int)statusCode >= 500 => new(PrintifyCatalogResultKind.NetworkFailure, "Printify is temporarily unavailable. Try again later."),
        HttpStatusCode.OK => null,
        _ => Unexpected()
    };

    private static PrintifyCatalogResult Unexpected() => new(PrintifyCatalogResultKind.UnexpectedResponse, "Printify returned an unexpected catalog response.");

    private static List<PrintifyCatalogBlueprintSummary> ParseBlueprintSummaries(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException();
        return document.RootElement.EnumerateArray().Select(item => new PrintifyCatalogBlueprintSummary(
            RequiredInt(item, "id"), RequiredString(item, "title"), OptionalString(item, "description"),
            OptionalString(item, "brand"), OptionalString(item, "model"))).ToList();
    }

    private static List<PrintifyCatalogProvider> ParseProviders(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException();
        return document.RootElement.EnumerateArray().Select(item => new PrintifyCatalogProvider(
            RequiredInt(item, "id"), RequiredString(item, "title"), [], [])).ToList();
    }

    private static List<PrintifyCatalogVariant> ParseVariants(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException();
        return document.RootElement.EnumerateArray().Select(item => new PrintifyCatalogVariant(
            RequiredInt(item, "id"), RequiredString(item, "title"),
            OptionalBool(item, "is_enabled", true), OptionalBool(item, "is_available", true),
            ParseOptionIds(item), ParsePlaceholders(item))).ToList();
    }

    private static List<int> ParseOptionIds(JsonElement item)
    {
        if (!item.TryGetProperty("options", out var options)) return [];
        if (options.ValueKind == JsonValueKind.Array)
            return options.EnumerateArray().Select(value => value.GetInt32()).ToList();
        return [];
    }

    private static List<PrintifyCatalogPlaceholder> ParsePlaceholders(JsonElement item)
    {
        if (!item.TryGetProperty("placeholders", out var placeholders) || placeholders.ValueKind != JsonValueKind.Array) return [];
        return placeholders.EnumerateArray().Select(placeholder => new PrintifyCatalogPlaceholder(
            RequiredString(placeholder, "position"), OptionalString(placeholder, "decoration_method") ?? "unknown",
            RequiredInt(placeholder, "width"), RequiredInt(placeholder, "height"))).ToList();
    }

    private static int RequiredInt(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) && result > 0 ? result : throw new JsonException();
    private static string RequiredString(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new JsonException();
    private static string? OptionalString(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool OptionalBool(JsonElement item, string name, bool fallback)
    {
        if (!item.TryGetProperty(name, out var value)) return fallback;
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
    }
}
