using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Integration.AI;

public sealed class OpenRouterClient :
    IAiCredentialValidator,
    IAiModelCatalogProvider,
    IAiImageModelCatalogProvider,
    IAiImageEndpointCatalogProvider,
    IAiTextProvider,
    IAiImageGenerationProvider
{
    public static readonly Uri DefaultBaseAddress = new("https://openrouter.ai/");
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(5);
    private const int MaximumResponseBytes = 8 * 1024 * 1024;
    private const int MaximumImageBytes = 25_000_000;
    private const int MaximumImageResponseBytes = 34 * 1024 * 1024;
    private const int MaximumDisplayText = 4096;

    private readonly HttpClient _httpClient;

    public OpenRouterClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress ??= DefaultBaseAddress;
    }

    public async Task<AiCredentialValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new(AiCredentialValidationKind.Invalid, "Enter an OpenRouter API key.");
        }

        try
        {
            using var response = await SendGetAsync("api/v1/key", apiKey, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new(AiCredentialValidationKind.Invalid, "The OpenRouter API key is invalid or revoked.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new(AiCredentialValidationKind.PermissionDenied, "The OpenRouter API key lacks permission.");
            }

            if ((int)response.StatusCode == 429)
            {
                return new(
                    AiCredentialValidationKind.RateLimited,
                    "OpenRouter rate-limited key validation.",
                    RetryAfter: ReadRetryAfter(response));
            }

            if (!response.IsSuccessStatusCode)
            {
                return new(AiCredentialValidationKind.ServiceUnavailable, "OpenRouter key validation is temporarily unavailable.");
            }

            using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            var data = RequiredObject(json.RootElement, "data");
            if (ReadBoolean(data, "is_management_key") || ReadBoolean(data, "is_provisioning_key"))
            {
                return new(AiCredentialValidationKind.ManagementKey, "Use an inference-capable OpenRouter API key.");
            }

            return new(
                AiCredentialValidationKind.Valid,
                "The OpenRouter API key is valid.",
                ReadDecimal(data, "limit_remaining"));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(AiCredentialValidationKind.NetworkFailure, "OpenRouter key validation timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or InvalidDataException)
        {
            return new(AiCredentialValidationKind.NetworkFailure, "OpenRouter key validation could not be completed.");
        }
    }

    public async Task<AiModelCatalog> GetModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SendGetAsync("api/v1/models", apiKey, cancellationToken).ConfigureAwait(false);
            EnsureCatalogSuccess(response);
            using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            var data = RequiredArray(json.RootElement, "data");
            var zdrModelIds = await GetZdrModelIdsAsync(requireZeroDataRetention, cancellationToken).ConfigureAwait(false);
            var models = new List<AiModelDescriptor>();
            foreach (var item in data.EnumerateArray())
            {
                var model = ParseModel(item, zdrModelIds);
                if (model is not null)
                {
                    models.Add(model);
                }
            }

            return new AiModelCatalog(
                requireZeroDataRetention,
                DateTimeOffset.UtcNow,
                models.OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase).ToArray());
        }
        catch (AiModelCatalogFetchException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.NetworkOrService,
                CatalogFailureMessage(AiModelCatalogFailureKind.NetworkOrService));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.NetworkOrService,
                CatalogFailureMessage(AiModelCatalogFailureKind.NetworkOrService));
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or InvalidOperationException)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.InvalidResponse,
                CatalogFailureMessage(AiModelCatalogFailureKind.InvalidResponse));
        }
    }

    private async Task<IReadOnlyCollection<string>> GetZdrModelIdsAsync(
        bool requireZeroDataRetention,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await FetchZdrEndpointIdentitiesAsync(cancellationToken).ConfigureAwait(false))
                .Select(endpoint => endpoint.ModelId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!requireZeroDataRetention)
        {
            return [];
        }
        catch (Exception)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.ZdrDataUnavailable,
                CatalogFailureMessage(AiModelCatalogFailureKind.ZdrDataUnavailable));
        }
    }

    private async Task<IReadOnlyList<ZdrEndpointIdentity>> FetchZdrEndpointIdentitiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendGetAsync("api/v1/endpoints/zdr", null, cancellationToken).ConfigureAwait(false);
            EnsureCatalogSuccess(response);
            using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            var data = RequiredArray(json.RootElement, "data");
            var endpoints = new List<ZdrEndpointIdentity>();
            foreach (var item in data.EnumerateArray())
            {
                var modelId = ReadString(item, "model_id");
                if (!string.IsNullOrWhiteSpace(modelId))
                {
                    endpoints.Add(new ZdrEndpointIdentity(
                        modelId,
                        ReadString(item, "tag"),
                        ReadString(item, "provider_name")));
                }
            }

            return endpoints;
        }
        catch (AiModelCatalogFetchException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.NetworkOrService,
                CatalogFailureMessage(AiModelCatalogFailureKind.NetworkOrService));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.NetworkOrService,
                CatalogFailureMessage(AiModelCatalogFailureKind.NetworkOrService));
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or InvalidOperationException)
        {
            throw new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.InvalidResponse,
                CatalogFailureMessage(AiModelCatalogFailureKind.InvalidResponse));
        }
    }

    private static void EnsureCatalogSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AiModelCatalogFailureKind.Authentication,
            (HttpStatusCode)429 => AiModelCatalogFailureKind.RateLimited,
            _ => AiModelCatalogFailureKind.NetworkOrService
        };
        throw new AiModelCatalogFetchException(
            kind,
            CatalogFailureMessage(kind),
            kind == AiModelCatalogFailureKind.RateLimited ? ReadRetryAfter(response) : null);
    }

    private static string CatalogFailureMessage(AiModelCatalogFailureKind kind) => kind switch
    {
        AiModelCatalogFailureKind.Authentication => "OpenRouter rejected the saved API key.",
        AiModelCatalogFailureKind.RateLimited => "OpenRouter rate-limited the model catalog refresh.",
        AiModelCatalogFailureKind.NetworkOrService => "OpenRouter model catalog is temporarily unavailable.",
        AiModelCatalogFailureKind.InvalidResponse => "OpenRouter returned an invalid catalog response.",
        AiModelCatalogFailureKind.ZdrDataUnavailable => "OpenRouter Zero Data Retention endpoint data could not be loaded.",
        _ => "OpenRouter model catalog could not be loaded."
    };

    public async Task<AiTextResult> GenerateAsync(
        AiProviderTextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(GenerationTimeout);

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/chat/completions");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
            message.Content = new StringContent(BuildRequestJson(request), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return await ParseFailureAsync(response, request.ModelId, timeout.Token).ConfigureAwait(false);
            }

            using var json = await ReadJsonAsync(response, timeout.Token).ConfigureAwait(false);
            return ParseSuccess(json.RootElement, request.ModelId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return AiTextResult.Failure(
                AiTextFailureKind.Timeout,
                "The generation request timed out; retrying could create additional usage.",
                request.ModelId);
        }
        catch (HttpRequestException)
        {
            return AiTextResult.Failure(
                AiTextFailureKind.NetworkFailure,
                "The generation connection failed; retrying could create additional usage.",
                request.ModelId);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
        {
            return AiTextResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "OpenRouter returned an invalid response.",
                request.ModelId);
        }
    }

    public async Task<AiModelCatalog> GetImageModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SendGetAsync("api/v1/images/models", apiKey, cancellationToken).ConfigureAwait(false);
            EnsureCatalogSuccess(response);
            using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            var zdrModelIds = await GetZdrModelIdsAsync(requireZeroDataRetention, cancellationToken).ConfigureAwait(false);
            var models = RequiredArray(json.RootElement, "data").EnumerateArray()
                .Select(item => ParseImageModel(item, zdrModelIds))
                .OfType<AiModelDescriptor>()
                .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new AiModelCatalog(requireZeroDataRetention, DateTimeOffset.UtcNow, models);
        }
        catch (AiModelCatalogFetchException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or InvalidDataException or InvalidOperationException)
        {
            throw new AiModelCatalogFetchException(AiModelCatalogFailureKind.InvalidResponse, "OpenRouter image model catalog could not be loaded.");
        }
    }

    public async Task<IReadOnlyList<AiImageEndpointCapabilities>> GetImageEndpointsAsync(
        string apiKey,
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId) || !modelId.Contains('/'))
            return [];

        var path = "api/v1/images/models/" + string.Join('/', modelId.Split('/').Select(Uri.EscapeDataString)) + "/endpoints";
        using var response = await SendGetAsync(path, apiKey, cancellationToken).ConfigureAwait(false);
        EnsureCatalogSuccess(response);
        using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        var zdrEndpoints = requireZeroDataRetention ? await FetchZdrEndpointIdentitiesAsync(cancellationToken).ConfigureAwait(false) : [];
        // The current endpoint catalog returns { "id": ..., "endpoints": [...] }.
        // Older responses used the same endpoint records in a top-level data array.
        var data = json.RootElement.TryGetProperty("endpoints", out var endpoints)
            && endpoints.ValueKind == JsonValueKind.Array
            ? endpoints
            : RequiredArray(json.RootElement, "data");
        return data.EnumerateArray()
            .Select(item => ParseImageEndpoint(item, modelId, zdrEndpoints))
            .OfType<AiImageEndpointCapabilities>()
            .ToArray();
    }

    public async Task<(AiImageGenerationResult? Result, AiImageGenerationFailure? Failure)> GenerateAsync(
        AiImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return (null, new(AiImageGenerationFailureKind.Authentication, "An OpenRouter API key is required."));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(GenerationTimeout);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/images");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
            message.Content = new StringContent(BuildImageRequestJson(request), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return (null, new(MapImageFailure(response.StatusCode), "OpenRouter could not complete the image request."));

            using var json = await ReadJsonAsync(response, timeout.Token, MaximumImageResponseBytes).ConfigureAwait(false);
            var data = RequiredArray(json.RootElement, "data");
            if (data.GetArrayLength() != 1)
                return (null, new(AiImageGenerationFailureKind.InvalidResponse, "OpenRouter did not return exactly one image."));
            var image = data[0];
            var encoded = ReadString(image, "b64_json");
            if (string.IsNullOrWhiteSpace(encoded) || !Convert.TryFromBase64String(encoded, new byte[MaximumImageBytes], out var _))
                return (null, new(AiImageGenerationFailureKind.InvalidResponse, "OpenRouter returned invalid image data."));
            var bytes = Convert.FromBase64String(encoded);
            var usageElement = ReadObject(json.RootElement, "usage");
            var usage = usageElement.ValueKind == JsonValueKind.Object
                ? new AiImageUsage(ReadInt32(usageElement, "prompt_tokens"), ReadInt32(usageElement, "completion_tokens"), ReadDecimal(usageElement, "cost"))
                : null;
            return (new AiImageGenerationResult(bytes, ReadString(image, "media_type") ?? "image/png", "OpenRouter", request.ModelId, ReadString(json.RootElement, "model") ?? request.ModelId, ReadString(json.RootElement, "id"), usage), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return (null, new(AiImageGenerationFailureKind.Cancelled, "The image request timed out.")); }
        catch (HttpRequestException) { return (null, new(AiImageGenerationFailureKind.Unavailable, "The OpenRouter image request failed.")); }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or FormatException)
        { return (null, new(AiImageGenerationFailureKind.InvalidResponse, "OpenRouter returned an invalid image response.")); }
    }

    private static string BuildImageRequestJson(AiImageGenerationRequest request)
    {
        var root = new JsonObject
        {
            ["model"] = request.ModelId,
            ["prompt"] = request.Prompt
        };
        if (request.Options is null)
        {
            root["size"] = $"{request.ProviderSize.Width}x{request.ProviderSize.Height}";
            root["output_format"] = "png";
            root["background"] = request.TransparentBackground ? "transparent" : "opaque";
            root["n"] = 1;
        }
        else
        {
            if (request.Options.Size is { } size) root["size"] = $"{size.Width}x{size.Height}";
            if (!string.IsNullOrWhiteSpace(request.Options.Resolution)) root["resolution"] = request.Options.Resolution;
            if (!string.IsNullOrWhiteSpace(request.Options.AspectRatio)) root["aspect_ratio"] = request.Options.AspectRatio;
            if (!string.IsNullOrWhiteSpace(request.Options.OutputFormat)) root["output_format"] = request.Options.OutputFormat;
            if (!string.IsNullOrWhiteSpace(request.Options.Background)) root["background"] = request.Options.Background;
            if (request.Options.Count is { } count) root["n"] = count;
        }
        var provider = new JsonObject { ["require_parameters"] = true, ["allow_fallbacks"] = false };
        if (request.RequireZeroDataRetention) provider["zdr"] = true;
        if (!string.IsNullOrWhiteSpace(request.ProviderTag)) provider["only"] = new JsonArray(JsonValue.Create(request.ProviderTag));
        root["provider"] = provider;
        return root.ToJsonString();
    }

    private static AiImageGenerationFailureKind MapImageFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AiImageGenerationFailureKind.Authentication,
        (HttpStatusCode)429 => AiImageGenerationFailureKind.RateLimited,
        HttpStatusCode.BadRequest => AiImageGenerationFailureKind.InvalidResponse,
        HttpStatusCode.NotFound => AiImageGenerationFailureKind.Unavailable,
        _ => AiImageGenerationFailureKind.Unknown
    };

    private async Task<HttpResponseMessage> SendGetAsync(
        string path,
        string? apiKey,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MetadataTimeout);

        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (apiKey is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token).ConfigureAwait(false);
            if (attempt > 0 || !IsRetryable(response.StatusCode))
            {
                return response;
            }

            var delay = ReadRetryAfter(response) ?? TimeSpan.FromMilliseconds(250);
            response.Dispose();
            if (delay > TimeSpan.FromSeconds(5))
            {
                delay = TimeSpan.FromSeconds(5);
            }

            await Task.Delay(delay, timeout.Token).ConfigureAwait(false);
        }
    }

    private static string BuildRequestJson(AiProviderTextRequest request)
    {
        var profile = request.Profile;
        var root = new JsonObject
        {
            ["model"] = request.ModelId,
            ["stream"] = false,
            ["messages"] = new JsonArray(request.Messages.Select(message =>
                new JsonObject
                {
                    ["role"] = message.Role.ToString().ToLowerInvariant(),
                    ["content"] = message.Text
                }).ToArray()),
            ["provider"] = new JsonObject
            {
                ["require_parameters"] = true
            }
        };

        if (request.RequireZeroDataRetention)
        {
            ((JsonObject)root["provider"]!)["zdr"] = true;
        }

        Add(root, "max_completion_tokens", profile.MaxCompletionTokens);
        Add(root, "temperature", profile.Temperature);
        Add(root, "top_p", profile.TopP);
        Add(root, "top_k", profile.TopK);
        Add(root, "min_p", profile.MinP);
        Add(root, "top_a", profile.TopA);
        Add(root, "frequency_penalty", profile.FrequencyPenalty);
        Add(root, "presence_penalty", profile.PresencePenalty);
        Add(root, "repetition_penalty", profile.RepetitionPenalty);
        Add(root, "seed", profile.Seed);
        if (profile.StopSequences.Length > 0)
        {
            root["stop"] = new JsonArray(
                profile.StopSequences.Select(value => JsonValue.Create(value)).ToArray());
        }

        root["reasoning"] = profile.Reasoning.Mode switch
        {
            AiReasoningMode.Disabled => new JsonObject { ["enabled"] = false },
            AiReasoningMode.Effort => new JsonObject { ["effort"] = profile.Reasoning.Effort },
            AiReasoningMode.TokenBudget => new JsonObject { ["max_tokens"] = profile.Reasoning.TokenBudget },
            _ => null
        };

        return root.ToJsonString();
    }

    private static void Add(JsonObject root, string name, int? value)
    {
        if (value is not null) root[name] = value.Value;
    }

    private static void Add(JsonObject root, string name, double? value)
    {
        if (value is not null) root[name] = value.Value;
    }

    private static AiModelDescriptor? ParseModel(JsonElement item, IReadOnlyCollection<string> zdrModelIds)
    {
        var id = ReadString(item, "id");
        var name = ReadString(item, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var architecture = ReadObject(item, "architecture");
        var inputs = ReadStrings(architecture, "input_modalities");
        var outputs = ReadStrings(architecture, "output_modalities");
        if (!inputs.Contains("text", StringComparer.OrdinalIgnoreCase) ||
            !outputs.Contains("text", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var topProvider = ReadObject(item, "top_provider");
        var pricing = ReadObject(item, "pricing");
        var supported = ReadStrings(item, "supported_parameters");
        return new AiModelDescriptor(
            id,
            name,
            id.Contains('/') ? id[..id.IndexOf('/')] : null,
            Bound(ReadString(item, "description")),
            inputs,
            outputs,
            supported,
            ReadInt32(item, "context_length"),
            ReadInt32(topProvider, "max_completion_tokens"),
            ReadDecimal(pricing, "prompt"),
            ReadDecimal(pricing, "completion"),
            zdrModelIds.Contains(id),
            ParseReasoning(item, supported));
    }

    private static AiModelDescriptor? ParseImageModel(JsonElement item, IReadOnlyCollection<string> zdrModelIds)
    {
        var id = ReadString(item, "id");
        var name = ReadString(item, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) return null;
        var architecture = ReadObject(item, "architecture");
        var outputs = ReadStrings(architecture, "output_modalities");
        if (!outputs.Contains("image", StringComparer.OrdinalIgnoreCase)) return null;
        var supported = ReadObject(item, "supported_parameters").ValueKind == JsonValueKind.Object
            ? ReadObject(item, "supported_parameters").EnumerateObject().Select(property => property.Name).ToArray()
            : Array.Empty<string>();
        return new AiModelDescriptor(id, name, id.Contains('/') ? id[..id.IndexOf('/')] : null,
            Bound(ReadString(item, "description")), ["text"], outputs, supported, null, null, null, null,
            zdrModelIds.Contains(id), null);
    }

    private static AiImageEndpointCapabilities? ParseImageEndpoint(
        JsonElement item,
        string modelId,
        IReadOnlyList<ZdrEndpointIdentity> zdrEndpoints)
    {
        var providerTag = ReadString(item, "provider_tag");
        var providerName = ReadString(item, "provider_name") ?? ReadString(item, "provider") ?? ReadString(ReadObject(item, "architecture"), "provider_name");
        var endpointId = providerTag ?? ReadString(item, "name") ?? providerName ?? ReadString(item, "id");
        if (string.IsNullOrWhiteSpace(endpointId)) return null;
        var endpointModel = ReadString(item, "model_id") ?? modelId;
        var supportedElement = ReadObject(item, "supported_parameters");
        var supported = supportedElement.ValueKind == JsonValueKind.Object
            ? supportedElement.EnumerateObject().Select(property => property.Name).ToArray()
            : ReadStrings(item, "supported_parameters");
        var supportsExplicitSize = supported.Contains("size", StringComparer.OrdinalIgnoreCase);
        var sizes = ReadImageSizes(item)
            .Concat(ParseImageSizes(ReadParameterValues(supportedElement, "size")))
            .Distinct()
            .ToArray();
        if (sizes.Length == 0 && supportsExplicitSize)
            sizes = [new AiImageSize(256, 256), new AiImageSize(512, 512), new AiImageSize(1024, 1024), new AiImageSize(1536, 1024), new AiImageSize(1024, 1536)];
        var supportsOutputFormat = supported.Contains("output_format", StringComparer.OrdinalIgnoreCase);
        var advertisedFormats = ReadParameterValues(supportedElement, "output_format");
        IReadOnlyList<string> formats = supportsOutputFormat
            ? advertisedFormats.Count > 0 ? advertisedFormats : ["png", "jpeg", "webp"]
            : ["png"];
        var backgrounds = ReadParameterValues(supportedElement, "background");
        var supportsTransparency = backgrounds.Contains("transparent", StringComparer.OrdinalIgnoreCase);
        var aspectRatios = ReadParameterValues(supportedElement, "aspect_ratio");
        var resolutions = ReadParameterValues(supportedElement, "resolution");
        var isZdrCompatible = zdrEndpoints.Any(endpoint =>
            string.Equals(endpoint.ModelId, endpointModel, StringComparison.OrdinalIgnoreCase) &&
            ((!string.IsNullOrWhiteSpace(providerTag) && string.Equals(endpoint.ProviderTag, providerTag, StringComparison.OrdinalIgnoreCase)) ||
             (string.IsNullOrWhiteSpace(providerTag) && !string.IsNullOrWhiteSpace(providerName) && string.Equals(endpoint.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))));
        return new AiImageEndpointCapabilities(
            endpointId,
            endpointModel,
            isZdrCompatible,
            true,
            formats,
            sizes,
            supportsTransparency,
            providerName,
            new AiImageEndpointParameterCapabilities(
                aspectRatios,
                resolutions,
                supportsExplicitSize,
                supportsOutputFormat,
                backgrounds,
                supported.Contains("n", StringComparer.OrdinalIgnoreCase)));
    }

    private static IReadOnlyList<AiImageSize> ReadImageSizes(JsonElement item)
    {
        var sizes = new List<AiImageSize>();
        foreach (var value in ReadStrings(item, "supported_sizes").Concat(ReadStrings(item, "sizes")))
        {
            var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height) && width > 0 && height > 0)
                sizes.Add(new AiImageSize(width, height));
        }
        return sizes.Distinct().ToArray();
    }

    private static IReadOnlyList<AiImageSize> ParseImageSizes(IReadOnlyList<string> values)
    {
        var sizes = new List<AiImageSize>();
        foreach (var value in values)
        {
            var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height) && width > 0 && height > 0)
                sizes.Add(new AiImageSize(width, height));
        }
        return sizes.Distinct().ToArray();
    }

    private static IReadOnlyList<string> ReadParameterValues(JsonElement supportedParameters, string parameterName)
    {
        if (supportedParameters.ValueKind != JsonValueKind.Object ||
            !supportedParameters.TryGetProperty(parameterName, out var descriptor) ||
            descriptor.ValueKind != JsonValueKind.Object ||
            !descriptor.TryGetProperty("values", out var values) ||
            values.ValueKind != JsonValueKind.Array)
            return [];
        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => Bound(value.GetString(), 128))
            .OfType<string>()
            .Take(128)
            .ToArray();
    }

    private static AiReasoningCapabilities? ParseReasoning(
        JsonElement item,
        IReadOnlyList<string> supported)
    {
        if (!supported.Contains(AiParameterRegistry.Reasoning, StringComparer.Ordinal))
        {
            return null;
        }

        var defaults = ReadObject(item, "default_parameters");
        var reasoning = ReadObject(defaults, "reasoning");
        var efforts = ReadStrings(reasoning, "supported_efforts");
        return new AiReasoningCapabilities(
            ReadBoolean(reasoning, "mandatory"),
            ReadBoolean(reasoning, "enabled"),
            efforts,
            ReadString(reasoning, "effort"),
            ReadBoolean(reasoning, "supports_token_budget"));
    }

    private static AiTextResult ParseSuccess(JsonElement root, string requestedModel)
    {
        var choices = RequiredArray(root, "choices");
        if (choices.GetArrayLength() == 0)
        {
            throw new JsonException("No completion choice was returned.");
        }

        var choice = choices[0];
        var message = RequiredObject(choice, "message");
        var text = ReadString(message, "content");
        var finishReason = ReadString(choice, "finish_reason");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new JsonException("The completion did not contain text.");
        }

        if (string.Equals(finishReason, "error", StringComparison.OrdinalIgnoreCase))
        {
            return AiTextResult.Failure(
                AiTextFailureKind.IncompleteGeneration,
                "OpenRouter reported an incomplete generation.",
                requestedModel,
                partialText: Bound(text));
        }

        var usageElement = ReadObject(root, "usage");
        var usage = usageElement.ValueKind == JsonValueKind.Object
            ? new AiTextUsage(
                ReadInt32(usageElement, "prompt_tokens"),
                ReadInt32(usageElement, "completion_tokens"),
                ReadInt32(usageElement, "total_tokens"),
                ReadDecimal(usageElement, "cost"))
            : null;
        var metadata = ReadObject(root, "openrouter_metadata");
        return AiTextResult.Success(
            text,
            requestedModel,
            ReadString(root, "model"),
            ReadString(metadata, "provider_name"),
            finishReason,
            usage,
            ReadString(root, "id"));
    }

    private static async Task<AiTextResult> ParseFailureAsync(
        HttpResponseMessage response,
        string requestedModel,
        CancellationToken cancellationToken)
    {
        try
        {
            using var json = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            _ = ReadString(ReadObject(json.RootElement, "error"), "message");
        }
        catch (Exception exception) when (exception is JsonException or IOException or InvalidDataException)
        {
        }

        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => AiTextFailureKind.Authentication,
            HttpStatusCode.PaymentRequired => AiTextFailureKind.InsufficientCredit,
            HttpStatusCode.Forbidden => AiTextFailureKind.Blocked,
            HttpStatusCode.NotFound => AiTextFailureKind.ModelUnavailable,
            HttpStatusCode.RequestTimeout => AiTextFailureKind.Timeout,
            (HttpStatusCode)429 => AiTextFailureKind.RateLimited,
            HttpStatusCode.BadGateway => AiTextFailureKind.ModelUnavailable,
            HttpStatusCode.ServiceUnavailable => AiTextFailureKind.NoEligibleProvider,
            _ => AiTextFailureKind.ProviderFailure
        };
        return AiTextResult.Failure(
            kind,
            SafeFailureMessage(kind),
            requestedModel,
            ReadRetryAfter(response));
    }

    private static string SafeFailureMessage(AiTextFailureKind kind) => kind switch
    {
        AiTextFailureKind.Authentication => "OpenRouter rejected the saved API key.",
        AiTextFailureKind.InsufficientCredit => "The OpenRouter account has insufficient credit.",
        AiTextFailureKind.RateLimited => "OpenRouter rate-limited the request.",
        AiTextFailureKind.Blocked => "OpenRouter blocked the request or denied permission.",
        AiTextFailureKind.ModelUnavailable => "The selected model is unavailable.",
        AiTextFailureKind.NoEligibleProvider => "No OpenRouter provider satisfies the request.",
        _ => "OpenRouter could not complete the request."
    };

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        int maximumResponseBytes = MaximumResponseBytes)
    {
        if (response.Content.Headers.ContentLength is { } contentLength &&
            contentLength > maximumResponseBytes)
        {
            throw new InvalidDataException("The OpenRouter response exceeds the size limit.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var bounded = new BoundedReadStream(source, maximumResponseBytes);
        return await JsonDocument.ParseAsync(bounded, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static bool IsRetryable(HttpStatusCode status) =>
        status is (HttpStatusCode)429 or HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        var retry = response.Headers.RetryAfter;
        if (retry?.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? null : delta;
        }

        if (retry?.Date is not { } date)
        {
            return null;
        }

        var remaining = date - DateTimeOffset.UtcNow;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private static JsonElement RequiredObject(JsonElement parent, string name)
    {
        var element = ReadObject(parent, name);
        return element.ValueKind == JsonValueKind.Object
            ? element
            : throw new JsonException($"Missing object: {name}.");
    }

    private static JsonElement RequiredArray(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Array
            ? element
            : throw new JsonException($"Missing array: {name}.");

    private static JsonElement ReadObject(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object &&
        parent.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.Object
            ? element
            : default;

    private static string? ReadString(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object &&
        parent.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static bool ReadBoolean(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object &&
        parent.TryGetProperty(name, out var element) &&
        element.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        element.GetBoolean();

    private static int? ReadInt32(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object &&
        parent.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.Number &&
        element.TryGetInt32(out var value)
            ? value
            : null;

    private static decimal? ReadDecimal(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value) => value,
            _ => null
        };
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(name, out var element) ||
            element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => Bound(item.GetString(), 128))
            .OfType<string>()
            .Take(128)
            .ToArray();
    }

    private static string? Bound(string? value, int maximum = MaximumDisplayText) =>
        string.IsNullOrEmpty(value)
            ? value
            : value.Length <= maximum
                ? value
                : value[..maximum];

    private sealed record ZdrEndpointIdentity(string ModelId, string? ProviderTag, string? ProviderName);

    private sealed class BoundedReadStream(Stream source, long maximumBytes) : Stream
    {
        private long _read;
        public override bool CanRead => source.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            Add(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            Add(read);
            return read;
        }

        private void Add(int count)
        {
            _read += count;
            if (_read > maximumBytes) throw new InvalidDataException("The OpenRouter response exceeds the size limit.");
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) source.Dispose();
            base.Dispose(disposing);
        }
    }
}
