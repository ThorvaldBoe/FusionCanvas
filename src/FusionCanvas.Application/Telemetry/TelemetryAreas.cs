namespace FusionCanvas.Application.Telemetry;

public static class TelemetryAreas
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Ideation",
        "Concept",
        "Design",
        "Listing",
        "Workspace",
        "Catalog",
        "Integration.OpenRouter",
        "Integration.Printify",
        "Persistence"
    ];
}
