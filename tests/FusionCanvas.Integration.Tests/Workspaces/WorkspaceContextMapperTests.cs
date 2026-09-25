using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspaces;

namespace FusionCanvas.Integration.Tests.Workspaces;

public sealed class WorkspaceContextMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Read_MapsExistingNotesMetadataToSemanticContext()
    {
        var workspace = NewWorkspace("""{"notes":"Keep this","custom":"Retain me"}""");

        var context = new WorkspaceContextMapper().Read(workspace);

        Assert.Equal("Description", context.Description);
        Assert.Equal("Keep this", context.Notes);
    }

    [Fact]
    public void Apply_UpdatesNotesAndPreservesUnknownMetadata()
    {
        var workspace = NewWorkspace("""{"notes":"Old notes","custom":"Retain me"}""");

        var updated = new WorkspaceContextMapper().Apply(workspace, new WorkspaceContext("Updated description", " New notes "));

        Assert.Equal("Updated description", updated.Description);
        using var metadata = System.Text.Json.JsonDocument.Parse(updated.MetadataJson);
        Assert.Equal("New notes", metadata.RootElement.GetProperty("notes").GetString());
        Assert.Equal("Retain me", metadata.RootElement.GetProperty("custom").GetString());
    }

    private static Workspace NewWorkspace(string metadataJson) =>
        new(Guid.NewGuid(), "Workspace", "Description", false, Now, Now, metadataJson);
}
