namespace FusionCanvas.Application.AI;

public interface IAiConfigurationProvider
{
    AiConfigurationSettings Current { get; }

    IReadOnlyList<AiModelDescriptor> AvailableModels { get; }

    Task<string?> ReadApiKeyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiImageEndpointCapabilities>> GetArtworkEndpointsAsync(
        CancellationToken cancellationToken = default);
}
