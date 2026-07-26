using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeClipboardPayload(WorkspaceTreeClipboardMode Mode, WorkspaceEntityKind Kind, Guid EntityId);
