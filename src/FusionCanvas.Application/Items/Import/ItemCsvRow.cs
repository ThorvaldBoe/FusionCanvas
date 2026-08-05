namespace FusionCanvas.Application.Items.Import;

public sealed record ItemCsvRow(
    string Title,
    string? BaseIdea,
    string? ConceptIdea,
    string? Phrase,
    string? Graphic,
    string? Notes,
    IReadOnlyList<string> Tags,
    int LineNumber);
