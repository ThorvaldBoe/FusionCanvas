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
        Assert.Equal("/api/v1/models/user", handler.Requests[0].Uri.AbsolutePath);
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

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AiModelCatalogFailureKind.Authentication)]
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
