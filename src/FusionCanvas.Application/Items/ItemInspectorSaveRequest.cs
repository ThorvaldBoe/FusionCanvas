namespace FusionCanvas.Application.Items;

public sealed record ItemInspectorSaveRequest(
    Guid ItemId,
    string Title,
    string? Description,
    string? Idea,
    string? Audience,
    string? Phrase,
    string? GraphicDirection,
    string? Notes,
    IReadOnlyList<string> TagNames);
