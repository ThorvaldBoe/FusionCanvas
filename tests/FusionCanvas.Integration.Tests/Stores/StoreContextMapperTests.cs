using FusionCanvas.Application.Stores;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Integration.Stores;

namespace FusionCanvas.Integration.Tests.Stores;

public sealed class StoreContextMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Read_MapsStoreDescriptionAndFeatureMetadata()
    {
        var store = NewStore("Description", """{"notes":" Notes ","targetMarket":"Coffee fans","brandDirection":"Warm","planningContext":"Fall","url":"https://example.test","printifyShopId":"123","printifyShopTitle":"DevTest"}""");

        var context = new StoreContextMapper().Read(store);

        Assert.Equal("Description", context.Description);
        Assert.Equal(" Notes ", context.Notes);
        Assert.Equal("Coffee fans", context.TargetMarket);
        Assert.Equal("Warm", context.BrandDirection);
        Assert.Equal("Fall", context.PlanningContext);
        Assert.Equal("https://example.test", context.Url);
        Assert.Equal(123, context.PrintifyShopId);
        Assert.Equal("DevTest", context.PrintifyShopTitle);
    }

    [Fact]
    public void Apply_SerializesFeatureMetadataAndPreservesUnknownKeys()
    {
        var store = NewStore(null, """{"custom":"Retain me","notes":"Old"}""");
        var context = new StoreContext(
            Description: " Updated description ",
            Notes: " New notes ",
            TargetMarket: "Coffee fans",
            BrandDirection: "Warm",
            PlanningContext: "Fall",
            Url: "https://example.test",
            PrintifyShopId: 123,
            PrintifyShopTitle: "DevTest");

        var updated = new StoreContextMapper().Apply(store, context);

        Assert.Equal(" Updated description ", updated.Description);
        using var metadata = System.Text.Json.JsonDocument.Parse(updated.MetadataJson);
        Assert.Equal("Retain me", metadata.RootElement.GetProperty("custom").GetString());
        Assert.Equal("New notes", metadata.RootElement.GetProperty("notes").GetString());
        Assert.Equal("Coffee fans", metadata.RootElement.GetProperty("targetMarket").GetString());
        Assert.Equal("123", metadata.RootElement.GetProperty("printifyShopId").GetString());
    }

    [Fact]
    public void Apply_RemovesClearedFeatureMetadataAndReturnsEmptyObjectWhenNothingRemains()
    {
        var store = NewStore(null, """{"notes":"Old","url":"https://example.test"}""");

        var updated = new StoreContextMapper().Apply(store, new StoreContext());

        Assert.Equal("{}", updated.MetadataJson);
    }

    private static Store NewStore(string? description, string metadataJson) =>
        new(Guid.NewGuid(), "Store", description, false, Now, Now, metadataJson);
}
