using FusionCanvas.Application.AI;

namespace FusionCanvas.Integration.Testing;

public sealed class MockOpenRouterClient :
    IAiCredentialValidator,
    IAiModelCatalogProvider,
    IAiImageModelCatalogProvider,
    IAiImageEndpointCatalogProvider,
    IAiTextProvider,
    IAiImageGenerationProvider
{
    private static readonly byte[] DefaultImageBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    public MockOpenRouterClient()
    {
        ModelCatalog = CreateCatalog(requireZeroDataRetention: false, "mock/text-model");
        ImageModelCatalog = CreateCatalog(requireZeroDataRetention: true, "mock/image-model");
        ImageEndpoints =
        [
            new(
                "mock/image-endpoint",
                "mock/image-model",
                ZeroDataRetentionCompatible: true,
                SupportsImageOutput: true,
                RasterFormats: ["png"],
                SupportedSizes: [new AiImageSize(1024, 1024)],
                SupportsTransparency: true,
                ProviderName: "Synthetic Provider",
                Parameters: new(
                    AspectRatios: ["1:1"],
                    Resolutions: ["1024x1024"],
                    SupportsExplicitSize: true,
                    SupportsOutputFormat: true,
                    Backgrounds: ["transparent", "opaque"],
                    SupportsImageCount: false))
        ];
        TextResult = AiTextResult.Success(
            "Synthetic OpenRouter response.",
            "mock/text-model",
            actualModel: "mock/text-model",
            provider: "Synthetic Provider",
            finishReason: "stop",
            usage: new AiTextUsage(3, 4, 7, 0m),
            generationId: "mock-text-generation");
        ImageResult = new AiImageGenerationResult(
            DefaultImageBytes.ToArray(),
            "image/png",
            "Synthetic Provider",
            "mock/image-model",
            "mock/image-model",
            ProviderRequestId: "mock-image-generation",
            Usage: new AiImageUsage(1, 1, 0m));
    }

    public AiCredentialValidationResult CredentialValidationResult { get; set; } =
        new(AiCredentialValidationKind.Valid, "Synthetic OpenRouter key is valid.", 100m);

    public AiModelCatalog ModelCatalog { get; set; }

    public AiModelCatalog ImageModelCatalog { get; set; }

    public IReadOnlyList<AiImageEndpointCapabilities> ImageEndpoints { get; set; }

    public AiTextResult TextResult { get; set; }

    public AiImageGenerationResult? ImageResult { get; set; }

    public AiImageGenerationFailure? ImageFailure { get; set; }

    public AiModelCatalogFetchException? ModelCatalogFailure { get; set; }

    public AiModelCatalogFetchException? ImageModelCatalogFailure { get; set; }

    public List<TextRequestObservation> TextRequests { get; } = [];

    public List<ImageRequestObservation> ImageRequests { get; } = [];

    public List<ImageEndpointRequestObservation> ImageEndpointRequests { get; } = [];

    public int CredentialValidationCalls { get; private set; }

    public Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CredentialValidationCalls++;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(new AiCredentialValidationResult(
                AiCredentialValidationKind.Invalid,
                "Enter a synthetic OpenRouter API key."));
        }

        return Task.FromResult(CredentialValidationResult);
    }

    public Task<AiModelCatalog> GetModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ModelCatalogFailure is not null) throw ModelCatalogFailure;
        return Task.FromResult(ModelCatalog with { RequireZeroDataRetention = requireZeroDataRetention });
    }

    public Task<AiModelCatalog> GetImageModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ImageModelCatalogFailure is not null) throw ImageModelCatalogFailure;
        return Task.FromResult(ImageModelCatalog with { RequireZeroDataRetention = requireZeroDataRetention });
    }

    public Task<IReadOnlyList<AiImageEndpointCapabilities>> GetImageEndpointsAsync(
        string apiKey,
        string modelId,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ImageEndpointRequests.Add(new(modelId, requireZeroDataRetention));
        return Task.FromResult<IReadOnlyList<AiImageEndpointCapabilities>>(
            ImageEndpoints
                .Where(endpoint => string.Equals(endpoint.ModelId, modelId, StringComparison.Ordinal))
                .Where(endpoint => !requireZeroDataRetention || endpoint.ZeroDataRetentionCompatible)
                .ToArray());
    }

    public Task<AiTextResult> GenerateAsync(
        AiProviderTextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        TextRequests.Add(new(request.ModelId, request.Messages.ToArray(), request.Profile, request.RequireZeroDataRetention));
        return Task.FromResult(TextResult);
    }

    public Task<(AiImageGenerationResult? Result, AiImageGenerationFailure? Failure)> GenerateAsync(
        AiImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ImageRequests.Add(new(
            request.ModelId,
            request.Prompt,
            request.ProviderSize,
            request.TransparentBackground,
            request.RequireZeroDataRetention,
            request.ProviderTag,
            request.Options));
        return Task.FromResult((ImageFailure is null ? ImageResult : null, ImageFailure));
    }

    private static AiModelCatalog CreateCatalog(bool requireZeroDataRetention, string modelId) =>
        new(
            requireZeroDataRetention,
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            [new(
                modelId,
                modelId.Contains("image", StringComparison.Ordinal) ? "Synthetic Image Model" : "Synthetic Text Model",
                "Synthetic Provider",
                "A deterministic model descriptor for tests.",
                ["text"],
                modelId.Contains("image", StringComparison.Ordinal) ? ["image"] : ["text"],
                [],
                4096,
                1024,
                0m,
                0m,
                true,
                null)]);

    public sealed record TextRequestObservation(
        string ModelId,
        IReadOnlyList<AiTextMessage> Messages,
        AiProfileSettings Profile,
        bool RequireZeroDataRetention);

    public sealed record ImageRequestObservation(
        string ModelId,
        string Prompt,
        AiImageSize ProviderSize,
        bool TransparentBackground,
        bool RequireZeroDataRetention,
        string? ProviderTag,
        AiImageGenerationOptions? Options);

    public sealed record ImageEndpointRequestObservation(string ModelId, bool RequireZeroDataRetention);
}
