using System.Text.Json;
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
        var metadata = ParseMetadata(workspace.MetadataJson);
        return new WorkspaceContext(workspace.Description, metadata.GetValueOrDefault(NotesKey));
    }

    public Workspace Apply(Workspace workspace, WorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = ParseMetadata(workspace.MetadataJson);
        SetOptional(metadata, NotesKey, context.Notes);
        return workspace with
        {
            Description = context.Description,
            MetadataJson = metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata)
        };
    }

    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    private static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }
}
