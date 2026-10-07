namespace FusionCanvas.Application.Listings;

public sealed record ListingPrice(Guid SourceVariantId, decimal ProductionCost, decimal RetailPrice);
