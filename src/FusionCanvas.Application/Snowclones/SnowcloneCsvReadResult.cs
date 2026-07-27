namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneCsvReadResult(
    bool Succeeded,
    IReadOnlyList<SnowcloneCsvRow> Rows,
    string? Error)
{
    public static SnowcloneCsvReadResult Success(IReadOnlyList<SnowcloneCsvRow> rows) =>
        new(true, rows, null);

    public static SnowcloneCsvReadResult Failure(string error) =>
        new(false, [], error);
}
