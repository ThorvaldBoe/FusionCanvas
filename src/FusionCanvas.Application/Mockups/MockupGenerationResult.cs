namespace FusionCanvas.Application.Mockups;
public sealed record MockupGenerationResult(bool Succeeded, string? Error, IReadOnlyList<MockupGenerationOutput> Outputs, IReadOnlyList<MockupGenerationDiagnostic> Diagnostics)
{
    public static MockupGenerationResult Failure(string error) => new(false, error, [], []);
}
