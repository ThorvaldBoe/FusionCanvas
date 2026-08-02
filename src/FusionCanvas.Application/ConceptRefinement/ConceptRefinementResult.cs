using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.ConceptRefinement;

public sealed record ConceptRefinementResult(
    bool Succeeded,
    string? ConceptIdea,
    string? Phrase,
    string? GraphicDirection,
    AiTextFailureKind? FailureKind,
    string? Error)
{
    public static ConceptRefinementResult Success(
        string? conceptIdea,
        string? phrase,
        string? graphicDirection) =>
        new(true, conceptIdea, phrase, graphicDirection, null, null);

    public static ConceptRefinementResult Failure(
        AiTextFailureKind kind,
        string error) =>
        new(false, null, null, null, kind, error);
}