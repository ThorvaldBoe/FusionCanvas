namespace FusionCanvas.Application.Items.Import;

public sealed record ItemCsvParseResult(
    IReadOnlyList<ItemCsvRow> Rows,
    IReadOnlyList<ItemCsvParseError> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public IReadOnlyList<string> ErrorText =>
        Errors.Select(error => $"Error on line {error.LineNumber}").ToArray();
}
