using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workspace.Transfer;

namespace FusionCanvas.Domain.Tests.Workspace.Transfer;

public class WorkspaceTransferPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ForWorkspace_IncludesCompleteOwnedSubgraphAndArchivedEntities()
    {
        var selected = CreateGraph("Selected", archived: true);
        var other = CreateGraph("Other");
        var validLink = new AssetLink(selected.Asset.Id, WorkspaceEntityKind.Item, selected.Item.Id);
        var crossWorkspaceLink = new AssetLink(selected.Asset.Id, WorkspaceEntityKind.Item, other.Item.Id);
        var source = Merge(selected.Snapshot, other.Snapshot) with
        {
            AssetLinks = [validLink, crossWorkspaceLink]
        };

        var result = WorkspaceSnapshotFilter.ForWorkspace(source, selected.Workspace.Id);

        Assert.Equal(selected.Snapshot.Workspaces, result.Snapshot.Workspaces);
        Assert.Equal(selected.Snapshot.Stores, result.Snapshot.Stores);
        Assert.Equal(selected.Snapshot.Niches, result.Snapshot.Niches);
        Assert.Equal(selected.Snapshot.Groups, result.Snapshot.Groups);
        Assert.Equal(selected.Snapshot.Items, result.Snapshot.Items);
        Assert.Equal(selected.Snapshot.Assets, result.Snapshot.Assets);
        Assert.Equal(selected.Snapshot.Prompts, result.Snapshot.Prompts);
        Assert.Equal(selected.Snapshot.Tags, result.Snapshot.Tags);
        Assert.Equal(selected.Snapshot.ItemTags, result.Snapshot.ItemTags);
        Assert.Equal([validLink], result.Snapshot.AssetLinks);
        Assert.Equal(selected.Snapshot.IdeationRejections, result.Snapshot.IdeationRejections);
        Assert.Equal([crossWorkspaceLink], result.DroppedAssetLinks);
        Assert.All(result.Snapshot.Workspaces.Cast<WorkspaceEntity>()
            .Concat(result.Snapshot.Stores)
            .Concat(result.Snapshot.Niches)
            .Concat(result.Snapshot.Groups)
            .Concat(result.Snapshot.Items)
            .Concat(result.Snapshot.Assets)
            .Concat(result.Snapshot.Prompts)
            .Concat(result.Snapshot.Tags), entity => Assert.True(entity.IsArchived));
    }

    [Fact]
    public void FindIdentityCollisions_CoversEverySnapshotList()
    {
        var graph = CreateGraph("Package");

        var collisions = WorkspaceImportPreflight.FindIdentityCollisions(graph.Snapshot, graph.Snapshot);

        Assert.Equal(
            ["Asset", "AssetLink", "Group", "IdeationRejection", "Item", "ItemTag", "Niche", "Prompt", "Store", "Tag", "Workspace"],
            collisions.Select(collision => collision.EntityType).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ForWorkspace_ExcludesRejectionWhoseGroupIsOutsideIncludedWorkspace()
    {
        var selected = CreateGraph("Selected");
        var other = CreateGraph("Other");
        var inconsistent = selected.Rejection with { Id = Guid.NewGuid(), GroupId = other.Group.Id };
        var source = Merge(selected.Snapshot, other.Snapshot) with
        {
            IdeationRejections = [selected.Rejection, inconsistent, other.Rejection]
        };

        var result = WorkspaceSnapshotFilter.ForWorkspace(source, selected.Workspace.Id);

        Assert.Equal(selected.Rejection, Assert.Single(result.Snapshot.IdeationRejections));
    }

    [Fact]
    public void ResolveImportName_UsesFirstAvailableSuffixAndOnlyProvidedActiveNames()
    {
        Assert.Equal("Brand", WorkspaceImportPreflight.ResolveImportName(" Brand ", ["Archived Brand"]));
        Assert.Equal("Brand (4)", WorkspaceImportPreflight.ResolveImportName(
            "Brand",
            ["brand", "Brand (2)", "BRAND (3)"]));
    }

    private static Graph CreateGraph(string name, bool archived = false)
    {
        var workspace = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), name, "description", archived, Now, Now, "{\"key\":\"value\"}");
        var store = new Store(Guid.NewGuid(), workspace.Id, $"{name} store", null, archived, Now, Now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, $"{name} niche", null, archived, Now, Now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, $"{name} group", null, archived, Now, Now, "{}", 3);
        var item = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, $"{name} item", null, ItemStatus.Draft, WorkflowStage.Design, archived, Now, Now, "{}");
        var asset = new Asset(Guid.NewGuid(), store.Id, $"{name} asset", null, AssetKind.ExportedImage, $"assets/{name}.png", null, false, archived, Now, Now, "{}");
        var prompt = new Prompt(Guid.NewGuid(), store.Id, item.Id, $"{name} prompt", null, "text", archived, Now, Now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, $"{name} tag", null, archived, Now, Now, "{}", "#123456");
        var itemTag = new ItemTag(item.Id, tag.Id);
        var assetLink = new AssetLink(asset.Id, WorkspaceEntityKind.Item, item.Id);
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            store.Id,
            niche.Id,
            group.Id,
            $"{name} rejected idea",
            "Already explored",
            IdeationMode.Snowclones,
            Now);
        var snapshot = new WorkspaceSnapshot(
            [workspace], [store], [niche], [group], [item], [asset], [prompt], [tag], [itemTag], [assetLink])
        {
            IdeationRejections = [rejection]
        };
        return new Graph(snapshot, workspace, group, item, asset, rejection);
    }

    private static WorkspaceSnapshot Merge(WorkspaceSnapshot left, WorkspaceSnapshot right) =>
        new(
            [.. left.Workspaces, .. right.Workspaces],
            [.. left.Stores, .. right.Stores],
            [.. left.Niches, .. right.Niches],
            [.. left.Groups, .. right.Groups],
            [.. left.Items, .. right.Items],
            [.. left.Assets, .. right.Assets],
            [.. left.Prompts, .. right.Prompts],
            [.. left.Tags, .. right.Tags],
            [.. left.ItemTags, .. right.ItemTags],
            [.. left.AssetLinks, .. right.AssetLinks])
        {
            IdeationRejections = [.. left.IdeationRejections, .. right.IdeationRejections]
        };

    private sealed record Graph(
        WorkspaceSnapshot Snapshot,
        FusionCanvas.Domain.Workspace.Workspace Workspace,
        TopicGroup Group,
        Item Item,
        Asset Asset,
        IdeationRejection Rejection);
}
