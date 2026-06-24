using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Views;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
        : this(new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()))
    {
    }

    public MainWindowViewModel(WorkflowStageNavigatorViewModel workflowNavigator)
    {
        WorkflowNavigator = workflowNavigator;
    }

    public WorkflowStageNavigatorViewModel WorkflowNavigator { get; }
}
