using FusionCanvas.Domain.Core;

namespace FusionCanvas.Domain.Tests;

public class CoreDomainModelTests
{
    [Fact]
    public void CoreModel_RepresentsTheSevenPhaseZeroEntities()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        var entities = new CoreEntity[]
        {
            new Store(storeId, "North Star Studio", "Primary POD brand"),
            new Niche(nicheId, storeId, "Coffee"),
            new Group(groupId, storeId, nicheId, null, "Seasonal Coffee"),
            new Listing(listingId, storeId, nicheId, groupId, "Autumn Espresso Shirt"),
            new Asset(assetId, storeId, "external:design-source", listingId, "Design source"),
            new Prompt(Guid.NewGuid(), storeId, "Create a warm fall coffee phrase.", "Listing prompt", listingId: listingId, assetId: assetId),
            new Tag(Guid.NewGuid(), storeId, "fall")
        };

        Assert.Collection(
            entities,
            entity => Assert.IsType<Store>(entity),
            entity => Assert.IsType<Niche>(entity),
            entity => Assert.IsType<Group>(entity),
            entity => Assert.IsType<Listing>(entity),
            entity => Assert.IsType<Asset>(entity),
            entity => Assert.IsType<Prompt>(entity),
            entity => Assert.IsType<Tag>(entity));
    }

    [Fact]
    public void Store_IsTopLevelBusinessContextWithoutParentOrMarketplaceRequirements()
    {
        var store = new Store(Guid.NewGuid(), "North Star Studio");

        Assert.Equal("North Star Studio", store.Name);
        Assert.DoesNotContain(store.GetType().GetProperties(), property => property.Name.Contains("Parent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(store.GetType().GetProperties(), property => property.Name.Contains("Marketplace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NicheAndGroup_AreTopicConceptsWithUnboundedNestedGroups()
    {
        var storeId = Guid.NewGuid();
        var niche = new Niche(Guid.NewGuid(), storeId, "Coffee");
        var parentGroup = new Group(Guid.NewGuid(), storeId, niche.Id, null, "Seasonal");
        var childGroup = new Group(Guid.NewGuid(), storeId, null, parentGroup.Id, "Autumn");
        var grandchildGroup = new Group(Guid.NewGuid(), storeId, null, childGroup.Id, "Espresso");

        Assert.IsAssignableFrom<ITopicEntity>(niche);
        Assert.IsAssignableFrom<ITopicEntity>(parentGroup);
        Assert.Equal(niche.Id, parentGroup.NicheId);
        Assert.Equal(parentGroup.Id, childGroup.ParentGroupId);
        Assert.Equal(childGroup.Id, grandchildGroup.ParentGroupId);
    }

    [Fact]
    public void Listing_IsInitialItemConceptInsideStoreTopicContext()
    {
        var storeId = Guid.NewGuid();
        var nicheId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var listing = new Listing(Guid.NewGuid(), storeId, nicheId, groupId, "Autumn Espresso Shirt");

        Assert.IsAssignableFrom<IItemEntity>(listing);
        Assert.IsNotAssignableFrom<ITopicEntity>(listing);
        Assert.Equal(storeId, listing.StoreId);
        Assert.Equal(nicheId, listing.NicheId);
        Assert.Equal(groupId, listing.GroupId);
        Assert.DoesNotContain(listing.GetType().GetProperties(), property => property.Name.Contains("Marketplace", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(listing.GetType().GetProperties(), property => property.Name.Contains("Published", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssetPromptAndTag_PreserveContextWithoutLaterWorkflowRequirements()
    {
        var storeId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var asset = new Asset(Guid.NewGuid(), storeId, "external:reference-board", listingId, "Reference board");
        var prompt = new Prompt(Guid.NewGuid(), storeId, "Generate phrase directions.", "Phrase prompt", listingId: listingId, assetId: asset.Id);
        var tag = new Tag(Guid.NewGuid(), storeId, "seasonal");
        var tagLinks = new[]
        {
            new TagLink(tag.Id, CoreEntityKind.Niche, Guid.NewGuid()),
            new TagLink(tag.Id, CoreEntityKind.Listing, listingId),
            new TagLink(tag.Id, CoreEntityKind.Asset, asset.Id)
        };

        Assert.Equal("external:reference-board", asset.ResourceReference);
        Assert.Equal(listingId, asset.ListingId);
        Assert.Equal("Generate phrase directions.", prompt.Text);
        Assert.Equal(asset.Id, prompt.AssetId);
        Assert.All(tagLinks, link => Assert.Equal(tag.Id, link.TagId));

        Assert.DoesNotContain(asset.GetType().GetProperties(), property => property.Name.Contains("WorkspaceRelativePath", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(prompt.GetType().GetProperties(), property => property.Name.Contains("Provider", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tag.GetType().GetProperties(), property => property.Name.Contains("Keyword", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CoreModel_DoesNotIntroduceAdvancedPhaseEntities()
    {
        var coreTypeNames = typeof(Store).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "FusionCanvas.Domain.Core")
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("Concept", coreTypeNames);
        Assert.DoesNotContain("Design", coreTypeNames);
        Assert.DoesNotContain("Mockup", coreTypeNames);
        Assert.DoesNotContain("MarketplaceProduct", coreTypeNames);
        Assert.DoesNotContain("PerformanceRecord", coreTypeNames);
        Assert.DoesNotContain("PluginData", coreTypeNames);
        Assert.DoesNotContain("WorkflowTemplate", coreTypeNames);
    }
}
