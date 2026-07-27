namespace FusionCanvas.Application.AI;

public interface IAiCredentialStore
{
    Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default);
    Task<AiCredentialOperationResult> SaveAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default);
}

public interface IAiCredentialValidator
{
    Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}

public interface IAiModelCatalogProvider
{
    Task<AiModelCatalog> GetModelsAsync(
        string apiKey,
        bool requireZeroDataRetention,
        CancellationToken cancellationToken = default);
}

public interface IAiModelCatalogCache
{
    Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default);
    Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default);
}

public interface IAiConfigurationProvider
{
    AiConfigurationSettings Current { get; }
}

public interface IAiTextProvider
{
    Task<AiTextResult> GenerateAsync(
        AiProviderTextRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiTextGenerationService
{
    Task<AiTextResult> GenerateAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default);
}
