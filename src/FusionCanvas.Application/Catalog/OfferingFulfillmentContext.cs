using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Application.Catalog;

public sealed record OfferingFulfillmentContext(BlueprintOfferingKind Kind, string DisplayName, bool IsVariableProviderNetwork, string CatalogSource = "Printify");
