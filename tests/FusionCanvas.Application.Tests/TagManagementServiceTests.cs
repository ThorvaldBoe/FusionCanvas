using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class TagManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTagAsync_CreatesActiveTagWithColorInActiveStore()
    {
        var store = NewStore();
        var tagId = Guid.NewGuid();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new TagManagementService(repository, () => Now, () => tagId);

        var result = await service.CreateTagAsync(new TagManagementCreateRequest(store.Id, " Evergreen ", "Always relevant", "#1Ab"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(tagId, result.Tag?.Id);
        Assert.Equal(store.Id, result.Tag?.StoreId);
        Assert.Equal("Evergreen", result.Tag?.Name);
        Assert.Equal("Always relevant", result.Tag?.Description);
        Assert.Equal("#11AABB", result.Tag?.Color);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(tagId, Assert.Single(saved.Tags).Id);
        Assert.False(result.State.NeedsFirstTag);
    }

    [Fact]
    public async Task CreateTagAsync_AllowsNullColorAndDescription()
    {
        var store = NewStore();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new TagManagementService(repository, () => Now, () => Guid.NewGuid());

        var result = await service.CreateTagAsync(new TagManagementCreateRequest(store.Id, "Draft", null, null), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(result.Tag?.Color);
        Assert.Null(result.Tag?.Description);
    }

    [Fact]
    public async Task CreateTagAsync_RejectsMissingStoreArchivedStoreAndDuplicateNames()
    {
        var active = NewStore();
        var other = NewStore();
        var archived = NewStore() with { IsArchived = true };
        var existing = NewTag(active.Id, "Coffee");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, other, archived], [], [], [], [], [], [existing], [], []));
        var service = new TagManagementService(repository);

        var missingStore = await service.CreateTagAsync(new TagManagementCreateRequest(Guid.NewGuid(), "Cats"), TestContext.Current.CancellationToken);
        var archivedStore = await service.CreateTagAsync(new TagManagementCreateRequest(archived.Id, "Cats"), TestContext.Current.CancellationToken);
        var duplicate = await service.CreateTagAsync(new TagManagementCreateRequest(active.Id, "coffee"), TestContext.Current.CancellationToken);
        var reusedInOtherStore = await service.CreateTagAsync(new TagManagementCreateRequest(other.Id, "Coffee"), TestContext.Current.CancellationToken);
        var blank = await service.CreateTagAsync(new TagManagementCreateRequest(active.Id, " "), TestContext.Current.CancellationToken);
        var multiline = await service.CreateTagAsync(new TagManagementCreateRequest(active.Id, "two\nlines"), TestContext.Current.CancellationToken);

        Assert.False(missingStore.Succeeded);
        Assert.Contains("store is required", missingStore.Error);
        Assert.False(archivedStore.Succeeded);
        Assert.Contains("Archived stores", archivedStore.Error);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already uses", duplicate.Error);
        Assert.True(reusedInOtherStore.Succeeded);
        Assert.False(blank.Succeeded);
        Assert.Contains("required", blank.Error);
        Assert.False(multiline.Succeeded);
        Assert.Contains("single line", multiline.Error);
    }

    [Fact]
    public async Task CreateTagAsync_RejectsInvalidColor()
    {
        var store = NewStore();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new TagManagementService(repository);

        var result = await service.CreateTagAsync(new TagManagementCreateRequest(store.Id, "Bad", null, "not-a-color"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Color", result.Error);
    }

    [Fact]
    public async Task UpdateTagAsync_RenamesRecolorsAndEditsDescriptionPreservingLinks()
    {
        var store = NewStore();
        var tag = NewTag(store.Id, "evergreen", "#111111");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Espresso", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var link = new ListingTag(listing.Id, tag.Id);
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [link], []));
        var service = new TagManagementService(repository, () => Now.AddMinutes(5));

        var result = await service.UpdateTagAsync(new TagManagementUpdateRequest(tag.Id, " Evergreen ", "Updated", "#f00"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Evergreen", result.Tag?.Name);
        Assert.Equal("Updated", result.Tag?.Description);
        Assert.Equal("#FF0000", result.Tag?.Color);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(tag.Id, Assert.Single(saved.Tags).Id);
        Assert.Equal(link, Assert.Single(saved.ListingTags));
    }

    [Fact]
    public async Task UpdateTagAsync_RejectsDuplicateActiveNameAndInvalidColor()
    {
        var store = NewStore();
        var first = NewTag(store.Id, "Coffee");
        var second = NewTag(store.Id, "Tea");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [first, second], [], []));
        var service = new TagManagementService(repository);

        var duplicate = await service.UpdateTagAsync(new TagManagementUpdateRequest(second.Id, "coffee"), TestContext.Current.CancellationToken);
        var invalidColor = await service.UpdateTagAsync(new TagManagementUpdateRequest(second.Id, "Tea", null, "purple"), TestContext.Current.CancellationToken);
        var blank = await service.UpdateTagAsync(new TagManagementUpdateRequest(second.Id, " "), TestContext.Current.CancellationToken);

        Assert.False(duplicate.Succeeded);
        Assert.Contains("already uses", duplicate.Error);
        Assert.False(invalidColor.Succeeded);
        Assert.Contains("Color", invalidColor.Error);
        Assert.False(blank.Succeeded);
        Assert.Contains("required", blank.Error);
    }

    [Fact]
    public async Task ArchiveRestoreAsync_PreservesLinksAndExcludesFromVocabulary()
    {
        var store = NewStore();
        var tag = NewTag(store.Id, "seasonal");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [new ListingTag(listing.Id, tag.Id)], []));
        var service = new TagManagementService(repository);

        var archived = await service.ArchiveTagAsync(tag.Id, TestContext.Current.CancellationToken);
        var vocabularyAfterArchive = await service.GetActiveTagVocabularyAsync(store.Id, TestContext.Current.CancellationToken);

        var restored = await service.RestoreTagAsync(tag.Id, TestContext.Current.CancellationToken);
        var vocabularyAfterRestore = await service.GetActiveTagVocabularyAsync(store.Id, TestContext.Current.CancellationToken);

        Assert.True(archived.Succeeded);
        Assert.True(archived.Tag?.IsArchived);
        Assert.Empty(vocabularyAfterArchive);
        Assert.Equal(new ListingTag(listing.Id, tag.Id), Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).ListingTags));
        Assert.True(restored.Succeeded);
        Assert.False(restored.Tag?.IsArchived);
        Assert.Single(vocabularyAfterRestore);
    }

    [Fact]
    public async Task RestoreTagAsync_BlocksActiveNameConflict()
    {
        var store = NewStore();
        var active = NewTag(store.Id, "Coffee");
        var archived = NewTag(store.Id, "coffee") with { IsArchived = true };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [active, archived], [], []));
        var service = new TagManagementService(repository);

        var result = await service.RestoreTagAsync(archived.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("already uses", result.Error);
        Assert.True((await repository.LoadAsync(TestContext.Current.CancellationToken)).Tags.Single(tag => tag.Id == archived.Id).IsArchived);
    }

    [Fact]
    public async Task DeleteTagAsync_RemovesTagAndAllListingTagLinksAfterConfirmation()
    {
        var store = NewStore();
        var tag = NewTag(store.Id, "seasonal");
        var firstListing = new Listing(Guid.NewGuid(), store.Id, null, null, "A", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var secondListing = new Listing(Guid.NewGuid(), store.Id, null, null, "B", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var otherTag = NewTag(store.Id, "evergreen");
        var links = new[]
        {
            new ListingTag(firstListing.Id, tag.Id),
            new ListingTag(secondListing.Id, tag.Id),
            new ListingTag(firstListing.Id, otherTag.Id)
        };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [firstListing, secondListing], [], [], [tag, otherTag], links, []));
        var service = new TagManagementService(repository);

        var unconfirmed = await service.DeleteTagAsync(new TagManagementDeleteRequest(tag.Id, ConfirmPermanentDeletion: false), TestContext.Current.CancellationToken);
        var confirmed = await service.DeleteTagAsync(new TagManagementDeleteRequest(tag.Id, ConfirmPermanentDeletion: true), TestContext.Current.CancellationToken);

        Assert.False(unconfirmed.Succeeded);
        Assert.Contains("confirmation", unconfirmed.Error);
        Assert.True(confirmed.Succeeded);
        Assert.Equal(2, confirmed.AffectedListingCount);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(saved.Tags, candidate => candidate.Id == tag.Id);
        Assert.Contains(saved.Tags, candidate => candidate.Id == otherTag.Id);
        Assert.Equal(new ListingTag(firstListing.Id, otherTag.Id), Assert.Single(saved.ListingTags));
        Assert.Equal(2, saved.Listings.Count);
    }

    [Fact]
    public async Task ApplyTagAsync_PersistsLinkAtomicallyAndRejectsCrossStoreAndDuplicates()
    {
        var store = NewStore();
        var other = NewStore();
        var ownTag = NewTag(store.Id, "evergreen");
        var otherTag = NewTag(other.Id, "evergreen");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store, other], [], [], [listing], [], [], [ownTag, otherTag], [], []));
        var service = new TagManagementService(repository);

        var first = await service.ApplyTagAsync(new ApplyTagRequest(listing.Id, ownTag.Id), TestContext.Current.CancellationToken);
        var duplicate = await service.ApplyTagAsync(new ApplyTagRequest(listing.Id, ownTag.Id), TestContext.Current.CancellationToken);
        var crossStore = await service.ApplyTagAsync(new ApplyTagRequest(listing.Id, otherTag.Id), TestContext.Current.CancellationToken);
        var missingTag = await service.ApplyTagAsync(new ApplyTagRequest(listing.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);
        var missingListing = await service.ApplyTagAsync(new ApplyTagRequest(Guid.NewGuid(), ownTag.Id), TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(duplicate.Succeeded);
        Assert.False(duplicate.CreatedNewTag);
        Assert.False(crossStore.Succeeded);
        Assert.Contains("same store", crossStore.Error);
        Assert.False(missingTag.Succeeded);
        Assert.False(missingListing.Succeeded);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new ListingTag(listing.Id, ownTag.Id), Assert.Single(saved.ListingTags));
    }

    [Fact]
    public async Task ApplyTagAsync_RejectsArchivedListingAndArchivedAncestor()
    {
        var store = NewStore();
        var activeNiche = new Niche(Guid.NewGuid(), store.Id, "Active", null, false, Now, Now, "{}");
        var archivedNiche = new Niche(Guid.NewGuid(), store.Id, "Archived", null, true, Now, Now, "{}");
        var archivedListing = new Listing(Guid.NewGuid(), store.Id, activeNiche.Id, null, "ArchivedShirt", null, ListingStatus.Draft, WorkflowStage.Idea, true, Now, Now, "{}");
        var hiddenListing = new Listing(Guid.NewGuid(), store.Id, archivedNiche.Id, null, "HiddenShirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var tag = NewTag(store.Id, "evergreen");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [activeNiche, archivedNiche], [], [archivedListing, hiddenListing], [], [], [tag], [], []));
        var service = new TagManagementService(repository);

        var archivedListingResult = await service.ApplyTagAsync(new ApplyTagRequest(archivedListing.Id, tag.Id), TestContext.Current.CancellationToken);
        var hiddenListingResult = await service.ApplyTagAsync(new ApplyTagRequest(hiddenListing.Id, tag.Id), TestContext.Current.CancellationToken);

        Assert.False(archivedListingResult.Succeeded);
        Assert.Contains("Archived listings", archivedListingResult.Error);
        Assert.False(hiddenListingResult.Succeeded);
        Assert.Contains("Restore", hiddenListingResult.Error);
    }

    [Fact]
    public async Task RemoveTagAsync_RemovesOnlySpecifiedLinkAtomically()
    {
        var store = NewStore();
        var firstTag = NewTag(store.Id, "evergreen");
        var secondTag = NewTag(store.Id, "seasonal");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var otherListing = new Listing(Guid.NewGuid(), store.Id, null, null, "Mug", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var links = new[]
        {
            new ListingTag(listing.Id, firstTag.Id),
            new ListingTag(listing.Id, secondTag.Id),
            new ListingTag(otherListing.Id, firstTag.Id)
        };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing, otherListing], [], [], [firstTag, secondTag], links, []));
        var service = new TagManagementService(repository);

        var result = await service.RemoveTagAsync(new RemoveTagRequest(listing.Id, firstTag.Id), TestContext.Current.CancellationToken);
        var alreadyRemoved = await service.RemoveTagAsync(new RemoveTagRequest(listing.Id, firstTag.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(alreadyRemoved.Succeeded);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, saved.ListingTags.Count);
        Assert.DoesNotContain(saved.ListingTags, link => link.ListingId == listing.Id && link.TagId == firstTag.Id);
        Assert.Contains(saved.ListingTags, link => link.ListingId == listing.Id && link.TagId == secondTag.Id);
        Assert.Contains(saved.ListingTags, link => link.ListingId == otherListing.Id && link.TagId == firstTag.Id);
        Assert.Equal(2, saved.Tags.Count);
    }

    [Fact]
    public async Task ApplyOrCreateTagAsync_CreatesAndAppliesNewTag()
    {
        var store = NewStore();
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [], [], []));
        var service = new TagManagementService(repository, () => Now, () => Guid.NewGuid());

        var result = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, " Brand new "), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(result.CreatedNewTag);
        Assert.Equal("Brand new", result.Tag?.Name);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var savedTag = Assert.Single(saved.Tags);
        Assert.Equal("Brand new", savedTag.Name);
        Assert.Equal(new ListingTag(listing.Id, savedTag.Id), Assert.Single(saved.ListingTags));
    }

    [Fact]
    public async Task ApplyOrCreateTagAsync_AppliesExistingActiveTagOnExactNameMatch()
    {
        var store = NewStore();
        var existing = NewTag(store.Id, "Coffee");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [existing], [], []));
        var service = new TagManagementService(repository);

        var result = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, "coffee"), TestContext.Current.CancellationToken);
        var duplicate = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, "coffee"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.False(result.CreatedNewTag);
        Assert.Equal(existing.Id, result.Tag?.Id);
        Assert.True(duplicate.Succeeded);
        Assert.False(duplicate.CreatedNewTag);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Single(saved.Tags);
        Assert.Equal(new ListingTag(listing.Id, existing.Id), Assert.Single(saved.ListingTags));
    }

    [Fact]
    public async Task ApplyOrCreateTagAsync_OffersRestoreForArchivedTag()
    {
        var store = NewStore();
        var archived = NewTag(store.Id, "coffee") with { IsArchived = true };
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [archived], [], []));
        var service = new TagManagementService(repository);

        var result = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, "Coffee"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("archived", result.Error);
        Assert.Equal(archived.Id, result.Tag?.Id);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Empty(saved.ListingTags);
        Assert.True(saved.Tags.Single().IsArchived);
    }

    [Fact]
    public async Task ApplyOrCreateTagAsync_RejectsBlankNameAndMultilineName()
    {
        var store = NewStore();
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [], [], []));
        var service = new TagManagementService(repository);

        var blank = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, " "), TestContext.Current.CancellationToken);
        var multiline = await service.ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, "two\nlines"), TestContext.Current.CancellationToken);

        Assert.False(blank.Succeeded);
        Assert.Contains("required", blank.Error);
        Assert.False(multiline.Succeeded);
        Assert.Contains("single line", multiline.Error);
    }

    [Fact]
    public async Task GetActiveTagVocabularyAsync_ExcludesArchivedTagsAndRespectsStoreScope()
    {
        var first = NewStore();
        var second = NewStore();
        var firstActive = NewTag(first.Id, "Coffee");
        var firstArchived = NewTag(first.Id, "Paused") with { IsArchived = true };
        var secondActive = NewTag(second.Id, "Tea");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [firstActive, firstArchived, secondActive], [], []));
        var service = new TagManagementService(repository);

        var firstVocabulary = await service.GetActiveTagVocabularyAsync(first.Id, TestContext.Current.CancellationToken);
        var secondVocabulary = await service.GetActiveTagVocabularyAsync(second.Id, TestContext.Current.CancellationToken);

        Assert.Equal(firstActive.Id, Assert.Single(firstVocabulary).Id);
        Assert.Equal(secondActive.Id, Assert.Single(secondVocabulary).Id);
    }

    [Fact]
    public async Task GetListingTagsAsync_ReturnsAppliedTagIds()
    {
        var store = NewStore();
        var firstTag = NewTag(store.Id, "evergreen");
        var secondTag = NewTag(store.Id, "seasonal");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [firstTag, secondTag], [new ListingTag(listing.Id, firstTag.Id)], []));
        var service = new TagManagementService(repository);

        var tags = await service.GetListingTagsAsync(listing.Id, TestContext.Current.CancellationToken);

        Assert.Equal([firstTag.Id], tags);
    }

    [Fact]
    public async Task LoadAsync_FiltersTagsToSelectedStoreAndSeparatesActiveAndArchived()
    {
        var first = NewStore();
        var second = NewStore();
        var firstActive = NewTag(first.Id, "Coffee");
        var firstArchived = NewTag(first.Id, "Paused") with { IsArchived = true };
        var secondActive = NewTag(second.Id, "Tea");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [], [], [], [], [], [firstActive, firstArchived, secondActive], [], []));
        var service = new TagManagementService(repository);

        var state = await service.LoadAsync(first.Id, TestContext.Current.CancellationToken);

        Assert.Equal(first.Id, state.ActiveStoreId);
        Assert.Equal(firstActive.Id, Assert.Single(state.ActiveTags).Id);
        Assert.Equal(firstArchived.Id, Assert.Single(state.ArchivedTags).Id);
    }

    [Fact]
    public async Task LoadAsync_ReportsNeedsFirstTagWhenStoreHasNoActiveTags()
    {
        var store = NewStore();
        var archived = NewTag(store.Id, "paused") with { IsArchived = true };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [archived], [], []));
        var service = new TagManagementService(repository);

        var state = await service.LoadAsync(store.Id, TestContext.Current.CancellationToken);

        Assert.True(state.NeedsFirstTag);
        Assert.Empty(state.ActiveTags);
    }

    [Fact]
    public async Task Mutations_PropagateRepositoryFailuresForUiRollback()
    {
        var store = NewStore();
        var tag = NewTag(store.Id, "evergreen");
        var listing = new Listing(Guid.NewGuid(), store.Id, null, null, "Shirt", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var link = new ListingTag(listing.Id, tag.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TagManagementService(new FailingWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [], [])))
                .UpdateTagAsync(new TagManagementUpdateRequest(tag.Id, "renamed"), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TagManagementService(new FailingWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [], [])))
                .ApplyTagAsync(new ApplyTagRequest(listing.Id, tag.Id), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TagManagementService(new FailingWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [link], [])))
                .RemoveTagAsync(new RemoveTagRequest(listing.Id, tag.Id), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TagManagementService(new FailingWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [listing], [], [], [tag], [], [])))
                .ApplyOrCreateTagAsync(new ApplyOrCreateTagRequest(listing.Id, "fresh"), TestContext.Current.CancellationToken));
    }

    private static Store NewStore() =>
        new(Guid.NewGuid(), "Studio", null, false, Now, Now, "{}");

    private static Tag NewTag(Guid storeId, string name, string? color = null) =>
        new(Guid.NewGuid(), storeId, name, null, false, Now, Now, "{}", color);

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class FailingWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Simulated persistence failure."));

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }
}
