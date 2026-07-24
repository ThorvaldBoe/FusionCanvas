namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeProjection(
    Guid StoreId,
    WorkspaceTreeQuery Query,
    IReadOnlyList<WorkspaceTreeProjectionNode> Roots,
    IReadOnlySet<Guid> VisibleEntityIds)
{
    public bool CanReorderBetweenSiblings => !Query.IsActive;
}
