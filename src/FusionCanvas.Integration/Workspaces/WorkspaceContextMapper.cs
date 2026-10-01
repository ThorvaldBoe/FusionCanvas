using FusionCanvas.Application.Metadata;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Integration.Workspaces;

/// <summary>
/// Maps workspace context to the existing JSON metadata column while preserving unknown keys.
/// </summary>
public sealed class WorkspaceContextMapper : IWorkspaceContextMapper
{
    private const string NotesKey = "notes";

    public WorkspaceContext Read(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var metadata = StringMetadataCodec.Parse(workspace.MetadataJson);
        return new WorkspaceContext(workspace.Description, metadata.GetValueOrDefault(NotesKey));
    }

    public Workspace Apply(Workspace workspace, WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = StringMetadataCodec.Parse(workspace.MetadataJson);
        StringMetadataCodec.SetOptional(metadata, NotesKey, context.Notes);
        return workspace with
        {
            Description = context.Description,
            MetadataJson = StringMetadataCodec.Serialize(metadata)
        };
    }
}
