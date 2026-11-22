using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.ArtworkStyles;

namespace FusionCanvas.Application.Tests.DesignFiles;

public sealed class ArtworkPromptCompilerTests
{
    [Fact]
    public void BuildDefault_UsesCurrentContextAndSeparatesContrastFromBackground()
    {
        var fixture = CreateFixture(ArtworkContrast.LightOnDark, staleSll: false);

        var first = ArtworkPromptCompiler.BuildDefault(fixture.Snapshot, fixture.Item, fixture.Area.Id, ArtworkContrast.LightOnDark);
        var second = ArtworkPromptCompiler.BuildDefault(fixture.Snapshot, fixture.Item, fixture.Area.Id, ArtworkContrast.LightOnDark);
        var prompt = Assert.IsType<string>(first);

        Assert.Equal(first, second);
        Assert.Contains("Phrase (verbatim artwork text", prompt, StringComparison.Ordinal);
        Assert.Contains("Two-tired for work", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("vintage screen print", prompt, StringComparison.Ordinal);
        Assert.Contains("Audience: commuters", prompt, StringComparison.Ordinal);
        Assert.Contains("Humor style: deadpan", prompt, StringComparison.Ordinal);
        Assert.Contains("Constraints: printable art only", prompt, StringComparison.Ordinal);
        Assert.Contains("Brand direction: playful retro typography", prompt, StringComparison.Ordinal);
        Assert.Contains("ARTWORK CONTRAST: Use light foreground artwork", prompt, StringComparison.Ordinal);
        Assert.Contains("Keep the background transparent or empty", prompt, StringComparison.Ordinal);
        Assert.Contains("Side-by-side text above the bike", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Assumptions", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("dark unisex tee", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDefault_UsesSelectedStyleAndReplacesNicheVisualDirection()
    {
        var fixture = CreateFixture(ArtworkContrast.DarkOnLight, staleSll: false);
        var now = DateTimeOffset.UtcNow;
        var style = new ArtworkStyle(Guid.NewGuid(), fixture.Snapshot.Stores[0].WorkspaceId, "Watercolor", "Soft painted edges and a muted palette.", now, now);
        var metadata = ItemMetadataCodec.ParseMetadata(fixture.Item.MetadataJson);
        metadata[ItemMetadataCodec.ArtworkStyleIdKey] = style.Id.ToString();
        var selectedItem = fixture.Item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata) };
        var snapshot = fixture.Snapshot with { Items = [selectedItem], ArtworkStyles = [style] };

        var prompt = ArtworkPromptCompiler.BuildDefault(snapshot, selectedItem, fixture.Area.Id, ArtworkContrast.DarkOnLight);

        Assert.Contains("Style title (creative data): <<<Watercolor>>>", prompt!, StringComparison.Ordinal);
        Assert.Contains("Style description (creative data): <<<Soft painted edges and a muted palette.>>>", prompt!, StringComparison.Ordinal);
        Assert.DoesNotContain("vintage screen print", prompt!, StringComparison.Ordinal);
        Assert.Contains("Audience: commuters", prompt!, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDefault_OmitsStaleSllCompositionAndUsesDarkOnLightDirection()
    {
        var fixture = CreateFixture(ArtworkContrast.DarkOnLight, staleSll: true);

        var prompt = ArtworkPromptCompiler.BuildDefault(fixture.Snapshot, fixture.Item, fixture.Area.Id, ArtworkContrast.DarkOnLight);

        Assert.Contains("ARTWORK CONTRAST: Use dark foreground artwork", prompt!, StringComparison.Ordinal);
        Assert.DoesNotContain("Side-by-side text above the bike", prompt!, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDefault_OmitsMalformedSllComposition()
    {
        var fixture = CreateFixture(ArtworkContrast.DarkOnLight, staleSll: false, malformedSll: true);

        var prompt = ArtworkPromptCompiler.BuildDefault(fixture.Snapshot, fixture.Item, fixture.Area.Id, ArtworkContrast.DarkOnLight);

        Assert.DoesNotContain("Composition guidance from the current SLL", prompt!, StringComparison.Ordinal);
    }

    private static Fixture CreateFixture(ArtworkContrast contrast, bool staleSll, bool malformedSll = false)
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Two Wheels", "Commuter humor", false, now, now,
            """{"brandDirection":"playful retro typography","targetMarket":"bike commuters"}""");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dad jokes", "Silly commuting puns", false, now, now,
            """{"audience":"commuters","humorStyle":"deadpan","visualStyleGuidance":"vintage screen print","constraints":"printable art only"}""");
        var metadata = new Dictionary<string, string>
        {
            [ItemMetadataCodec.IdeaKey] = "Two tired bicycle commute pun",
            [ItemMetadataCodec.ConceptIdeaKey] = "A bike commuter feels too tired for work",
            [ItemMetadataCodec.PhraseKey] = "Two-tired for work",
            [ItemMetadataCodec.GraphicDirectionKey] = "A sleek side-profile city bike",
            [ItemMetadataCodec.SllKey] = malformedSll ? "{broken" : JsonSerializer.Serialize(new SllDocument(
                ["Assumption: dark unisex tee"],
                new SllCommunication("commuter", "dad-joke fan", null, "two wheels"),
                new SllTriangle("Bike pun", "Two-tired for work", "City bike", "Side-by-side text above the bike", null),
                "TEXT\n  BIKE", new SllNotes("Text above bike", "Condensed", "Vintage", "Black and cream", "Light distress", "Center chest", "DTG"),
                new SllValidation("top-down", null, null, null))),
            [ItemMetadataCodec.SllSourceFingerprintKey] = staleSll ? "stale" : ItemMetadataCodec.ComputeSllSourceFingerprint(
                "Two tired bicycle commute pun", "A bike commuter feels too tired for work", "Two-tired for work", "A sleek side-profile city bike")
        };
        var item = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Bike pun", null, ItemStatus.Draft, WorkflowStage.Design,
            false, now, now, ItemMetadataCodec.SerializeMetadata(metadata));
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider,
            "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3600, 4800, null, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([], [store], [niche], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new ItemListingConfiguration(item.Id, offering.Id)]
        };
        return new Fixture(snapshot, item, area);
    }

    private sealed record Fixture(WorkspaceSnapshot Snapshot, Item Item, DesignArea Area);
}
