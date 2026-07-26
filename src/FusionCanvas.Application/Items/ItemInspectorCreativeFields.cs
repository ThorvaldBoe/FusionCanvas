namespace FusionCanvas.Application.Items;

public sealed record ItemInspectorCreativeFields(
    string? Idea,
    string? Audience,
    string? ConceptIdea,
    string? Phrase,
    string? GraphicDirection)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(Idea)
        || !string.IsNullOrWhiteSpace(Audience)
        || !string.IsNullOrWhiteSpace(ConceptIdea)
        || !string.IsNullOrWhiteSpace(Phrase)
        || !string.IsNullOrWhiteSpace(GraphicDirection);
}
