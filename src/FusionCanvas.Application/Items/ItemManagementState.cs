namespace FusionCanvas.Application.Items;

public sealed record ItemManagementState(
    Guid? ActiveStoreId,
    Guid? ActiveItemId,
    ItemSummary? ActiveItem,
    IReadOnlyList<ItemSummary> ActiveItems,
    IReadOnlyList<ItemSummary> ArchivedListings,
    IReadOnlyList<ItemDestination> ValidDestinations,
    bool NeedsFirstListing);
