namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextCreationDefaults(
    Guid StoreId,
    Guid? NicheId,
    Guid? GroupId,
    ToolContextScopeKind ScopeKind,
    IReadOnlyList<ToolContextInheritedValue> Metadata,
    IReadOnlyList<ToolContextInheritedValue> Tags);
