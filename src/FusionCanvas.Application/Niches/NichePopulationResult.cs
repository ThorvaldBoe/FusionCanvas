namespace FusionCanvas.Application.Niches;

public sealed record NichePopulationResult(
    bool Succeeded,
    IReadOnlyDictionary<NichePopulationField, string> Suggestions,
    NichePopulationFailureKind? FailureKind,
    string? Message)
{
    public static NichePopulationResult Success(IReadOnlyDictionary<NichePopulationField, string> suggestions) =>
        new(true, suggestions, null, null);

    public static NichePopulationResult Failure(NichePopulationFailureKind kind, string message) =>
        new(false, new Dictionary<NichePopulationField, string>(), kind, message);
}
