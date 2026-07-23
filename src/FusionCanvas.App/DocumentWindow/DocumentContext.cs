using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.DocumentWindow;

public enum DocumentContextKind
{
    Item = 0,
    Topic = 1,
    WorkingView = 2
}

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

public sealed record DocumentContext
{
    public DocumentContext(
        Guid id,
        string title,
        DocumentContextKind kind,
        DocumentNavigationLocation? navigationLocation,
        ActiveItemWorkflowContext? workflow,
        WorkflowStage workflowStage,
        string detailViewKey,
        WorkspaceEntityKind entityKind = WorkspaceEntityKind.Item)
    {
        Id = id == Guid.Empty
            ? throw new ArgumentException("Identifier must not be empty.", nameof(id))
            : id;
        Title = string.IsNullOrWhiteSpace(title)
            ? throw new ArgumentException("Title is required.", nameof(title))
            : title;
        Kind = kind;
        NavigationLocation = navigationLocation;
        Workflow = workflow;
        WorkflowStage = workflowStage;
        DetailViewKey = string.IsNullOrWhiteSpace(detailViewKey)
            ? throw new ArgumentException("Detail view key is required.", nameof(detailViewKey))
            : detailViewKey;
        EntityKind = entityKind;
    }

    public Guid Id { get; init; }

    public string Title { get; init; }

    public DocumentContextKind Kind { get; init; }

    public WorkspaceEntityKind EntityKind { get; init; }

    public DocumentNavigationLocation? NavigationLocation { get; init; }

    public ActiveItemWorkflowContext? Workflow { get; init; }

    public WorkflowStage WorkflowStage { get; init; }

    public string DetailViewKey { get; init; }

    public string WorkflowStageLabel => WorkflowStages.GetDisplayName(WorkflowStage);
}
