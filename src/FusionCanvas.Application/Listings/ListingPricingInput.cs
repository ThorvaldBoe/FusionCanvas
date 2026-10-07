namespace FusionCanvas.Application.Listings;

public sealed record ListingPricingInput(ListingPricingPolicy Policy, decimal Amount)
{
    public ListingPricingInput Normalize()
    {
        if (Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "Pricing amount cannot be negative.");
        return this;
    }
}
