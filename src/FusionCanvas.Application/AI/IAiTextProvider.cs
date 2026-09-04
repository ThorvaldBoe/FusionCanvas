namespace FusionCanvas.Application.AI;

public interface IAiTextProvider
{
    Task<AiTextResult> GenerateAsync(
        AiProviderTextRequest request,
        CancellationToken cancellationToken = default);
}
