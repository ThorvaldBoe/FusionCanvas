using System.Net;
using System.Text;
using FusionCanvas.Application.Listings;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public sealed class PrintifyListingClientTests
{
    [Fact]
    public async Task Connection_and_create_use_shared_authenticated_transport_and_product_payload()
    {
        var storeId = Guid.NewGuid();
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/shops/42.json", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, "{\"id\":42}")
            : Json(HttpStatusCode.Created, "{\"id\":\"product-1\",\"visible\":false,\"is_locked\":false}"));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(storeId, http, _ => Task.FromResult<string?>("secret-sentinel"));

        var readiness = await client.CheckAsync(new ListingConnectionRequest(storeId, "42", false), TestContext.Current.CancellationToken);
        var mutation = await client.CreateAsync("42", Projection(), new Dictionary<Guid, ListingImageReference>(), TestContext.Current.CancellationToken);

        Assert.True(readiness.ProductOperationsAvailable);
        Assert.True(mutation.IsDefinitive);
        Assert.Equal("product-1", mutation.ProductId);
        Assert.All(handler.Requests, request => Assert.Equal("secret-sentinel", request.Headers.Authorization!.Parameter));
        Assert.Contains(handler.Bodies, body => body.Contains("\"blueprint_id\":68", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"print_provider_id\":23", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"id\":33719", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"shipping_template_id\":\"shipping-template-1\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Network_failure_is_marked_ambiguous_for_mutation()
    {
        var client = new PrintifyListingClient(Guid.NewGuid(), new HttpClient(new ThrowingHandler()), _ => Task.FromResult<string?>("key"));

        var result = await client.CreateAsync("42", Projection(), new Dictionary<Guid, ListingImageReference>(), TestContext.Current.CancellationToken);

        Assert.False(result.IsDefinitive);
        Assert.Equal(PrintifyTransportOutcome.NetworkFailure.ToString(), result.ErrorCode);
    }

    [Fact]
    public async Task Product_read_maps_visibility_external_identity_and_shipping_template()
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"id\":\"product-1\",\"visible\":true,\"is_locked\":false,\"external\":[{\"id\":\"shopify-1\",\"handle\":\"dad-joke-loading\",\"shipping_template_id\":\"shipping-template-1\"}]}")));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var product = await client.GetAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal(ListingPublicationState.Published, product!.PublicationState);
        Assert.Equal("shopify-1", product.ExternalSalesChannelId);
        Assert.Equal("dad-joke-loading", product.ExternalHandle);
        Assert.Equal("shipping-template-1", product.Snapshot.Fields["shippingProfile"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, PrintifyTransportOutcome.InvalidCredential)]
    [InlineData(HttpStatusCode.Forbidden, PrintifyTransportOutcome.PermissionDenied)]
    [InlineData(HttpStatusCode.NotFound, PrintifyTransportOutcome.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, PrintifyTransportOutcome.RateLimited)]
    [InlineData(HttpStatusCode.Locked, PrintifyTransportOutcome.Locked)]
    [InlineData(HttpStatusCode.BadRequest, PrintifyTransportOutcome.UnexpectedResponse)]
    [InlineData(HttpStatusCode.InternalServerError, PrintifyTransportOutcome.NetworkFailure)]
    public async Task Mutation_failures_are_classified_without_exposing_response_body(HttpStatusCode status, PrintifyTransportOutcome expected)
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(status, PrintifyApiFixtures.ValidationError)));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("secret-sentinel"));

        var result = await client.CreateAsync("42", Projection(), new Dictionary<Guid, ListingImageReference>(), TestContext.Current.CancellationToken);

        Assert.Equal(expected.ToString(), result.ErrorCode);
        Assert.DoesNotContain("variant is invalid", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-sentinel", result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(expected != PrintifyTransportOutcome.NetworkFailure, result.IsDefinitive);
    }

    [Fact]
    public async Task Malformed_success_response_is_definitive_but_does_not_create_an_identity()
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.Created, PrintifyApiFixtures.Malformed)));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var result = await client.CreateAsync("42", Projection(), new Dictionary<Guid, ListingImageReference>(), TestContext.Current.CancellationToken);

        Assert.True(result.IsDefinitive);
        Assert.Null(result.ProductId);
        Assert.Null(result.ErrorCode);
    }

    private static ListingProductProjection Projection() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Dad Joke Loading… – T-shirt",
        "A joke shirt.",
        "shipping-template-1",
        null,
        new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, 30m),
        [new ListingVariantProjection(Guid.NewGuid(), "Black / S", "Black", [], 10m, new ListingPrice(Guid.NewGuid(), 10m, 30m)) { ExternalVariantId = 33719 }],
        [])
    {
        ExternalBlueprintId = 68,
        ExternalProviderId = 23
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return responder(request);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("synthetic outage");
    }
}
