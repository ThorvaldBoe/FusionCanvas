using DomainWorkspace = FusionCanvas.Domain.Workspace.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests.Workspace;

public class WorkspaceNamePolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HasActiveNameCollision_NormalizesCandidateAndExistingNames()
    {
        var existing = NewWorkspace("Client Work");
        var archived = NewWorkspace("Archived") with { IsArchived = true };

        Assert.True(WorkspaceNamePolicy.HasActiveNameCollision(" client work ", [existing, archived]));
        Assert.False(WorkspaceNamePolicy.HasActiveNameCollision("archived", [archived]));
    }

    [Fact]
    public void HasActiveNameCollision_ExcludesTheWorkspaceBeingEdited()
    {
        var existing = NewWorkspace("Client Work");

        Assert.False(WorkspaceNamePolicy.HasActiveNameCollision(existing.Name, [existing], existing.Id));
    }

    [Fact]
    public void ResolveImportName_UsesFirstAvailableSuffixAndIgnoresArchivedNames()
    {
        var active = NewWorkspace("brand");
        var archived = NewWorkspace("Archived Brand") with { IsArchived = true };
        var suffixTwo = NewWorkspace("Brand (2)");
        var suffixThree = NewWorkspace("BRAND (3)");

        Assert.Equal("Archived Brand", WorkspaceNamePolicy.ResolveImportName(" Archived Brand ", [archived]));
        Assert.Equal("Brand (4)", WorkspaceNamePolicy.ResolveImportName(
            "Brand",
            [active, archived, suffixTwo, suffixThree]));
    }

    private static DomainWorkspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");
}
