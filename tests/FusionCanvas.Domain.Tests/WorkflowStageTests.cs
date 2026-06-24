using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

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
}
