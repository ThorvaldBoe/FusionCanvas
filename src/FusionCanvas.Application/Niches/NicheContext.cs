namespace FusionCanvas.Application.Niches;

public sealed record NicheContext(
    string? Description = null,
    string? Audience = null,
    string? HumorStyle = null,
    string? VisualStyleGuidance = null,
    string? Constraints = null,
    string? Risks = null,
    string? ResearchNotes = null,
    string? Notes = null);
