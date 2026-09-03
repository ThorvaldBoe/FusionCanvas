namespace FusionCanvas.Application.AI;

public sealed class AiModelCatalogFetchException : Exception
{
    public AiModelCatalogFetchException(AiModelCatalogFailureKind kind, string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        Kind = kind;
        RetryAfter = retryAfter;
    }

    public AiModelCatalogFailureKind Kind { get; }

    public TimeSpan? RetryAfter { get; }
}
