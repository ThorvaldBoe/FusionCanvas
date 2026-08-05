using FusionCanvas.Application.Items;

namespace FusionCanvas.App.Items;

public sealed class NullItemCsvCodec : IItemCsvCodec
{
    public static NullItemCsvCodec Instance { get; } = new();

    private NullItemCsvCodec()
    {
    }

    public Task WriteAsync(
        Stream stream,
        IReadOnlyList<ItemCsvRow> rows,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "The item CSV export codec is not configured. The composition root must inject it.");
    }
}
