namespace FusionCanvas.Application.Telemetry;

public static class TelemetryAreas
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Ideation",
        "Concept",
        "Design",
        "Application.ArtworkGeneration",
        "Listing",
        "Workspace",
        "Catalog",
        "Integration.OpenRouter",
        "Integration.Printify",
        "Persistence"
    ];
}
