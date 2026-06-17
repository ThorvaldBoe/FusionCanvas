using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.Integration.Tests;

public class SqliteWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadAsync_PreservesCoreEntitiesAndRelationships()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var databasePath = Path.Combine(tempDirectory.FullName, "workspace.db");
        var repository = new SqliteWorkspaceRepository(databasePath);

        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "North Star Studio", "POD brand", false, now, now, """{"currency":"USD"}""");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Fall launch", null, false, now, now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", null, ListingStatus.Draft, false, now, now, "{}");
        var asset = new Asset(Guid.NewGuid(), store.Id, "design.png", null, AssetKind.ExportedImage, "assets/2026/06/design.png", "C:/imports/design.png", false, false, now, now, "{}");
        var prompt = new Prompt(Guid.NewGuid(), store.Id, listing.Id, "Listing prompt", null, "Create a warm fall coffee phrase.", false, now, now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "seasonal", null, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot(
            [store],
            [niche],
            [group],
            [listing],
            [asset],
            [prompt],
            [tag],
            [new ListingTag(listing.Id, tag.Id)],
            [new AssetLink(asset.Id, WorkspaceEntityKind.Listing, listing.Id)]);

        await repository.SaveAsync(snapshot);

        var loaded = await repository.LoadAsync();

        Assert.Equal(store, Assert.Single(loaded.Stores));
        Assert.Equal(niche, Assert.Single(loaded.Niches));
        Assert.Equal(group, Assert.Single(loaded.Groups));
        Assert.Equal(listing, Assert.Single(loaded.Listings));
        Assert.Equal(asset, Assert.Single(loaded.Assets));
        Assert.Equal(prompt, Assert.Single(loaded.Prompts));
        Assert.Equal(tag, Assert.Single(loaded.Tags));
        Assert.Equal(snapshot.ListingTags[0], Assert.Single(loaded.ListingTags));
        Assert.Equal(snapshot.AssetLinks[0], Assert.Single(loaded.AssetLinks));
    }
}
