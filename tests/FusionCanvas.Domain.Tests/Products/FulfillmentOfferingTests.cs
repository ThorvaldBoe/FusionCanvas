using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class FulfillmentOfferingTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    private static FulfillmentOffering Offering(
        FulfillmentKind kind,
        string? providerName = null,
        string? name = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name ?? (kind == FulfillmentKind.FixedProvider ? "Printful" : "Choice"),
            null, kind, providerName, null, Now, Now, "{}");

    [Fact]
    public void FixedProvider_RequiresProviderName()
    {
        Assert.Throws<ArgumentException>(() => Offering(FulfillmentKind.FixedProvider, providerName: null));
        Assert.Throws<ArgumentException>(() => Offering(FulfillmentKind.FixedProvider, providerName: "  "));
    }

    [Fact]
    public void FixedProvider_RetainsProviderName()
    {
        Assert.Equal("Printful", Offering(FulfillmentKind.FixedProvider, providerName: "Printful").ProviderName);
    }

    [Fact]
    public void ChoiceNetwork_ForbidsProviderName()
    {
        Assert.Throws<ArgumentException>(() => Offering(FulfillmentKind.PrintifyChoiceNetwork, providerName: "Printful"));
    }

    [Fact]
    public void ChoiceNetwork_HasNoProviderName()
    {
        Assert.Null(Offering(FulfillmentKind.PrintifyChoiceNetwork).ProviderName);
    }

    [Fact]
    public void ChoiceNetwork_RequiresNoProvider()
    {
        var offering = Offering(FulfillmentKind.PrintifyChoiceNetwork);
        Assert.Equal(FulfillmentKind.PrintifyChoiceNetwork, offering.Kind);
    }

    [Fact]
    public void UndefinedKind_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Offering((FulfillmentKind)99));
    }

    [Fact]
    public void Offering_RequiresGlobalFields()
    {
        Assert.Throws<ArgumentException>(() => new FulfillmentOffering(
            Guid.Empty, Guid.NewGuid(), "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}"));
        Assert.Throws<ArgumentException>(() => new FulfillmentOffering(
            Guid.NewGuid(), Guid.Empty, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}"));
    }
}
