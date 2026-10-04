namespace FusionCanvas.Application.Mockups;

public sealed record MockupOutputInvalidationResult(
    bool Succeeded,
    int RemovedCount,
    string? Error = null,
    IReadOnlyList<string>? Diagnostics = null)
{
    public IReadOnlyList<string> CleanupDiagnostics => Diagnostics ?? [];
}
