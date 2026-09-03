using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Application.Navigation;

namespace FusionCanvas.App.Navigation;

public sealed class NavigationTreePresentationState
{
    private readonly HashSet<Guid> _expandedNodeIds = [];

    public Guid? SelectedNodeId { get; private set; }

    public Guid? RevealedNodeId { get; private set; }

    public IReadOnlyCollection<Guid> ExpandedNodeIds => _expandedNodeIds;

    public bool IsExpanded(Guid nodeId) => _expandedNodeIds.Contains(nodeId);

    public void Select(Guid nodeId) => SelectedNodeId = RequireNodeId(nodeId);

    public void Expand(Guid nodeId) => _expandedNodeIds.Add(RequireNodeId(nodeId));

    public void Collapse(Guid nodeId) => _expandedNodeIds.Remove(RequireNodeId(nodeId));

    public void RevealPath(IReadOnlyList<Guid> nodePath)
    {
        ArgumentNullException.ThrowIfNull(nodePath);

        if (nodePath.Count == 0)
        {
            throw new ArgumentException("Reveal path must include at least one node.", nameof(nodePath));
        }

        foreach (var nodeId in nodePath.Take(nodePath.Count - 1))
        {
            Expand(nodeId);
        }

        RevealedNodeId = RequireNodeId(nodePath[^1]);
        SelectedNodeId = RevealedNodeId;
    }

    private static Guid RequireNodeId(Guid nodeId) =>
        nodeId == Guid.Empty
            ? throw new ArgumentException("Identifier must not be empty.", nameof(nodeId))
            : nodeId;
}
