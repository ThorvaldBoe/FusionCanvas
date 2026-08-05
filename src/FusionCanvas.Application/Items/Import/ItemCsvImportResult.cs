namespace FusionCanvas.Application.Items.Import;

public sealed record ItemCsvImportResult(
    bool Succeeded,
    int ImportedCount,
    IReadOnlyList<string> Errors)
{
    public static ItemCsvImportResult Success(int importedCount) =>
        new(true, importedCount, []);

    public static ItemCsvImportResult Failure(IReadOnlyList<string> errors) =>
        new(false, 0, errors);

    public static ItemCsvImportResult Failure(string error) =>
        new(false, 0, [error]);
}
