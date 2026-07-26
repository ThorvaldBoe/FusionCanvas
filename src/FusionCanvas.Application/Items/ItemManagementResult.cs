namespace FusionCanvas.Application.Items;

public sealed record ItemManagementResult(
    bool Succeeded,
    string? Error,
    ItemSummary? Item,
    ItemManagementState State)
{
    public static ItemManagementResult Success(ItemSummary? listing, ItemManagementState state) =>
        new(true, null, listing, state);

    public static ItemManagementResult Failure(string error, ItemManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Listing operation failed." : error, null, state);
}
