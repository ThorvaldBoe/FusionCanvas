using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests.Workspace;

public class WorkspaceNamePolicyTests
{
    [Fact]
    public void IsTaken_UsesTrimmedCaseInsensitiveNames()
    {
        Assert.True(WorkspaceNamePolicy.IsTaken(" client work ", ["Client Work"]));
        Assert.False(WorkspaceNamePolicy.IsTaken("Client Work", ["Archived", "Other"]));
    }

    [Fact]
    public void ResolveUniqueName_UsesFirstAvailableSuffix()
    {
        var result = WorkspaceNamePolicy.ResolveUniqueName(" Brand ", ["brand", "Brand (2)", "BRAND (3)"]);

        Assert.Equal("Brand (4)", result);
    }
}
