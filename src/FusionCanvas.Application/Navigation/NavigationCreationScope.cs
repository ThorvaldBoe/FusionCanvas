using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Navigation;

public sealed record NavigationCreationScope(Guid StoreId, Guid? TopicId, WorkspaceEntityKind? TopicKind);
