namespace FusionCanvas.Application.Updates;

public interface IUpdateSource
{
    Task<UpdateManifest?> GetLatestAsync(CancellationToken cancellationToken = default);
}
