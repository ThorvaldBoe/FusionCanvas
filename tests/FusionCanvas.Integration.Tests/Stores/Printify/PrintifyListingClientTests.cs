using System.Net;
using System.Text;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public sealed class PrintifyListingClientTests
{
    [Fact]
    public async Task Connection_and_create_use_shared_authenticated_transport_and_product_payload()
    {
        var storeId = Guid.NewGuid();
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/shops.json", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, "[{\"id\":42,\"title\":\"Main Shop\"}]")
            : Json(HttpStatusCode.Created, "{\"id\":\"product-1\",\"visible\":false,\"is_locked\":false}"));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(storeId, http, _ => Task.FromResult<string?>("secret-sentinel"));

        var readiness = await client.CheckAsync(new ListingConnectionRequest(storeId, "42", false), TestContext.Current.CancellationToken);
        var mutation = await client.CreateAsync("42", Projection(), new Dictionary<Guid, ListingImageReference>(), TestContext.Current.CancellationToken);

        Assert.True(readiness.ProductOperationsAvailable);
        Assert.EndsWith("/v1/shops.json", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.True(mutation.IsDefinitive);
        Assert.Equal("product-1", mutation.ProductId);
        Assert.All(handler.Requests, request => Assert.Equal("secret-sentinel", request.Headers.Authorization!.Parameter));
        Assert.Contains(handler.Bodies, body => body.Contains("\"blueprint_id\":68", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"print_provider_id\":23", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"id\":33719", StringComparison.Ordinal));
        Assert.Contains(handler.Bodies, body => body.Contains("\"shipping_template_id\":\"shipping-template-1\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Shop_product_reconciliation_reads_all_pages_and_parses_matching_identity_fields()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, "{\"current_page\":1,\"last_page\":2,\"data\":[{\"id\":\"product-1\",\"title\":\"Dad Joke Shirt\",\"blueprint_id\":68,\"print_provider_id\":23}]}")
            : Json(HttpStatusCode.OK, "{\"current_page\":2,\"last_page\":2,\"data\":[{\"id\":\"product-2\",\"title\":\"Other Shirt\",\"blueprint_id\":69,\"print_provider_id\":24}]}"));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var products = await client.ListShopProductsAsync("42", TestContext.Current.CancellationToken);

        Assert.Equal(2, products.Count);
        Assert.Equal(new ListingRemoteProductSummary("product-1", "Dad Joke Shirt", 68, 23), products[0]);
        Assert.Equal(new ListingRemoteProductSummary("product-2", "Other Shirt", 69, 24), products[1]);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/shops/42/products.json?limit=50&page=1", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Contains("/shops/42/products.json?limit=50&page=2", handler.Requests[1].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Printify_listing_requests_are_recorded_in_enabled_debug_telemetry()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"current_page\":1,\"last_page\":1,\"data\":[{\"id\":\"product-1\",\"title\":\"Dad Joke Shirt\",\"blueprint_id\":68,\"print_provider_id\":23}]}"));
        var telemetry = new RecordingTelemetryRecorder();
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("secret-sentinel"), telemetry: telemetry);

        await client.ListShopProductsAsync("42", TestContext.Current.CancellationToken);

        var request = Assert.Single(telemetry.Requests);
        Assert.Equal("Integration.Printify", request.Area);
        Assert.Equal("HttpRequest", request.Name);
        using var requestDetails = System.Text.Json.JsonDocument.Parse(request.RequestDetailsJson!);
        Assert.EndsWith("/shops/42/products.json?limit=50&page=1", requestDetails.RootElement.GetProperty("uri").GetString());
        Assert.DoesNotContain("secret-sentinel", request.RequestDetailsJson);
    }

    [Fact]
    public async Task Product_update_includes_artwork_when_another_field_changed()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"id\":\"product-1\"}"));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));
        var assetId = Guid.NewGuid();
        var projection = Projection() with
        {
            Artwork =
            [
                new ListingArtworkProjection(Guid.NewGuid(), assetId, "artwork/front.png", new ArtworkDimensions(1000, 1000), new ArtworkDimensions(4500, 5400), new ArtworkPlacement(1, 0.5, 0.5, 0))
                {
                    Position = "front"
                }
            ]
        };

        var result = await client.UpdateAsync("42", "product-1", projection,
            new Dictionary<Guid, ListingImageReference> { [assetId] = new("uploaded-image-1", "fingerprint") },
            new HashSet<string>(["title"], StringComparer.Ordinal), TestContext.Current.CancellationToken);

        Assert.True(result.IsDefinitive);
        Assert.Equal(HttpMethod.Put, handler.Requests.Single().Method);
        using var payload = System.Text.Json.JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal(["title", "print_areas"], payload.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(Projection().Title, payload.RootElement.GetProperty("title").GetString());
        var area = Assert.Single(payload.RootElement.GetProperty("print_areas").EnumerateArray());
        Assert.Equal("front", area.GetProperty("placeholders")[0].GetProperty("position").GetString());
        Assert.Equal("uploaded-image-1", area.GetProperty("placeholders")[0].GetProperty("images")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Publish_sends_documented_field_selection_and_unpublish_posts_without_a_body()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, string.Empty));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var published = await client.PublishAsync("42", "product-1", TestContext.Current.CancellationToken);
        var unpublished = await client.UnpublishAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.True(published.IsDefinitive);
        Assert.True(unpublished.IsDefinitive);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/products/product-1/publish.json", handler.Requests[0].RequestUri!.AbsolutePath);
        using var publishPayload = System.Text.Json.JsonDocument.Parse(handler.Bodies[0]);
        Assert.All(new[] { "title", "description", "images", "variants", "tags", "keyFeatures", "shipping_template" },
            field => Assert.True(publishPayload.RootElement.GetProperty(field).GetBoolean()));
        Assert.EndsWith("/products/product-1/unpublish.json", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.Bodies[1]);
    }

    [Fact]
    public async Task Publishing_failed_sends_a_reason_to_the_documented_unlock_endpoint()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, string.Empty));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var result = await client.PublishingFailedAsync("42", "product-1", "No connected channel handler.", TestContext.Current.CancellationToken);

        Assert.True(result.IsDefinitive);
        Assert.EndsWith("/products/product-1/publishing_failed.json", handler.Requests.Single().RequestUri!.AbsolutePath);
        using var payload = System.Text.Json.JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("No connected channel handler.", payload.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Publishing_succeeded_sends_external_channel_identity()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, string.Empty));
        using var http = new HttpClient(handler);
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var result = await client.PublishingSucceededAsync("42", "product-1", "channel-product-1", "https://example.com/products/product-1", TestContext.Current.CancellationToken);

        Assert.True(result.IsDefinitive);
        Assert.EndsWith("/products/product-1/publishing_succeeded.json", handler.Requests.Single().RequestUri!.AbsolutePath);
        using var payload = System.Text.Json.JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("channel-product-1", payload.RootElement.GetProperty("external").GetProperty("id").GetString());
        Assert.Equal("https://example.com/products/product-1", payload.RootElement.GetProperty("external").GetProperty("handle").GetString());
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
    public async Task Product_read_maps_external_publication_identity_and_shipping_template()
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"id\":\"product-1\",\"shop_id\":42,\"visible\":true,\"is_locked\":false,\"external\":[{\"id\":\"shopify-1\",\"handle\":\"dad-joke-loading\",\"shipping_template_id\":\"shipping-template-1\"}]}")));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var product = await client.GetAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal(ListingPublicationState.Published, product!.PublicationState);
        Assert.Equal("shopify-1", product.ExternalSalesChannelId);
        Assert.Equal("dad-joke-loading", product.ExternalHandle);
        Assert.Equal("shipping-template-1", product.Snapshot.Fields["shippingProfile"]);
        Assert.Equal("42", product.ShopId);
        Assert.True(product.HasVerifiedIdentity);
    }

    [Theory]
    [InlineData("{\"visible\":true}", ListingPublicationState.Unpublished)]
    [InlineData("{\"visible\":true,\"external\":[]}", ListingPublicationState.Unpublished)]
    [InlineData("{\"visible\":false,\"external\":[{\"id\":\"channel-product-1\"}]}", ListingPublicationState.Published)]
    [InlineData("{\"visible\":true,\"external\":\"unexpected\"}", ListingPublicationState.Unverified)]
    public async Task Product_read_does_not_treat_visibility_as_channel_publication(string productJson, ListingPublicationState expected)
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, productJson)));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var product = await client.GetAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal(expected, product!.PublicationState);
    }

    [Fact]
    public async Task Product_read_ignores_disabled_variant_prices_for_drift_snapshot()
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK,
            "{\"id\":\"product-1\",\"variants\":[{\"id\":33719,\"price\":1000,\"is_enabled\":true},{\"id\":33720,\"price\":1200,\"is_enabled\":false}]}")));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var product = await client.GetAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal("33719:10", product!.Snapshot.Fields["variants"]);
    }

    [Fact]
    public async Task Connection_is_unavailable_when_selected_shop_is_missing()
    {
        var storeId = Guid.NewGuid();
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, "[{\"id\":84,\"title\":\"Other Shop\"}]")));
        var client = new PrintifyListingClient(storeId, http, _ => Task.FromResult<string?>("key"));

        var readiness = await client.CheckAsync(new ListingConnectionRequest(storeId, "42", false), TestContext.Current.CancellationToken);

        Assert.False(readiness.ProductOperationsAvailable);
        Assert.Contains("not available", readiness.Issues.Single().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[{\"title\":\"No ID\"}]")]
    [InlineData("<html>")]
    public async Task Connection_is_unavailable_when_shop_list_is_invalid(string body)
    {
        var storeId = Guid.NewGuid();
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, body)));
        var client = new PrintifyListingClient(storeId, http, _ => Task.FromResult<string?>("key"));

        var readiness = await client.CheckAsync(new ListingConnectionRequest(storeId, "42", false), TestContext.Current.CancellationToken);

        Assert.False(readiness.ProductOperationsAvailable);
        Assert.Contains("invalid shop", readiness.Issues.Single().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{\"id\":\"other-product\",\"shop_id\":42}", false)]
    [InlineData("{\"id\":\"product-1\",\"shop_id\":84}", false)]
    [InlineData("{\"shop_id\":42}", false)]
    [InlineData("{\"id\":\"product-1\",\"shop_id\":42}", true)]
    public async Task Product_read_retains_identity_validation_data(string body, bool expectedMatch)
    {
        using var http = new HttpClient(new RecordingHandler(_ => Json(HttpStatusCode.OK, body)));
        var client = new PrintifyListingClient(Guid.NewGuid(), http, _ => Task.FromResult<string?>("key"));

        var product = await client.GetAsync("42", "product-1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal(expectedMatch, product!.HasVerifiedIdentity && product.ShopId == "42");
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

    private sealed class RecordingTelemetryRecorder : ITelemetryRecorder
    {
        public bool IsCaptureEnabled => true;
        public List<TelemetryEventRequest> Requests { get; } = [];

        public Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}

