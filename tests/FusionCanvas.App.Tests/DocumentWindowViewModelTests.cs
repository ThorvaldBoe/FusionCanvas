using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class DocumentWindowViewModelTests
{
    [Fact]
    public void Open_AddsMultipleContextsAndActivatesLatest()
    {
        var viewModel = new DocumentWindowViewModel();
        var idea = NewContext("Idea", WorkflowStage.Idea);
        var design = NewContext("Design", WorkflowStage.Design);

        viewModel.Open(idea);
        viewModel.Open(design);

        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.Equal(design.Id, viewModel.ActiveContext?.Id);
        Assert.True(viewModel.Tabs.Single(tab => tab.Context.Id == design.Id).IsActive);
        Assert.False(viewModel.Tabs.Single(tab => tab.Context.Id == idea.Id).IsActive);
    }

    [Fact]
    public void Open_ActivatesExistingTabForDuplicateContext()
    {
        var viewModel = new DocumentWindowViewModel();
        var idea = NewContext("Idea", WorkflowStage.Idea);
        var design = NewContext("Design", WorkflowStage.Design);

        var firstIdeaTab = viewModel.Open(idea);
        viewModel.Open(design);
        var secondIdeaTab = viewModel.Open(idea);

        Assert.Same(firstIdeaTab, secondIdeaTab);
        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.Equal(idea.Id, viewModel.ActiveContext?.Id);
    }

    [Fact]
    public void SelectTab_UpdatesActiveDocumentContext()
    {
        var viewModel = new DocumentWindowViewModel();
        var ideaTab = viewModel.Open(NewContext("Idea", WorkflowStage.Idea));
        viewModel.Open(NewContext("Design", WorkflowStage.Design));

        viewModel.SelectTab(ideaTab);

        Assert.Equal(ideaTab.Context.Id, viewModel.ActiveContext?.Id);
        Assert.True(ideaTab.IsActive);
    }

    [Fact]
    public void CloseTab_RemovesInactiveTabWithoutChangingActiveContext()
    {
        var viewModel = new DocumentWindowViewModel();
        var ideaTab = viewModel.Open(NewContext("Idea", WorkflowStage.Idea));
        var designTab = viewModel.Open(NewContext("Design", WorkflowStage.Design));

        viewModel.CloseTab(ideaTab);

        Assert.Single(viewModel.Tabs);
        Assert.Equal(designTab.Context.Id, viewModel.ActiveContext?.Id);
        Assert.True(designTab.IsActive);
    }

    [Fact]
    public void CloseTab_SelectsNeighborWhenActiveTabCloses()
    {
        var viewModel = new DocumentWindowViewModel();
        var ideaTab = viewModel.Open(NewContext("Idea", WorkflowStage.Idea));
        var designTab = viewModel.Open(NewContext("Design", WorkflowStage.Design));

        viewModel.CloseTab(designTab);

        Assert.Single(viewModel.Tabs);
        Assert.Equal(ideaTab.Context.Id, viewModel.ActiveContext?.Id);
        Assert.True(ideaTab.IsActive);
    }

    [Fact]
    public void CloseTab_LastTabReturnsToEmptyState()
    {
        var viewModel = new DocumentWindowViewModel();
        var tab = viewModel.Open(NewContext("Idea", WorkflowStage.Idea));

        viewModel.CloseTab(tab);

        Assert.Empty(viewModel.Tabs);
        Assert.Null(viewModel.ActiveContext);
        Assert.False(viewModel.HasActiveDocument);
    }

    [Fact]
    public void ChangeActiveWorkflowStage_UpdatesDetailHostState()
    {
        var viewModel = new DocumentWindowViewModel();
        viewModel.Open(NewContext("Idea", WorkflowStage.Idea));

        viewModel.ChangeActiveWorkflowStage(WorkflowStage.Concept);

        Assert.Equal(WorkflowStage.Concept, viewModel.ActiveContext?.WorkflowStage);
        Assert.Equal("Concept", viewModel.ActiveWorkflowStageLabel);
        Assert.Equal("concept-stage-tool", viewModel.ActiveDetailViewKey);
    }

    private static DocumentContext NewContext(string title, WorkflowStage stage)
    {
        var contextId = Guid.NewGuid();
        var workflow = new ActiveItemWorkflowContext(contextId, stage, WorkflowStages.Ordered);

        return new DocumentContext(
            contextId,
            title,
            DocumentContextKind.Item,
            new DocumentNavigationLocation([Guid.NewGuid(), contextId], $"Workspace / {title}"),
            workflow,
            stage,
            DocumentWindowViewModel.GetDefaultDetailViewKey(stage));
    }
}
