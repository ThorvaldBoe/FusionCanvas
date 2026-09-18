namespace FusionCanvas.Application.AI;

public sealed record AiImageProvenance(string Provider, string SelectedModelId, string ResolvedModelId, string Prompt, AiImageSize RequestedSize, AiImageSize FinalSize, bool TransparencyRequested, bool ResultHasTransparency, DateTimeOffset GeneratedAt, string? ProviderRequestId = null, AiImageUsage? Usage = null, IReadOnlyList<string>? Warnings = null, Guid? IntendedDesignAreaId = null);
