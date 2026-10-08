namespace FusionCanvas.Domain.Products;

/// <summary>Manual price and expected non-shipping fulfillment cost for one configured Variant.</summary>
public sealed record ItemVariantListingTerms(Guid ItemId, Guid VariantId, decimal? SellingPrice, decimal? ExpectedFulfillmentCost)
{
    public Guid ItemId { get; init; } = ProductRecordValidation.RequireId(ItemId, nameof(ItemId));
    public Guid VariantId { get; init; } = ProductRecordValidation.RequireId(VariantId, nameof(VariantId));
    public decimal? SellingPrice { get; init; } = Validate(SellingPrice, nameof(SellingPrice));
    public decimal? ExpectedFulfillmentCost { get; init; } = Validate(ExpectedFulfillmentCost, nameof(ExpectedFulfillmentCost));

    private static decimal? Validate(decimal? amount, string name) => amount is < 0 or > 999999999.99m
        ? throw new ArgumentOutOfRangeException(name, "Amount must be between 0 and 999,999,999.99.")
        : amount;
}
