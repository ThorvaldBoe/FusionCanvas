namespace FusionCanvas.Application.DesignFiles;

public interface IArtworkGenerationService
{
    Task<DesignStageResult> GenerateAsync(ArtworkGenerationRequest request, CancellationToken cancellationToken = default);
}
