using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Tests;

public class GroupManagementServiceTests
{
    [Fact]
    public async Task CreateGroupAsync_CreatesRootAndNestedGroupsWithContextAndSelection()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var ids = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var service = new GroupManagementService(repository, () => sample.Now.AddMinutes(1), () => ids.Dequeue());

        var root = await service.CreateGroupAsync(new GroupManagementCreateRequest(
            new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            " Campaign ",
            new GroupContext(" Seasonal work ", " Launch notes ")));
        var nested = await service.CreateGroupAsync(new GroupManagementCreateRequest(
            new GroupParentReference(WorkspaceEntityKind.Group, root.Group!.Id),
            "Shirts"));

        Assert.True(root.Succeeded);
        Assert.Equal("Campaign", root.Group.Name);
        Assert.Equal("Seasonal work", root.Group.Context.Description);
        Assert.Equal("Launch notes", root.Group.Context.Notes);
        Assert.True(nested.Succeeded);
        Assert.Equal(root.Group.Id, nested.Group!.Parent.Id);
        Assert.Equal(nested.Group.Id, nested.State.ActiveGroupId);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAndUpdateGroupAsync_EnforceActiveSiblingNameUniqueness()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot);
        var service = new GroupManagementService(repository);

        var duplicate = await service.CreateGroupAsync(new GroupManagementCreateRequest(
            new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            " seasonal "));
        var separateBranch = await service.CreateGroupAsync(new GroupManagementCreateRequest(
            new GroupParentReference(WorkspaceEntityKind.Group, sample.RootGroup!.Id),
            "Seasonal"));
        var renameDuplicate = await service.UpdateGroupAsync(new GroupManagementUpdateRequest(
            sample.ChildGroup!.Id,
            "Seasonal"));

        Assert.False(duplicate.Succeeded);
        Assert.True(separateBranch.Succeeded);
        Assert.False(renameDuplicate.Succeeded);
        Assert.Equal("Child", repository.Snapshot.Groups.Single(group => group.Id == sample.ChildGroup.Id).Name);
    }

    [Fact]
    public async Task MoveGroupAsync_MovesAcrossNichesAndPreservesSubtreeAndItemContext()
    {
        var sample = Sample.CreateWithGroups();
        var otherNiche = new Niche(Guid.NewGuid(), sample.Store.Id, "Dogs", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Niches = [.. sample.Snapshot.Niches, otherNiche] };
        var repository = new TestRepository(snapshot);
        var service = new GroupManagementService(repository);

        var result = await service.MoveGroupAsync(new GroupManagementMoveRequest(
            sample.RootGroup!.Id,
            new GroupParentReference(WorkspaceEntityKind.Niche, otherNiche.Id)));

        Assert.True(result.Succeeded);
        var root = repository.Snapshot.Groups.Single(group => group.Id == sample.RootGroup.Id);
        var child = repository.Snapshot.Groups.Single(group => group.Id == sample.ChildGroup!.Id);
        var listing = Assert.Single(repository.Snapshot.Items);
        Assert.Equal(otherNiche.Id, root.NicheId);
        Assert.Equal(root.Id, child.ParentGroupId);
        Assert.Equal(child.Id, listing.GroupId);
        Assert.Equal(otherNiche.Id, listing.NicheId);
        Assert.Equal(sample.RootGroup.Id, result.State.ActiveGroupId);
    }

    [Fact]
    public async Task MoveGroupAsync_RejectsCyclesArchivedDestinationsAndCrossStoreMoves()
    {
        var sample = Sample.CreateWithGroups();
        var archived = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Archived", null, true, sample.Now, sample.Now, "{}");
        var otherStore = new Store(Guid.NewGuid(), "Other", null, false, sample.Now, sample.Now, "{}");
        var otherNiche = new Niche(Guid.NewGuid(), otherStore.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Stores = [.. sample.Snapshot.Stores, otherStore],
            Niches = [.. sample.Snapshot.Niches, otherNiche],
            Groups = [.. sample.Snapshot.Groups, archived]
        };
        var repository = new TestRepository(snapshot);
        var service = new GroupManagementService(repository);

        var cycle = await service.MoveGroupAsync(new GroupManagementMoveRequest(
            sample.RootGroup!.Id,
            new GroupParentReference(WorkspaceEntityKind.Group, sample.ChildGroup!.Id)));
        var archivedMove = await service.MoveGroupAsync(new GroupManagementMoveRequest(
            sample.ChildGroup.Id,
            new GroupParentReference(WorkspaceEntityKind.Group, archived.Id)));
        var crossStore = await service.MoveGroupAsync(new GroupManagementMoveRequest(
            sample.ChildGroup.Id,
            new GroupParentReference(WorkspaceEntityKind.Niche, otherNiche.Id)));

        Assert.False(cycle.Succeeded);
        Assert.False(archivedMove.Succeeded);
        Assert.False(crossStore.Succeeded);
        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(sample.RootGroup.Id, repository.Snapshot.Groups.Single(group => group.Id == sample.ChildGroup.Id).ParentGroupId);
    }

    [Fact]
    public async Task ArchiveAndRestoreGroupAsync_HideSubtreeAndPreserveExplicitDescendantArchive()
    {
        var sample = Sample.CreateWithGroups(childArchived: true);
        var repository = new TestRepository(sample.Snapshot);
        var service = new GroupManagementService(repository);
        await service.SelectGroupAsync(sample.RootGroup!.Id);

        var archived = await service.ArchiveGroupAsync(sample.RootGroup.Id);
        var restored = await service.RestoreGroupAsync(sample.RootGroup.Id);

        Assert.True(archived.Succeeded);
        Assert.Null(archived.State.ActiveGroupId);
        Assert.Empty(archived.State.ActiveGroups);
        Assert.Single(archived.State.ArchivedGroupRoots);
        Assert.True(restored.Succeeded);
        Assert.Equal(sample.RootGroup.Id, restored.State.ActiveGroupId);
        Assert.True(repository.Snapshot.Groups.Single(group => group.Id == sample.ChildGroup!.Id).IsArchived);
    }

    [Fact]
    public async Task RestoreGroupAsync_BlocksNameConflictAndArchivedParent()
    {
        var sample = Sample.CreateWithGroups();
        var archivedSibling = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Conflict", null, true, sample.Now, sample.Now, "{}");
        var activeSibling = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Conflict", null, false, sample.Now, sample.Now, "{}");
        var archivedChild = sample.ChildGroup! with { IsArchived = true };
        var archivedParent = sample.RootGroup! with { IsArchived = true };
        var snapshot = sample.Snapshot with
        {
            Groups = [archivedParent, archivedChild, archivedSibling, activeSibling]
        };
        var repository = new TestRepository(snapshot);
        var service = new GroupManagementService(repository);

        var conflict = await service.RestoreGroupAsync(archivedSibling.Id);
        var parentBlocked = await service.RestoreGroupAsync(archivedChild.Id);

        Assert.False(conflict.Succeeded);
        Assert.False(parentBlocked.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SaveFailure_ReturnsRecoverableErrorAndLeavesSelectionAndSnapshotUnchanged()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new GroupManagementService(repository);

        var result = await service.UpdateGroupAsync(new GroupManagementUpdateRequest(
            sample.RootGroup!.Id,
            "Changed",
            new GroupContext(Notes: "Keep me")));

        Assert.False(result.Succeeded);
        Assert.Contains("could not be saved", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(sample.RootGroup.Name, repository.Snapshot.Groups.Single(group => group.Id == sample.RootGroup.Id).Name);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task MoveGroupAsync_ReordersSiblingsAndNormalizesPositions()
    {
        var sample = Sample.CreateWithGroups();
        var second = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Second", null, false, sample.Now, sample.Now, "{}", 1);
        var third = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Third", null, false, sample.Now, sample.Now, "{}", 2);
        var root = sample.RootGroup! with { SortOrder = 0 };
        var repository = new TestRepository(sample.Snapshot with { Groups = [root, sample.ChildGroup!, second, third] });
        var service = new GroupManagementService(repository);

        var result = await service.MoveGroupAsync(new GroupManagementMoveRequest(
            third.Id,
            new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            new GroupPlacement(GroupPlacementKind.Before, root.Id)));

        Assert.True(result.Succeeded, result.Error);
        var siblings = repository.Snapshot.Groups
            .Where(group => group.NicheId == sample.Niche.Id)
            .OrderBy(group => group.SortOrder)
            .ToArray();
        Assert.Equal([third.Id, root.Id, second.Id], siblings.Select(group => group.Id));
        Assert.Equal([0, 1, 2], siblings.Select(group => group.SortOrder));
    }

    [Fact]
    public async Task CopyGroupAsync_CopiesGroupSubtreeWithNewIdentitiesOnly()
    {
        var sample = Sample.CreateWithGroups();
        var copiedRootId = Guid.NewGuid();
        var copiedChildId = Guid.NewGuid();
        var ids = new Queue<Guid>([copiedRootId, copiedChildId]);
        var repository = new TestRepository(sample.Snapshot);
        var service = new GroupManagementService(repository, () => sample.Now.AddMinutes(1), () => ids.Dequeue());

        var result = await service.CopyGroupAsync(new GroupManagementCopyRequest(
            sample.RootGroup!.Id,
            new GroupParentReference(WorkspaceEntityKind.Niche, sample.Niche.Id)));

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(copiedRootId, result.Group!.Id);
        Assert.Equal("Seasonal copy", result.Group.Name);
        var copiedChild = repository.Snapshot.Groups.Single(group => group.Id == copiedChildId);
        Assert.Equal(copiedRootId, copiedChild.ParentGroupId);
        Assert.Single(repository.Snapshot.Items);
    }

    [Fact]
    public async Task ResolveCreateParentAsync_UsesSelectionBeforePersistedDefaultNiche()
    {
        var sample = Sample.CreateWithGroups();
        var otherNiche = new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var store = sample.Store with { DefaultNicheId = otherNiche.Id };
        var repository = new TestRepository(sample.Snapshot with
        {
            Stores = [store],
            Niches = [sample.Niche, otherNiche]
        });
        var service = new GroupManagementService(repository);

        var selected = await service.ResolveCreateParentAsync(
            store.Id,
            new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.RootGroup!.Id));
        var fallback = await service.ResolveCreateParentAsync(store.Id, null);

        Assert.Equal(new GroupParentReference(WorkspaceEntityKind.Group, sample.RootGroup.Id), selected.Parent);
        Assert.Equal(new GroupParentReference(WorkspaceEntityKind.Niche, otherNiche.Id), fallback.Parent);
    }

    [Fact]
    public async Task DeleteGroupAsync_RequiresConfirmationAndLeavesSnapshotUntouched()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot);
        var service = new GroupManagementService(repository);

        var result = await service.DeleteGroupAsync(new GroupManagementDeleteRequest(
            sample.RootGroup!.Id,
            ConfirmPermanentDeletion: false));

        Assert.False(result.Succeeded);
        Assert.Contains("confirmation", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(2, repository.Snapshot.Groups.Count);
        Assert.Single(repository.Snapshot.Items);
    }

    [Fact]
    public async Task DeleteGroupAsync_RemovesCompleteSubtreeAndItemRelationshipsButPreservesAssets()
    {
        var sample = Sample.CreateWithGroups();
        var child = sample.ChildGroup! with { SortOrder = 0 };
        var sibling = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, sample.RootGroup!.Id, "Sibling", null, false, sample.Now, sample.Now, "{}", 3);
        var grandchild = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, child.Id, "Grandchild", null, false, sample.Now, sample.Now, "{}", 0);
        var listing = sample.Snapshot.Items.Single() with { GroupId = grandchild.Id };
        var prompt = new Prompt(Guid.NewGuid(), sample.Store.Id, listing.Id, "Prompt", null, "Text", false, sample.Now, sample.Now, "{}");
        var tag = new Tag(Guid.NewGuid(), sample.Store.Id, "Tag", null, false, sample.Now, sample.Now, "{}");
        var asset = new Asset(Guid.NewGuid(), sample.Store.Id, "Asset", null, AssetKind.SourceDesign, "assets/source.png", null, false, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Groups = [sample.RootGroup, child, sibling, grandchild],
            Items = [listing],
            Prompts = [prompt],
            Tags = [tag],
            ItemTags = [new ItemTag(listing.Id, tag.Id)],
            Assets = [asset],
            AssetLinks =
            [
                new AssetLink(asset.Id, WorkspaceEntityKind.Item, listing.Id),
                new AssetLink(asset.Id, WorkspaceEntityKind.Group, sample.RootGroup.Id)
            ]
        };
        var repository = new TestRepository(snapshot);
        var service = new GroupManagementService(repository);
        await service.SelectGroupAsync(child.Id);

        var result = await service.DeleteGroupAsync(new GroupManagementDeleteRequest(
            child.Id,
            ConfirmPermanentDeletion: true));

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal([sample.RootGroup.Id, sibling.Id], repository.Snapshot.Groups.Select(group => group.Id));
        Assert.Equal(0, repository.Snapshot.Groups.Single(group => group.Id == sibling.Id).SortOrder);
        Assert.Empty(repository.Snapshot.Items);
        Assert.Empty(repository.Snapshot.Prompts);
        Assert.Empty(repository.Snapshot.ItemTags);
        Assert.Single(repository.Snapshot.Assets);
        Assert.Equal(sample.RootGroup.Id, Assert.Single(repository.Snapshot.AssetLinks).EntityId);
        Assert.Equal(sample.RootGroup.Id, result.State.ActiveGroupId);
    }

    [Fact]
    public async Task DeleteGroupAsync_SaveFailureIsAtomic()
    {
        var sample = Sample.CreateWithGroups();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new GroupManagementService(repository);

        var result = await service.DeleteGroupAsync(new GroupManagementDeleteRequest(
            sample.RootGroup!.Id,
            ConfirmPermanentDeletion: true));

        Assert.False(result.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("Test failure.");
            }

            Snapshot = snapshot;
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        DateTimeOffset Now,
        Store Store,
        Niche Niche,
        TopicGroup? RootGroup = null,
        TopicGroup? ChildGroup = null)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
            return new Sample(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []), now, store, niche);
        }

        public static Sample CreateWithGroups(bool childArchived = false)
        {
            var sample = Create();
            var root = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Seasonal", null, false, sample.Now, sample.Now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, root.Id, "Child", null, childArchived, sample.Now, sample.Now, "{}");
            var listing = new Item(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, child.Id, "Listing", null, ItemStatus.Draft, WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
            return sample with
            {
                Snapshot = sample.Snapshot with { Groups = [root, child], Items = [listing] },
                RootGroup = root,
                ChildGroup = child
            };
        }
    }
}
