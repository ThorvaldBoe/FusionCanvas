using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeSelection(WorkspaceEntityKind Kind, Guid Id);
