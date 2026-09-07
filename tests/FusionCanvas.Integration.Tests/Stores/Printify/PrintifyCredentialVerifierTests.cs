using System.Net;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public class PrintifyCredentialVerifierTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(200, "[]", PrintifyConfigurationKind.Verified)]
    [InlineData(200, "[{\"id\":1,\"title\":\"Shop\"}]", PrintifyConfigurationKind.Verified)]
    [InlineData(200, "{}", PrintifyConfigurationKind.UnexpectedResponse)]
    [InlineData(200, "<html>", PrintifyConfigurationKind.UnexpectedResponse)]
    [InlineData(401, "secret-sentinel", PrintifyConfigurationKind.InvalidKey)]
    [InlineData(403, "secret-sentinel", PrintifyConfigurationKind.PermissionDenied)]
    [InlineData(429, "secret-sentinel", PrintifyConfigurationKind.RateLimited)]
    [InlineData(503, "secret-sentinel", PrintifyConfigurationKind.NetworkFailure)]
    [InlineData(302, "secret-sentinel", PrintifyConfigurationKind.UnexpectedResponse)]
    public async Task Request_IsReadOnlyBoundedAndSafelyClassified(int status, string body, PrintifyConfigurationKind expected)
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((request, _) =>
        {
            calls++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.printify.com/v1/shops.json", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("synthetic-key", request.Headers.Authorization.Parameter);
            Assert.Contains("FusionCanvas", request.Headers.UserAgent.ToString());
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
        }));
        var result = await new PrintifyCredentialVerifier(client).VerifyAsync("synthetic-key", Ct);
        Assert.Equal(expected, result.Kind);
        Assert.DoesNotContain("secret-sentinel", result.Message);
        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OversizedResponse_IsRejected()
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string(' ', 1024 * 1024 + 1)) })));
        Assert.Equal(PrintifyConfigurationKind.UnexpectedResponse,
            (await new PrintifyCredentialVerifier(client).VerifyAsync("key", Ct)).Kind);
    }

    [Fact]
    public async Task SuccessfulResponse_ReturnsShopOptionsWithoutExtraData()
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\":42,\"title\":\"Main Shop\",\"sales_channel\":\"Shopify\"}]")
            })));

        var result = await new PrintifyCredentialVerifier(client).VerifyAsync("key", Ct);

        var shop = Assert.Single(result.Shops!);
        Assert.Equal(42, shop.Id);
        Assert.Equal("Main Shop", shop.Title);
        Assert.Equal("Main Shop (42)", shop.DisplayName);
    }

    [Fact]
    public async Task CancellationAndTransportFailure_DoNotRevealExceptions()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new HttpRequestException("secret-sentinel")));
        var verifier = new PrintifyCredentialVerifier(client);
        Assert.Equal(PrintifyConfigurationKind.NetworkFailure, (await verifier.VerifyAsync("key", Ct)).Kind);
        await Assert.ThrowsAsync<OperationCanceledException>(() => verifier.VerifyAsync("key", new CancellationToken(true)));
        using var timeoutClient = new HttpClient(new Handler((_, _) => throw new TaskCanceledException("secret-sentinel")));
        Assert.Equal(PrintifyConfigurationKind.NetworkFailure,
            (await new PrintifyCredentialVerifier(timeoutClient).VerifyAsync("key", Ct)).Kind);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
