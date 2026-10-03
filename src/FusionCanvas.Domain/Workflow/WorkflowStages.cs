namespace FusionCanvas.Domain.Workflow;

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

    public static int GetPosition(WorkflowStage stage)
    {
        for (var index = 0; index < Ordered.Count; index++)
        {
            if (Ordered[index] == stage)
            {
                return index;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage.");
    }
}
