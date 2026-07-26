using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.Application.Items;

public sealed record ItemSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    ItemTopicReference Topic,
    string Name,
    ItemContext Context,
    ItemStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    IReadOnlyList<Guid> Path,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
