using System.Net;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public sealed class PrintifyListingImportClientTests
{
    [Fact]
    public async Task Shop_listing_discovery_follows_pages_and_uses_only_get_requests()
    {
        var requests = new List<string>();
        using var api = new HttpClient(new Handler((request, _) =>
        {
            requests.Add($"{request.Method} {request.RequestUri}");
            var second = request.RequestUri!.Query.Contains("page=2", StringComparison.Ordinal);
            return Task.FromResult(Json(second
                ? "{\"last_page\":2,\"data\":[{\"id\":\"p2\",\"title\":\"Second\",\"visible\":false,\"blueprint_id\":68,\"print_provider_id\":9}]}"
                : "{\"last_page\":2,\"data\":[{\"id\":\"p1\",\"title\":\"First\",\"visible\":true,\"blueprint_id\":68,\"print_provider_id\":9}]}") );
        }));
        using var images = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var client = new PrintifyListingImportClient(api, images);

        var products = await client.GetShopProductsAsync("synthetic-key", 42, TestContext.Current.CancellationToken);

        Assert.Equal(["p1", "p2"], products.Select(value => value.ProductId));
        Assert.Equal([true, false], products.Select(value => value.IsVisible));
        Assert.Equal(2, requests.Count);
        Assert.All(requests, request => Assert.StartsWith("GET ", request));
    }

    [Fact]
    public async Task Empty_shop_returns_empty_and_duplicate_product_identity_is_rejected()
    {
        using var emptyApi = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{\"last_page\":1,\"data\":[]}"))));
        using var images = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var empty = await new PrintifyListingImportClient(emptyApi, images).GetShopProductsAsync("synthetic-key", 42, TestContext.Current.CancellationToken);
        Assert.Empty(empty);

        using var duplicateApi = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{\"last_page\":1,\"data\":[{\"id\":\"same\",\"title\":\"A\",\"blueprint_id\":68,\"print_provider_id\":9},{\"id\":\"same\",\"title\":\"B\",\"blueprint_id\":68,\"print_provider_id\":9}]}"))));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PrintifyListingImportClient(duplicateApi, images).GetShopProductsAsync("synthetic-key", 42, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Product_detail_preserves_variant_artwork_identity_and_marks_layers()
    {
        const string body = """
        {"id":"p1","title":"Listing","description":"Description","visible":true,"blueprint_id":68,"print_provider_id":9,
         "options":[{"name":"Color","type":"color","values":[{"id":1,"title":"Black"}]}],
         "variants":[{"id":11,"title":"Black / M","price":2500,"is_enabled":true,"is_available":true,"options":[1]}],
         "print_areas":[{"variant_ids":[11],"placeholders":[{"position":"front","images":[{"id":"img-1","src":"https://images.printify.com/art/img-1.png?signature=secret","name":"Artwork","type":"image/png","variant_ids":[11]}]}]}]}
        """;
        using var api = new HttpClient(new Handler((_, _) => Task.FromResult(Json(body))));
        using var images = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var client = new PrintifyListingImportClient(api, images);

        var product = await client.GetProductAsync("synthetic-key", 42, "p1", TestContext.Current.CancellationToken);

        Assert.NotNull(product);
        Assert.Equal(11, Assert.Single(product.Variants).Id);
        var artwork = Assert.Single(Assert.Single(product.PrintAreas).Images);
        Assert.Equal("img-1", artwork.ImageId);
        Assert.Equal([11], artwork.VariantIds);
        Assert.False(Assert.Single(product.PrintAreas).HasUnsupportedLayers);
    }

    [Fact]
    public async Task Artwork_download_is_unauthed_bounded_and_requires_safe_https_and_valid_image_bytes()
    {
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };
        HttpRequestMessage? captured = null;
        using var api = new HttpClient(new Handler((_, _) => Task.FromResult(Json("[]"))));
        using var images = new HttpClient(new Handler((request, _) =>
        {
            captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(png) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") } }
            });
        }));
        var client = new PrintifyListingImportClient(api, images);

        var result = await client.DownloadArtworkAsync("https://images.printify.com/art.png", TestContext.Current.CancellationToken);

        Assert.Equal(".png", result.Extension);
        Assert.Equal(png, result.Content);
        Assert.Null(captured!.Headers.Authorization);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DownloadArtworkAsync("http://127.0.0.1/private", TestContext.Current.CancellationToken));

        using var badApi = new HttpClient(new Handler((_, _) => Task.FromResult(Json("[]"))));
        using var badImages = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") } }
        })));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PrintifyListingImportClient(badApi, badImages).DownloadArtworkAsync("https://images.printify.com/bad.png", TestContext.Current.CancellationToken));
    }

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value) };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }
}
