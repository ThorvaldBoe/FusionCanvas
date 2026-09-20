using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public class OpenRouterClientTests
{
    [Fact]
    public async Task ValidateAsync_UsesCurrentKeyEndpointAndRejectsManagementKey()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """{"data":{"is_management_key":true,"limit_remaining":12.5}}"""));
        var client = CreateClient(handler);

        var result = await client.ValidateAsync("secret", TestContext.Current.CancellationToken);

        Assert.Equal(AiCredentialValidationKind.ManagementKey, result.Kind);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/api/v1/key", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal("Bearer", handler.Requests[0].Scheme);
        Assert.Equal("secret", handler.Requests[0].Parameter);
    }

    [Fact]
    public async Task GetModelsAsync_DerivesZdrFlagsFromEndpointListAndSendsNoZdrQuery()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """
                {"data":[
                  {"id":"zdr/model","name":"ZDR","description":"ok","architecture":{"input_modalities":["text"],"output_modalities":["text"]},"supported_parameters":["temperature","future"],"context_length":1000,"top_provider":{"max_completion_tokens":200},"pricing":{"prompt":"0.1","completion":"0.2"}},
                  {"id":"plain/model","name":"Plain","architecture":{"input_modalities":["text"],"output_modalities":["text"]},"supported_parameters":[],"context_length":1000,"top_provider":{},"pricing":{}},
                  {"id":"image/model","name":"Image","architecture":{"input_modalities":["text"],"output_modalities":["image"]},"supported_parameters":[]}
                ]}
                """),
            Json(HttpStatusCode.OK, """{"data":[{"model_id":"zdr/model"}]}"""));
        var client = CreateClient(handler);

        var catalog = await client.GetModelsAsync("secret", true, TestContext.Current.CancellationToken);

        Assert.Equal(2, catalog.Models.Count);
        var zdr = Assert.Single(catalog.Models, m => m.Id == "zdr/model");
        var plain = Assert.Single(catalog.Models, m => m.Id == "plain/model");
        Assert.True(zdr.ZeroDataRetentionCompatible);
        Assert.False(plain.ZeroDataRetentionCompatible);
        Assert.Contains("future", zdr.SupportedParameters);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/api/v1/models", handler.Requests[0].Uri.AbsolutePath);
        Assert.DoesNotContain("zdr", handler.Requests[0].Uri.Query);
        Assert.Equal("Bearer", handler.Requests[0].Scheme);
        Assert.Equal("secret", handler.Requests[0].Parameter);
        Assert.Equal("/api/v1/endpoints/zdr", handler.Requests[1].Uri.AbsolutePath);
        Assert.Null(handler.Requests[1].Scheme);
    }

    [Fact]
    public async Task GetModelsAsync_DegradesZdrListFailureWhenNotRequired()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """
                {"data":[{"id":"plain/model","name":"Plain","architecture":{"input_modalities":["text"],"output_modalities":["text"]},"supported_parameters":[],"context_length":1,"top_provider":{},"pricing":{}}]}
                """),
            Json(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"temp"}}"""));
        var client = CreateClient(handler);

        var catalog = await client.GetModelsAsync("secret", false, TestContext.Current.CancellationToken);

        var model = Assert.Single(catalog.Models);
        Assert.False(model.ZeroDataRetentionCompatible);
    }

    [Fact]
    public async Task GetImageModelsAsync_UsesDedicatedImageCatalogAndKeepsImageOnlyModels()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """{"data":[{"id":"image/model","name":"Image","architecture":{"input_modalities":["text"],"output_modalities":["image"]},"supported_parameters":{"size":{"type":"string"}}}]}"""),
            Json(HttpStatusCode.OK, """{"data":[{"model_id":"image/model"}]}"""));
        var client = CreateClient(handler);

        var catalog = await client.GetImageModelsAsync("secret", true, TestContext.Current.CancellationToken);

        var model = Assert.Single(catalog.Models);
        Assert.Equal("image/model", model.Id);
        Assert.Contains("size", model.SupportedParameters);
        Assert.Equal("/api/v1/images/models", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetImageEndpointsAsync_ReadsCurrentEndpointsEnvelopeAndUsesProviderTag()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, """
            {
              "id":"image/model",
              "endpoints":[
                {
                  "provider_name":"OpenAI",
                  "provider_tag":"openai",
                  "supported_parameters":{
                    "size":{"type":"string"},
                    "background":{"type":"enum","values":["transparent","opaque"]},
                    "output_format":{"type":"enum","values":["png"]}
                  }
                }
              ]
            }
            """));
        var client = CreateClient(handler);

        var endpoints = await client.GetImageEndpointsAsync("secret", "image/model", false, TestContext.Current.CancellationToken);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("openai", endpoint.EndpointId);
        Assert.Equal("OpenAI", endpoint.ProviderName);
        Assert.True(endpoint.SupportsImageOutput);
        Assert.True(endpoint.SupportsTransparency);
        Assert.Contains(new AiImageSize(1024, 1024), endpoint.SupportedSizes);
        Assert.Equal("/api/v1/images/models/image/model/endpoints", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetImageEndpointsAsync_RetainsLegacyDataEnvelopeCompatibility()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, """
            {
              "data":[
                {
                  "name":"legacy-provider",
                  "supported_parameters":["size"]
                }
              ]
            }
            """));
        var client = CreateClient(handler);

        var endpoints = await client.GetImageEndpointsAsync("secret", "image/model", false, TestContext.Current.CancellationToken);

        Assert.Equal("legacy-provider", Assert.Single(endpoints).EndpointId);
    }

    [Fact]
    public async Task GetImageEndpointsAsync_ParsesCurrentGptImageCapabilitiesWithoutInventingTransparency()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, """
            {
              "id":"openai/gpt-5.4-image-2",
              "endpoints":[
                {
                  "provider_name":"OpenAI",
                  "provider_slug":"openai",
                  "provider_tag":"openai",
                  "supported_parameters":{
                    "aspect_ratio":{"type":"enum","values":["1:1","3:2","2:3","4:3","3:4","16:9","9:16","21:9","auto"]},
                    "quality":{"type":"enum","values":["auto","low","medium","high"]},
                    "background":{"type":"enum","values":["auto","opaque"]},
                    "n":{"type":"range","min":1,"max":10},
                    "input_references":{"type":"range","min":0,"max":16},
                    "output_compression":{"type":"range","min":0,"max":100}
                  }
                }
              ]
            }
            """));
        var client = CreateClient(handler);

        var endpoints = await client.GetImageEndpointsAsync("secret", "openai/gpt-5.4-image-2", false, TestContext.Current.CancellationToken);

        var endpoint = Assert.Single(endpoints);
        Assert.True(endpoint.SupportsImageOutput);
        Assert.False(endpoint.SupportsTransparency);
        Assert.Empty(endpoint.SupportedSizes);
        Assert.Equal(["1:1", "3:2", "2:3", "4:3", "3:4", "16:9", "9:16", "21:9", "auto"], endpoint.Parameters!.AspectRatios);
        Assert.Equal(["auto", "opaque"], endpoint.Parameters.Backgrounds);
        Assert.False(endpoint.Parameters.SupportsExplicitSize);
        Assert.False(endpoint.Parameters.SupportsOutputFormat);
        Assert.True(endpoint.Parameters.SupportsImageCount);
        var opaque = AiImageEndpointPolicy.SelectEndpoint(endpoints, endpoint.ModelId, false, false, new(3000, 4500));
        Assert.NotNull(opaque);
        Assert.Equal("2:3", opaque.Options.AspectRatio);
        Assert.Null(AiImageEndpointPolicy.SelectEndpoint(endpoints, endpoint.ModelId, false, true, new(3000, 4500)));
    }

    [Fact]
    public async Task GetImageEndpointsAsync_MarksOnlyThePublishedProviderTagAsZdrCompatible()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """
                {"id":"image/model","endpoints":[
                  {"provider_name":"Provider A","provider_tag":"provider-a","supported_parameters":{"aspect_ratio":{"type":"enum","values":["1:1"]}}},
                  {"provider_name":"Provider B","provider_tag":"provider-b","supported_parameters":{"aspect_ratio":{"type":"enum","values":["1:1"]}}}
                ]}
                """),
            Json(HttpStatusCode.OK, """{"data":[{"model_id":"image/model","provider_name":"Provider B","tag":"provider-b"}]}"""));
        var client = CreateClient(handler);

        var endpoints = await client.GetImageEndpointsAsync("secret", "image/model", true, TestContext.Current.CancellationToken);

        Assert.False(Assert.Single(endpoints, endpoint => endpoint.EndpointId == "provider-a").ZeroDataRetentionCompatible);
        Assert.True(Assert.Single(endpoints, endpoint => endpoint.EndpointId == "provider-b").ZeroDataRetentionCompatible);
    }

    [Fact]
    public async Task ImageGenerateAsync_SendsOnePinnedRequestWithoutRetry()
    {
        var encoded = Convert.ToBase64String([1, 2, 3]);
        var handler = new RecordingHandler(Json(HttpStatusCode.OK,
            "{\"id\":\"img-1\",\"model\":\"resolved/model\",\"data\":[{\"b64_json\":\"" + encoded + "\",\"media_type\":\"image/png\"}],\"usage\":{\"prompt_tokens\":2,\"completion_tokens\":3,\"cost\":0.04}}"));
        var client = CreateClient(handler);

        var (result, failure) = await client.GenerateAsync(new AiImageGenerationRequest(
            "selected/model", "safe prompt", new AiImageSize(512, 512), true,
            "secret", true, "provider-tag"), TestContext.Current.CancellationToken);

        Assert.Null(failure);
        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result.ImageBytes);
        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("512x512", body.RootElement.GetProperty("size").GetString());
        Assert.Equal("transparent", body.RootElement.GetProperty("background").GetString());
        Assert.False(body.RootElement.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ImageGenerateAsync_SendsOnlyThePlannedAdvertisedParameters()
    {
        var encoded = Convert.ToBase64String([1, 2, 3]);
        var handler = new RecordingHandler(Json(HttpStatusCode.OK,
            "{\"data\":[{\"b64_json\":\"" + encoded + "\"}]}"));
        var client = CreateClient(handler);

        var (_, failure) = await client.GenerateAsync(new AiImageGenerationRequest(
            "openai/gpt-5.4-image-2", "safe prompt", new AiImageSize(1200, 1600), false,
            "secret", false, "openai", new AiImageGenerationOptions(
                AspectRatio: "3:4", Background: "opaque", Count: 1)), TestContext.Current.CancellationToken);

        Assert.Null(failure);
        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("3:4", body.RootElement.GetProperty("aspect_ratio").GetString());
        Assert.Equal("opaque", body.RootElement.GetProperty("background").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("n").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("size", out _));
        Assert.False(body.RootElement.TryGetProperty("resolution", out _));
        Assert.False(body.RootElement.TryGetProperty("output_format", out _));
    }

    [Fact]
    public async Task GetModelsAsync_FailsClosedWhenZdrListUnavailableWhileRequired()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """{"data":[]}"""),
            Json(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"temp"}}"""));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiModelCatalogFetchException>(
            () => client.GetModelsAsync("secret", true, TestContext.Current.CancellationToken));

        Assert.Equal(AiModelCatalogFailureKind.ZdrDataUnavailable, exception.Kind);
    }

    [Fact]
    public async Task GetModelsAsync_ToleratesNullNumericFields()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.OK, """
                {"data":[
                  {"id":"null/model","name":"Null","architecture":{"input_modalities":["text"],"output_modalities":["text"]},"supported_parameters":[],"context_length":null,"top_provider":{"max_completion_tokens":null},"pricing":{"prompt":null,"completion":null}},
                  {"id":"ok/model","name":"Ok","architecture":{"input_modalities":["text"],"output_modalities":["text"]},"supported_parameters":[],"context_length":4096,"top_provider":{"max_completion_tokens":1024},"pricing":{"prompt":"0","completion":"0"}}
                ]}
                """),
            Json(HttpStatusCode.OK, """{"data":[]}"""));
        var client = CreateClient(handler);

        var catalog = await client.GetModelsAsync("secret", false, TestContext.Current.CancellationToken);

        Assert.Equal(2, catalog.Models.Count);
        var nullModel = Assert.Single(catalog.Models, m => m.Id == "null/model");
        Assert.Null(nullModel.ContextLength);
        Assert.Null(nullModel.MaxCompletionTokens);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, AiModelCatalogFailureKind.Authentication)]
    [InlineData(HttpStatusCode.InternalServerError, AiModelCatalogFailureKind.NetworkOrService)]
    public async Task GetModelsAsync_MapsCatalogStatusFailures(
        HttpStatusCode status,
        AiModelCatalogFailureKind expected)
    {
        var handler = new RecordingHandler(Json(status, """{"error":{"message":"x"}}"""));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiModelCatalogFetchException>(
            () => client.GetModelsAsync("secret", false, TestContext.Current.CancellationToken));

        Assert.Equal(expected, exception.Kind);
        Assert.DoesNotContain("secret", exception.Message);
    }

    [Fact]
    public async Task GetModelsAsync_MapsRateLimitedWithRetryAfter()
    {
        var handler = new RecordingHandler(
            JsonWithRetry((HttpStatusCode)429, TimeSpan.FromSeconds(2), """{"error":{"message":"slow"}}"""),
            JsonWithRetry((HttpStatusCode)429, TimeSpan.FromSeconds(2), """{"error":{"message":"slow"}}"""));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiModelCatalogFetchException>(
            () => client.GetModelsAsync("secret", false, TestContext.Current.CancellationToken));

        Assert.Equal(AiModelCatalogFailureKind.RateLimited, exception.Kind);
        Assert.Equal(TimeSpan.FromSeconds(2), exception.RetryAfter);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AiCredentialValidationKind.Invalid)]
    [InlineData(HttpStatusCode.Forbidden, AiCredentialValidationKind.PermissionDenied)]
    [InlineData(HttpStatusCode.TooManyRequests, AiCredentialValidationKind.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AiCredentialValidationKind.ServiceUnavailable)]
    public async Task ValidateAsync_MapsKeyEndpointFailures(
        HttpStatusCode status,
        AiCredentialValidationKind expected)
    {
        var error = "{\"error\":{\"message\":\"secret-safe\"}}";
        var responses = status is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError
            ? new[] { Json(status, error), Json(status, error) }
            : new[] { Json(status, error) };
        var handler = new RecordingHandler(responses);
        var result = await CreateClient(handler).ValidateAsync("secret", TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Kind);
        Assert.DoesNotContain("secret", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetModelsAsync_CancellationPreventsAnyProviderCall()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, "{\"data\":[]}"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateClient(handler).GetModelsAsync("secret", true, new CancellationToken(canceled: true)));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GenerateAsync_SendsStrictPrivateTypedRequestAndNormalizesUsage()
    {
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, """
            {"id":"gen-1","model":"actual/model","choices":[{"message":{"content":"answer"},"finish_reason":"stop"}],"usage":{"prompt_tokens":2,"completion_tokens":3,"total_tokens":5,"cost":0.01}}
            """));
        var client = CreateClient(handler);
        var profile = AiProfileSettings.Empty with
        {
            ModelId = "requested/model",
            MaxCompletionTokens = 100,
            Temperature = 0.5,
            Seed = 7,
            StopSequences = ["END"],
            Reasoning = new AiReasoningSettings(AiReasoningMode.Effort, "high")
        };

        var result = await client.GenerateAsync(
            new AiProviderTextRequest(
                "secret",
                "requested/model",
                [new AiTextMessage(AiMessageRole.User, "hello")],
                profile,
                true),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("answer", result.Text);
        Assert.Equal("actual/model", result.ActualModel);
        Assert.Equal(5, result.Usage!.TotalTokens);
        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.True(body.RootElement.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
        Assert.True(body.RootElement.GetProperty("provider").GetProperty("zdr").GetBoolean());
        Assert.Equal("high", body.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.False(body.RootElement.GetProperty("reasoning").TryGetProperty("max_tokens", out _));
        Assert.False(body.RootElement.TryGetProperty("tools", out _));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GenerateAsync_TreatsHostileProviderTextAsBoundedData()
    {
        var hostile = "<script>throw new Error('execute')</script>";
        var handler = new RecordingHandler(Json(HttpStatusCode.OK, $$"""
            {"id":"gen-hostile","model":"model","choices":[{"message":{"content":"{{hostile}}"},"finish_reason":"stop"}]}
            """));
        var client = CreateClient(handler);

        var result = await client.GenerateAsync(
            new AiProviderTextRequest(
                "secret",
                "model",
                [new AiTextMessage(AiMessageRole.User, "prompt")],
                AiProfileSettings.Empty with { ModelId = "model" },
                false),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(hostile, result.Text);
        Assert.DoesNotContain("secret", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AiTextFailureKind.Authentication)]
    [InlineData(HttpStatusCode.PaymentRequired, AiTextFailureKind.InsufficientCredit)]
    [InlineData(HttpStatusCode.Forbidden, AiTextFailureKind.Blocked)]
    [InlineData((HttpStatusCode)429, AiTextFailureKind.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AiTextFailureKind.NoEligibleProvider)]
    public async Task GenerateAsync_MapsFailuresWithoutRetry(
        HttpStatusCode status,
        AiTextFailureKind expected)
    {
        var handler = new RecordingHandler(Json(status, """{"error":{"message":"safe error"}}"""));
        var client = CreateClient(handler);

        var result = await client.GenerateAsync(
            new AiProviderTextRequest(
                "secret",
                "model",
                [new AiTextMessage(AiMessageRole.User, "prompt")],
                AiProfileSettings.Empty with { ModelId = "model" },
                false),
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.FailureKind);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain("secret", result.Message);
        Assert.DoesNotContain("prompt", result.Message);
    }

    [Fact]
    public async Task SafeGet_RetriesAtMostOnce()
    {
        var handler = new RecordingHandler(
            Json(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"temporary"}}"""),
            Json(HttpStatusCode.OK, """{"data":{"is_management_key":false}}"""));
        var client = CreateClient(handler);

        var result = await client.ValidateAsync("secret", TestContext.Current.CancellationToken);

        Assert.Equal(AiCredentialValidationKind.Valid, result.Kind);
        Assert.Equal(2, handler.Requests.Count);
    }

    private static OpenRouterClient CreateClient(RecordingHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = OpenRouterClient.DefaultBaseAddress });

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage JsonWithRetry(HttpStatusCode status, TimeSpan retryAfter, string json)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
        return response;
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<RequestRecord> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RequestRecord(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return _responses.Dequeue();
        }
    }

    private sealed record RequestRecord(
        HttpMethod Method,
        Uri Uri,
        string? Scheme,
        string? Parameter,
        string? Body);
}
