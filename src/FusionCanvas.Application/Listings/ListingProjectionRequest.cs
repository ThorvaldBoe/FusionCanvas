using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Listings;

public sealed record ListingProjectionRequest(
    Item Item,
    Blueprint Blueprint,
    BlueprintOffering Offering,
    PrintProvider Provider,
    IReadOnlyList<OfferingVariant> Variants,
    IReadOnlyList<OfferingOption> Options,
    IReadOnlyList<OfferingOptionValue> OptionValues,
    IReadOnlyList<DesignSelectedColor> SelectedColors,
    IReadOnlyList<DesignVariantRow> VariantRows,
    IReadOnlyList<DesignVariantRowColor> VariantRowColors,
    IReadOnlyList<DesignSlotAssignment> SlotAssignments,
    IReadOnlyList<DesignArea> DesignAreas,
    IReadOnlyList<Asset> Assets,
    IReadOnlyDictionary<Guid, ArtworkDimensions> ArtworkDimensions,
    ListingPricingInput Pricing,
    string? ShippingProfile = null,
    string? OutOfStockPolicy = null);
