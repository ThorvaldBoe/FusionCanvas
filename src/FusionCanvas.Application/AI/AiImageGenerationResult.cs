namespace FusionCanvas.Application.AI;

public sealed record AiImageGenerationResult(byte[] ImageBytes, string MediaType, string Provider, string SelectedModelId, string ResolvedModelId, string? ProviderRequestId = null, AiImageUsage? Usage = null, IReadOnlyDictionary<string, string>? SafeMetadata = null);
