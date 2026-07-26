using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.StageTools;

public static class BuiltInStageTools
{
    public static IStageToolRegistry CreateDefaultRegistry() =>
        new InMemoryStageToolRegistry(CreateDefaultTools());

    public static IReadOnlyList<StageToolDescriptor> CreateDefaultTools() =>
    [
        new(
            "built-in-idea-tool",
            "Idea",
            "Default Idea-stage workspace tool.",
            "idea-stage-tool",
            [WorkflowStage.Idea],
            RequiresSelectedItem: false,
            IsDefault: true),
        new(
            "built-in-concept-tool",
            "Concept",
            "Default Concept-stage item tool.",
            "concept-stage-tool",
            [WorkflowStage.Concept],
            RequiresSelectedItem: true,
            IsDefault: true),
        new(
            "built-in-design-tool",
            "Design",
            "Default Design-stage item tool.",
            "design-stage-tool",
            [WorkflowStage.Design],
            RequiresSelectedItem: true,
            IsDefault: true),
        new(
            "built-in-listing-tool",
            "Listing",
            "Default Item-stage item tool.",
            "listing-stage-tool",
            [WorkflowStage.Listing],
            RequiresSelectedItem: true,
            IsDefault: true)
    ];
}
