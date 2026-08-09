using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Stores;

public sealed record Store : WorkspaceEntity
{
    public Store(
        Guid id,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson,
        Guid? defaultNicheId = null,
        FulfillmentStrategy fulfillmentStrategy = FulfillmentStrategy.Manual)
        : this(id, WorkspaceDefaults.DefaultWorkspaceId, name, description, isArchived, createdAt, updatedAt, metadataJson, defaultNicheId, fulfillmentStrategy)
    {
    }

    public Store(
        Guid id,
        Guid workspaceId,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson,
        Guid? defaultNicheId = null,
        FulfillmentStrategy fulfillmentStrategy = FulfillmentStrategy.Manual)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        WorkspaceId = workspaceId == Guid.Empty
            ? throw new ArgumentException("Workspace identifier must not be empty.", nameof(workspaceId))
            : workspaceId;
        DefaultNicheId = defaultNicheId == Guid.Empty
            ? throw new ArgumentException("Default niche identifier must not be empty.", nameof(defaultNicheId))
            : defaultNicheId;
        FulfillmentStrategy = Enum.IsDefined(fulfillmentStrategy)
            ? fulfillmentStrategy
            : throw new ArgumentOutOfRangeException(nameof(fulfillmentStrategy), fulfillmentStrategy, "Fulfillment strategy is not supported.");
    }

    public Guid WorkspaceId { get; init; }

    public Guid? DefaultNicheId { get; init; }

    public FulfillmentStrategy FulfillmentStrategy { get; init; }
}
