namespace FusionCanvas.Application.Listings;

public static class ListingPricingCalculator
{
    public static IReadOnlyList<ListingPrice> Calculate(
        ListingPricingInput input,
        IReadOnlyDictionary<Guid, decimal?> productionCosts)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(productionCosts);
        input.Normalize();

        if (productionCosts.Count == 0)
        {
            throw new ArgumentException("At least one enabled variant is required.", nameof(productionCosts));
        }

        return productionCosts
            .OrderBy(value => value.Key)
            .Select(value =>
            {
                if (value.Value is < 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(productionCosts), value.Value, "Production cost cannot be negative.");
                }

                var retailPrice = input.Policy switch
                {
                    ListingPricingPolicy.FixedRetailPrice => input.Amount,
                    ListingPricingPolicy.FixedProfitAmount => value.Value is decimal cost
                        ? cost + input.Amount
                        : throw new InvalidOperationException("Fixed-profit pricing requires a production cost for every enabled variant."),
                    _ => throw new ArgumentOutOfRangeException(nameof(input), input.Policy, "Pricing policy is not supported.")
                };

                return new ListingPrice(value.Key, value.Value, retailPrice);
            })
            .ToArray();
    }
}
