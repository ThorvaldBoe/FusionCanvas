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
    public void OpenAdditional_AllowsSameItemAndRefreshContextsUpdatesEveryMatchingTab()
    {
        var viewModel = new DocumentWindowViewModel();
        var original = NewContext("Original", WorkflowStage.Idea);
        var first = viewModel.Open(original);
        var second = viewModel.OpenAdditional(original);
        var refreshed = original with
        {
            Title = "Updated",
            Workflow = original.Workflow with { CurrentStage = WorkflowStage.Concept },
            WorkflowStage = WorkflowStage.Concept,
            DetailViewKey = DocumentWindowViewModel.GetDefaultDetailViewKey(WorkflowStage.Concept)
        };

        viewModel.RefreshContexts(original.Id, WorkspaceEntityKind.Item, refreshed);

        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.NotEqual(first.TabId, second.TabId);
        Assert.All(viewModel.Tabs, tab => Assert.Equal("Updated", tab.Title));
        Assert.All(viewModel.Tabs, tab => Assert.Equal(WorkflowStage.Concept, tab.Context.WorkflowStage));
        Assert.Equal(WorkflowStage.Concept, viewModel.ActiveContext?.WorkflowStage);
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
    public void CloseTab_LastTabIsKeptAsPersistentWorkingContext()
    {
        var viewModel = new DocumentWindowViewModel();
        var tab = viewModel.Open(NewContext("Idea", WorkflowStage.Idea));

        viewModel.CloseTab(tab);

        Assert.Same(tab, Assert.Single(viewModel.Tabs));
        Assert.Equal(tab.Context.Id, viewModel.ActiveContext?.Id);
        Assert.True(viewModel.HasActiveDocument);
    }

    [Fact]
    public void OpenOrReplaceActive_KeepsOneTabWhileNormalSelectionChangesContext()
    {
        var viewModel = new DocumentWindowViewModel();
        var first = NewContext("First", WorkflowStage.Idea);
        var second = NewContext("Second", WorkflowStage.Design);

        var tab = viewModel.OpenOrReplaceActive(first);
        var replaced = viewModel.OpenOrReplaceActive(second);

        Assert.Same(tab, replaced);
        Assert.Single(viewModel.Tabs);
        Assert.Equal(second.Id, viewModel.ActiveContext?.Id);
        Assert.Equal("Second", tab.Title);
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

    [Fact]
    public void ApplyStageToolHostState_ExposesSelectorWhenMultipleToolsAreAvailable()
    {
        var viewModel = new DocumentWindowViewModel();
        var state = NewHostState(
            NewAvailability("basic-idea", isAvailable: true),
            NewAvailability("advanced-idea", isAvailable: true));

        viewModel.ApplyStageToolHostState(state);

        Assert.True(viewModel.HasMultipleStageTools);
        Assert.Equal(2, viewModel.AvailableStageTools.Count);
        Assert.Equal("basic-idea", viewModel.SelectedStageTool?.Id);
    }

    [Fact]
    public void ApplyStageToolHostState_HidesSelectorWhenOneToolIsAvailable()
    {
        var viewModel = new DocumentWindowViewModel();

        viewModel.ApplyStageToolHostState(NewHostState(NewAvailability("basic-idea", isAvailable: true)));

        Assert.False(viewModel.HasMultipleStageTools);
        Assert.Single(viewModel.AvailableStageTools);
    }

    [Fact]
    public void ApplyStageToolHostState_ExposesUnavailableReason()
    {
        var viewModel = new DocumentWindowViewModel();
        var unavailable = NewAvailability(
            "concept-tool",
            isAvailable: false,
            StageToolAvailabilityKind.RequiresSelectedItem,
            "This tool requires a selected item.");

        viewModel.ApplyStageToolHostState(new StageToolHostState(
            WorkflowStage.Concept,
            StageToolContextKind.Topic,
            [unavailable],
            [],
            null,
            unavailable.ToolContext));

        Assert.False(viewModel.CanRunActiveTool);
        Assert.Equal("This tool requires a selected item.", viewModel.ActiveStageToolUnavailableMessage);
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

    private static StageToolHostState NewHostState(params StageToolAvailability[] tools) =>
        new(
            WorkflowStage.Idea,
            StageToolContextKind.Topic,
            tools,
            tools.Where(tool => tool.IsAvailable).ToArray(),
            tools.FirstOrDefault(tool => tool.IsAvailable),
            tools.FirstOrDefault(tool => tool.IsAvailable)?.ToolContext);

    private static StageToolAvailability NewAvailability(
        string id,
        bool isAvailable,
        StageToolAvailabilityKind unavailableKind = StageToolAvailabilityKind.Unavailable,
        string? reason = null)
    {
        var descriptor = new StageToolDescriptor(
            id,
            id,
            $"{id} description",
            $"{id}-view",
            [WorkflowStage.Idea],
            RequiresSelectedItem: false,
            IsDefault: id.StartsWith("basic", StringComparison.Ordinal));
        var summary = new ToolContextScopeSummary(ToolContextScopeKind.CurrentTopic, "Topic: Test", "Test", isAvailable);
        var resolution = isAvailable
            ? new ToolContextResolution(true, null, summary, null)
            : ToolContextResolution.Unavailable(reason ?? "Unavailable.");

        return new StageToolAvailability(
            descriptor,
            isAvailable ? StageToolAvailabilityKind.Available : unavailableKind,
            reason,
            resolution);
    }
}
