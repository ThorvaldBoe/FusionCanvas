using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class WorkspaceDomainModelTests
{
    [Fact]
    public void CoreModel_RepresentsTopicAndItemRelationships()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var store = new Store(storeId, "North Star Studio", "Primary POD brand", false, now, now, "{}");
        var niche = new Niche(nicheId, store.Id, "Coffee", null, false, now, now, "{}");
        var group = new TopicGroup(groupId, store.Id, niche.Id, null, "Seasonal Coffee", null, false, now, now, "{}");
        var listing = new Listing(listingId, store.Id, niche.Id, group.Id, "Autumn Espresso Shirt", null, ListingStatus.Draft, false, now, now, "{}");
        var tag = new Tag(tagId, store.Id, "fall", null, false, now, now, "{}");

        Assert.Equal(store.Id, niche.StoreId);
        Assert.Equal(niche.Id, group.NicheId);
        Assert.Equal(group.Id, listing.GroupId);
        Assert.Equal(new ListingTag(listing.Id, tag.Id), new ListingTag(listing.Id, tag.Id));
    }

    [Fact]
    public void Asset_StoresWorkspacePathInsteadOfBinaryContent()
    {
        var now = DateTimeOffset.UtcNow;
        var asset = new Asset(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "mockup.png",
            "Generated listing mockup",
            AssetKind.MockupImage,
            "assets/2026/06/mockup.png",
            "C:/imports/mockup.png",
            false,
            false,
            now,
            now,
            "{}");

        Assert.Equal("assets/2026/06/mockup.png", asset.WorkspaceRelativePath);
        Assert.False(asset.IsMissing);
    }
}
