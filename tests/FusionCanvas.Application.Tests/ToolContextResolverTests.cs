using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ToolContextResolverTests
{
    [Fact]
    public void Resolve_TopicContextIncludesHierarchyStageMetadataAndScopeSummary()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();

        var resolution = resolver.Resolve(new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Topic,
            WorkspaceEntityKind.Group,
            sample.Group.Id,
            WorkflowStage.Idea));

        Assert.True(resolution.IsAvailable);
        Assert.Equal("Topic: Autumn", resolution.ScopeSummary.Label);
        Assert.Equal(sample.Store.Id, resolution.Context?.ActiveStore.Id);
        Assert.Equal(sample.Niche.Id, resolution.Context?.ActiveNiche?.Id);
        Assert.Equal([sample.Niche.Id, sample.Group.Id], resolution.Context?.TopicPath.Select(topic => topic.EntityId));
        Assert.Equal(WorkflowStage.Idea, resolution.Context?.WorkflowStage);
        Assert.Contains(resolution.Context!.InheritedMetadata, value => value.Key == "brand" && value.Value == "North Star" && value.IsInherited);
        Assert.Contains(resolution.Context.InheritedMetadata, value => value.Key == "tone" && value.Source.EntityId == sample.Niche.Id);
        Assert.Contains(resolution.Context.InheritedMetadata, value => value.Key == "season" && value.Source.EntityId == sample.Group.Id);
    }

    [Fact]
    public void Resolve_ItemContextIncludesSelectedItemParentPathTagsAndAvailability()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();

        var resolution = resolver.Resolve(new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Item,
            WorkspaceEntityKind.Listing,
            sample.Listing.Id,
            WorkflowStage.Design,
            RequiresSelectedItem: true));

        Assert.True(resolution.IsAvailable);
        Assert.Equal("Item: Pumpkin espresso", resolution.ScopeSummary.Label);
        Assert.Equal(sample.Listing.Id, resolution.Context?.SelectedItem?.Id);
        Assert.Equal(sample.Group.Id, resolution.Context?.SelectedTopic?.EntityId);
        Assert.Equal([sample.Niche.Id, sample.Group.Id], resolution.Context?.TopicPath.Select(topic => topic.EntityId));
        Assert.Contains(resolution.Context!.ExplicitTags, tag => tag.Key == "tag" && tag.Value == sample.Tag.Name && !tag.IsInherited);
        Assert.Contains(resolution.Context.ExplicitMetadata, value => value.Key == "phrase" && value.Value == "Pumpkin power");
    }

    [Fact]
    public void ResolveCreationDefaults_PlacesNewWorkUnderSelectedTopicWithInheritedMetadata()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();
        var resolution = resolver.Resolve(new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Topic,
            WorkspaceEntityKind.Group,
            sample.Group.Id));

        var defaults = resolver.ResolveCreationDefaults(resolution);

        Assert.Equal(sample.Store.Id, defaults.StoreId);
        Assert.Equal(sample.Niche.Id, defaults.NicheId);
        Assert.Equal(sample.Group.Id, defaults.GroupId);
        Assert.Contains(defaults.Metadata, value => value.Key == "brand");
        Assert.Contains(defaults.Metadata, value => value.Key == "season");
    }

    [Fact]
    public void ResolveScope_OverridesScopeAndReResolvesSummary()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();
        var request = new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Topic,
            WorkspaceEntityKind.Group,
            sample.Group.Id);

        var resolution = resolver.ResolveScope(request, ToolContextScopeKind.CurrentSubtree);

        Assert.True(resolution.IsAvailable);
        Assert.Equal(ToolContextScopeKind.CurrentSubtree, resolution.Context?.ScopeKind);
        Assert.StartsWith("Subtree:", resolution.ScopeSummary.Label);
    }

    [Fact]
    public void Resolve_IncludesBoundedNearbyActiveAndArchivedWork()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();

        var resolution = resolver.Resolve(new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Topic,
            WorkspaceEntityKind.Group,
            sample.Group.Id,
            NearbyWorkLimit: 3));

        Assert.Equal(3, resolution.Context?.NearbyWork.Count);
        Assert.Contains(resolution.Context!.NearbyWork, work => work.EntityId == sample.OtherListing.Id && work.State == NearbyWorkState.Active);
        Assert.Contains(resolution.Context.NearbyWork, work => work.EntityId == sample.ArchivedListing.Id && work.State == NearbyWorkState.RejectedOrArchived);
    }

    [Fact]
    public void Resolve_ItemBoundToolWithoutItemReturnsUnavailable()
    {
        var sample = ToolContextSample.Create();
        var resolver = new ToolContextResolver();

        var resolution = resolver.Resolve(new ToolContextResolveRequest(
            sample.Snapshot,
            ToolContextSelectionKind.Topic,
            WorkspaceEntityKind.Group,
            sample.Group.Id,
            RequiresSelectedItem: true));

        Assert.False(resolution.IsAvailable);
        Assert.Equal("This tool requires a selected item.", resolution.UnavailableReason);
        Assert.Null(resolution.Context);
    }

    private sealed record ToolContextSample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        TopicGroup Group,
        Listing Listing,
        Listing OtherListing,
        Listing ArchivedListing,
        Tag Tag)
    {
        public static ToolContextSample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "North Star Studio", null, false, now, now, """{"brand":"North Star"}""");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, """{"tone":"warm"}""");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Autumn", null, false, now, now, """{"season":"fall"}""");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Pumpkin espresso", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, """{"phrase":"Pumpkin power"}""");
            var otherListing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Latte leaves", null, ListingStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
            var archivedListing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Retired roast", null, ListingStatus.Draft, WorkflowStage.Idea, true, now, now, "{}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "evergreen", null, false, now, now, "{}");

            return new ToolContextSample(
                new WorkspaceSnapshot(
                    [store],
                    [niche],
                    [group],
                    [listing, otherListing, archivedListing],
                    [],
                    [],
                    [tag],
                    [new ListingTag(listing.Id, tag.Id)],
                    []),
                store,
                niche,
                group,
                listing,
                otherListing,
                archivedListing,
                tag);
        }
    }
}
