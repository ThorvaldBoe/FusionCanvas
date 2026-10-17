namespace FusionCanvas.Application.Updates;

public interface IUpdateSource
{
    Task<IReadOnlyList<UpdateRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<UpdateManifest?> GetManifestAsync(UpdateRelease release, CancellationToken cancellationToken = default);
}
