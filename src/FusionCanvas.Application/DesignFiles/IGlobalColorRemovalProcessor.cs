namespace FusionCanvas.Application.DesignFiles;

public interface IGlobalColorRemovalProcessor
{
    Task ValidateAsync(
        Stream source,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalRasterPreview> PreviewAsync(
        Stream source,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalRasterResult> ApplyAsync(
        Stream source,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalColor?> SampleAsync(
        Stream source,
        int x,
        int y,
        CancellationToken cancellationToken = default);
}
