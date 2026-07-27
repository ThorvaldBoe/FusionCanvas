namespace FusionCanvas.Application.Snowclones;

public interface IBundledSnowcloneSource
{
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
}
