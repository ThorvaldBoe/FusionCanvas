namespace FusionCanvas.Domain.Listings;

public sealed record ListingProviderState
{
    public ListingProviderState(
        Guid itemId,
        string provider,
        string channel,
        string externalId,
        ListingSyncStatus syncStatus = ListingSyncStatus.NotConnected,
        string? providerMetadataJson = null,
        string? lastResult = null,
        string? errorMessage = null,
        string? conflictDetails = null,
        DateTimeOffset? lastAttemptAt = null,
        DateTimeOffset? externalStateAt = null,
        bool isLocked = false)
    {
        if (itemId == Guid.Empty) throw new ArgumentException("An item identifier is required.", nameof(itemId));
        Provider = RequireText(provider, nameof(provider));
        Channel = RequireText(channel, nameof(channel));
        ExternalId = RequireText(externalId, nameof(externalId));
        ItemId = itemId;
        SyncStatus = syncStatus;
        ProviderMetadataJson = string.IsNullOrWhiteSpace(providerMetadataJson) ? "{}" : providerMetadataJson;
        LastResult = NormalizeOptional(lastResult);
        ErrorMessage = NormalizeOptional(errorMessage);
        ConflictDetails = NormalizeOptional(conflictDetails);
        LastAttemptAt = lastAttemptAt;
        ExternalStateAt = externalStateAt;
        IsLocked = isLocked;
    }

    public Guid ItemId { get; init; }
    public string Provider { get; init; }
    public string Channel { get; init; }
    public string ExternalId { get; init; }
    public ListingSyncStatus SyncStatus { get; init; }
    public string ProviderMetadataJson { get; init; }
    public string? LastResult { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ConflictDetails { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
    public DateTimeOffset? ExternalStateAt { get; init; }
    public bool IsLocked { get; init; }

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", parameterName) : value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
