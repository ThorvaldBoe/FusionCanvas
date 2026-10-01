using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.Testing;

namespace FusionCanvas.Integration.Tests.Testing;

public sealed class MockExternalApiServiceTests
{
    [Fact]
    public void MocksImplementTheCompleteCurrentExternalServiceContracts()
    {
        Assert.Contains(typeof(IAiCredentialValidator), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IAiModelCatalogProvider), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IAiImageModelCatalogProvider), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IAiImageEndpointCatalogProvider), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IAiTextProvider), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IAiImageGenerationProvider), typeof(MockOpenRouterClient).GetInterfaces());
        Assert.Contains(typeof(IPrintifyCatalogClient), typeof(MockPrintifyCatalogClient).GetInterfaces());
        Assert.Contains(typeof(IPrintifyCredentialVerifier), typeof(MockPrintifyCredentialVerifier).GetInterfaces());
    }

    [Fact]
    public async Task OpenRouterMockReturnsStableSyntheticDataAndObservesRequestsWithoutTheKey()
    {
        var mock = new MockOpenRouterClient();
        var request = new AiProviderTextRequest(
            "synthetic-openrouter-key",
            "mock/text-model",
            [new(AiMessageRole.User, "synthetic prompt")],
            AiProfileSettings.Empty with { ModelId = "mock/text-model" },
            RequireZeroDataRetention: true);

        var first = await mock.GenerateAsync(request, TestContext.Current.CancellationToken);
        var second = await mock.GenerateAsync(request, TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.Equal(first.Text, second.Text);
        Assert.Equal(2, mock.TextRequests.Count);
        var observation = JsonSerializer.Serialize(mock.TextRequests);
        Assert.DoesNotContain("synthetic-openrouter-key", observation, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", observation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenRouterMockSupportsConfiguredFailuresAndCancellation()
    {
        var mock = new MockOpenRouterClient
        {
            TextResult = AiTextResult.Failure(AiTextFailureKind.RateLimited, "Synthetic rate limit.", "mock/text-model"),
            ImageFailure = new(AiImageGenerationFailureKind.Unavailable, "Synthetic image service unavailable.")
        };

        var text = await mock.GenerateAsync(
            new AiProviderTextRequest(
                "synthetic-key",
                "mock/text-model",
                [new(AiMessageRole.User, "prompt")],
                AiProfileSettings.Empty,
                false),
            TestContext.Current.CancellationToken);
        var image = await mock.GenerateAsync(
            new AiImageGenerationRequest("mock/image-model", "prompt", new(1024, 1024), false, "synthetic-key"),
            TestContext.Current.CancellationToken);

        Assert.Equal(AiTextFailureKind.RateLimited, text.FailureKind);
        Assert.Null(image.Result);
        Assert.Equal(AiImageGenerationFailureKind.Unavailable, image.Failure!.Kind);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mock.GetModelsAsync("synthetic-key", false, new CancellationToken(canceled: true)));
        Assert.DoesNotContain(mock.TextRequests, observation => observation.ModelId == "never-called");
    }

    [Fact]
    public async Task PrintifyCatalogMockSupportsSyntheticSelectionAndSecretSafeObservations()
    {
        var mock = new MockPrintifyCatalogClient();
        var products = await mock.LoadShopProductsAsync("synthetic-printify-key", 9001, TestContext.Current.CancellationToken);
        var selected = await mock.LoadSelectedProductsAsync(
            "synthetic-printify-key",
            9001,
            ["mock-product-1"],
            TestContext.Current.CancellationToken);

        Assert.True(products.Succeeded);
        Assert.Equal("mock-product-1", Assert.Single(products.Products!).ProductId);
        Assert.True(selected.Succeeded);
        Assert.Equal(1001, Assert.Single(selected.SelectedProducts!).Summary.Id);
        Assert.Contains(mock.Requests, request => request.Operation == nameof(mock.LoadShopProductsAsync) && request.ShopId == 9001);
        var observations = JsonSerializer.Serialize(mock.Requests);
        Assert.DoesNotContain("synthetic-printify-key", observations, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrintifyMocksSupportConfiguredFailuresAndCancellation()
    {
        var catalog = new MockPrintifyCatalogClient
        {
            ShopProductsResultOverride = new(PrintifyCatalogResultKind.NetworkFailure, "Synthetic outage.")
        };
        var verifier = new MockPrintifyCredentialVerifier
        {
            Result = new(PrintifyConfigurationKind.PermissionDenied, "Synthetic permission failure.")
        };

        var load = await catalog.LoadShopProductsAsync("synthetic-key", 9001, TestContext.Current.CancellationToken);
        var verify = await verifier.VerifyAsync("synthetic-key", TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.NetworkFailure, load.Kind);
        Assert.Equal(PrintifyConfigurationKind.PermissionDenied, verify.Kind);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            verifier.VerifyAsync("synthetic-key", new CancellationToken(canceled: true)));
        Assert.Equal(1, verifier.VerificationCalls);
    }

    [Fact]
    public async Task AiTextApplicationServiceCanUseTheReusableOpenRouterMock()
    {
        var provider = new MockOpenRouterClient();
        var settings = new TestAiConfigurationProvider();
        var credentials = new TestAiCredentialReader();
        var catalog = new TestAiModelCatalogReader(provider.ModelCatalog);
        var service = new AiTextGenerationService(settings, credentials, catalog, provider);

        var result = await service.GenerateAsync(
            new AiTextRequest(
                AiRequestPurpose.Ideation,
                [new(AiMessageRole.User, "synthetic prompt")]),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("Synthetic OpenRouter response.", result.Text);
        Assert.Single(provider.TextRequests);
    }

    private sealed class TestAiConfigurationProvider : IAiConfigurationProvider
    {
        public AiConfigurationSettings Current { get; } = AiConfigurationSettings.Default with
        {
            RequireZeroDataRetention = false,
            General = AiProfileSettings.Empty with { ModelId = "mock/text-model" }
        };

        public IReadOnlyList<AiModelDescriptor> AvailableModels { get; } =
        [new(
            "mock/text-model",
            "Synthetic Text Model",
            "Synthetic Provider",
            "Synthetic model for tests.",
            ["text"],
            ["text"],
            [],
            4096,
            1024,
            0m,
            0m,
            true,
            null)];

        public Task<string?> ReadApiKeyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("synthetic-key");

        public Task<IReadOnlyList<AiImageEndpointCapabilities>> GetArtworkEndpointsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiImageEndpointCapabilities>>([]);
    }

    private sealed class TestAiCredentialReader : IAiCredentialReader
    {
        public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCredentialReadResult(AiCredentialStateKind.Available, "synthetic-key"));
    }

    private sealed class TestAiModelCatalogReader(AiModelCatalog catalog) : IAiModelCatalogReader
    {
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult<AiModelCatalog?>(catalog with { RequireZeroDataRetention = requireZeroDataRetention });
    }
}
