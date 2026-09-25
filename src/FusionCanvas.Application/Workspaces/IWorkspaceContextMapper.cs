using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspaces;

/// <summary>
/// Maps the semantic workspace context to and from a workspace entity.
/// Implementations belong at the persistence boundary.
/// </summary>
public interface IWorkspaceContextMapper
{
    WorkspaceContext Read(Workspace workspace);

    Workspace Apply(Workspace workspace, WorkspaceContext context);
}
