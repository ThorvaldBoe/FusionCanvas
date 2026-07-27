using FusionCanvas.Application.Ideation;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Ideation;

public sealed class IdeationScopeResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GroupScope_IsExactAndItemScopeUsesItsParent()
    {
        var store = NewStore();
        var niche = NewNiche(store.Id);
        var group = NewGroup(store.Id, niche.Id, "Pugs");
        var item = NewItem(store.Id, niche.Id, group.Id, "Selected");
        var snapshot = new WorkspaceSnapshot([store], [niche], [group], [item], [], [], [], [], []);
        var resolver = new IdeationScopeResolver();

        var fromGroup = resolver.Resolve(snapshot, WorkspaceEntityKind.Group, group.Id);
        var fromItem = resolver.Resolve(snapshot, WorkspaceEntityKind.Item, item.Id);

        Assert.True(fromGroup.IsAvailable);
        Assert.Equal(group.Id, fromGroup.Scope!.GroupId);
        Assert.Equal(fromGroup.Scope, fromItem.Scope);
    }

    [Fact]
    public void NicheScope_HasNoGroupAndCreatesAtNicheRoot()
    {
        var store = NewStore();
        var niche = NewNiche(store.Id);
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []);

        var result = new IdeationScopeResolver().Resolve(snapshot, WorkspaceEntityKind.Niche, niche.Id);

        Assert.True(result.IsAvailable);
        Assert.Null(result.Scope!.GroupId);
        Assert.Equal(WorkspaceEntityKind.Niche, result.Scope.CreationTopic.Kind);
        Assert.Equal(niche.Id, result.Scope.CreationTopic.Id);
    }

    [Fact]
    public void InactiveNiche_IsUnavailable()
    {
        var store = NewStore();
        var niche = NewNiche(store.Id) with { IsArchived = true };
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []);

        var result = new IdeationScopeResolver().Resolve(snapshot, WorkspaceEntityKind.Niche, niche.Id);

        Assert.False(result.IsAvailable);
        Assert.Contains("active niche", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    private static Store NewStore() =>
        new(Guid.NewGuid(), "Dog Shop", "Funny shirts", false, Now, Now, """{"brand":"playful"}""");

    private static Niche NewNiche(Guid storeId) =>
        new(Guid.NewGuid(), storeId, "Dogs", "Dog owners", false, Now, Now, """{"humor":"dry"}""");

    private static TopicGroup NewGroup(Guid storeId, Guid nicheId, string name) =>
        new(Guid.NewGuid(), storeId, nicheId, null, name, null, false, Now, Now, "{}");

    private static Item NewItem(Guid storeId, Guid nicheId, Guid? groupId, string idea) =>
        new(Guid.NewGuid(), storeId, nicheId, groupId, idea, null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, $$"""{"idea":"{{idea}}"}""");
}
