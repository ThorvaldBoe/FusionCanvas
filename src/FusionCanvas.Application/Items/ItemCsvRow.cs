namespace FusionCanvas.Application.Items;

public sealed record ItemCsvRow(
    string Title,
    string? BaseIdea,
    string? ConceptIdea,
    string? Phrase,
    string? Graphic,
    string? Notes,
    string Tags);
