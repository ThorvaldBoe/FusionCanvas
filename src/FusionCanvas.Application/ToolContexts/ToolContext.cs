using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.ToolContexts;

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
