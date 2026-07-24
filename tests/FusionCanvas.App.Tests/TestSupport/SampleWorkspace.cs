using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.TestSupport;

internal static class SampleWorkspace
{
    internal static readonly Guid StoreNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    internal static readonly Guid NicheNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    internal static readonly Guid TopicNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    internal static readonly Guid IdeaNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    internal static readonly Guid DesignNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    internal static readonly Guid ListingNodeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    internal static WorkspaceSnapshot Create()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(StoreNodeId, "North Star Studio", null, false, now, now, """{"brand":"North Star"}""");
        var niche = new Niche(NicheNodeId, store.Id, "Coffee", null, false, now, now, """{"tone":"warm"}""");
        var topic = new TopicGroup(TopicNodeId, store.Id, niche.Id, null, "Dogs and coffee", null, false, now, now, """{"humor":"gentle"}""");
        var idea = new Item(IdeaNodeId, store.Id, niche.Id, topic.Id, "Morning coffee idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, """{"phrase":"But first, walkies"}""");
        var design = new Item(DesignNodeId, store.Id, niche.Id, topic.Id, "Retro mug design", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
        var item = new Item(ListingNodeId, store.Id, niche.Id, topic.Id, "Espresso listing draft", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [topic],
            [idea, design, item],
            [],
            [],
            [],
            [],
            []);
    }
}

internal sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
{
    private WorkspaceSnapshot _snapshot = snapshot;

    public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshot = snapshot;
        return Task.CompletedTask;
    }

    public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot);
}

internal static class MainWindowViewModelFactory
{
    internal static MainWindowViewModel CreateSample() =>
        new(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindow.DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            new InMemoryWorkspaceRepository(SampleWorkspace.Create()),
            SampleWorkspace.Create());
}
