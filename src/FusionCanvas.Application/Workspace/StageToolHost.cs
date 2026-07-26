using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Workspace;

public enum StageToolSourceKind
{
    BuiltIn = 0,
    Contributed = 1
}

public enum StageToolAvailabilityKind
{
    Available = 0,
    UnsupportedStage = 1,
    RequiresSelectedItem = 2,
    Disabled = 3,
    Failed = 4,
    Unavailable = 5
}

public enum StageToolContextKind
{
    Store = 0,
    Topic = 1,
    Item = 2
}

public sealed record StageToolDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string DetailViewKey,
    IReadOnlyCollection<WorkflowStage> SupportedStages,
    bool RequiresSelectedItem,
    bool IsDefault,
    StageToolSourceKind SourceKind = StageToolSourceKind.BuiltIn,
    bool IsEnabled = true,
    string? FailureMessage = null)
{
    public string Id { get; } = RequireText(Id, nameof(Id));

    public string DisplayName { get; } = RequireText(DisplayName, nameof(DisplayName));

    public string Description { get; } = RequireText(Description, nameof(Description));

    public string DetailViewKey { get; } = RequireText(DetailViewKey, nameof(DetailViewKey));

    public IReadOnlyCollection<WorkflowStage> SupportedStages { get; } =
        SupportedStages.Count > 0
            ? SupportedStages.ToArray()
            : throw new ArgumentException("At least one workflow stage is required.", nameof(SupportedStages));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value;
}

public sealed record StageToolAvailability(
    StageToolDescriptor Tool,
    StageToolAvailabilityKind Kind,
    string? Reason,
    ToolContextResolution? ToolContext)
{
    public bool IsAvailable => Kind == StageToolAvailabilityKind.Available;
}

public sealed record StageToolSelectionKey(WorkflowStage Stage, StageToolContextKind ContextKind);

public sealed record StageToolHostRequest(
    WorkspaceSnapshot Snapshot,
    ToolContextSelectionKind SelectionKind,
    WorkspaceEntityKind SelectedEntityKind,
    Guid SelectedEntityId,
    WorkflowStage WorkflowStage,
    string? RequestedToolId = null,
    ToolContextScopeKind? ScopeOverride = null,
    int NearbyWorkLimit = 8)
{
    public Guid SelectedEntityId { get; } = SelectedEntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(SelectedEntityId))
        : SelectedEntityId;
}

public sealed record StageToolHostState(
    WorkflowStage WorkflowStage,
    StageToolContextKind ContextKind,
    IReadOnlyList<StageToolAvailability> ToolStates,
    IReadOnlyList<StageToolAvailability> AvailableTools,
    StageToolAvailability? SelectedTool,
    ToolContextResolution? ActiveToolContext)
{
    public bool HasMultipleAvailableTools => AvailableTools.Count > 1;
}

public interface IStageToolRegistry
{
    IReadOnlyList<StageToolDescriptor> Tools { get; }
}

public sealed class InMemoryStageToolRegistry : IStageToolRegistry
{
    public InMemoryStageToolRegistry(IEnumerable<StageToolDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        Tools = tools.ToArray();
    }

    public IReadOnlyList<StageToolDescriptor> Tools { get; }
}

public interface IStageToolHostService
{
    StageToolHostState Build(StageToolHostRequest request);

    void SelectTool(StageToolSelectionKey key, string toolId);
}

public sealed class StageToolHostService : IStageToolHostService
{
    private readonly IStageToolRegistry _registry;
    private readonly IToolContextResolver _toolContextResolver;
    private readonly Dictionary<StageToolSelectionKey, string> _selectedToolIds = [];

    public StageToolHostService(IStageToolRegistry registry, IToolContextResolver toolContextResolver)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _toolContextResolver = toolContextResolver ?? throw new ArgumentNullException(nameof(toolContextResolver));
    }

    public StageToolHostState Build(StageToolHostRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Snapshot);

        var contextKind = ToContextKind(request.SelectionKind);
        var selectionKey = new StageToolSelectionKey(request.WorkflowStage, contextKind);
        var states = _registry.Tools
            .Select(tool => Evaluate(tool, request))
            .Where(state => state.Kind != StageToolAvailabilityKind.UnsupportedStage)
            .ToArray();
        var available = states
            .Where(state => state.IsAvailable)
            .ToArray();
        var selected = SelectAvailability(selectionKey, request.RequestedToolId, available);

        return new StageToolHostState(
            request.WorkflowStage,
            contextKind,
            states,
            available,
            selected,
            selected?.ToolContext);
    }

    public void SelectTool(StageToolSelectionKey key, string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            throw new ArgumentException("Tool id is required.", nameof(toolId));
        }

        _selectedToolIds[key] = toolId;
    }

    private StageToolAvailability? SelectAvailability(
        StageToolSelectionKey key,
        string? requestedToolId,
        IReadOnlyList<StageToolAvailability> available)
    {
        if (available.Count == 0)
        {
            return null;
        }

        var requested = FindAvailable(available, requestedToolId);
        if (requested is not null)
        {
            _selectedToolIds[key] = requested.Tool.Id;
            return requested;
        }

        if (_selectedToolIds.TryGetValue(key, out var selectedToolId))
        {
            var remembered = FindAvailable(available, selectedToolId);
            if (remembered is not null)
            {
                return remembered;
            }
        }

        var fallback = available.FirstOrDefault(state => state.Tool.IsDefault)
            ?? available[0];
        _selectedToolIds[key] = fallback.Tool.Id;
        return fallback;
    }

    private StageToolAvailability Evaluate(StageToolDescriptor tool, StageToolHostRequest request)
    {
        if (!tool.SupportedStages.Contains(request.WorkflowStage))
        {
            return new StageToolAvailability(
                tool,
                StageToolAvailabilityKind.UnsupportedStage,
                "Tool does not support the active workflow stage.",
                null);
        }

        if (!tool.IsEnabled)
        {
            return new StageToolAvailability(
                tool,
                StageToolAvailabilityKind.Disabled,
                "Tool is disabled.",
                null);
        }

        if (!string.IsNullOrWhiteSpace(tool.FailureMessage))
        {
            return new StageToolAvailability(
                tool,
                StageToolAvailabilityKind.Failed,
                tool.FailureMessage,
                null);
        }

        var resolution = _toolContextResolver.Resolve(new ToolContextResolveRequest(
            request.Snapshot,
            request.SelectionKind,
            request.SelectedEntityKind,
            request.SelectedEntityId,
            request.WorkflowStage,
            request.ScopeOverride,
            tool.RequiresSelectedItem,
            request.NearbyWorkLimit));

        if (resolution.IsAvailable)
        {
            return new StageToolAvailability(tool, StageToolAvailabilityKind.Available, null, resolution);
        }

        var kind = tool.RequiresSelectedItem && request.SelectionKind != ToolContextSelectionKind.Item
            ? StageToolAvailabilityKind.RequiresSelectedItem
            : StageToolAvailabilityKind.Unavailable;

        return new StageToolAvailability(tool, kind, resolution.UnavailableReason, resolution);
    }

    private static StageToolAvailability? FindAvailable(
        IReadOnlyList<StageToolAvailability> available,
        string? toolId) =>
        string.IsNullOrWhiteSpace(toolId)
            ? null
            : available.FirstOrDefault(state => string.Equals(state.Tool.Id, toolId, StringComparison.Ordinal));

    private static StageToolContextKind ToContextKind(ToolContextSelectionKind selectionKind) =>
        selectionKind switch
        {
            ToolContextSelectionKind.Store => StageToolContextKind.Store,
            ToolContextSelectionKind.Topic => StageToolContextKind.Topic,
            ToolContextSelectionKind.Item => StageToolContextKind.Item,
            _ => throw new ArgumentOutOfRangeException(nameof(selectionKind), selectionKind, "Unsupported selection kind.")
        };
}

public static class BuiltInStageTools
{
    public static IStageToolRegistry CreateDefaultRegistry() =>
        new InMemoryStageToolRegistry(CreateDefaultTools());

    public static IReadOnlyList<StageToolDescriptor> CreateDefaultTools() =>
    [
        new(
            "built-in-idea-tool",
            "Idea",
            "Default Idea-stage workspace tool.",
            "idea-stage-tool",
            [WorkflowStage.Idea],
            RequiresSelectedItem: false,
            IsDefault: true),
        new(
            "built-in-concept-tool",
            "Concept",
            "Default Concept-stage item tool.",
            "concept-stage-tool",
            [WorkflowStage.Concept],
            RequiresSelectedItem: true,
            IsDefault: true),
        new(
            "built-in-design-tool",
            "Design",
            "Default Design-stage item tool.",
            "design-stage-tool",
            [WorkflowStage.Design],
            RequiresSelectedItem: true,
            IsDefault: true),
        new(
            "built-in-listing-tool",
            "Listing",
            "Default Item-stage item tool.",
            "listing-stage-tool",
            [WorkflowStage.Listing],
            RequiresSelectedItem: true,
            IsDefault: true)
    ];
}

public interface IStageToolCommandGateway
{
    Task ExecuteAsync(string commandName, ToolContext context, CancellationToken cancellationToken = default);
}
