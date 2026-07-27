namespace FusionCanvas.Application.Snowclones;

public interface ISnowcloneRepository
{
    Task<SnowcloneLibrarySnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SnowcloneLibrarySnapshot snapshot, CancellationToken cancellationToken = default);
}
