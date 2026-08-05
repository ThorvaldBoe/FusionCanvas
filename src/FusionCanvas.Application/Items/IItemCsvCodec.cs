namespace FusionCanvas.Application.Items;

public interface IItemCsvCodec
{
    Task WriteAsync(
        Stream stream,
        IReadOnlyList<ItemCsvRow> rows,
        CancellationToken cancellationToken = default);
}
