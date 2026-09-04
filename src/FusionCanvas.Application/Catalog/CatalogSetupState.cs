using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CatalogSetupState(
    Guid StoreId,
    bool IsReadOnly,
    IReadOnlyList<Blueprint> Blueprints,
    IReadOnlyList<PrintProvider> PrintProviders,
    IReadOnlyList<BlueprintOffering> Offerings,
    IReadOnlyList<OfferingOption> Options,
    IReadOnlyList<OfferingOptionValue> OptionValues,
    IReadOnlyList<OfferingVariant> Variants,
    IReadOnlyList<OfferingPlaceholder> Placeholders);
