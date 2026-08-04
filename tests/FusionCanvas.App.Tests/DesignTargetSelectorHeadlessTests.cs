using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public class DesignTargetSelectorHeadlessTests
{
    [AvaloniaFact]
    public void DesignTargetSelector_ShowsTargetCheckboxAndSaveAction()
    {
        using var fixture = new MainWindowFixture();
        var designContext = fixture.ViewModel.NavigationContexts.First(c =>
            c.Context.EntityKind == WorkspaceEntityKind.Item
            && c.Context.Id == SampleWorkspace.DesignNodeId);
        fixture.ViewModel.OpenFromNavigation(designContext);
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Design);
        fixture.PumpLayout();

        Assert.True(fixture.ViewModel.ShowsDesignStageTool);

        var saveButton = fixture.FindControl<Button>(b =>
            AutomationProperties.GetName(b) == "Save design targets" && b.IsVisible);
        Assert.NotNull(saveButton);

        var targetCheckbox = fixture.FindControlOrDefault<CheckBox>(cb =>
            cb.IsVisible && (cb.Content as string)?.Contains("Gildan 64000") == true);
        Assert.NotNull(targetCheckbox);
    }

    [AvaloniaFact]
    public void DesignTargetSelector_SaveIsEnabledWhileEditable()
    {
        using var fixture = new MainWindowFixture();
        var designContext = fixture.ViewModel.NavigationContexts.First(c =>
            c.Context.EntityKind == WorkspaceEntityKind.Item
            && c.Context.Id == SampleWorkspace.DesignNodeId);
        fixture.ViewModel.OpenFromNavigation(designContext);
        fixture.ViewModel.SelectWorkflowStage(WorkflowStage.Design);
        fixture.PumpLayout();

        var saveButton = fixture.FindControl<Button>(b =>
            AutomationProperties.GetName(b) == "Save design targets" && b.IsVisible);
        Assert.NotNull(saveButton);
        Assert.True(saveButton!.IsEnabled);
    }
}
