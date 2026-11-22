using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ListingColorSelectionServiceTests
{
    [Fact]
    public async Task FavoritesAreOfferingScopedAndDoNotSelectColors()
    {
        var fixture = CreateFixture();
        var service = new ListingColorSelectionService(fixture.Repository);

        await service.SetFavoriteAsync(fixture.Offering.Id, fixture.White.Id, true);
        var state = await service.LoadAsync(fixture.Item.Id);

        Assert.Equal(fixture.White.Id, Assert.Single(state.Favorites).OptionValueId);
        Assert.Empty(state.OtherColors.Where(value => value.IsSelected));
        Assert.Empty(fixture.Repository.Snapshot.ListingColorSelections);
        Assert.Equal(2, state.Favorites.Single().ActiveVariantCount);
        Assert.Equal(1, state.Favorites.Single().OutOfStockVariantCount);
    }

    [Fact]
    public async Task SelectingMultipleColorsDefaultsEachToFirstArtworkAndKeepsAllSizes()
    {
        var fixture = CreateFixture();
        var service = new ListingColorSelectionService(fixture.Repository);

        await service.SetSelectedColorsAsync(fixture.Item.Id, [fixture.Black.Id, fixture.White.Id]);
        var selections = fixture.Repository.Snapshot.ListingColorSelections.OrderBy(value => value.SortOrder).ToArray();

        Assert.Equal(2, selections.Length);
        Assert.All(selections, value => Assert.Equal(fixture.DefaultArtwork.Id, value.ArtworkVariantId));
        Assert.Equal(4, fixture.Repository.Snapshot.OfferingVariants.Count(value => selections.Any(selection => value.OptionValueIds.Contains(selection.ColorOptionValueId!.Value))));

        await service.SetArtworkVariantAsync(fixture.Item.Id, fixture.Black.Id, fixture.AlternateArtwork.Id);
        Assert.Equal(fixture.AlternateArtwork.Id, fixture.Repository.Snapshot.ListingColorSelections.Single(value => value.ColorOptionValueId == fixture.Black.Id).ArtworkVariantId);
    }

    [Fact]
    public async Task CannotSelectColorWithoutConfiguredProviderVariants()
    {
        var fixture = CreateFixture();
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            OfferingVariants = fixture.Repository.Snapshot.OfferingVariants
                .Where(value => !value.OptionValueIds.Contains(fixture.White.Id)).ToArray()
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ListingColorSelectionService(fixture.Repository).SetSelectedColorsAsync(fixture.Item.Id, [fixture.White.Id]));

        Assert.Contains("no configured provider Variants", error.Message);
        Assert.Empty(fixture.Repository.Snapshot.ListingColorSelections);
    }

    [Fact]
    public async Task ResolvingAnUnmatchedLegacyColorKeepsItsArtworkAssignment()
    {
        var fixture = CreateFixture();
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            ListingColorSelections = [new ListingColorSelection(fixture.Item.Id, null, fixture.AlternateArtwork.Id, 4, "Legacy Heather")]
        };
        var service = new ListingColorSelectionService(fixture.Repository);

        Assert.Equal("Legacy Heather", Assert.Single((await service.LoadAsync(fixture.Item.Id)).UnresolvedColors).Label);
        await service.ResolveUnresolvedColorAsync(fixture.Item.Id, 4, fixture.White.Id);

        var resolved = Assert.Single(fixture.Repository.Snapshot.ListingColorSelections);
        Assert.Equal(fixture.White.Id, resolved.ColorOptionValueId);
        Assert.Equal(fixture.AlternateArtwork.Id, resolved.ArtworkVariantId);
        Assert.Null(resolved.LegacyColorLabel);
    }

    private static Fixture CreateFixture()
    {
        var now = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
        var workspace = WorkspaceSnapshot.DefaultWorkspace(now);
        var store = new Store(workspace.Id, "Store", null, false, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "Tee", null, false, now, now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "Provider", "42", false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Tee", null, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, "1:42", false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var white = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "White", 1);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var large = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "L", 1);
        var variants = new[]
        {
            new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, now, now) { ProviderIsAvailable = true },
            new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / L", [black.Id, large.Id], false, now, now) { ProviderIsAvailable = true },
            new OfferingVariant(Guid.NewGuid(), offering.Id, "White / M", [white.Id, medium.Id], false, now, now) { ProviderIsAvailable = false },
            new OfferingVariant(Guid.NewGuid(), offering.Id, "White / L", [white.Id, large.Id], false, now, now) { ProviderIsAvailable = true }
        };
        var defaultArtwork = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var alternateArtwork = new DesignVariantRow(Guid.NewGuid(), item.Id, false, 1);
        var snapshot = new WorkspaceSnapshot([workspace], [store], [], [], [item], [], [], [], [], [])
        {
            Blueprints = [blueprint],
            PrintProviders = [provider],
            BlueprintOfferings = [offering],
            ItemListingConfigurations = [new ItemListingConfiguration(item.Id, offering.Id)],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, white, medium, large],
            OfferingVariants = variants,
            DesignVariantRows = [defaultArtwork, alternateArtwork]
        };
        return new(new MemoryRepository(snapshot), item, offering, black, white, defaultArtwork, alternateArtwork);
    }

    private sealed record Fixture(MemoryRepository Repository, Item Item, BlueprintOffering Offering, OfferingOptionValue Black, OfferingOptionValue White, DesignVariantRow DefaultArtwork, DesignVariantRow AlternateArtwork);

    private sealed class MemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default) { Snapshot = value; return Task.CompletedTask; }
    }
}
