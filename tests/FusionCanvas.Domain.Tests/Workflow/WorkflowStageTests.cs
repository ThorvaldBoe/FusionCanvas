using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Domain.Tests.Workflow;

public class WorkflowStageTests
{
    [Fact]
    public void WorkflowStages_ExposeCoreStagesInOrder()
    {
        Assert.Equal(
            [WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design, WorkflowStage.Listing],
            WorkflowStages.Ordered);
    }

    [Fact]
    public void WorkflowStages_ExposeDisplayNamesWithoutUiFrameworkTypes()
    {
        var labels = WorkflowStages.Ordered.Select(WorkflowStages.GetDisplayName).ToArray();

        Assert.Equal(["Idea", "Concept", "Design", "Listing"], labels);

        var referencedAssemblies = typeof(WorkflowStage).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
        Assert.DoesNotContain("Avalonia", referencedAssemblies);
    }

    [Theory]
    [InlineData(WorkflowStage.Idea, 0)]
    [InlineData(WorkflowStage.Concept, 1)]
    [InlineData(WorkflowStage.Design, 2)]
    [InlineData(WorkflowStage.Listing, 3)]
    public void WorkflowStages_ExposeCanonicalPositions(WorkflowStage stage, int expectedPosition)
    {
        Assert.Equal(expectedPosition, WorkflowStages.GetPosition(stage));
    }
}
