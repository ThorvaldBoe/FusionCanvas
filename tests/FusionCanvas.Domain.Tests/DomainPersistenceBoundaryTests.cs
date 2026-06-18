namespace FusionCanvas.Domain.Tests;

public class DomainPersistenceBoundaryTests
{
    [Fact]
    public void DomainProject_DoesNotReferencePersistencePackagesOrAdapters()
    {
        var projectFile = FindRepoFile("src", "FusionCanvas.Domain", "FusionCanvas.Domain.csproj");
        var projectContents = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Microsoft.Data.Sqlite", projectContents, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FusionCanvas.Integration", projectContents, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FusionCanvas.Application", projectContents, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. pathParts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repo file '{Path.Combine(pathParts)}'.");
    }
}
