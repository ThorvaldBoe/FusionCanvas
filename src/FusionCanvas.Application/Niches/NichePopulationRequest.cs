namespace FusionCanvas.Application.Niches;

public sealed record NichePopulationRequest(
    string NicheName,
    IReadOnlyList<NichePopulationField> Fields);
