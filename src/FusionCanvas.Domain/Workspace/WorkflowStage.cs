namespace FusionCanvas.Domain.Workspace;

public enum WorkflowStage
{
    Idea = 0,
    Concept = 1,
    Design = 2,
    Listing = 3
}

public static class WorkflowStages
{
    public static IReadOnlyList<WorkflowStage> Ordered { get; } =
    [
        WorkflowStage.Idea,
        WorkflowStage.Concept,
        WorkflowStage.Design,
        WorkflowStage.Listing
    ];

    public static string GetDisplayName(WorkflowStage stage) =>
        stage switch
        {
            WorkflowStage.Idea => "Idea",
            WorkflowStage.Concept => "Concept",
            WorkflowStage.Design => "Design",
            WorkflowStage.Listing => "Listing",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage.")
        };
}
