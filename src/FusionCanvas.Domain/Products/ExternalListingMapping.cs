namespace FusionCanvas.Domain.Products;

public sealed record ExternalListingMapping
{
    public ExternalListingMapping(
        Guid storeId,
        Guid itemId,
        string providerKey,
        string shopId,
        string? productId,
        string? externalPublicationId,
        string? externalHandle,
        ExternalListingSyncState synchronizationState,
        ExternalListingPublicationState publicationState,
        ExternalListingOperationState operationState,
        string? snapshotJson,
        string? operationJson,
        DateTimeOffset? lastSynchronizedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? uploadReferencesJson = null,
        string? integrationValuesJson = null)
    {
        StoreId = RequireId(storeId, nameof(storeId));
        ItemId = RequireId(itemId, nameof(itemId));
        ProviderKey = RequireText(providerKey, nameof(providerKey));
        ShopId = RequireText(shopId, nameof(shopId));
        ProductId = Normalize(productId);
        ExternalPublicationId = Normalize(externalPublicationId);
        ExternalHandle = Normalize(externalHandle);
        SynchronizationState = RequireDefined(synchronizationState, nameof(synchronizationState));
        PublicationState = RequireDefined(publicationState, nameof(publicationState));
        OperationState = RequireDefined(operationState, nameof(operationState));
        SnapshotJson = string.IsNullOrWhiteSpace(snapshotJson) ? null : snapshotJson;
        OperationJson = string.IsNullOrWhiteSpace(operationJson) ? null : operationJson;
        UploadReferencesJson = string.IsNullOrWhiteSpace(uploadReferencesJson) ? null : uploadReferencesJson;
        IntegrationValuesJson = string.IsNullOrWhiteSpace(integrationValuesJson) ? null : integrationValuesJson;
        LastSynchronizedAt = lastSynchronizedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid StoreId { get; init; }
    public Guid ItemId { get; init; }
    public string ProviderKey { get; init; }
    public string ShopId { get; init; }
    public string? ProductId { get; init; }
    public string? ExternalPublicationId { get; init; }
    public string? ExternalHandle { get; init; }
    public ExternalListingSyncState SynchronizationState { get; init; }
    public ExternalListingPublicationState PublicationState { get; init; }
    public ExternalListingOperationState OperationState { get; init; }
    public string? SnapshotJson { get; init; }
    public string? OperationJson { get; init; }
    public string? UploadReferencesJson { get; init; }
    public string? IntegrationValuesJson { get; init; }
    public DateTimeOffset? LastSynchronizedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    private static Guid RequireId(Guid value, string name) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;

    private static string RequireText(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) : value.Trim();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static T RequireDefined<T>(T value, string name) where T : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(name, value, "Unsupported listing state.");
}
