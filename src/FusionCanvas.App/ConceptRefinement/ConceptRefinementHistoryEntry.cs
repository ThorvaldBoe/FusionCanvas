using FusionCanvas.Domain.Concepts;

namespace FusionCanvas.App.ConceptRefinement;

public sealed record ConceptRefinementHistoryEntry(
    string Label,
    string ConceptIdea,
    string Phrase,
    string GraphicDirection,
    DateTimeOffset Timestamp);