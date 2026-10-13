namespace FusionCanvas.ApiVerification.Printify;

internal sealed record PrintifyResourceJournal(
    int SchemaVersion,
    string RunId,
    int ShopId,
    string ShopName,
    string LastOperation,
    string? ProductId,
    string Status,
    DateTimeOffset UpdatedAt);
