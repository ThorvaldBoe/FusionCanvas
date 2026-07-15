using FusionCanvas.App.Views;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void OpenFromNavigation_OpensTabAndCoordinatesNavigationAndWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var navigationContext = ReadyListingContext(viewModel);

        viewModel.OpenFromNavigation(navigationContext);

        Assert.Single(viewModel.DocumentWindow.Tabs);
        Assert.Equal(navigationContext.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(navigationContext.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.RevealedNodeId);
        Assert.Equal(navigationContext.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.SelectedNodeId);
        Assert.Equal(WorkflowStage.Design, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void OpenFromNavigation_DoesNotDiscardExistingTabs()
    {
        var viewModel = new MainWindowViewModel();
        var first = DraftListingContext(viewModel);
        var second = ActiveListingContext(viewModel);

        viewModel.OpenFromNavigation(first);
        viewModel.OpenFromNavigation(second);

        Assert.Equal(2, viewModel.DocumentWindow.Tabs.Count);
        Assert.Equal(second.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
    }

    [Fact]
    public void SelectingOpenTab_RecoordinatesNavigationAndWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var draft = DraftListingContext(viewModel);
        var active = ActiveListingContext(viewModel);
        var first = viewModel.DocumentWindow.Open(draft.Context);
        viewModel.DocumentWindow.Open(active.Context);

        viewModel.DocumentWindow.SelectTab(first);

        Assert.Equal(draft.Context.Id, viewModel.DocumentWindow.ActiveContext?.Id);
        Assert.Equal(draft.Context.NavigationLocation?.NodePath[^1], viewModel.NavigationState.RevealedNodeId);
        Assert.Equal(WorkflowStage.Idea, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void SelectWorkflowStage_UpdatesDocumentAndNavigator()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(DraftListingContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Listing);

        Assert.Equal(WorkflowStage.Listing, viewModel.DocumentWindow.ActiveContext?.WorkflowStage);
        Assert.Equal("listing-stage-tool", viewModel.DocumentWindow.ActiveDetailViewKey);
        Assert.Equal(WorkflowStage.Listing, viewModel.WorkflowNavigator.ActiveViewStage);
    }

    [Fact]
    public void ClosingLastTab_ClearsWorkflowNavigator()
    {
        var viewModel = new MainWindowViewModel();
        var tab = viewModel.DocumentWindow.Open(DraftListingContext(viewModel).Context);

        viewModel.DocumentWindow.CloseTab(tab);

        Assert.False(viewModel.DocumentWindow.HasActiveDocument);
        Assert.False(viewModel.WorkflowNavigator.HasActiveItem);
    }

    [Fact]
    public void OpenFromNavigation_ResolvesVisibleToolContextScope()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFromNavigation(GroupContext(viewModel));

        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Equal("Idea", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("Topic: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
        Assert.Contains("Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeDescription);
    }

    [Fact]
    public void ChangeToolScopeCommand_ReResolvesVisibleToolScope()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.DocumentWindow.ChangeToolScopeCommand.Execute(ToolContextScopeKind.CurrentSubtree);

        Assert.Equal("Subtree: Dogs and coffee", viewModel.DocumentWindow.ActiveToolScopeLabel);
    }

    [Fact]
    public void SelectWorkflowStage_RefreshesStageToolHost()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(DraftListingContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Design);

        Assert.Equal("Design", viewModel.DocumentWindow.ActiveStageToolName);
        Assert.Equal("built-in-design-tool", viewModel.DocumentWindow.SelectedStageTool?.Id);
        Assert.True(viewModel.DocumentWindow.CanRunActiveTool);
    }

    [Fact]
    public void TopicContextInItemBoundStage_ShowsItemRequiredState()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFromNavigation(GroupContext(viewModel));

        viewModel.SelectWorkflowStage(WorkflowStage.Concept);

        Assert.False(viewModel.DocumentWindow.CanRunActiveTool);
        Assert.Contains("requires a selected item", viewModel.DocumentWindow.ActiveStageToolUnavailableMessage);
    }

    private static NavigationDocumentContext GroupContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Group &&
            context.Context.Title == "Dogs and coffee");

    private static NavigationDocumentContext DraftListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Morning coffee idea");

    private static NavigationDocumentContext ReadyListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Retro mug design");

    private static NavigationDocumentContext ActiveListingContext(MainWindowViewModel viewModel) =>
        viewModel.NavigationContexts.Single(context =>
            context.Context.EntityKind == WorkspaceEntityKind.Listing &&
            context.Context.Title == "Espresso listing draft");
}
