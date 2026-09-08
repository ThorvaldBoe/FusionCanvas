using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyCatalogClient(HttpClient client) : IPrintifyCatalogClient
{
    public static Uri CatalogBaseUri { get; } = new("https://api.printify.com/v1/catalog/");
    private const int MaximumResponseBytes = 4 * 1024 * 1024;

    public static HttpClient CreateHttpClient() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = CatalogBaseUri,
        Timeout = Timeout.InfiniteTimeSpan
    };

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
