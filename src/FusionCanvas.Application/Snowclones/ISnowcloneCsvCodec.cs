namespace FusionCanvas.Application.Snowclones;

public interface ISnowcloneCsvCodec
{
    Task<SnowcloneCsvReadResult> ReadAsync(Stream stream, CancellationToken cancellationToken = default);

    Task WriteAsync(
        Stream stream,
        IReadOnlyList<SnowcloneCsvRow> rows,
        CancellationToken cancellationToken = default);
}
