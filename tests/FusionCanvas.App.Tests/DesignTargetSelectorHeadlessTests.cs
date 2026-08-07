using Avalonia.Headless.XUnit;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public class DesignTargetSelectorHeadlessTests
{
    [AvaloniaFact]
    public void DesignTool_LoadsWithoutCrashing()
    {
        using var fixture = new MainWindowFixture();
        var designContext = fixture.ViewModel.NavigationContexts.First(c =>
            c.Context.EntityKind == FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Item
            && c.Context.Id == SampleWorkspace.DesignNodeId);
        fixture.ViewModel.OpenFromNavigation(designContext);
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Design);
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowsDesignStageTool);
        Assert.NotNull(fixture.ViewModel.DesignTool);
    }
}