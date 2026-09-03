using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.App.DocumentWindow;

public sealed record DocumentNavigationLocation(IReadOnlyList<Guid> NodePath, string DisplayPath)
{
    public IReadOnlyList<Guid> NodePath { get; } = RequireNodePath(NodePath);

    public string DisplayPath { get; } = string.IsNullOrWhiteSpace(DisplayPath)
        ? throw new ArgumentException("Display path is required.", nameof(DisplayPath))
        : DisplayPath;

    private static IReadOnlyList<Guid> RequireNodePath(IReadOnlyList<Guid> nodePath)
    {
        ArgumentNullException.ThrowIfNull(nodePath);

        if (nodePath.Count == 0)
        {
            throw new ArgumentException("Navigation path must include at least one node.", nameof(nodePath));
        }

        if (nodePath.Any(nodeId => nodeId == Guid.Empty))
        {
            throw new ArgumentException("Navigation path cannot include an empty identifier.", nameof(nodePath));
        }

        return nodePath.ToArray();
    }
}
