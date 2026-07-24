using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Workspace;

public enum ToolContextScopeKind
{
    CurrentItem = 0,
    CurrentTopic = 1,
    TopicPath = 2,
    Niche = 3,
    Store = 4,
    CurrentSubtree = 5,
    Unsupported = 6,
    Unavailable = 7
}

public enum ToolContextSelectionKind
{
    Store = 0,
    Topic = 1,
    Item = 2
}

public enum NearbyWorkState
{
    Active = 0,
    RejectedOrArchived = 1
}

public sealed record ToolContextEntityReference(
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name)
{
    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;

    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Name is required.", nameof(Name))
        : Name;
}

public sealed record ToolContextInheritedValue(
    string Key,
    string Value,
    ToolContextEntityReference Source,
    bool IsInherited)
{
    public string Key { get; } = string.IsNullOrWhiteSpace(Key)
        ? throw new ArgumentException("Key is required.", nameof(Key))
        : Key;

    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}

public sealed record ToolContextNearbyWorkSummary(
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    NearbyWorkState State,
    bool IsSibling)
{
    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;

    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Name is required.", nameof(Name))
        : Name;
}

public sealed record ToolContextScopeSummary(
    ToolContextScopeKind Kind,
    string Label,
    string Description,
    bool CanRun)
{
    public string Label { get; } = string.IsNullOrWhiteSpace(Label)
        ? throw new ArgumentException("Label is required.", nameof(Label))
        : Label;

    public string Description { get; } = string.IsNullOrWhiteSpace(Description)
        ? throw new ArgumentException("Description is required.", nameof(Description))
        : Description;
}

public sealed record ToolContextResolveRequest(
    WorkspaceSnapshot Snapshot,
    ToolContextSelectionKind SelectionKind,
    WorkspaceEntityKind SelectedEntityKind,
    Guid SelectedEntityId,
    WorkflowStage? WorkflowStage = null,
    ToolContextScopeKind? ScopeOverride = null,
    bool RequiresSelectedItem = false,
    int NearbyWorkLimit = 8)
{
    public Guid SelectedEntityId { get; } = SelectedEntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(SelectedEntityId))
        : SelectedEntityId;

    public int NearbyWorkLimit { get; } = NearbyWorkLimit > 0
        ? NearbyWorkLimit
        : throw new ArgumentOutOfRangeException(nameof(NearbyWorkLimit), NearbyWorkLimit, "Nearby work limit must be positive.");
}

public sealed record ToolContextResolution(
    bool IsAvailable,
    string? UnavailableReason,
    ToolContextScopeSummary ScopeSummary,
    ToolContext? Context)
{
    public static ToolContextResolution Unavailable(string reason) =>
        new(
            false,
            string.IsNullOrWhiteSpace(reason) ? "Tool context is unavailable." : reason,
            new ToolContextScopeSummary(ToolContextScopeKind.Unavailable, "Unavailable", reason, false),
            null);
}

public sealed record ToolContext(
    Store ActiveStore,
    Niche? ActiveNiche,
    IReadOnlyList<ToolContextEntityReference> TopicPath,
    ToolContextEntityReference? SelectedTopic,
    Item? SelectedItem,
    WorkflowStage? WorkflowStage,
    ToolContextScopeKind ScopeKind,
    ToolContextScopeSummary ScopeSummary,
    IReadOnlyList<ToolContextInheritedValue> InheritedMetadata,
    IReadOnlyList<ToolContextInheritedValue> ExplicitMetadata,
    IReadOnlyList<ToolContextInheritedValue> InheritedTags,
    IReadOnlyList<ToolContextInheritedValue> ExplicitTags,
    IReadOnlyList<ToolContextNearbyWorkSummary> NearbyWork)
{
    public bool HasSelectedItem => SelectedItem is not null;
}

public sealed record ToolContextCreationDefaults(
    Guid StoreId,
    Guid? NicheId,
    Guid? GroupId,
    ToolContextScopeKind ScopeKind,
    IReadOnlyList<ToolContextInheritedValue> Metadata,
    IReadOnlyList<ToolContextInheritedValue> Tags);

public interface IToolContextResolver
{
    ToolContextResolution Resolve(ToolContextResolveRequest request);

    ToolContextCreationDefaults ResolveCreationDefaults(ToolContextResolution resolution);

    ToolContextResolution ResolveScope(ToolContextResolveRequest request, ToolContextScopeKind scope);
}
