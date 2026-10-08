namespace FusionCanvas.Domain.Products;

/// <summary>Local, customer-facing listing values for one Item and its active fulfillment setup.</summary>
public sealed record ItemListingDetails
{
    public ItemListingDetails(Guid itemId, Guid? offeringId, string? title, string? description, string? currencyCode,
        string? shippingOptionName = null, decimal? customerShippingCharge = null,
        decimal? expectedSellerShippingCost = null, string? deliveryEstimate = null)
    {
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        OfferingId = offeringId is null ? null : ProductRecordValidation.RequireId(offeringId.Value, nameof(offeringId));
        Title = ProductRecordValidation.NormalizeOptional(title);
        Description = ProductRecordValidation.NormalizeOptional(description);
        CurrencyCode = NormalizeCurrency(currencyCode);
        ShippingOptionName = ProductRecordValidation.NormalizeOptional(shippingOptionName);
        CustomerShippingCharge = Amount(customerShippingCharge, nameof(customerShippingCharge));
        ExpectedSellerShippingCost = Amount(expectedSellerShippingCost, nameof(expectedSellerShippingCost));
        DeliveryEstimate = ProductRecordValidation.NormalizeOptional(deliveryEstimate);
    }

    public Guid ItemId { get; init; }
    public Guid? OfferingId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CurrencyCode { get; init; }
    public string? ShippingOptionName { get; init; }
    public decimal? CustomerShippingCharge { get; init; }
    public decimal? ExpectedSellerShippingCost { get; init; }
    public string? DeliveryEstimate { get; init; }

    private static string? NormalizeCurrency(string? value)
    {
        var normalized = ProductRecordValidation.NormalizeOptional(value)?.ToUpperInvariant();
        if (normalized is not null && (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z') || !KnownCurrencies.Contains(normalized)))
            throw new ArgumentException("Currency must be a three-letter ISO currency code.", nameof(value));
        return normalized;
    }

    private static readonly HashSet<string> KnownCurrencies = System.Globalization.CultureInfo
        .GetCultures(System.Globalization.CultureTypes.AllCultures)
        .Where(culture => !string.IsNullOrEmpty(culture.Name))
        .Select(culture =>
        {
            try { return new System.Globalization.RegionInfo(culture.Name).ISOCurrencySymbol; }
            catch (ArgumentException) { return string.Empty; }
        })
        .Where(code => code.Length == 3)
        .ToHashSet(StringComparer.Ordinal);

    private static decimal? Amount(decimal? amount, string parameterName)
    {
        if (amount is < 0 or > 999999999.99m)
            throw new ArgumentOutOfRangeException(parameterName, "Amount must be between 0 and 999,999,999.99.");
        return amount;
    }
}
