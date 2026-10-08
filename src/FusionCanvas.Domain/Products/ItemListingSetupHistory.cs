namespace FusionCanvas.Domain.Products;

/// <summary>Immutable snapshot of a previous local fulfillment setup.</summary>
public sealed record ItemListingSetupHistory(
    Guid Id,
    Guid ItemId,
    Guid? OfferingId,
    string OfferingName,
    string? Title,
    string? Description,
    string? CurrencyCode,
    string? ShippingOptionName,
    decimal? CustomerShippingCharge,
    decimal? ExpectedSellerShippingCost,
    string? DeliveryEstimate,
    string VariantTermsJson,
    DateTimeOffset ArchivedAt)
{
    public Guid Id { get; init; } = ProductRecordValidation.RequireId(Id, nameof(Id));
    public Guid ItemId { get; init; } = ProductRecordValidation.RequireId(ItemId, nameof(ItemId));
    public string OfferingName { get; init; } = ProductRecordValidation.RequireText(OfferingName, nameof(OfferingName));
    public string VariantTermsJson { get; init; } = string.IsNullOrWhiteSpace(VariantTermsJson) ? "[]" : VariantTermsJson;
}
