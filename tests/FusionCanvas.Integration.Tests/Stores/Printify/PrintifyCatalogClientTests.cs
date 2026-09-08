using System.Net;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public sealed class PrintifyCatalogClientTests
{
    [Fact]
    public async Task LoadsBlueprintSummariesWithoutMutatingRequests()
    {
        var requests = new List<HttpRequestMessage>();
        using var client = new HttpClient(new Handler((request, _) =>
        {
            requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\":68,\"title\":\"Tee\",\"description\":\"Cotton\",\"brand\":\"Gildan\",\"model\":\"5000\"}]")
            });
        })) { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };

        var result = await new PrintifyCatalogClient(client).LoadBlueprintsAsync("synthetic-key", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var summary = Assert.Single(result.Blueprints!);
        Assert.Equal(68, summary.Id);
        Assert.Equal("Tee", summary.Title);
        var request = Assert.Single(requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.printify.com/v1/catalog/blueprints.json", request.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("synthetic-key", request.Headers.Authorization.Parameter);
        Assert.Null(client.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public async Task LoadsSelectedBlueprintProvidersVariantsAndPlaceholders()
    {
        using var client = new HttpClient(new Handler((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = path.EndsWith("blueprints.json", StringComparison.Ordinal)
                ? "[{\"id\":68,\"title\":\"Tee\"}]"
                : path.EndsWith("print_providers.json", StringComparison.Ordinal)
                    ? "[{\"id\":9,\"title\":\"Provider\"}]"
                    : "[{\"id\":33719,\"title\":\"Black / M\",\"is_enabled\":true,\"is_available\":true,\"options\":[1],\"placeholders\":[{\"position\":\"front\",\"decoration_method\":\"dtg\",\"width\":4350,\"height\":5850}]}]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        })) { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };

        var result = await new PrintifyCatalogClient(client).LoadSelectedAsync("synthetic-key", [68], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var blueprint = Assert.Single(result.SelectedCatalog!);
        var provider = Assert.Single(blueprint.Providers);
        var variant = Assert.Single(provider.Variants);
        var placeholder = Assert.Single(variant.Placeholders);
        Assert.Equal(9, provider.Id);
        Assert.Equal(33719, variant.Id);
        Assert.Equal(4350, placeholder.Width);
        Assert.Equal(5850, placeholder.Height);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, PrintifyCatalogResultKind.InvalidKey)]
    [InlineData(HttpStatusCode.Forbidden, PrintifyCatalogResultKind.PermissionDenied)]
    [InlineData(HttpStatusCode.TooManyRequests, PrintifyCatalogResultKind.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, PrintifyCatalogResultKind.NetworkFailure)]
    [InlineData(HttpStatusCode.Redirect, PrintifyCatalogResultKind.UnexpectedResponse)]
    public async Task MapsProviderFailuresWithoutLeakingResponseBody(HttpStatusCode status, PrintifyCatalogResultKind expected)
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(status) { Content = new StringContent("secret-sentinel") })))
        { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };

        var result = await new PrintifyCatalogClient(client).LoadBlueprintsAsync("synthetic-key", TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Kind);
        Assert.DoesNotContain("secret-sentinel", result.Message);
    }

    [Fact]
    public async Task RejectsInvalidJsonAndOversizedResponses()
    {
        using var invalid = new HttpClient(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") })))
        { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };
        Assert.Equal(PrintifyCatalogResultKind.UnexpectedResponse,
            (await new PrintifyCatalogClient(invalid).LoadBlueprintsAsync("key", TestContext.Current.CancellationToken)).Kind);

        using var oversized = new HttpClient(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 4 * 1024 * 1024 + 1)) })))
        { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };
        Assert.Equal(PrintifyCatalogResultKind.UnexpectedResponse,
            (await new PrintifyCatalogClient(oversized).LoadBlueprintsAsync("key", TestContext.Current.CancellationToken)).Kind);
    }

    [Fact]
    public async Task HonorsCallerCancellationBeforeSendingARequest()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        })) { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new PrintifyCatalogClient(client).LoadBlueprintsAsync("key", new CancellationToken(true)));
        Assert.Equal(0, calls);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
