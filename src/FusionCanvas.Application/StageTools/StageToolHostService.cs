using FusionCanvas.Application.ToolContexts;

namespace FusionCanvas.Application.StageTools;

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
