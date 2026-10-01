namespace FusionCanvas.Domain.Concepts;

/// <summary>
/// Defines the invariant required to initialize a Concept design triangle from its original idea.
/// </summary>
public static class ConceptRefinementInitializationPolicy
{
    public static bool AreConceptFieldsEmpty(
        string? conceptIdea,
        string? phrase,
        string? graphicDirection) =>
        string.IsNullOrWhiteSpace(conceptIdea)
        && string.IsNullOrWhiteSpace(phrase)
        && string.IsNullOrWhiteSpace(graphicDirection);

    public static bool CanInitializeFromBaseIdea(
        string? originalIdea,
        string? conceptIdea,
        string? phrase,
        string? graphicDirection) =>
        !string.IsNullOrWhiteSpace(originalIdea)
        && AreConceptFieldsEmpty(conceptIdea, phrase, graphicDirection);
}
