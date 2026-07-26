namespace FusionCanvas.Domain.Items;

public static class ItemStatuses
{
    public static IReadOnlyList<ItemStatus> Ordered { get; } =
    [
        ItemStatus.Draft,
        ItemStatus.Published,
        ItemStatus.Paused,
        ItemStatus.Rejected
    ];

    public static string GetDisplayName(ItemStatus status) =>
        status switch
        {
            ItemStatus.Draft => "Draft",
            ItemStatus.Published => "Published",
            ItemStatus.Paused => "Paused",
            ItemStatus.Rejected => "Rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported item lifecycle status.")
        };
}
