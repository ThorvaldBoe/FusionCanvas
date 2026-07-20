using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ListingInspectorServiceTests
{
    [Fact]
    public async Task Load_BuildsStateFromListingMetadataTagsAndAssets()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(2), newId: Guid.NewGuid);

        var state = await service.LoadAsync(sample.Listing.Id);

        Assert.NotNull(state);
        Assert.Equal(sample.Listing.Name, state!.Title);
        Assert.Equal("Notes", state.Notes);
        Assert.Equal("idea-value", state.Creative.Idea);
        Assert.Equal("phrase-value", state.Creative.Phrase);
        Assert.Equal("graphic-value", state.Creative.GraphicDirection);
        Assert.Equal(sample.Tag.Name, state.Tags.Single().Name);
        Assert.Equal(AssetKind.SourceDesign, state.Assets.Single().Kind);
        Assert.Contains(sample.Tag.Name, state.AvailableTagNames);
        Assert.Equal("Niche / Root / Child", state.DisplayPath);
        Assert.True(state.IsEffectivelyActive);
        Assert.False(state.IsReadOnly);
    }

    [Fact]
    public async Task Load_ReturnsNullForMissingListing()
    {
        var sample = Sample.Create();
        var service = new ListingInspectorService(new TestRepository(sample.Snapshot));

        var state = await service.LoadAsync(Guid.NewGuid());

        Assert.Null(state);
    }

    [Fact]
    public async Task Load_ReportsEmptyCreativeTagsAndAssetsWithoutFabricating()
    {
        var sample = Sample.Create();
        var listing = sample.Listing with { MetadataJson = "{}", Description = null };
        var repository = new TestRepository(sample.Snapshot with
        {
            Listings = [listing],
            ListingTags = [],
            AssetLinks = []
        });
        var service = new ListingInspectorService(repository);

        var state = await service.LoadAsync(listing.Id);

        Assert.NotNull(state);
        Assert.Null(state!.Notes);
        Assert.Null(state.Creative.Idea);
        Assert.Null(state.Creative.Phrase);
        Assert.Null(state.Creative.GraphicDirection);
        Assert.Empty(state.Tags);
        Assert.Empty(state.Assets);
        Assert.False(state.Creative.HasAny);
    }

    [Fact]
    public async Task Save_NormalizesFieldsAndPersistsAtomically()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(3), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(
            sample.Listing.Id,
            "  Renamed Title ",
            "  A multi\nline idea  ",
            "phrase one\nphrase two",
            "  Graphic direction  ",
            "  Revised notes  ",
            []));

        Assert.True(result.Succeeded);
        Assert.Equal("Renamed Title", result.State!.Title);
        Assert.Equal("Revised notes", result.State.Notes);
        Assert.Equal("A multi\nline idea", result.State.Creative.Idea);
        Assert.Equal("phrase one phrase two", result.State.Creative.Phrase);
        Assert.Equal("Graphic direction", result.State.Creative.GraphicDirection);
        var persisted = repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal("Renamed Title", persisted.Name);
        Assert.Contains("\"idea\":\"A multi\\nline idea\"", persisted.MetadataJson);
        Assert.Contains("\"phrase\":\"phrase one phrase two\"", persisted.MetadataJson);
        Assert.Contains("\"graphicDirection\":\"Graphic direction\"", persisted.MetadataJson);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Save_PreservesUnknownMetadataAndInheritedProvenance()
    {
        var sample = Sample.Create();
        var listing = sample.Listing with
        {
            MetadataJson = "{\"notes\":\"old\",\"idea\":\"old idea\",\"custom\":\"keep\",\"inheritedFrom:custom\":\"Niche:" + sample.Niche.Id + "\"}"
        };
        var repository = new TestRepository(sample.Snapshot with { Listings = [listing] });
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(
            listing.Id,
            listing.Name,
            Idea: "new idea",
            Phrase: null,
            GraphicDirection: null,
            Notes: "new notes",
            TagNames: []));

        Assert.True(result.Succeeded);
        var persisted = repository.Snapshot.Listings.Single(candidate => candidate.Id == listing.Id);
        Assert.Contains("\"custom\":\"keep\"", persisted.MetadataJson);
        Assert.Contains($"\"inheritedFrom:custom\":\"Niche:{sample.Niche.Id}\"", persisted.MetadataJson);
        Assert.DoesNotContain("\"idea\":\"old idea\"", persisted.MetadataJson);
        Assert.Contains("\"idea\":\"new idea\"", persisted.MetadataJson);
        Assert.DoesNotContain("\"phrase\"", persisted.MetadataJson);
        Assert.Contains("\"notes\":\"new notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Save_RejectsBlankOrMultiLineTitleAndKeepsSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository);

        var blank = await service.SaveAsync(new(sample.Listing.Id, "   ", null, null, null, null, []));
        var multiLine = await service.SaveAsync(new(sample.Listing.Id, "two\nlines", null, null, null, null, []));

        Assert.False(blank.Succeeded);
        Assert.False(multiLine.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_LinksExistingTagCaseInsensitivelyWithoutDuplicates()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, null, null, null, null, ["TAG", "tag"]));

        Assert.True(result.Succeeded);
        var links = repository.Snapshot.ListingTags.Where(link => link.ListingId == sample.Listing.Id).ToArray();
        Assert.Single(links);
        Assert.Equal(sample.Tag.Id, links[0].TagId);
        Assert.Single(repository.Snapshot.Tags);
    }

    [Fact]
    public async Task Save_CreatesMissingTagAndLinksIt()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var newTagId = Guid.NewGuid();
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => newTagId);

        var result = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, null, null, null, null, ["New Tag"]));

        Assert.True(result.Succeeded);
        var created = repository.Snapshot.Tags.SingleOrDefault(tag => tag.Name == "New Tag");
        Assert.NotNull(created);
        Assert.Equal(newTagId, created!.Id);
        Assert.Equal(sample.Store.Id, created.StoreId);
        Assert.Contains(repository.Snapshot.ListingTags, link => link.ListingId == sample.Listing.Id && link.TagId == newTagId);
    }

    [Fact]
    public async Task Save_RemovesTagLinkButPreservesReusableTag()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, null, null, null, null, []));

        Assert.True(result.Succeeded);
        Assert.Empty(repository.Snapshot.ListingTags.Where(link => link.ListingId == sample.Listing.Id));
        Assert.Contains(repository.Snapshot.Tags, tag => tag.Id == sample.Tag.Id);
    }

    [Fact]
    public async Task Save_RejectsInvalidTagNames()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository);

        var blank = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, null, null, null, null, ["   "]));
        var multiLine = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, null, null, null, null, ["two\nlines"]));

        Assert.False(blank.Succeeded);
        Assert.False(multiLine.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_BlocksInactiveListingAndKeepsSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var archived = sample.Listing with { IsArchived = true };
        var repository = new TestRepository(sample.Snapshot with { Listings = [archived] });
        var service = new ListingInspectorService(repository);

        var result = await service.SaveAsync(new(archived.Id, archived.Name, "idea", null, null, null, []));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_FailureLeavesNoPartialChanges()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Listing.Id, sample.Listing.Name, "idea", null, null, null, ["New Tag"]));

        Assert.False(result.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Empty(repository.Snapshot.Tags.Where(tag => tag.Name == "New Tag"));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_RoundTripsThroughReloadedSnapshot()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ListingInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        await service.SaveAsync(new(sample.Listing.Id, "Updated", "idea", "phrase", "graphic", "notes", []));
        var reloaded = await service.LoadAsync(sample.Listing.Id);

        Assert.Equal("Updated", reloaded!.Title);
        Assert.Equal("idea", reloaded.Creative.Idea);
        Assert.Equal("phrase", reloaded.Creative.Phrase);
        Assert.Equal("graphic", reloaded.Creative.GraphicDirection);
        Assert.Equal("notes", reloaded.Notes);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        DateTimeOffset Now,
        Store Store,
        Niche Niche,
        TopicGroup Root,
        TopicGroup Child,
        Listing Listing,
        Tag Tag,
        Asset Asset)
    {
        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ListingStatus.Draft, WorkflowStage.Design, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var asset = new Asset(
                Guid.NewGuid(), store.Id, "Asset", null, AssetKind.SourceDesign, "assets/asset.png", null,
                isMissing: false, isArchived: false, now, now, "{}");
            var snapshot = WorkspaceSnapshot.FromStores(
                [store], [niche], [root, child], [listing],
                withRelationships ? [asset] : [],
                [],
                [tag],
                [new ListingTag(listing.Id, tag.Id)],
                withRelationships ? [new AssetLink(asset.Id, WorkspaceEntityKind.Listing, listing.Id)] : []);
            return new(snapshot, now, store, niche, root, child, listing, tag, asset);
        }
    }
}
