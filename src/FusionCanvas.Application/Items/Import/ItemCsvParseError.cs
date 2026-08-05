namespace FusionCanvas.Application.Items.Import;

public sealed record ItemCsvParseError(int LineNumber, string Message);
