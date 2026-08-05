namespace FusionCanvas.Application.Items.Import;

public sealed record ItemCsvImportRequest(
    ItemTopicReference Target,
    IReadOnlyList<ItemCsvRow> Rows);
