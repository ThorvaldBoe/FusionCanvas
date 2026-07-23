using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class StageToolHostServiceTests
{
    [Fact]
    public void Build_FiltersToolsByActiveWorkflowStage()
    {
        var sample = StageToolSample.Create();
        var service = new StageToolHostService(
            new InMemoryStageToolRegistry(
            [
                Tool("idea-tool", WorkflowStage.Idea, requiresSelectedItem: false, isDefault: true),
                Tool("design-tool", WorkflowStage.Design, requiresSelectedItem: true, isDefault: true)
            ]),
            new ToolContextResolver());

        var state = service.Build(sample.ItemRequest(WorkflowStage.Idea));

        Assert.Single(state.AvailableTools);
        Assert.Equal("idea-tool", state.SelectedTool?.Tool.Id);
        Assert.DoesNotContain(state.ToolStates, tool => tool.Tool.Id == "design-tool");
    }

    [Fact]
    public void Build_DistinguishesTopicToolsFromItemRequiredTools()
    {
        var sample = StageToolSample.Create();
        var service = new StageToolHostService(
            new InMemoryStageToolRegistry(
            [
                Tool("topic-tool", WorkflowStage.Concept, requiresSelectedItem: false, isDefault: true),
                Tool("item-tool", WorkflowStage.Concept, requiresSelectedItem: true, isDefault: false)
            ]),
            new ToolContextResolver());

        var state = service.Build(sample.TopicRequest(WorkflowStage.Concept));

        Assert.Single(state.AvailableTools);
        Assert.Equal("topic-tool", state.SelectedTool?.Tool.Id);
        Assert.Contains(state.ToolStates, tool =>
            tool.Tool.Id == "item-tool" &&
            tool.Kind == StageToolAvailabilityKind.RequiresSelectedItem);
    }

    [Fact]
    public void Build_FallsBackToDefaultWhenRememberedSelectionIsUnavailable()
    {
        var sample = StageToolSample.Create();
        var service = new StageToolHostService(
            new InMemoryStageToolRegistry(
            [
                Tool("default-concept", WorkflowStage.Concept, requiresSelectedItem: false, isDefault: true),
                Tool("advanced-concept", WorkflowStage.Concept, requiresSelectedItem: false, isDefault: false)
            ]),
            new ToolContextResolver());
        var key = new StageToolSelectionKey(WorkflowStage.Concept, StageToolContextKind.Topic);
        service.SelectTool(key, "advanced-concept");

        var selected = service.Build(sample.TopicRequest(WorkflowStage.Concept));
        var fallbackService = new StageToolHostService(
            new InMemoryStageToolRegistry(
            [
                Tool("default-concept", WorkflowStage.Concept, requiresSelectedItem: false, isDefault: true)
            ]),
            new ToolContextResolver());
        fallbackService.SelectTool(key, selected.SelectedTool!.Tool.Id);
        var fallback = fallbackService.Build(sample.TopicRequest(WorkflowStage.Concept));

        Assert.Equal("advanced-concept", selected.SelectedTool?.Tool.Id);
        Assert.Equal("default-concept", fallback.SelectedTool?.Tool.Id);
    }

    [Fact]
    public void Build_BuiltInDefaultsAreDiscoverableWithoutContributedTools()
    {
        var sample = StageToolSample.Create();
        var service = new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver());

        var state = service.Build(sample.ItemRequest(WorkflowStage.Design));

        Assert.Equal("built-in-design-tool", state.SelectedTool?.Tool.Id);
        Assert.All(state.AvailableTools, tool => Assert.Equal(StageToolSourceKind.BuiltIn, tool.Tool.SourceKind));
    }

    [Fact]
    public void Build_DefaultToolRemainsSelectableWhenContributedToolFails()
    {
        var sample = StageToolSample.Create();
        var service = new StageToolHostService(
            new InMemoryStageToolRegistry(
            [
                Tool("default-design", WorkflowStage.Design, requiresSelectedItem: true, isDefault: true),
                Tool(
                    "plugin-design",
                    WorkflowStage.Design,
                    requiresSelectedItem: true,
                    isDefault: false,
                    StageToolSourceKind.Contributed,
                    failureMessage: "Plugin failed to load.")
            ]),
            new ToolContextResolver());

        var state = service.Build(sample.ItemRequest(WorkflowStage.Design));

        Assert.Equal("default-design", state.SelectedTool?.Tool.Id);
        Assert.Contains(state.ToolStates, tool =>
            tool.Tool.Id == "plugin-design" &&
            tool.Kind == StageToolAvailabilityKind.Failed);
    }

    private static StageToolDescriptor Tool(
        string id,
        WorkflowStage stage,
        bool requiresSelectedItem,
        bool isDefault,
        StageToolSourceKind sourceKind = StageToolSourceKind.BuiltIn,
        string? failureMessage = null) =>
        new(
            id,
            id,
            $"{id} description",
            $"{id}-view",
            [stage],
            requiresSelectedItem,
            isDefault,
            sourceKind,
            FailureMessage: failureMessage);

    private sealed record StageToolSample(WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup Group, Item Item)
    {
        public StageToolHostRequest TopicRequest(WorkflowStage stage) =>
            new(
                Snapshot,
                ToolContextSelectionKind.Topic,
                WorkspaceEntityKind.Group,
                Group.Id,
                stage);

        public StageToolHostRequest ItemRequest(WorkflowStage stage) =>
            new(
                Snapshot,
                ToolContextSelectionKind.Item,
                WorkspaceEntityKind.Item,
                Item.Id,
                stage);

        public static StageToolSample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Autumn", null, false, now, now, "{}");
            var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

            return new StageToolSample(
                new WorkspaceSnapshot(
                    [store],
                    [niche],
                    [group],
                    [listing],
                    [],
                    [],
                    [],
                    [],
                    []),
                store,
                niche,
                group,
                listing);
        }
    }
}
