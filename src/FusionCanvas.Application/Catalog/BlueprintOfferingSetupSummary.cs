namespace FusionCanvas.Application.Catalog;

public sealed record BlueprintOfferingSetupSummary(OfferingContext Context, string Name, string? Description, bool IsArchived, OfferingFulfillmentContext Fulfillment, OfferingSetupCounts Counts);
